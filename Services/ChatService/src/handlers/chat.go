package handlers

import (
	"io"
	"net/http"
	"os"
	"willow/chatservice/data"
	"willow/chatservice/database"
	jsonerrors "willow/chatservice/errors"
	"willow/chatservice/logging"
	"willow/chatservice/websocket"

	"strconv"

	"github.com/gorilla/mux"
)

/*
 * This structure will have the handler functions for all the chat endpoints
 * It contains a connection to the database structure.
 */
type Chat struct {
	dbConn database.IConnection
	logger logging.ILogger
}

/*
 * This function will create a new Chat structure given the logger parameter and the dbConn parameter
 */
func NewChat(dbConn database.IConnection, logger logging.ILogger) *Chat {
	return &Chat{dbConn: dbConn, logger: logger}
}

/*
 * This function will create a new private room for the users specified in the body of the request
 * This endpoint will be a POST one
 */
func (ch *Chat) CreatePrivateRoom(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /privateroom/create hit (POST request)")
	//Get the data from the request body
	roomData := &data.CreatePrivateRoom{}
	//Try to decode the request body data
	err := roomData.FromJSON(r.Body)
	//Check if an error occured during the decoding of the request body data
	ch.logger.Debug(*roomData)
	if err != nil {
		//An error occured during the parsing of the json string
		ch.logger.Error("Error occured when parsing json string", err.Error())
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Invalid json format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}
	//The data from the request body is in the right format
	//Create the private room
	roomID, err := ch.dbConn.CreatePrivateRoom()
	ch.logger.Debug(roomID)
	//Check if an error occuring while creating the new private room
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "An error occured"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Add the users into the newly create room
	err = ch.dbConn.InsertUserIntoRoom(roomData.SenderID, roomID)
	//Check if an error occuring during the insertion of the user into the room
	if err != nil {
		jsonError := jsonerrors.JsonError{Message: "An error occured"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Add the second user into the same room
	err = ch.dbConn.InsertUserIntoRoom(roomData.ReceiverID, roomID)
	//Check if an error occuring during the insertion of the user into the room
	if err != nil {
		jsonError := jsonerrors.JsonError{Message: "An error occured"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	//Send the succes message to the client
	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Room has been created with the 2 users inside"))
}

/*
 * This function will get all the rooms that the user is in
 * The request that will come to this endpoint will be a get request and will be in this form: /chat/rooms/:userid
 */
func (ch *Chat) GetPrivateRooms(rw http.ResponseWriter, r *http.Request) {
	//Get the id from the request URI (gorilla mux)
	ch.logger.Info("Endpoint /privaterooms/{id:[0-9]+} hit (GET Method)")
	vars := mux.Vars(r)
	//Check if the id could be parsed (it should always be, but just to be safe, test it)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Invalid id format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}
	//Log the id received from the client
	ch.logger.Debug("Id received from the client", id)
	rw.WriteHeader(http.StatusOK)
}

/*
 * This function will handle when a client connects to the websocket endpoint
 */
func (ch *Chat) ServeWs(pool *websocket.Pool, rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /ws hit")
	ws, err := websocket.Upgrade(rw, r)
	if err != nil {
		ch.logger.Error(err.Error())
		return
	}

	client := &websocket.Client{
		Conn:   ws,
		Pool:   pool,
		Status: "Offline",
		Id:     0,
	}

	pool.Register <- client
	client.Read()
}

/*
 * This function will send the client the room id of a private conversation that it requested
 */
func (ch *Chat) GetRoomId(rw http.ResponseWriter, r *http.Request) {
	//Get the accountid and the friendId from the body of the request
	ch.logger.Info("Endpoint /privateroom hit (POST request)")
	bodyData := data.GetRoomId{}
	err := bodyData.FromJSON(r.Body)
	ch.logger.Debug(bodyData)
	if err != nil {
		//Send an error message back
		ch.logger.Error(err.Error())
		jsonError := jsonerrors.JsonError{Message: "Invalid json format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}
	//Get the roomid the client requested
	roomId, err := ch.dbConn.GetRoomId(bodyData.AccountID, bodyData.FriendID)
	if err != nil {
		//Send an error message back
		ch.logger.Error(err.Error())
		jsonError := jsonerrors.JsonError{Message: "Internal server error"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
	}
	//Send the room id back to the client
	respData := data.RoomIdResponse{RoomID: roomId}
	rw.WriteHeader(http.StatusOK)
	respData.ToJSON(rw)
}

/*
 * This function will get the last message from a room specified by the
 */
func (ch *Chat) GetRoomLastMessage(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /history/{id:[0-9]}/lastmessage hit (GET Method)")
	vars := mux.Vars(r)
	//Check if the id could be parsed (it should always be, but just to be safe, test it)
	id, err := strconv.Atoi(vars["id"])
	ch.logger.Debug(id)
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Invalid id format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}
	//Get the last message for the room
	lastMessageText, lastMessageTimestamp, _, _ := ch.dbConn.GetLastMessageFromRoom(int64(id))
	/* 	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Error occured"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
	} */
	lastMessage := data.LastMessage{MessageText: lastMessageText, MessageTimestamp: lastMessageTimestamp}
	ch.logger.Debug(lastMessage)
	rw.WriteHeader(http.StatusOK)
	lastMessage.ToJSON(rw)
	//rw.Write([]byte(lastMessage))

}

/*
 * This function will send the room history of messages to the client
 */
func (ch *Chat) GetRoomHistory(rw http.ResponseWriter, r *http.Request) {
	//Get the room id from the uri
	//Get the id from the request URI (gorilla mux)
	ch.logger.Info("Endpoint /history/{id:[0-9]+} hit (GET Method)")
	vars := mux.Vars(r)
	//Check if the id could be parsed (it should always be, but just to be safe, test it)
	id, err := strconv.Atoi(vars["id"])
	ch.logger.Debug(id)
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Invalid id format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}

	//Get all the messages in the database for the room id
	messages, err := ch.dbConn.GetHistory(int64(id))
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Internal server error"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	rw.WriteHeader(http.StatusOK)
	messages.ToJSON(rw)
}

/*
 * This function will create a new group (room that will have a name)
 */
func (ch *Chat) CreateGroup(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /group/create hit (POST request)")
	//Extract the data from the request body
	createGroupData := data.CreateGroup{}
	err := createGroupData.FromJSON(r.Body)
	if err != nil {
		ch.logger.Error("Error occured when parsing json", err.Error())
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Internal server error"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Create a new group (insert a new room into the database)
	roomId, err := ch.dbConn.CreateGroup(createGroupData.GroupName, createGroupData.CreatorId)
	if err != nil {
		ch.logger.Info("Error occured when creating group", err.Error())
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Cannot create group"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Add all the participants into the group
	for _, accId := range createGroupData.Participants {
		err := ch.dbConn.InsertUserIntoRoom(accId, roomId)
		if err != nil {
			ch.logger.Error("Error occured when inserting user into group", err.Error())
			//Send an error message back
			jsonError := jsonerrors.JsonError{Message: "Internal server error"}
			rw.WriteHeader(http.StatusInternalServerError)
			jsonError.ToJSON(rw)
			return
		}
	}
	groupResponse := data.CreateGroupResponse{}
	groupResponse.GroupId = roomId
	rw.WriteHeader(http.StatusOK)
	groupResponse.ToJSON(rw)
}

/*
 * This function will send all the groups back to the user
 */
func (ch *Chat) GetGroups(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /chat/groups/{id:[0-9]+} hit (GET request)")
	vars := mux.Vars(r)
	//Check if the id could be parsed (it should always be, but just to be safe, test it)
	id, err := strconv.Atoi(vars["id"])
	ch.logger.Debug(id)
	if err != nil {
		ch.logger.Error("Error occured when parsing the id", err.Error())
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Internal server error"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	//Get all the groups that the user has
	groups, err := ch.dbConn.GetUserGroups(int64(id))
	if err != nil {
		ch.logger.Error("Error occured when getting the groups", err.Error())
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Internal server error"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	for i, _ := range groups {
		lastMessage, lastMessageTimestamp, senderId, _ := ch.dbConn.GetLastMessageFromRoom(groups[i].RoomId)
		ch.logger.Debug(lastMessageTimestamp)
		if lastMessage != "" {
			if senderId == id {
				groups[i].LastMessage = "You: " + lastMessage
				groups[i].LastMessageSender = int64(id)
			} else {
				groups[i].LastMessage = lastMessage
				groups[i].LastMessageSender = int64(senderId)
			}
			groups[i].LastMessageTimestamp = lastMessageTimestamp
		} else {
			groups[i].LastMessage = "Start conversation"
			groups[i].LastMessageTimestamp = ""
		}
	}
	ch.logger.Debug(groups)

	rw.WriteHeader(http.StatusOK)
	groups.ToJSON(rw)
}

/*
 * This function will return all the common groups of 2 users
 */
func (ch *Chat) GetCommonGroups(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /chat/commongroups/{idfirst:[0-9]+}/{idsecond:[0-9]+} hit (GET method)")
	vars := mux.Vars(r)
	idFirst, err := strconv.Atoi(vars["idfirst"])
	//Check if an error occured during the parsing of the id
	if err != nil {
		ch.logger.Error("Error occured when parsing the first id", err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	idSecond, err := strconv.Atoi(vars["idsecond"])
	//Check if an error occured
	if err != nil {
		ch.logger.Error("Error occured when parsing second id", err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	//Get the common groups of the accounts
	commonGroups, err := ch.dbConn.GetCommonGroups(int64(idFirst), int64(idSecond))
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	ch.logger.Debug(commonGroups)

	rw.WriteHeader(http.StatusOK)
	commonGroups.ToJSON(rw)
}

/*
 * This function will update the group photo by saving the multipart form data on the disk
 */
func (ch *Chat) UpdateGroupPicture(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /chat/group/updatepicture hit (GET method)")
	//Parse the multipart request (maximum 20MB upload)
	r.ParseMultipartForm(10 << 20)

	roomId := r.FormValue("roomId")
	if roomId == "" {
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Room id has to be specified in the form"))
		return
	}

	file, _, err := r.FormFile("groupPicture")
	//Check if an error occured
	if err != nil {
		ch.logger.Info("Cannot get the file or the handler")
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Failed!"))
		return
	}
	//Close the file when the function ends
	defer file.Close()

	//Create an empty file on the disk
	dst, err := os.Create("./static/" + roomId + ".png")
	//Check for errors
	if err != nil {
		ch.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	defer dst.Close()

	// Copy the uploaded file to the created file on the filesystem
	if _, err := io.Copy(dst, file); err != nil {
		ch.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rId, err := strconv.ParseInt(roomId, 10, 64)
	if err != nil {
		//Remove the file created
		ch.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	//Update the value in the database
	_, err = ch.dbConn.UpdateGroupPicture(rId, roomId+".png")
	if err != nil {
		ch.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Group picture updated"))
}
