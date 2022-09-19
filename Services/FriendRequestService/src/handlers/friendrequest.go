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
 */
func (frReq *FriendRequest) AddFriendRequest(rw http.ResponseWriter, r *http.Request) {
	//Extract the accounts data from the request body
	friendRequest := data.FriendRequest{}
	err := friendRequest.FromJSON(r.Body)
	if err != nil {
		//Send an error message back
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
 */
func (frReq *FriendRequest) DeleteFriendRequest(rw http.ResponseWriter, r *http.Request) {

}

/*
 * This function will get all the friend requests of an user
 */
func (frReq *FriendRequest) GetFriendRequests(rw http.ResponseWriter, r *http.Request) {
	//Get the id from the request URI (gorilla mux)
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
