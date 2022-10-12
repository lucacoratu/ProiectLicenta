package handlers

import (
	"net/http"
	"strconv"
	"willow/friendrequestservice/data"
	"willow/friendrequestservice/database"
	jsonerrors "willow/friendrequestservice/errors"
	"willow/friendrequestservice/logging"

	"github.com/gorilla/mux"
)

type FriendRequest struct {
	logger logging.ILogger
	dbConn *database.Connection
}

func NewFriendRequest(logger logging.ILogger, dbConn *database.Connection) *FriendRequest {
	return &FriendRequest{logger: logger, dbConn: dbConn}
}

/*
 * This function will add a new friend request into the database
 * First it will parse the data in the POST request body, then it will validate that the fields hold correct data types
 * If the data is valid, it will check if the user is trying to be friend with itself
 * Afterwards it will try to insert the friend request into the database and will check for errors
 * If there are no errors a success message will be sent back to the client
 * Else an error in JSON format will be sent
 */
func (frReq *FriendRequest) AddFriendRequest(rw http.ResponseWriter, r *http.Request) {
	//Extract the accounts data from the request body
	frReq.logger.Info("Endpoint /add reached (POST method)")
	friendRequest := data.FriendRequest{}
	err := friendRequest.FromJSON(r.Body)
	if err != nil {
		//Send an error message back
		frReq.logger.Error("Error occured when parsing the request body", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Invalid JSON format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}

	//Validate the json data received
	err = friendRequest.Validate()
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Invalid JSON data"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}

	//Check that the accID and senderID are different (cannot be friend with yourself)
	if friendRequest.AccID == friendRequest.SenderID {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Cannot be friend with yourself"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}

	//Add the friend request to the database
	err = frReq.dbConn.AddFriendRequest(friendRequest.AccID, friendRequest.SenderID)
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Cannot add the friend request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	//The account has been successfully added
	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Friend request sent"))
}

/*
 * This function will remove a friend request form the database
 * First it will try to parse and validate the data received from the user
 * If the data is not valid then an error message will be sent to the client
 * After data validation, it will try to delete the friendship from the database, and check for errors
 * If there are no errors during the deletion process, a success message will be sent back to the client
 */
func (frReq *FriendRequest) DeleteFriendRequest(rw http.ResponseWriter, r *http.Request) {
	//Get the data from the request body
	frReq.logger.Info("Endpint /delete hit (POST Method)")
	friendRequest := data.FriendRequest{}
	err := friendRequest.FromJSON(r.Body)
	//Check if an error occured during JSON parsing
	if err != nil {
		jsonError := jsonerrors.JsonError{Message: "Invalid JSON format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}
	//Validate the data received
	err = friendRequest.Validate()
	if err != nil {
		jsonError := jsonerrors.JsonError{Message: "Invalid JSON data"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
	}
	//Delete the friend request from the database
	err = frReq.dbConn.DeleteFriendRequest(friendRequest.AccID, friendRequest.SenderID)
	if err != nil {
		jsonError := jsonerrors.JsonError{Message: "Cannot delete the friend request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
	}
	//The deletion was successful
	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Friend request deleted!"))
}

/*
 * This function will get all the friend requests of an user
 * The URI must have an id which will be the ID of the account that wants to view the friend requests
 * If the friend requests have been succesfully extracted from the database, they will be sent back to the client
 */
func (frReq *FriendRequest) GetFriendRequests(rw http.ResponseWriter, r *http.Request) {
	//Get the id from the request URI (gorilla mux)
	frReq.logger.Info("Enpoint /view/{id:[0-9]+} hit (GET Method)")
	vars := mux.Vars(r)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Invalid id format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}

	//Extract the friend requests for the account specified in the body
	friendRequests, err := frReq.dbConn.ViewFriendRequests(id)
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Cannot get the friend requests"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
	}

	//Send the friend requests back
	rw.WriteHeader(http.StatusOK)
	friendRequests.ToJSON(rw)
}

/*
 * This function will get all the friend requests of an user
 * The URI must have an id which will be the ID of the account that wants to view the friend requests
 * If the friend requests have been succesfully extracted from the database, they will be sent back to the client
 */
func (frReq *FriendRequest) GetSentFriendRequests(rw http.ResponseWriter, r *http.Request) {
	//Get the id from the request URI (gorilla mux)
	frReq.logger.Info("Enpoint /viewsent/{id:[0-9]+} hit (GET Method)")
	vars := mux.Vars(r)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Invalid id format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonError.ToJSON(rw)
		return
	}

	//Extract the friend requests for the account specified in the body
	friendRequests, err := frReq.dbConn.ViewSentFriendRequests(id)
	if err != nil {
		//Send an error message back
		jsonError := jsonerrors.JsonError{Message: "Cannot get the friend requests"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
	}

	//Send the friend requests back
	rw.WriteHeader(http.StatusOK)
	friendRequests.ToJSON(rw)
}

/*
 * This function will send back to the client true (if the friend request exists),
 * between the 2 users specified in the request body, or false if the friend request does not exist
 * If there is any error during the parsing of the message or when interacting with the database,
 * then http.StatusInternalServerError will be returned and an error message
 */
func (frReq *FriendRequest) ExistsFriendRequest(rw http.ResponseWriter, r *http.Request) {
	//This will be a POST method on /arefriends
	frReq.logger.Info("Endpoint /arefriends hit (POST Method)")
	//Parse the data received from the client in the request body
	frData := &data.FriendRequest{}
	err := frData.FromJSON(r.Body)
	//Check if an error occured when parsing the data from the client
	if err != nil {
		//An error occured during the parsing of the JSON data
		jsonError := jsonerrors.JsonError{Message: "Invalid JSON format"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//The JSON data has been parsed succesfully
	//Check if the 2 accounts share a friend request (in the database)
	flag, err := frReq.dbConn.AreFriends(frData.AccID, frData.SenderID)
	//Check if an error occured while querying the database
	if err != nil {
		//An error occured when querying the database
		jsonError := jsonerrors.JsonError{Message: "error occured"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Return the friend request status between the 2 accounts (true if there is a friend request, false if there isn't)
	af := data.AreFriends{Message: flag}
	rw.WriteHeader(http.StatusOK)
	af.ToJSON(rw)
}
