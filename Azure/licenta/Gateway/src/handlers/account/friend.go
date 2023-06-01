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

type Friend struct {
	logger        logging.ILogger
	configuration data.Configuration
}

func NewFriend(l logging.ILogger, conf data.Configuration) *Friend {
	return &Friend{logger: l, configuration: conf}
}

/*
 * This function will forward a message to the url: proxyProtocol://proxyHost/uri. When forwarding the headers of the request will remain intact
 * It will return an error if somethign happens during the forwarding of the message or the response body if the request was forwarded succesfully
 */
func (f *Friend) ForwardRequest(proxyProtocol string, proxyHost string, r *http.Request) ([]byte, error) {
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
	f.logger.Debug(proxyReq.Header)

	httpClient := &http.Client{}
	resp, err := httpClient.Do(proxyReq)
	if err != nil {
		//http.Error(rw, err.Error(), http.StatusBadGateway)
		return nil, err
	}
	body2, err := ioutil.ReadAll(resp.Body)
	f.logger.Debug(string(body2))
	defer resp.Body.Close()
	return body2, nil
}

/*
 * Get all the friends of the account
 */
func (f *Friend) GetFriends(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /friend/view/{id} hit (GET request)")
	f.logger.Debug("Forwarding message to AccountService")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * Accept a friend request
 */
func (f *Friend) AddFriend(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /friend/add hit (GET request)")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}
