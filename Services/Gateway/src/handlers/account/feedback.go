package handlers

import (
	"bytes"
	"fmt"
	"io/ioutil"
	"net/http"
	"willow/gateway/data"
	"willow/gateway/logging"
)

type Feedback struct {
	logger        logging.ILogger
	configuration data.Configuration
}

func NewFeedback(l logging.ILogger, conf data.Configuration) *Feedback {
	return &Feedback{logger: l, configuration: conf}
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
 * This function will forward the request to the account service which will be able to add the bug report into the database
 */
func (f *Feedback) AddBugReport(rw http.ResponseWriter, r *http.Request) {
	//Log that the /account/bugreport POST method has been hit
	f.logger.Info("/accounts/reportbug hit (POST method)")
	f.logger.Debug("Forwarding message to Account service")

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
 * This function will forward the request to the account service which will be able to all the bug report categories from the database
 */
func (f *Feedback) GetAllReportCategories(rw http.ResponseWriter, r *http.Request) {
	//Log that the /account/bugreport POST method has been hit
	f.logger.Info("/accounts/reportcategories hit (GET method)")
	f.logger.Debug("Forwarding message to Account service")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		http.Error(rw, "Request failed", http.StatusInternalServerError)
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
 * This function will forward the request to the account service in order to get the bug reports of the user
 */
func (f *Feedback) GetUserBugReports(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/accounts/{id:[0-9]+}/bugreports hit (GET method)")
	f.logger.Debug("Forwarding message to Account Service")
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
 * This function will forward the request to the account service which will update the profile picture of the user
 */
func (f *Feedback) UpdateProfilePicture(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/accounts/picture hit (POST method)")
	f.logger.Debug("Forwarding message to Account service")

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
 * This function will forward the request to the account service which will send the profile picture back to the client
 */
func (f *Feedback) GetProfilePicture(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/accounts/static hit (GET method)")
	f.logger.Debug("Forwarding message to Account service")

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
 * This function will forward the request to the chat service to get all the groups of an user
 */
func (f *Feedback) GetGroups(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/chat/groups/{id:[0-9]+} hit (GET method)")
	f.logger.Debug("Forwarding message to Account service")

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

func (f *Feedback) GetProfile(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/profile/{id:[0-9]+} hit (GET method)")
	f.logger.Debug("Forwarding message to Account service")

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

func (f *Feedback) GetCommonGroups(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/chat/commongroups/{idFirst:[0-9]+}/{idSecond:[0-9]+} hit (GET method)")
	f.logger.Debug("Forwarding the message to Account Service")

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

func (f *Feedback) GetGroupPicture(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("/chat/groups/static/ hit (GET method)")
	f.logger.Debug("Forwarding message to Account Service")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		f.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	f.logger.Info(returnData)

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

func (f *Feedback) UpdateGroupPicture(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /chat/group/updatepicture hit (GET method)")
	f.logger.Debug("Forwarding message to Account Service")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		f.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

func (f *Feedback) UpdateAboutMessage(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /account/update/about hit (POST method)")
	f.logger.Debug("Forwarding request to Account Service")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		f.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

func (f *Feedback) CanSendFriendRequest(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /account/cansend/friendrequest hit (POST method)")
	f.logger.Debug("Forwarding request to Account Service")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		f.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

func (f *Feedback) GetNewerFriends(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /friend/viewnew/{accountId}/{lastId} hit (GET Method)")
	f.logger.Debug("Forwarding request to the Account Service")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		f.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

func (f *Feedback) GetUserStatus(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /status/{userId:[0-9]+} hit (GET method)")
	f.logger.Debug("Forwarding request to Account Service")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		f.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

func (f *Feedback) GetGroupsWithId(rw http.ResponseWriter, r *http.Request) {
	f.logger.Info("Endpoint /chat/groups/{id:[0-9]+}/{lastGroupId:[0-9]+} hit (GET method)")
	f.logger.Debug("Forwarding request to Account Service")

	//Get the name of the service from conf
	url, err := f.configuration.GetServiceURL("accountservice")
	if err != nil {
		f.logger.Error("Cannot get the account service url from config")
		return
	}

	returnData, err := f.ForwardRequest("http", url, r)
	if err != nil {
		f.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}
