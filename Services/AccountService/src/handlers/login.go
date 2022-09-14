package handlers

import (
	"crypto/sha256"
	"encoding/hex"
	"log"
	"net/http"
	"willow/accountservice/data"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/jwt"

	"github.com/go-playground/validator/v10"
)

/*
 * This structure is the placeholder for all the data
 * that will be used by the login functionality of the account
 * service
 */
type Login struct {
	l      *log.Logger
	dbConn *database.Connection
}

/*
 * The NewLogin function will create a new object of the Register struct
 * Factory method for creating an object of Login easily
 */
func NewLogin(l *log.Logger, dbConn *database.Connection) *Login {
	return &Login{l: l, dbConn: dbConn}
}

/*
 * This function will retrieve the login credentials from the HTTP POST request body and
 * will check if the credentials are corrent. If the credentials are correct then
 */
func (login *Login) LoginAccount(rw http.ResponseWriter, r *http.Request) {
	login.l.Println("Endpoint /login reached (POST method)")
	data := &data.LoginAccount{}
	//Convert the data in the request body from json to the LoginAcount structure
	err := data.FromJSON(r.Body)
	//Check if an error occured during decoding
	if err != nil {
		//An error occured so return a json error to the client
		jsonErr := jsonerrors.JsonError{Message: "Invalid json format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}

	//Validate the fields in the structure
	err = data.Validate()
	//Check if there are any field erorrs after validation
	if err != nil {
		//Return only the first error to the client in a pretty format
		for _, e := range err.(validator.ValidationErrors) {
			jsonErr := jsonerrors.JsonError{Message: e.Field() + " Invalid format"}
			rw.WriteHeader(http.StatusBadRequest)
			jsonErr.ToJSON(rw)
			return
		}
	}

	//Get the salt of the username
	saltHex, err := login.dbConn.GetSalt(data.Username)
	if err != nil {
		//The login failed because the salt could not bet retrieved
		jsonErr := jsonerrors.JsonError{Message: "Invalid credentials"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}
	//Salt was retrieved so compute the hash of the password received from the user
	//Decode the hash from hex
	salt, err := hex.DecodeString(saltHex)
	if err != nil {
		login.l.Println("Error occured during decoding the salt from hex")
		jsonErr := jsonerrors.JsonError{Message: "Invalid credentials"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
	}

	//Concatenate the salt to the password
	hashdata := []byte(data.Password)
	hashdata = append(hashdata, salt...)

	//Compute the sha2-256 hash of the password concatenated with the salt
	hash := sha256.New()
	_, err = hash.Write(hashdata)
	if err != nil {
		login.l.Println("Cannot compute the hash for the password", err.Error())
		jsonErr := jsonerrors.JsonError{Message: "Invalid credentials"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}
	//Compute the hash of the concatenated data
	hashPassword := hash.Sum(nil)

	//Convert the hash into hexstring
	hashHex := hex.EncodeToString(hashPassword)
	acc, err := login.dbConn.LoginIntoAccount(data.Username, hashHex)
	//Check if the login failed (error is not nil)
	if err != nil {
		login.l.Println("Login failed for username ", data.Username)
		jsonErr := jsonerrors.JsonError{Message: "Invalid credentials"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}

	//Generate the JWT token for the session of the client
	token, err := jwt.GenerateJWT(uint64(acc.ID), acc.DisplayName, acc.Email)
	if err != nil {
		login.l.Println("JWT Token generation failed!")
		jsonErr := jsonerrors.JsonError{Message: "Something happened"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonErr.ToJSON(rw)
		return
	}

	login.l.Println("Token:", token)

	//The login succeded so return the account data to the client
	login.l.Println("Login succeded, username", acc.Username)
	rw.Header().Add("Set-Cookie", "session="+token)
	rw.WriteHeader(http.StatusOK)
	acc.ToJSON(rw)
}
