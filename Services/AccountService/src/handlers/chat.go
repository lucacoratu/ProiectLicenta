package handlers

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"net/http"
	"willow/accountservice/data"
	"willow/accountservice/database"
	"willow/accountservice/logging"
)

type Chat struct {
	logger logging.ILogger
	dbConn *database.Connection
}

func NewChat(logger logging.ILogger, dbConn *database.Connection) *Chat {
	return &Chat{logger: logger, dbConn: dbConn}
}

/*
 * This function will forward a message to the url: proxyProtocol://proxyHost/uri. When forwarding the headers of the request will remain intact
 * It will return an error if somethign happens during the forwarding of the message or the response body if the request was forwarded succesfully
 */
func (chat *Chat) ForwardRequest(proxyProtocol string, proxyHost string, r *http.Request) ([]byte, error) {
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
	chat.logger.Debug(proxyReq.Header)

	httpClient := &http.Client{}
	resp, err := httpClient.Do(proxyReq)
	if err != nil {
		//http.Error(rw, err.Error(), http.StatusBadGateway)
		return nil, err
	}
	body2, err := ioutil.ReadAll(resp.Body)
	chat.logger.Debug(resp.Status)
	//chat.logger.Debug(string(body2))
	defer resp.Body.Close()
	return body2, nil
}

/*
 * This function will get all the groups for a user
 */
func (ch *Chat) GetGroups(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /chat/groups/{id:[0-9]+} (GET request)")
	//Forward the request to the chat service
	returnData, err := ch.ForwardRequest("http", "localhost:8087", r)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	groups := data.GetGroups{}
	err = json.Unmarshal(returnData, &groups)
	if err != nil {
		ch.logger.Error(err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	//ch.logger.Debug(groups)

	//Complete the data that will be returned to the client
	for index, group := range groups {
		group.ParticipantNames = make([]string, 0)
		ch.logger.Debug(group.Participants)
		for _, participant := range group.Participants {
			accDetails, err := ch.dbConn.GetAccountDetails(participant)
			ch.logger.Debug(accDetails)
			if err != nil {
				http.Error(rw, err.Error(), http.StatusInternalServerError)
				return
			}
			group.ParticipantNames = append(group.ParticipantNames, accDetails.DisplayName)
			if group.LastMessageSender == int64(accDetails.ID) {
				group.LastMessage = accDetails.DisplayName + ": " + group.LastMessage
			}
		}
		groups[index] = group
	}
	ch.logger.Debug(groups)

	rw.WriteHeader(http.StatusOK)
	groups.ToJSON(rw)
}

/*
 * This function will forward the request to the chat service to get the common groups of the user
 */
func (ch *Chat) GetCommonGroups(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /chat/commongroups/{idFirst:[0-9]+}/{idSecond:[0-9]+} hit (GET Method)")

	returnData, err := ch.ForwardRequest("http", "localhost:8087", r)
	if err != nil {
		ch.logger.Error("Error occured in chat service", err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	commonGroups := data.CommonGroups{}
	err = json.Unmarshal(returnData, &commonGroups)
	if err != nil {
		ch.logger.Error("Error occured when parsing the data from chat service", err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	//Complete the participant names
	for index, group := range commonGroups {
		group.ParticipantNames = make([]string, 0)
		//ch.logger.Debug(group.Participants)
		for _, participant := range group.Participants {
			accDetails, err := ch.dbConn.GetAccountDetails(participant)
			//ch.logger.Debug(accDetails)
			if err != nil {
				ch.logger.Error("Error occured when getting the account details of participant", participant, err.Error())
				http.Error(rw, err.Error(), http.StatusInternalServerError)
				return
			}
			group.ParticipantNames = append(group.ParticipantNames, accDetails.DisplayName)
		}
		commonGroups[index] = group
	}
	ch.logger.Debug(commonGroups)

	rw.WriteHeader(http.StatusOK)
	commonGroups.ToJSON(rw)
}

/*
 * This function will forward the update group picture request to chat service
 */
func (ch *Chat) UpdateGroupPicture(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /chat/group/updatepicture hit (POST Method)")

	returnData, err := ch.ForwardRequest("http", "localhost:8087", r)
	if err != nil {
		ch.logger.Error("Error occured in chat service", err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}

/*
 * This function will forward the request to the chat service in order to get the group picture
 */
func (ch *Chat) GetGroupPicture(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /chat/groups/static hit (GET method)")
	ch.logger.Debug("Forwarding data to the chat service")

	ch.logger.Debug(r.RequestURI)

	returnData, err := ch.ForwardRequest("http", "localhost:8087", r)
	if err != nil {
		ch.logger.Error("Error occured in chat service", err.Error())
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write(returnData)
}
