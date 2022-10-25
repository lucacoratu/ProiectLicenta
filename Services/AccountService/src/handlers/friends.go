package handlers

import (
	"io"
	"net/http"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/logging"

	"github.com/gorilla/mux"
)

type Friends struct {
	logger      logging.ILogger
	dbConn *database.Connection
}

func NewFriends(l logging.ILogger, db *database.Connection) *Friends {
	return &Friends{logger: l, dbConn: db}
}

/*
 * This function will forward the request body to the FriendService and send the response back to the client
 * If an error occurs during the request forwarding then a jsonerror will be returned back to the client
 */
func (f *Friends) AddFriend(rw http.ResponseWriter, r *http.Request) {

}

/*
 * This function will forward the request body to the FriendService and send the response back to the client
 * If an error occurs during the request forwarding then a jsonerror will be returned back to the client
 */
func (f *Friends) GetFriends(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /friend/view/{id} reached (GET method) - sending data to FriendService")
	vars := mux.Vars(r)
	f.logger.Debug("Id received is", vars["id"])
	response, err := http.Get("http://localhost:8084/friend/view/" + vars["id"])
	if err != nil {
		f.logger.Error("Cannot send data to FriendService", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot send request to FriendService"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Read the data from the response
	respBody, err := io.ReadAll(response.Body)
	if err != nil {
		f.logger.Error("Cannot read response data from FriendService response", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Cannot read data from FriendService response body"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}

	//Debug log the response from the FriendService
	f.logger.Debug("Response from FriendService", respBody)

	rw.WriteHeader(response.StatusCode)
	rw.Write(respBody)
}
