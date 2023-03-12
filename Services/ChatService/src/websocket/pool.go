package websocket

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"willow/chatservice/data"
	"willow/chatservice/database"
	jsonerrors "willow/chatservice/errors"
	"willow/chatservice/logging"
)

/*
 * This structure will handle concurrent connections using channels
 * Each channel will have a particular functionality such as:
 * - The Register channel will handle a new connection to the chat service
 * - The Unregister channel will handle a client disconnecting from the chat service
 * - Clients is a map of client connections to the state of the connection (true for online)
 * - Broadcast channel will be used to broadcast the message to all the connected clients of the chat service
 */
type Pool struct {
	Register   chan *Client
	Unregister chan *Client
	Clients    map[*Client]bool
	Broadcast  chan Message
	logger     logging.ILogger
	dbConn     database.IConnection
}

/*
 * This function will create a new pool that can then be used when starting the chat service
 */
func NewPool(l logging.ILogger, dbConn database.IConnection) *Pool {
	return &Pool{
		Register:   make(chan *Client),
		Unregister: make(chan *Client),
		Clients:    make(map[*Client]bool),
		Broadcast:  make(chan Message),
		logger:     l,
		dbConn:     dbConn,
	}
}

/*
 * This function will handle the register of a client (the client connected to the websocket)
 * It should be registered in the pool and the status of the user in the account service should be changed to online
 * TO DO...The connected clients should get the message that the user changed it's status to online
 */
func (pool *Pool) ClientRegistered(c *Client) {
	pool.logger.Debug("Client connected to websocket")
}

func (pool *Pool) ClientUnregistered(c *Client) {
	pool.logger.Debug("Client disconnected from websocket")
	//Update the status of the account to offline if it is connected to an account (ID field is set)
	if c.Id != 0 {
		err := pool.sendUpdateStatusAccountService(int(c.Id), "Offline")
		if err != nil {
			pool.logger.Error("Could not send request to update status", err.Error())
		}
		//Send to all the other users the message that this client disconnected
		for client, _ := range pool.Clients {
			if client != c {
				pool.logger.Debug("Account disconnected, id = ", c.Id)
				err := client.Conn.WriteJSON(ChangeStatusMessage{Text: "Change status", AccountId: int(c.Id), NewStatus: "Offline"})
				if err != nil {
					//Log the error message
					pool.logger.Error(err.Error())
				}
			}
		}
	}
}

func (pool *Pool) sendUpdateStatusAccountService(accountId int, newStatus string) error {
	httpClient := &http.Client{}
	json, err := json.Marshal(data.UpdateStatus{AccountID: accountId, NewStatus: newStatus})
	if err != nil {
		pool.logger.Error(err.Error())
		return err
	}
	req, err := http.NewRequest(http.MethodPut, "http://localhost:8081/status", bytes.NewBuffer(json))
	if err != nil {
		pool.logger.Error(err.Error())
		return err
	}
	resp, err := httpClient.Do(req)
	if err != nil {
		pool.logger.Error(err.Error())
		return err
	}

	if resp.StatusCode != http.StatusOK {
		pool.logger.Error("Request to update status failed")
		return errors.New("request failed")
	}
	return nil
}

/*
 * This function will handle when a message is recevied from a client
 * There should be more types of messages that can be received from the client
 * (SetAccountID which will set the connection id with the account id for easier identification of the connection)
 * (PrivateMessage which will have a single destination id)
 * (GroupMessage which will have multiple destination ids)
 */
