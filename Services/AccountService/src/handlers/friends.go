package handlers

import (
	"io"
	"net/http"
	"strconv"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/logging"
	"willow/accountservice/data"

	"github.com/gorilla/mux"
)

type Friends struct {
	logger      logging.ILogger
	dbConn 		*database.Connection
}

func NewFriends(l logging.ILogger, db *database.Connection) *Friends {
	return &Friends{logger: l, dbConn: db}
}

/*
 * This function will forward the request body to the FriendService and send the response back to the client
 * If an error occurs during the request forwarding then a jsonerror will be returned back to the client
 */
func (f *Friends) AddFriend(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /friend/add reached (POST Method) - sending data to FriendService")
	//Send data to FriendService with the request unmodified
	response, err := http.Post("http://localhost:8084/add", "application/json", r.Body)
	if err != nil {
		f.logger.Error("Cannot send data to FriendService", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot send request to FriendService"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Read the data from the response
	respbody, err := io.ReadAll(response.Body)
	if err != nil {
		f.logger.Error("Cannot read response data from FriendService response", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot read data from FriendService response body"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Debug log the response from the service
	f.logger.Debug("Response from FriendService", respbody)

	//Send the response back
	rw.WriteHeader(response.StatusCode)
	rw.Write(respbody)
}

/*
 * This function will forward the request body to the FriendService and send the response back to the client
 */
func (f *Friends) DeleteFriends(rw http.ResponseWriter, r *http.Request) {

}

/*
 * This function will forward the request body to the FriendService and send the response back to the client
 * If an error occurs during the request forwarding then a jsonerror will be returned back to the client
 */
func (f *Friends) GetFriends(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /friend/view/{id} reached (GET method) - sending data to FriendService")
	vars := mux.Vars(r)
	f.logger.Debug("Id received is", vars["id"])
	idReceiver, err := strconv.Atoi(vars["id"])
	idRecv := int64(idReceiver)
	response, err := http.Get("http://localhost:8084/friend/view/" + vars["id"])
	if err != nil {
		f.logger.Error("Cannot send data to FriendService", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot send request to FriendService"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Read the data from the response
	respData := &data.FriendResponses{}
	respData.FromJSON(response.Body)

	accs := make(data.FriendAccounts, 0)
	for _, rd := range *respData {
		//f.logger.Info(rd)
		frID := int64(-1)
		if idRecv == rd.FriendID {
			frID = rd.AccountID
		} else {
			frID = rd.FriendID
		}
		f.logger.Debug(frID)
		//Check if an error occured while fetching the details from the database
		accDetails, err := f.dbConn.GetAccountDetails(frID)
		if err != nil {
			f.logger.Info(err.Error())
			continue
		}
		//f.logger.Info(*accDetails)
		frAccount := data.FriendAccount{FriendID: frID, DisplayName: accDetails.DisplayName, BefriendDate: rd.BefriendDate, LastOnline: accDetails.LastOnline, JoinDate: accDetails.JoinDate}
		f.logger.Debug(frAccount)
		accs = append(accs, frAccount)
	}
	f.logger.Info(accs)

	//Debug log the response from the FriendService
	//f.logger.Debug("Response from FriendService", respBody)

	rw.WriteHeader(response.StatusCode)
	accs.ToJSON(rw)
}
