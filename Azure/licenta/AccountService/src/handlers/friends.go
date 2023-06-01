package handlers

import (
	"bytes"
	"fmt"
	"io"
	"io/ioutil"
	"net/http"
	"strconv"
	"willow/accountservice/data"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/logging"

	"github.com/gorilla/mux"
)

type Friends struct {
	logger        logging.ILogger
	dbConn        *database.Connection
	configuration data.Configuration
}

func NewFriends(l logging.ILogger, db *database.Connection, conf data.Configuration) *Friends {
	return &Friends{logger: l, dbConn: db, configuration: conf}
}

/*
 * This function will forward a message to the url: proxyProtocol://proxyHost/uri. When forwarding the headers of the request will remain intact
 * It will return an error if somethign happens during the forwarding of the message or the response body if the request was forwarded succesfully
 */
func (f *Friends) ForwardRequest(proxyProtocol string, proxyHost string, r *http.Request) ([]byte, error) {
	// we need to buffer the body if we want to read it here and send it
	// in the request.
	body, err := ioutil.ReadAll(r.Body)
	if err != nil {
		//http.Error(rw, err.Error(), http.StatusInternalServerError)
		return nil, err
	}

	// you can reassign the body if you need to parse it as multipart
	r.Body = ioutil.NopCloser(bytes.NewReader(body))

	// create a new url from the raw RequestURI sent by the client
	url := fmt.Sprintf("%s://%s%s", proxyProtocol, proxyHost, r.RequestURI)

	proxyReq, _ := http.NewRequest(r.Method, url, bytes.NewReader(body))

	// We may want to filter some headers, otherwise we could just use a shallow copy
	// proxyReq.Header = req.Header
	proxyReq.Header = make(http.Header)
	for h, val := range r.Header {
		proxyReq.Header[h] = val
	}
	f.logger.Info(proxyReq.Header)

	httpClient := &http.Client{}
	resp, err := httpClient.Do(proxyReq)
	if err != nil {
		//http.Error(rw, err.Error(), http.StatusBadGateway)
		return nil, err
	}
	body2, _ := ioutil.ReadAll(resp.Body)
	f.logger.Debug(string(body2))
	defer resp.Body.Close()
	return body2, nil
}

/*
 * This function will forward the request body to the FriendService and send the response back to the client
 * If an error occurs during the request forwarding then a jsonerror will be returned back to the client
 */
func (f *Friends) AddFriend(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /friend/add reached (POST Method) - sending data to FriendService")
	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("friendservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}
	//Send data to FriendService with the request unmodified
	response, err := http.Post("http://"+url+"/friend/add", "application/json", r.Body)
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
	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("friendservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	vars := mux.Vars(r)
	f.logger.Debug("Id received is", vars["id"])
	idReceiver, err := strconv.Atoi(vars["id"])
	idRecv := int64(idReceiver)
	response, err := http.Get("http://" + url + "/friend/view/" + vars["id"])
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
	f.logger.Debug(respData)

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
		frAccount := data.FriendAccount{FriendID: frID, DisplayName: accDetails.DisplayName, RoomID: rd.RoomID, BefriendDate: rd.BefriendDate, LastOnline: accDetails.LastOnline, Status: accDetails.Status, JoinDate: accDetails.JoinDate, LastMessage: rd.LastMessage, LastMessageTimestamp: rd.LastMessageTimestamp, ProfilePictureUrl: accDetails.ProfilePictureUrl, About: accDetails.About}
		f.logger.Debug(frAccount)
		accs = append(accs, frAccount)
	}
	f.logger.Info(accs)

	//Debug log the response from the FriendService
	//f.logger.Debug("Response from FriendService", respBody)

	rw.WriteHeader(response.StatusCode)
	accs.ToJSON(rw)
}

/*
 * This function will get the friends of an account which have the account id greater than one specified in the get
 */
func (f *Friends) GetNewerFriends(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /friend/viewnew/{accountId}/{lastId} reached (GET method) - sending data to FriendService")
	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("friendservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}
	vars := mux.Vars(r)
	f.logger.Debug("Id received is", vars["accountId"])
	idReceiver, err := strconv.Atoi(vars["accountId"])
	idRecv := int64(idReceiver)
	lastId, err := strconv.Atoi(vars["lastId"])
	lastFriendId := int64(lastId)
	f.logger.Debug("Last friend id", lastFriendId)

	response, err := http.Get("http://" + url + "/friend/view/" + vars["accountId"])
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
	f.logger.Debug(*respData)

	//Get the last id of the friendship
	lastFriendshipId := int64(0)
	for _, rd := range *respData {
		frID := int64(-1)
		if idRecv == rd.FriendID {
			frID = rd.AccountID
		} else {
			frID = rd.FriendID
		}
		if frID == lastFriendId {
			lastFriendshipId = rd.ID
		}
	}
	f.logger.Debug("Last friendship id", lastFriendshipId)
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
		if rd.ID > lastFriendshipId {
			frAccount := data.FriendAccount{FriendID: frID, DisplayName: accDetails.DisplayName, RoomID: rd.RoomID, BefriendDate: rd.BefriendDate, LastOnline: accDetails.LastOnline, Status: accDetails.Status, JoinDate: accDetails.JoinDate, LastMessage: rd.LastMessage, LastMessageTimestamp: rd.LastMessageTimestamp, ProfilePictureUrl: accDetails.ProfilePictureUrl, About: accDetails.About, IdentityPublicKey: accDetails.IdentityPublicKey, PreSignedPublicKey: accDetails.PreSignedPublicKey}
			f.logger.Debug(frAccount)
			accs = append(accs, frAccount)
		}
	}
	f.logger.Info(accs)

	//Debug log the response from the FriendService
	//f.logger.Debug("Response from FriendService", respBody)

	rw.WriteHeader(response.StatusCode)
	accs.ToJSON(rw)
}

/*
 * This function will forward the request to the friend service and get the response
 */
func (f *Friends) CanSendFriendRequest(rw http.ResponseWriter, r *http.Request) {
	//Log that the endpoint has been hit
	f.logger.Info("Endpoint /account/cansend/friendrequest hit (POST method)")
	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("friendservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}
	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		f.logger.Error("Error occured on friend service", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Internal server error"))
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}
