package handlers

import (
	//"io"
	"bytes"
	"fmt"
	"io/ioutil"
	"net/http"
	"willow/gateway/data"
	"willow/gateway/logging"
)

type FriendRequest struct {
	logger        logging.ILogger
	configuration data.Configuration
}

func NewFriendRequest(l logging.ILogger, conf data.Configuration) *FriendRequest {
	return &FriendRequest{logger: l, configuration: conf}
}

/*
 * This function will forward a message to the url: proxyProtocol://proxyHost/uri. When forwarding the headers of the request will remain intact
 * It will return an error if somethign happens during the forwarding of the message or the response body if the request was forwarded succesfully
 */
func (freq *FriendRequest) ForwardRequest(proxyProtocol string, proxyHost string, r *http.Request) ([]byte, error) {
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

	proxyReq, err := http.NewRequest(r.Method, url, bytes.NewReader(body))

	// We may want to filter some headers, otherwise we could just use a shallow copy
	// proxyReq.Header = req.Header
	proxyReq.Header = make(http.Header)
	for h, val := range r.Header {
		proxyReq.Header[h] = val
	}
	freq.logger.Debug(proxyReq.Header)

	httpClient := &http.Client{}
	resp, err := httpClient.Do(proxyReq)
	if err != nil {
		//http.Error(rw, err.Error(), http.StatusBadGateway)
		return nil, err
	}
	body2, err := ioutil.ReadAll(resp.Body)
	freq.logger.Debug(string(body2))
	defer resp.Body.Close()
	return body2, nil
}

/*
 * This function will get the friendrequests of the accounts with id.
 */
func (freq *FriendRequest) GetFriendRequests(rw http.ResponseWriter, r *http.Request) {
	freq.logger.Info("Endpoint /friendrequest/view/{id} hit (GET request)")
	freq.logger.Debug("Forwarding message to AccountService")

	//Get the name of the service from conf
	url, err := freq.configuration.GetServiceURL("accountservice")
	if err != nil {
		freq.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := freq.ForwardRequest("http", url, r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will get the friendrequests of the accounts with id.
 */
func (freq *FriendRequest) GetSentFriendRequests(rw http.ResponseWriter, r *http.Request) {
	freq.logger.Info("Endpoint /friendrequest/viewsent/{id} hit (GET request)")
	freq.logger.Debug("Forwarding message to AccountService")

	//Get the name of the service from conf
	url, err := freq.configuration.GetServiceURL("accountservice")
	if err != nil {
		freq.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := freq.ForwardRequest("http", url, r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will add a new friendrequest for the account specified
 */
func (freq *FriendRequest) AddFriendRequest(rw http.ResponseWriter, r *http.Request) {
	freq.logger.Info("Endpoint /friendrequest/add hit (POST request)")
	freq.logger.Debug("Forwarding message to AccountService")

	//Get the name of the service from conf
	url, err := freq.configuration.GetServiceURL("accountservice")
	if err != nil {
		freq.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := freq.ForwardRequest("http", url, r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will delete a friendrequest for the account specified
 */
func (freq *FriendRequest) DeleteFriendRequest(rw http.ResponseWriter, r *http.Request) {
	freq.logger.Info("Endpoint /friendrequest/delete hit (POST request)")
	freq.logger.Debug("Forwarding message to AccountService")

	//Get the name of the service from conf
	url, err := freq.configuration.GetServiceURL("accountservice")
	if err != nil {
		freq.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := freq.ForwardRequest("http", url, r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will get the friend request recommendations from the account service
 */
func (freq *FriendRequest) GetFriendRecommendations(rw http.ResponseWriter, r *http.Request) {
	freq.logger.Info("Endpoint /account/{id:[0-9]+}/friendrecommendations hit (GET method)")
	freq.logger.Debug("Forwarding request to AccountService")

	//Get the name of the service from conf
	url, err := freq.configuration.GetServiceURL("accountservice")
	if err != nil {
		freq.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := freq.ForwardRequest("http", url, r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}
