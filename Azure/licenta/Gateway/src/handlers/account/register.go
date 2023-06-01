package handlers

import (
	"io"
	"net/http"
	"willow/gateway/data"
	"willow/gateway/logging"
)

/*
 * This structure will hold the neccessary data for the interaction with
 * the Account Service register functionality
 */
type AccountRegister struct {
	logger        logging.ILogger
	configuration data.Configuration
}

/*
 * This function will create a new object of the AccountRegister struct
 */
func NewAccountRegister(l logging.ILogger, conf data.Configuration) *AccountRegister {
	return &AccountRegister{logger: l, configuration: conf}
}

/*
 * This function will be called when a POST request with the uri /accounts/register comes on the server
 * It will forward the request to the account service and will get the response that will be forwarded
 * to the client
 */
func (reg *AccountRegister) RegisterAccount(rw http.ResponseWriter, r *http.Request) {
	//Log that the /account/register endpoint has been hit
	reg.logger.Info("Endpoint /accounts/register hit, url: ", r.URL.Path)
	reg.logger.Debug("Sending data to Account Service")

	//Get the name of the service from conf
	url, err := reg.configuration.GetServiceURL("accountservice")
	if err != nil {
		reg.logger.Error("Cannot get the account service url from config")
		return
	}

	//Forward the request to the Account Service /register
	response, err := http.Post("http://"+url+"/register", "application/json", r.Body)
	//Check if there was an error in the POST request
	if err != nil {
		//There was an error so notify the client
		reg.logger.Warning("POST request to Account Service /register failed", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Bad request")) //TO DO ... Add pretty error message for the client
		return
	}

	//Read the data from the response body
	respBody, err := io.ReadAll(response.Body)
	//Check if there was an error during the read operation
	if err != nil {
		//There was an error during the read operation so notify the client
		reg.logger.Warning("Cannot read data from response body from Account Service /register", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Bad request")) //TO DO ... Add pretty error message for the client
		return
	}

	//Received response from the Account Service
	reg.logger.Info("Received response from Account Service")
	rw.WriteHeader(response.StatusCode)
	rw.Write(respBody)
}
