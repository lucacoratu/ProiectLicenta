package handlers

import (
	"io"
	"net/http"
	"willow/gateway/logging"
)

/*
 * This structure will hold the neccessary data for the interaction with
 * the Account Service login functionality
 */
type AccountLogin struct {
	logger logging.ILogger
}

/*
 * This function wil create a new AccountLogin object
 */
func NewAccountLogin(l logging.ILogger) *AccountLogin {
	return &AccountLogin{logger: l}
}

/*
 * This function will forward the request to the AccountService /login and wait for the
 * response which will then be sent back to the client
 */
func (login *AccountLogin) LoginIntoAccount(rw http.ResponseWriter, r *http.Request) {
	//Log that the endpoint for login has been hit
	login.logger.Info("Endpoint /accounts/login hit, url:", r.URL.Path)
	login.logger.Debug("Sending data to Account Service")

	//Forward the request to the Account Service /login
	response, err := http.Post("http://localhost:8080/login", "application/json", r.Body)
	//Check if there was an error during the POST request
	if err != nil {
		//There was an error in the request so notify the client about it
		login.logger.Warning("POST request to Account Service /login failed, ", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Bad request"))
	}

	//Read the data from the response body
	respBody, err := io.ReadAll(response.Body)
	//Check if there was an error while reading from response body
	if err != nil {
		//There was an error durring the read operation so notify the client
		login.logger.Warning("Cannot read from response body from Account Service /login, ", err)
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Bad request"))
	}

	//The Account Service responsed, so forward the response to the client
	login.logger.Info("Received response from Account Service")
	rw.WriteHeader(response.StatusCode)
	rw.Write(respBody)
}
