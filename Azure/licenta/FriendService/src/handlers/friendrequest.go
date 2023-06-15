package handlers

import (
	"bytes"
	"encoding/json"
	"io"
	"net/http"
	"willow/friendservice/data"
	"willow/friendservice/database"
	jsonerrors "willow/friendservice/errors"
	"willow/friendservice/logging"

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
 * This function will forward the request to the FriendRequestService and wait for the response
 */
func (frReq *FriendRequest) AddFriendRequest(rw http.ResponseWriter, r *http.Request) {
	//TO DO...Check if the friend request service is online
	frReq.logger.Info("Endpoint /friendrequest/add reached (POST method) - sending data to FriendRequestService")
	response, err := http.Post("http://frequestservice:8083/add", "application/json", r.Body)
	if err != nil {
		frReq.logger.Error("Cannot send data to FriendRequestService", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot send request to FriendRequestService"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Read the data from the response
	respbody, err := io.ReadAll(response.Body)
	if err != nil {
		frReq.logger.Error("Cannot read response data from FriendRequestService response", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot read data from FriendRequestService response body"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Debug log the response from the service
	frReq.logger.Debug("Response from FriendRequestService", respbody)

	//Send the response back
	rw.WriteHeader(response.StatusCode)
	rw.Write(respbody)
}

/*
 * This function will forward the request to the FriendRequestService and wait for the response
 */
func (frReq *FriendRequest) DeleteFriendRequest(rw http.ResponseWriter, r *http.Request) {
	//TO DO...Check if the friend request service is online
	frReq.logger.Info("Endpoint /friendrequest/delete reached (POST method) - sending data to FriendRequestService")
	response, err := http.Post("http://frequestservice:8083/delete", "application/json", r.Body)
	if err != nil {
		frReq.logger.Error("Cannot send data to FriendRequestService", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot send request to FriendRequestService"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Read the data from the response
	respbody, err := io.ReadAll(response.Body)
	if err != nil {
		frReq.logger.Error("Cannot read response data from FriendRequestService response", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot read data from FriendRequestService response body"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Debug log the response from the service
	frReq.logger.Debug("Response from FriendRequestService", respbody)

	//Send the response back
	rw.WriteHeader(response.StatusCode)
	rw.Write(respbody)
}

/*
 * This function will forward the request to the FriendRequestService and wait for the response
 */
func (frReq *FriendRequest) ViewFriendRequests(rw http.ResponseWriter, r *http.Request) {
	frReq.logger.Info("Endpoint /friendrequest/view/{id} reached (GET method) - sending data to FriendRequestService")
	vars := mux.Vars(r)
	frReq.logger.Debug("Id received is", vars["id"])
	response, err := http.Get("http://frequestservice:8083/view/" + vars["id"])
	if err != nil {
		frReq.logger.Error("Cannot send data to FriendRequestService", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot send request to FriendRequestService"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Read the data from the response
	respBody, err := io.ReadAll(response.Body)
	if err != nil {
		frReq.logger.Error("Cannot read response data from FriendRequestService response", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot read data from FriendRequestService response body"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	//Debug log the response from the FriendRequestService
	frReq.logger.Debug("Response from FriendRequestService", respBody)

	rw.WriteHeader(response.StatusCode)
	rw.Write(respBody)
}

/*
 * This function will forward the request to the FriendRequestService and wait for the response
 */
func (frReq *FriendRequest) ViewSentFriendRequests(rw http.ResponseWriter, r *http.Request) {
	frReq.logger.Info("Endpoint /friendrequest/viewsent/{id} reached (GET method) - sending data to FriendRequestService")
	vars := mux.Vars(r)
	frReq.logger.Debug("Id received is", vars["id"])
	response, err := http.Get("http://frequestservice:8083/viewsent/" + vars["id"])
	if err != nil {
		frReq.logger.Error("Cannot send data to FriendRequestService", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot send request to FriendRequestService"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Read the data from the response
	respBody, err := io.ReadAll(response.Body)
	if err != nil {
		frReq.logger.Error("Cannot read response data from FriendRequestService response", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot read data from FriendRequestService response body"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	//Debug log the response from the FriendRequestService
	frReq.logger.Debug("Response from FriendRequestService", respBody)

	rw.WriteHeader(response.StatusCode)
	rw.Write(respBody)
}

/*
 * This function will verify if a user is friend with another user or if it has a friend request already sent to that user
 * If the user is already a friend of the other user then he should not be able to send a friend request
 * If the user has a pending friend request to the other user then he should not be able to send a friend request
 * If the user is not friend with the other user nor it has a pending friend request, then he should be able to send a friend request
 */
func (frReq *FriendRequest) CanSendFriendRequest(rw http.ResponseWriter, r *http.Request) {
	//Log that the endpoint has been hit
	frReq.logger.Info("Endpoint /account/cansend/friendrequest hit (POST method)")
	//Check if the account has a friend request from the other account
	//Parse the request data
	jsonData := data.CheckSendFriendRequest{}
	err := jsonData.FromJSON(r.Body)
	//Check if an error occured when parsing the request body
	if err != nil {
		frReq.logger.Error("Error occured when parsing data from client to check send friend request", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Internal server error"))
		return
	}

	jsonSendData := data.AreFriendsRequest{AccID: int(jsonData.SenderId), SenderID: int(jsonData.UserId)}
	bodyData, err := json.Marshal(jsonSendData)
	//Check if an error occured when transforming to json
	if err != nil {
		frReq.logger.Error("Error occured when marshaling to json the request for friend request service", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Internal server error"))
		return
	}

	//Send a POST request to the friendrequest service
	resp, err := http.Post("http://frequestservice:8083/arefriends", "application/json", bytes.NewReader(bodyData))
	//Check if an error occured while sending the request to the friendrequest service
	if err != nil {
		//An error occured so send an error message back to the client
		frReq.logger.Error("Could not send the request to friendrequest service", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Error occured, cannot forward the message"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Check if an error occured in the friendrequest service
	if resp.StatusCode == http.StatusInternalServerError {
		//An error occured in the friendrequest service
		jsonError := jsonerrors.JsonError{}
		err = jsonError.FromJSON(resp.Body)
		if err != nil {
			//An error occured while reading the data from the response body from the friendrequest service
			frReq.logger.Error("Could not read data from the response body from friendrequest service", err.Error())
			jsonError := jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
			rw.WriteHeader(http.StatusInternalServerError)
			jsonError.ToJSON(rw)
			return
		}
		frReq.logger.Warning("Error occured in the friendrequest service", jsonError.Message)
		jsonError = jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Check if there is a friend request between these 2 accounts (answer from friendrequest service)
	areFriendsData := &data.AreFriends{}
	err = areFriendsData.FromJSON(resp.Body)
	//Check if there was an error when parsing the response from the friendrequest service
	if err != nil {
		//An error occured when parsing the JSON response from the friendrequest service
		frReq.logger.Error("Could not parse response data from friendrequest service", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	exists, err := frReq.dbConn.CheckFriendshipExists(int(jsonData.SenderId), int(jsonData.UserId))
	//Check if an error occured when querying the database
	if err != nil {
		frReq.logger.Error("Error occured when querying the database for friendship existance", jsonData.SenderId, jsonData.UserId, err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Internal server error"))
		return
	}

	returnData := data.CanSendFriendRequest{}
	if !exists && !areFriendsData.Message {
		returnData.CanSendRequest = true
	} else {
		returnData.CanSendRequest = false
	}

	//Send the response to the client
	rw.WriteHeader(http.StatusOK)
	returnData.ToJSON(rw)
}
