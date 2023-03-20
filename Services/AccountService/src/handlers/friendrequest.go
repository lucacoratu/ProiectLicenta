package handlers

import (
	"io"
	"net/http"
	"strconv"
	"willow/accountservice/data"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/logging"

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
	frReq.logger.Debug("Response from FriendRequestService", string(respbody))

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
	/* 	respBody, err := io.ReadAll(response.Body)
	   	if err != nil {
	   		frReq.logger.Error("Cannot read response data from FriendRequestService response", err.Error())
	   		jsonError := jsonerrors.JsonError{Message: "Cannot read data from FriendRequestService response body"}
	   		rw.WriteHeader(http.StatusInternalServerError)
	   		jsonError.ToJSON(rw)
	   		return
	   	} */
	respData := &data.FriendRequestResponses{}
	respData.FromJSON(response.Body)
	frReq.logger.Info(*respData)

	accs := make(data.FriendRequestAccounts, 0)
	for _, rd := range *respData {
		frReq.logger.Info(rd)
		//Check if an error occured while fetching the details from the database
		accDetails, err := frReq.dbConn.GetAccountDetails(rd.SenderID)
		if err != nil {
			frReq.logger.Info(err.Error())
			continue
		}
		//f.logger.Info(*accDetails)
		frAccount := data.FriendRequestAccount{AccountID: rd.SenderID, DisplayName: accDetails.DisplayName, RequestDate: rd.RequestDate, LastOnline: accDetails.LastOnline, JoinDate: accDetails.JoinDate, ProfilePictureUrl: accDetails.ProfilePictureUrl}
		frReq.logger.Debug(frAccount)
		accs = append(accs, frAccount)
	}
	frReq.logger.Info(accs)

	//Debug log the response from the FriendRequestService
	//frReq.logger.Debug("Response from FriendRequestService", respBody)

	rw.WriteHeader(response.StatusCode)
	accs.ToJSON(rw)
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
	/* 	respBody, err := io.ReadAll(response.Body)
	   	if err != nil {
	   		frReq.logger.Error("Cannot read response data from FriendRequestService response", err.Error())
	   		jsonError := jsonerrors.JsonError{Message: "Cannot read data from FriendRequestService response body"}
	   		rw.WriteHeader(http.StatusInternalServerError)
	   		jsonError.ToJSON(rw)
	   		return
	   	} */
	respData := &data.FriendRequestResponses{}
	respData.FromJSON(response.Body)
	frReq.logger.Info(*respData)

	accs := make(data.FriendRequestAccounts, 0)
	for _, rd := range *respData {
		frReq.logger.Info(rd)
		//Check if an error occured while fetching the details from the database
		accDetails, err := frReq.dbConn.GetAccountDetails(rd.AccID)
		if err != nil {
			frReq.logger.Info(err.Error())
			continue
		}
		//f.logger.Info(*accDetails)
		frAccount := data.FriendRequestAccount{AccountID: rd.AccID, DisplayName: accDetails.DisplayName, RequestDate: rd.RequestDate, LastOnline: accDetails.LastOnline, JoinDate: accDetails.JoinDate}
		frReq.logger.Debug(frAccount)
		accs = append(accs, frAccount)
	}
	frReq.logger.Info(accs)

	//Debug log the response from the FriendRequestService
	//frReq.logger.Debug("Response from FriendRequestService", respBody)

	rw.WriteHeader(response.StatusCode)
	//rw.Write(respBody)
	accs.ToJSON(rw)
}

/*
 * This function will get as list of accounts that the user can send friend requests to
 */
func (frReq *FriendRequest) GetFriendsRecommendations(rw http.ResponseWriter, r *http.Request) {
	frReq.logger.Info("Endpoint /account/{id:[0-9]+}/friendrecommendations hit (GET method)")
	//Get the account id from the url
	vars := mux.Vars(r)
	accountId, err := strconv.Atoi(vars["id"])
	//Check if an error occured when parsing the account id in the url
	if err != nil {
		frReq.logger.Error("Error occured when parsing the account id", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Internal server error"))
		return
	}
	//Get the friend recommendations
	users, err := frReq.dbConn.GetUsers(int64(accountId))
	//Check if an error occured
	if err != nil {
		frReq.logger.Error("Error occured when getting the friend recommendations", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Internal server error"))
		return
	}

	rw.WriteHeader(http.StatusOK)
	users.ToJSON(rw)
}
