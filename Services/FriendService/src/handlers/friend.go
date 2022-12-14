package handlers

import (
	"net/http"
	"strconv"
	"strings"
	"willow/friendservice/data"
	"willow/friendservice/database"
	jsonerrors "willow/friendservice/errors"
	"willow/friendservice/logging"

	"encoding/json"

	"github.com/gorilla/mux"
)

type Friend struct {
	logger logging.ILogger
	dbConn *database.Connection
}

func NewFriend(logger logging.ILogger, dbConn *database.Connection) *Friend {
	return &Friend{logger: logger, dbConn: dbConn}
}

/*
 * This function will add a friendship between 2 accounts if there already exists a friend request between these accounts. To determine if the 2 accounts share a friend request
 * A call to the friendrequest service has to be made which will return either true or false. If the friendrequest service return true, which means there is a friend request between
 * those accounts, the friendship will be added in the database, then the friend request will be deleted from the friendrequest service
 */
func (fr *Friend) AddFriend(rw http.ResponseWriter, r *http.Request) {
	//Get the accountID and the friendID from the request body
	addFriendData := &data.AddFriend{}
	err := addFriendData.FromJSON(r.Body)
	//Check if an error occured when reading the json data from the body
	if err != nil {
		//An error occured when parsing the json in the request body
		jsonError := jsonerrors.JsonError{Message: "Invalid json format"}
		jsonError.ToJSON(rw)
		rw.WriteHeader(http.StatusInternalServerError)
		return
	}
	//Check if the account has a friend request from the other account
	//Send a POST request to the friendrequest service
	resp, err := http.Post("http://localhost:8083/arefriends", "application/json", r.Body)
	//Check if an error occured while sending the request to the friendrequest service
	if err != nil {
		//An error occured so send an error message back to the client
		fr.logger.Error("Could not send the request to friendrequest service", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Error occured, cannot forward the message"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Check if an error occured in the friendrequest service
	if resp.StatusCode == http.StatusInternalServerError {
		//An error occured in the friendrequest service
		jsonError := jsonerrors.JsonError{}
		err = jsonError.FromJSON(resp.Body)
		if err != nil {
			//An error occured while reading the data from the response body from the friendrequest service
			fr.logger.Error("Could not read data from the response body from friendrequest service", err.Error())
			jsonError := jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
			rw.WriteHeader(http.StatusInternalServerError)
			jsonError.ToJSON(rw)
			return
		}
		fr.logger.Warning("Error occured in the friendrequest service", jsonError.Message)
		jsonError = jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Check if there is a friend request between these 2 accounts (answer from friendrequest service)
	data := data.AreFriends{}
	err = data.FromJSON(resp.Body)
	//Check if there was an error when parsing the response from the friendrequest service
	if err != nil {
		//An error occured when parsing the JSON response from the friendrequest service
		fr.logger.Error("Could not parse response data from friendrequest service", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Check if the response from the friendrequest service is true
	if !data.Message {
		//The accounts do not have a friendrequest between them
		fr.logger.Warning("Could not add the friendship because there is not friendrequest between the accounts", addFriendData.AccountID, addFriendData.FriendID)
		jsonError := jsonerrors.JsonError{Message: "Cannot add a friend without a friend request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Add the friend in the database
	err = fr.dbConn.AddFriend(addFriendData.AccountID, addFriendData.FriendID)
	//Check if an error occured while inserting the friendship in the database
	if err != nil {
		fr.logger.Error("Could not insert the friendship in the database", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Erase the friend request from the friend requests service database
	//Send a request to the friendrequest service to delete the friendrequest between these 2 accounts
	resp, err = http.Post("http://localhost:8083/delete", "application/json", r.Body)
	//Check if an error occured when sending the data to the friendrequest service
	if err != nil {
		//An error occured
		//Log the error
		fr.logger.Error("Could not send the request to friendrequest service", err.Error())
		jsonError := jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Check if the request was succesful
	if resp.StatusCode == http.StatusInternalServerError {
		//Extract the data from the response body
		jsonError := jsonerrors.JsonError{}
		err = jsonError.FromJSON(resp.Body)
		if err != nil {
			//An error occured while reading the data from the response body from the friendrequest service
			fr.logger.Error("Could not read data from the response body from friendrequest service", err.Error())
			jsonError := jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
			rw.WriteHeader(http.StatusInternalServerError)
			jsonError.ToJSON(rw)
			return
		}
		fr.logger.Warning("Error occured in the friendrequest service", jsonError.Message)
		jsonError = jsonerrors.JsonError{Message: "Error occured, cannot handle the request"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
	}
	//Everything was allright
	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Friend added"))
}

/*
 * This function will delete a friendship from the database and will notify the client that the friendship has been deleted
 *
 */
func (fr *Friend) DeleteFriend(rw http.ResponseWriter, r *http.Request) {

}

/*
 * This function will send back to the client all the friends that he has. It will get the friendships from the database and return them in json format
 * If an error occurs during the parsing of the json body of the request, or while interacting with the database then the request could not be completed
 * so send an error message back to the client
 */
func (fr *Friend) GetFriends(rw http.ResponseWriter, r *http.Request) {
	fr.logger.Info("Endpoint /friend/view/{id} hit (GET Method)")
	//Get the id from the mux vars
	vars := mux.Vars(r)
	//Convert the id string into a number
	id, err := strconv.Atoi(vars["id"])
	fr.logger.Info(id)
	//Check if an error occured during the conversion from string to int
	if err != nil {
		jsonError := jsonerrors.JsonError{Message: "Could not convert id to int"}
		jsonError.ToJSON(rw)
		rw.WriteHeader(http.StatusInternalServerError)
		return
	}
	//Get the friends of the account from the database
	friends, err := fr.dbConn.ViewFriendships(id)
	//Check if an error occured when trying to fetch the friendships from the database
	if err != nil {
		//An error occured when trying to get the friendships from the database
		jsonError := jsonerrors.JsonError{Message: "Could not get the friends"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError.ToJSON(rw)
		return
	}
	//Get the last message from every friend
	friendsMessages := make(data.FriendsAndLastMessages, 0)
	for _, friend := range friends {
		//Create the structure for the request
		reqBody := data.GetRoomId{AccountID: int64(friend.AccountID), FriendID: int64(friend.FriendID)}
		strData, _ := json.Marshal(reqBody)
		fr.logger.Debug(strData)
		reqBodyReader := strings.NewReader(string(strData))
		//Get the room id of the private conversation of this user with the friend
		resp, _ := http.Post("http://localhost:8087/privateroom", "application/json", reqBodyReader)
		//Check for errors
		roomIdResp := data.RoomIdResponse{}
		_ = roomIdResp.FromJSON(resp.Body)
		//Get the last message of the private conversation from the ChatService
		resp, _ = http.Get("http://localhost:8087/history/" + strconv.FormatInt(roomIdResp.RoomID, 10) + "/lastmessage")
		lastMessage := data.LastMessage{}
		_ = lastMessage.FromJSON(resp.Body)
		fr.logger.Debug("Last message = ", lastMessage)
		if string(lastMessage.MessageText) != "" {
			friendsMessages = append(friendsMessages, data.FriendAndLastMessage{AccountID: friend.AccountID, FriendID: friend.FriendID, RoomID: roomIdResp.RoomID, BefriendDate: friend.BefriendDate, LastMessage: string(lastMessage.MessageText), LastMessageTimestamp: lastMessage.MessageTimestamp})
		} else {
			friendsMessages = append(friendsMessages, data.FriendAndLastMessage{AccountID: friend.AccountID, FriendID: friend.FriendID, RoomID: roomIdResp.RoomID, BefriendDate: friend.BefriendDate, LastMessage: "Start conversation", LastMessageTimestamp: ""})
		}
	}
	fr.logger.Info(friendsMessages)

	//Send the friends back to the client
	rw.WriteHeader(http.StatusOK)
	friendsMessages.ToJSON(rw)
}