func (pool *Pool) MessageReceived(message Message) {
	//Log that a message has been received on the websocket
	pool.logger.Info("Message received on the websocket")
	//Check what type of message it is
	//Check if it is a set AccountID message
	setIdData := &SetAccountIDMessage{}
	err := json.Unmarshal([]byte(message.Body), &setIdData)
	var isSetId bool = true
	if err != nil {
		pool.logger.Error(err.Error())
		isSetId = false
	}

	pool.logger.Info(setIdData)
	if isSetId && setIdData.SetID != 0 {
		//The message is a setID message so set the id of the connection to the id said in the message
		//Loop through all the connections and send the message
		pool.logger.Info("Message is SetAccountID Message, AccountID = ", setIdData.SetID)
		for client, _ := range pool.Clients {
			if client == message.C {
				//Set the client Id
				client.Id = setIdData.SetID
				//Send a message back which will confirm the id was set
				err = client.Conn.WriteJSON(ResponseMessage{Type: 1, Body: "Account ID has been set"})
				if err != nil {
					//Log the error message
					pool.logger.Error(err.Error())
				}
				//Sent request to account service to update the status in the database
				err = pool.sendUpdateStatusAccountService(int(client.Id), "Online")
				if err != nil {
					pool.logger.Error("Could not send request to update status", err.Error())
				}
				client.Status = "Online"
			} else {
				//Send a message to all the other users that the account is online
				pool.logger.Debug(setIdData.SetID)
				err = client.Conn.WriteJSON(ChangeStatusMessage{Text: "Change status", AccountId: int(setIdData.SetID), NewStatus: "Online"})
				if err != nil {
					//Log the error message
					pool.logger.Error(err.Error())
				}
			}
		}

		//Exit the function as the message has been handled
		return
	}
	//Check if the message is create group message
	createGroupData := CreateGroupMessage{}
	err = json.Unmarshal([]byte(message.Body), &createGroupData)
	var isCreateGroup bool = true
	if err != nil {
		pool.logger.Error(err.Error())
		isCreateGroup = false
	}

	pool.logger.Debug(createGroupData)

	if isCreateGroup && createGroupData.CreatorId != 0 {
		pool.logger.Info("Message is CreateGroupMessage")
		//Create the group and add all the participants in it
		//Create a new group (insert a new room into the database)
		roomId, err := pool.dbConn.CreateGroup(createGroupData.GroupName, createGroupData.CreatorId)
		if err != nil {
			pool.logger.Info("Error occured when creating group", err.Error())
			//Send an error message back
			jsonError := jsonerrors.JsonError{Message: "Cannot create group"}
			message.C.Conn.WriteJSON(jsonError)
			return
		}
		//Add all the participants into the group
		for _, accId := range createGroupData.Participants {
			err := pool.dbConn.InsertUserIntoRoom(accId, roomId)
			if err != nil {
				pool.logger.Error("Error occured when inserting user into group", err.Error())
				//Send an error message back
				jsonError := jsonerrors.JsonError{Message: "Internal server error"}
				message.C.Conn.WriteJSON(jsonError)
				return
			}
		}
		err = pool.dbConn.InsertUserIntoRoom(createGroupData.CreatorId, roomId)
		if err != nil {
			pool.logger.Error("Error occured when inserting creator into group", err.Error())
			//Send an error message back
			jsonError := jsonerrors.JsonError{Message: "Internal server error"}
			message.C.Conn.WriteJSON(jsonError)
			return
		}

		groupResponse := CreateGroupResponse{}
		groupResponse.RoomId = roomId
		groupResponse.CreatorId = createGroupData.CreatorId
		groupResponse.GroupName = createGroupData.GroupName
		groupResponse.Participants = createGroupData.Participants
		//Notify all the connected clients that have been added in the group
		var found bool = false
		for _, id := range createGroupData.Participants {
			for client, _ := range pool.Clients {
				if client.Id == id {
					//Send the message to him
					found = true
					err = client.Conn.WriteJSON(groupResponse)
					if err != nil {
						pool.logger.Error(err.Error())
						break
					}
					pool.logger.Info("Sent the message to the receiver")
				}
			}
			if !found {
				pool.logger.Info("The message could not be sent to the receiver, he might be offline")
			}
		}

		//Notify the client that sent the request that the group has been created
		for client, _ := range pool.Clients {
			if client.Id == createGroupData.CreatorId {
				err = client.Conn.WriteJSON(groupResponse)
				if err != nil {
					pool.logger.Error(err.Error())
					break
				}
			}
		}
		return
	}

	//Check if the message is a chat message
	chatMessageData := ChatMessage{}
	err = json.Unmarshal([]byte(message.Body), &chatMessageData)
	var isChatMessage bool = true
	if err != nil {
		pool.logger.Error(err.Error())
		isChatMessage = false
	}

	if isChatMessage && chatMessageData.RoomID != 0 && chatMessageData.Data != "" {
		pool.logger.Info("Chat message received on the socket")
		messageId, err := pool.dbConn.InsertMessageIntoRoom(chatMessageData.RoomID, chatMessageData.MessageType, message.C.Id, chatMessageData.Data)
		//Check if the insert was succesfull
		if err != nil {
			//Return an error message to the client
			err = message.C.Conn.WriteJSON(ResponseMessage{Type: 1, Body: "Message insertion failed"})
			if err != nil {
				//Log the error message
				pool.logger.Error(err.Error())
			}
		}
		pool.logger.Info("The new message has been inserted into the database")
		//Get the participants ids from the database
		accIds, err := pool.dbConn.GetRoomParticipants(message.C.Id, chatMessageData.RoomID)
		if err != nil {
			//Return an error message to the client
			err = message.C.Conn.WriteJSON(ResponseMessage{Type: 1, Body: "Message insertion failed"})
			if err != nil {
				//Log the error message
				pool.logger.Error(err.Error())
			}
		}
		pool.logger.Info("Found the particapants ids,", accIds)
		//Check if the other user is connected, if it is then sent the message to the other user as well
		var found bool = false
		response := PrivateMessageResponse{MessageID: messageId, SenderID: message.C.Id, RoomID: chatMessageData.RoomID, Data: chatMessageData.Data, MessageType: chatMessageData.MessageType}
		for _, id := range accIds {
			for client, _ := range pool.Clients {
				if client.Id == id {
					//Send the message to him
					found = true
					err = client.Conn.WriteJSON(response)
					if err != nil {
						pool.logger.Error(err.Error())
						break
					}
					pool.logger.Info("Sent the message to the receiver")
				}
			}
			if !found {
				pool.logger.Info("The message could not be sent to the receiver, he might be offline")
			}
		}
		message.C.Conn.WriteJSON(response)
		//Send the message back to the client as well
		// for client, _ := range pool.Clients {
		// 	if client.Id == message.C.Id {
		// 		//Send the message to him
		// 		found = true
		// 		response := PrivateMessageResponse{MessageID: messageId, SenderID: message.C.Id, RoomID: chatMessageData.RoomID, Data: chatMessageData.Data, MessageType: chatMessageData.MessageType}
		// 		err = client.Conn.WriteJSON(response)
		// 		if err != nil {
		// 			pool.logger.Error(err.Error())
		// 			break
		// 		}
		// 		pool.logger.Info("Sent the message to the sender")
		// 	}
		// }
		return
	}

	//Check if the message is call user message
	callMessageData := CallAccount{}
	err = json.Unmarshal([]byte(message.Body), &callMessageData)
	var isCallMessage bool = true
	if err != nil {
		pool.logger.Error(err.Error())
		isCallMessage = false
	}

	if isCallMessage && callMessageData.Callee != 0 && callMessageData.Caller != 0 {
		//Send the other client notification that a friend is calling him
		pool.logger.Info("Received message to call", callMessageData.Caller, "from", callMessageData.Callee)
		var foundCallee bool = false
		for client, _ := range pool.Clients {
			pool.logger.Debug(client.Id)
			if client.Id == int64(callMessageData.Callee) {
				//Send a message back which will confirm the id was set
				err = client.Conn.WriteJSON(callMessageData)
				if err != nil {
					//Log the error message
					pool.logger.Error(err.Error())
				}
				foundCallee = true
			}
		}
		if !foundCallee {
			//Send a message to the caller that the account is offline
			pool.logger.Info("Callee is not online at the moment!")
		}
		return
	}

	//Check if the message is update profile picture
	updateProfilePicture := UpdateProfilePicture{}
	err = json.Unmarshal([]byte(message.Body), &updateProfilePicture)
	isUpdateProfilePictureMessage := true
	if err != nil {
		pool.logger.Error(err.Error())
		isUpdateProfilePictureMessage = false
	}

	if isUpdateProfilePictureMessage && updateProfilePicture.Id != 0 && updateProfilePicture.NewPhoto != "" {
		pool.logger.Info("Received message to update profile picture", updateProfilePicture.Id)
		for client, _ := range pool.Clients {
			pool.logger.Debug(client.Id)
			if client.Id != int64(updateProfilePicture.Id) {
				err = client.Conn.WriteJSON(updateProfilePicture)
				if err != nil {
					//Log the error message
					pool.logger.Error(err.Error())
				}
			}
		}
	}

	//Check if the message is react to message
	sendReactionMessage := data.SendReact{}
	err = json.Unmarshal([]byte(message.Body), &sendReactionMessage)
	isSendReaction := true
	if err != nil {
		pool.logger.Error(err.Error())
		isSendReaction = false
	}
	if isSendReaction && sendReactionMessage.MessageId != 0 && sendReactionMessage.SenderId != 0 && sendReactionMessage.RoomId != 0 {
		pool.logger.Debug("Received reaction for message with id", sendReactionMessage.SenderId, "in room with id", sendReactionMessage.RoomId, "reaction", sendReactionMessage.EmojiReaction)
		_, err := pool.dbConn.AddMessageReaction(sendReactionMessage)
		if err != nil {
			message.C.Conn.WriteMessage(1, []byte("could not insert reaction"))
			return
		}
		//Get all the participants of the room
		participants, err := pool.dbConn.GetRoomParticipants(sendReactionMessage.SenderId, sendReactionMessage.RoomId)
		pool.logger.Debug(participants)
		//Check if an error occured when getting the room participants
		if err != nil {
			message.C.Conn.WriteMessage(1, []byte("could not insert reaction"))
			return
		}
		//Announce the sender that the reaction has been registered
		err = message.C.Conn.WriteJSON(sendReactionMessage)
		//Check if an error occured
		if err != nil {
			pool.logger.Error("Error occured when announcing the user that his reaction has been registered", err.Error())
		}
		//Send a message to all the users in the room that the user reacted to a message
		for client := range pool.Clients {
			for _, participant := range participants {
				if client.Id == participant {
					//Send the message that a user reacted to a message
					err := client.Conn.WriteJSON(sendReactionMessage)
					if err != nil {
						pool.logger.Error("Error occured when announcing the other users in a room that a new reaction has been added", err.Error())
						continue
					}
				}
			}
		}
	}
}

