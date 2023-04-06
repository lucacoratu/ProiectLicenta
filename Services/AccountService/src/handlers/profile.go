package handlers

import (
	"io"
	"net/http"
	"os"
	"strconv"
	"willow/accountservice/data"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/logging"

	"github.com/gorilla/mux"
)

type Profile struct {
	l      logging.ILogger
	dbConn *database.Connection
}

func NewProfile(l logging.ILogger, db *database.Connection) *Profile {
	return &Profile{l: l, dbConn: db}
}

func (prof *Profile) ViewProfile(rw http.ResponseWriter, r *http.Request) {
	//This function should send the profile of the account that it is connected to back to the client
	prof.l.Info("Endpoint /profile/{id:[0-9]+} reached")
	vars := mux.Vars(r)
	prof.l.Debug("Id received is", vars["id"])
	idReceiver, err := strconv.Atoi(vars["id"])
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	idRecv := int64(idReceiver)
	accDetails, err := prof.dbConn.GetAccountDetails(idRecv)
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	rw.WriteHeader(http.StatusOK)
	accDetails.ToJSON(rw)
}

/*
 * This function will update the status of the account specified in the jwt token
 * It will be called when a PUT request with uri /status comes to the server
 * The new status that the client wants will be specified in the body of the request
 * as a JSON string with field status
 */
// func (prof *Profile) UpdateStatus(rw http.ResponseWriter, r *http.Request) {
// 	prof.l.Info("Endpoint /status PUT reached")

// 	//Get the JWTClaims where ID, DisplayName and Email can be found
// 	cookie, err := r.Cookie("session")
// 	if err != nil {
// 		jsonErr := jsonerrors.JsonError{Message: "Invalid session"}
// 		rw.WriteHeader(http.StatusBadRequest)
// 		jsonErr.ToJSON(rw)
// 	}
// 	value := cookie.Value
// 	claims, err := jwt.ExtractClaims(value)
// 	if err != nil {
// 		jsonErr := jsonerrors.JsonError{Message: "Invalid session"}
// 		rw.WriteHeader(http.StatusBadRequest)
// 		jsonErr.ToJSON(rw)
// 	}

// 	status := &data.Status{}
// 	err = status.FromJSON(r.Body)
// 	if err != nil {
// 		jsonErr := jsonerrors.JsonError{Message: "Invalid json data"}
// 		rw.WriteHeader(http.StatusBadRequest)
// 		jsonErr.ToJSON(rw)
// 	}

// 	//Update the status
// 	err = prof.dbConn.UpdateStatus(int(claims.ID), claims.DisplayName, status.NewStatus)
// 	if err != nil {
// 		jsonErr := jsonerrors.JsonError{Message: "Status update failed"}
// 		rw.WriteHeader(http.StatusBadRequest)
// 		jsonErr.ToJSON(rw)
// 	}
// }

/*
 * This function will update the status of the account specified in the body of the request
 * It will not be used after adding authentication on the websocket in the chat service
 */
func (prof *Profile) UpdateStatusUnauthenticated(rw http.ResponseWriter, r *http.Request) {
	prof.l.Info("Endpoint /status PUT reached")

	newStatus := &data.StatusUnauthenticated{}
	err := newStatus.FromJSON(r.Body)
	if err != nil {
		prof.l.Error("Invalid json data in /status", err.Error())
		jsonErr := jsonerrors.JsonError{Message: "Invalid json data"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
	}

	//Update the status
	err = prof.dbConn.UpdateStatus(int(newStatus.AccountID), newStatus.NewStatus)
	if err != nil {
		jsonErr := jsonerrors.JsonError{Message: "Status update failed"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
	}
}

func (prof *Profile) GetUserStatus(rw http.ResponseWriter, r *http.Request) {
	prof.l.Info("Endpoint /status/{userId:[0-9]+} hit (GET method)")
	//Get the user id from the vars
	vars := mux.Vars(r)
	userIdReq := vars["userId"]
	userId, err := strconv.Atoi(userIdReq)
	if err != nil {
		prof.l.Error("Error occured when parsing the user id", err.Error())
		jsonErr := jsonerrors.JsonError{Message: "Internal server error"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonErr.ToJSON(rw)
	}
	status, err := prof.dbConn.GetAccountStatus(int64(userId))
	if err != nil {
		jsonErr := jsonerrors.JsonError{Message: "Internal server error"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonErr.ToJSON(rw)
	}

	accountStatus := data.Status{}
	accountStatus.NewStatus = status
	rw.WriteHeader(http.StatusOK)
	accountStatus.ToJSON(rw)
}

/*
 * This function will update the profile picture of an account
 */
func (prof *Profile) UpdateProfilePicture(rw http.ResponseWriter, r *http.Request) {
	prof.l.Info("Endpoint /picture reached (POST method)")

	//Parse the multipart request (maximum 20MB upload)
	r.ParseMultipartForm(10 << 20)

	accountId := r.FormValue("accountId")
	if accountId == "" {
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Account id has to be specified in the form"))
		return
	}

	file, _, err := r.FormFile("profile")
	//Check if an error occured
	if err != nil {
		prof.l.Info("Cannot get the file or the handler")
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Failed!"))
		return
	}
	//Close the file when the function ends
	defer file.Close()

	//Create an empty file on the disk
	dst, err := os.Create("./static/" + accountId + ".png")
	//Check for errors
	if err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}
	defer dst.Close()

	// Copy the uploaded file to the created file on the filesystem
	if _, err := io.Copy(dst, file); err != nil {
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	accId, err := strconv.ParseInt(accountId, 10, 64)
	if err != nil {
		//Remove the file created
		http.Error(rw, err.Error(), http.StatusInternalServerError)
		return
	}

	//Save the url of the image in the database
	err = prof.dbConn.UpdateProfilePictureURL(accId, accountId+".png")
	if err != nil {
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Cannot update the profile picture"))
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("File upload succesful"))
}

/*
 * This function will update the account about message
 */
func (prof *Profile) UpdateAboutMessage(rw http.ResponseWriter, r *http.Request) {
	//Log that the endpoint has beem hit
	prof.l.Info("Endpoint /account/update/about hit (GET Method)")
	//Parse the request body
	updateAbout := data.UpdateAbout{}
	err := updateAbout.FromJSON(r.Body)
	//Check if an error occured when parsing the update about json body
	if err != nil {
		prof.l.Error("Error occured when parsing the update about json body", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Internal server error"))
		return
	}
	//Update the about in the database
	res, err := prof.dbConn.UpdateAbout(updateAbout.AccountId, updateAbout.NewAbout)
	if !res {
		prof.l.Error("Error occured when updating the about in the database", err.Error())
		rw.WriteHeader(http.StatusInternalServerError)
		rw.Write([]byte("Internal server error"))
		return
	}

	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("About updated"))
}
