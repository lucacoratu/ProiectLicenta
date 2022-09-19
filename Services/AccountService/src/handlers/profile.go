package handlers

import (
	"log"
	"net/http"
	"willow/accountservice/data"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/jwt"
)

type Profile struct {
	l      *log.Logger
	dbConn *database.Connection
}

func NewProfile(l *log.Logger, db *database.Connection) *Profile {
	return &Profile{l: l, dbConn: db}
}

func (prof *Profile) ViewProfile(rw http.ResponseWriter, r *http.Request) {
	//This function should send the profile of the account that it is connected to back to the client
	prof.l.Println("Endpoint /profile reached")
	jsonErr := jsonerrors.JsonError{Message: "ProfileViewed"}
	rw.WriteHeader(http.StatusOK)
	jsonErr.ToJSON(rw)
}

/*
 * This function will update the status of the account specified in the jwt token
 * It will be called when a PUT request with uri /status comes to the server
 * The new status that the client wants will be specified in the body of the request
 * as a JSON string with field status
 */
func (prof *Profile) UpdateStatus(rw http.ResponseWriter, r *http.Request) {
	prof.l.Println("Endpoint /status PUT reached")

	//Get the JWTClaims where ID, DisplayName and Email can be found
	cookie, err := r.Cookie("session")
	if err != nil {
		jsonErr := jsonerrors.JsonError{Message: "Invalid session"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
	}
	value := cookie.Value
	claims, err := jwt.ExtractClaims(value)
	if err != nil {
		jsonErr := jsonerrors.JsonError{Message: "Invalid session"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
	}

	status := &data.Status{}
	err = status.FromJSON(r.Body)
	if err != nil {
		jsonErr := jsonerrors.JsonError{Message: "Invalid json data"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
	}

	//Update the status
	err = prof.dbConn.UpdateStatus(int(claims.ID), claims.DisplayName, status.NewStatus)
	if err != nil {
		jsonErr := jsonerrors.JsonError{Message: "Status update failed"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
	}
}