/*
 * This function will start the pool which will handle client connections, client disconnections and broadcast messages
 */
func (pool *Pool) Start() {
	//Loop infinetly
	for {
		//Check what kind of event occured (connect, disconnect, broadcast message)
		select {
		case client := <-pool.Register:
			//A new client connected on the chat service websocket uri
			//Add the client connection to the pool of current connections
			pool.Clients[client] = true
			fmt.Println("Size of Connection Pool: ", len(pool.Clients))
			//Announce the user that it connected to the chat service
			for client, _ := range pool.Clients {
				fmt.Println(client)
				client.Conn.WriteJSON(Message{Type: 1, Body: "New User Joined..."})
			}
			pool.ClientRegistered(client)
			break
		case client := <-pool.Unregister:
			//A client disconnected from the chat service
			//Delete the client connection from the current connections
			//Announce the client that is has disconnected from the chat service
			for client, _ := range pool.Clients {
				client.Conn.WriteJSON(Message{Type: 1, Body: "User Disconnected..."})
			}
			pool.ClientUnregistered(client)
			delete(pool.Clients, client)
			fmt.Println("Size of Connection Pool: ", len(pool.Clients))
			break
		case message := <-pool.Broadcast:
			//Broadcast a message to all the current websocket connections
			pool.MessageReceived(message)
			break
		}
	}
}
