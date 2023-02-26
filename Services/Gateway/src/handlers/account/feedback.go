package handlers

import (
	"bytes"
	"fmt"
	"io/ioutil"
	"net/http"
	"willow/gateway/logging"
)

type Feedback struct {
	logger logging.ILogger
}

func NewFeedback(l logging.ILogger) *Feedback {
	return &Feedback{logger: l}
}

/*
 * This function will forward a message to the url: proxyProtocol://proxyHost/uri. When forwarding the headers of the request will remain intact
 * It will return an error if somethign happens during the forwarding of the message or the response body if the request was forwarded succesfully
 */
func (f *Feedback) ForwardRequest(proxyProtocol string, proxyHost string, r *http.Request) ([]byte, error) {
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
	f.logger.Debug(proxyReq.Header)

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
 * This function will forward the request to the account service which will be able to add the bug report into the database
 */
func (f *Feedback) AddBugReport(rw http.ResponseWriter, r *http.Request) {
	//Log that the /account/bugreport POST method has been hit
	f.logger.Info("/accounts/reportbug hit (POST method)")
	f.logger.Debug("Forwarding message to Account service")

	returnData, err := f.ForwardRequest("http", "localhost:8081", r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will forward the request to the account service which will be able to all the bug report categories from the database
 */
func (f *Feedback) GetAllReportCategories(rw http.ResponseWriter, r *http.Request) {
	//Log that the /account/bugreport POST method has been hit
	f.logger.Info("/accounts/reportcategories hit (GET method)")
	f.logger.Debug("Forwarding message to Account service")

	returnData, err := f.ForwardRequest("http", "localhost:8081", r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will forward the request to the account service which will update the profile picture of the user
 */
func (f *Feedback) UpdateProfilePicture(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/accounts/picture hit (POST method)")
	f.logger.Debug("Forwarding message to Account service")

	returnData, err := f.ForwardRequest("http", "localhost:8081", r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will forward the request to the account service which will send the profile picture back to the client
 */
func (f *Feedback) GetProfilePicture(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/accounts/static hit (GET method)")
	f.logger.Debug("Forwarding message to Account service")

	returnData, err := f.ForwardRequest("http", "localhost:8081", r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will forward the request to the chat service to get all the groups of an user
 */
func (f *Feedback) GetGroups(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/chat/groups/{id:[0-9]+} hit (GET method)")
	f.logger.Debug("Forwarding message to Account service")

	returnData, err := f.ForwardRequest("http", "localhost:8081", r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

func (f *Feedback) GetProfile(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/profile/{id:[0-9]+} hit (GET method)")
	f.logger.Debug("Forwarding message to Account service")

	returnData, err := f.ForwardRequest("http", "localhost:8081", r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}
