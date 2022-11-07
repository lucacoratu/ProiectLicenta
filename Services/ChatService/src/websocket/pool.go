package websocket

import (
	"encoding/json"
	"fmt"
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
func (pool *Pool) ClientRegistered() {

}

func (pool *Pool) ClientUnregistered() {

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
				break
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

	if isChatMessage {
		pool.logger.Info("Chat message received on the socket")
		err = pool.dbConn.InsertMessageIntoRoom(chatMessageData.RoomID, chatMessageData.MessageType, message.C.Id, chatMessageData.Data)
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
		for _, id := range accIds {
			for client, _ := range pool.Clients {
				if client.Id == id {
					//Send the message to him
					found = true
					response := PrivateMessageResponse{SenderID: message.C.Id, RoomID: chatMessageData.RoomID, Data: chatMessageData.Data, MessageType: chatMessageData.MessageType}
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
		return
	}

	//Check if the message is a private message to a user
	/* privMessageData := &PrivateMessage{}
	err = json.Unmarshal([]byte(message.Body), &privMessageData)
	var isPrivateMessage bool = true
	if err != nil {
		pool.logger.Error(err.Error())
		isPrivateMessage = false
	}

	if isPrivateMessage {
		pool.logger.Info("Private message has been received")
		//Insert the message in the database
		err = pool.dbConn.InsertMessageIntoRoom(privMessageData.RoomID, privMessageData.MessageType, message.C.Id, privMessageData.Data)
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
		//Get the second user id knowing the senderId and the roomId
		accId, err := pool.dbConn.GetPrivateRoomUser(message.C.Id, privMessageData.RoomID)
		if err != nil {
			//Return an error message to the client
			err = message.C.Conn.WriteJSON(ResponseMessage{Type: 1, Body: "Message insertion failed"})
			if err != nil {
				//Log the error message
				pool.logger.Error(err.Error())
			}
		}
		pool.logger.Info("Found the receiver id of the message, ID = ", accId)
		//Check if the other user is connected, if it is then sent the message to the other user as well
		var found bool = false
		for client, _ := range pool.Clients {
			if client.Id == accId {
				//Send the message to him
				found = true
				response := PrivateMessageResponse{SenderID: message.C.Id, RoomID: privMessageData.RoomID, Data: privMessageData.Data, MessageType: privMessageData.MessageType}
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

	//Check if the message is a group message
	groupMessageData := &GroupMessage{}
	err = json.Unmarshal([]byte(message.Body), &groupMessageData)
	var isGroupMessage bool = true
	if err != nil {
		pool.logger.Error(err.Error())
		isPrivateMessage = false
	}

	if isGroupMessage {
		pool.logger.Info("Group message has been received")
		//Insert the message in the database
		err = pool.dbConn.InsertMessageIntoRoom(privMessageData.RoomID, privMessageData.MessageType, message.C.Id, privMessageData.Data)
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
		accIds, err := pool.dbConn.GetRoomParticipants(message.C.Id, privMessageData.RoomID)
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
		for _, id := range accIds {
			for client, _ := range pool.Clients {
				if client.Id == id {
					//Send the message to him
					found = true
					response := PrivateMessageResponse{SenderID: message.C.Id, RoomID: privMessageData.RoomID, Data: privMessageData.Data, MessageType: privMessageData.MessageType}
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
	} */
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
			break
		case client := <-pool.Unregister:
			//A client disconnected from the chat service
			//Delete the client connection from the current connections
			delete(pool.Clients, client)
			fmt.Println("Size of Connection Pool: ", len(pool.Clients))
			//Announce the client that is has disconnected from the chat service
			for client, _ := range pool.Clients {
				client.Conn.WriteJSON(Message{Type: 1, Body: "User Disconnected..."})
			}
			break
		case message := <-pool.Broadcast:
			//Broadcast a message to all the current websocket connections
			pool.MessageReceived(message)
			break
		}
	}
}
