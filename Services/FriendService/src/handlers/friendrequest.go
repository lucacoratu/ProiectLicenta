package handlers

import (
	"io"
	"net/http"
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
	response, err := http.Post("http://localhost:8083/add", "application/json", r.Body)
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
	response, err := http.Post("http://localhost:8083/delete", "application/json", r.Body)
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
	response, err := http.Get("http://localhost:8083/view/" + vars["id"])
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
	response, err := http.Get("http://localhost:8083/viewsent/" + vars["id"])
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