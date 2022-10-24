package handlers

import (
	"net/http"
	"willow/chatservice/data"
	"willow/chatservice/logging"
	"willow/chatservice/database"
	"willow/chatservice/websocket"
	jsonerrors "willow/chatservice/errors"

	"github.com/gorilla/mux"
	"strconv"
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
func NewChat(dbConn database.IConnection, logger logging.ILogger) *Chat{
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
func (ch *Chat) GetRooms(rw http.ResponseWriter, r *http.Request) {
	//Get the id from the request URI (gorilla mux)
	ch.logger.Info("Endpoint /rooms/{id:[0-9]+} hit (GET Method)")
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

func (ch *Chat) ServeWs(pool *websocket.Pool, rw http.ResponseWriter, r * http.Request) {
	ch.logger.Info("Endpoint /ws hit")
	ws, err := websocket.Upgrade(rw, r)
	if err != nil{
		ch.logger.Error(err.Error())
		return
	}

	client := &websocket.Client{
		Conn: ws,
		Pool: pool,
	}

	pool.Register <- client
	client.Read()
}