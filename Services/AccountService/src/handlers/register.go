package handlers

import (
	"crypto/rand"
	"crypto/sha256"
	"encoding/hex"
	"errors"
	"net/http"
	"willow/accountservice/data"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/logging"

	"github.com/go-playground/validator/v10"
)

/*
 * This structure is the placeholder for all the data
 * that will be used by the register functionality of the account
 * service
 */
type Register struct {
	l      logging.ILogger
	dbConn *database.Connection
}

/*
 * The NewRegister function will create a new object of the Register struct
 * Factory method for creating an object of Register easily
 */
func NewRegister(l logging.ILogger, dbConn *database.Connection) *Register {
	return &Register{l: l, dbConn: dbConn}
}

/*
 * This function will insert an account into the database, it will also create the password salt and the password hash
 * and will add them to the account structure that will be inserted. (salt will be 6 random bytes and hash-algorithm will be sha2-256)
 * If the query to the database server fails then an error will be returned
 * If the query succeds then nil will be returned
 */
func (register *Register) insertUser(regAcc *data.RegisterAccount) error {
	//Create an object of data.Account with the fields that are required in the register
	//The object has the Username, DisplayName, Email and Password set from the login

	//Generate the salt for the password
	salt := make([]byte, 6)
	count, err := rand.Read(salt)
	if count != 6 || err != nil {
		//The salt generation failed
		register.l.Error("Salt generation failed")
		return err
	}

	//Concatenate the salt to the password
	hashdata := []byte(regAcc.Password)
	hashdata = append(hashdata, salt...)

	//Compute the sha2-256 hash of the password concatenated with the salt
	hash := sha256.New()
	_, err = hash.Write(hashdata)
	if err != nil {
		register.l.Error("Cannot compute the hash for the password", err.Error())
		return err
	}
	//Compute the hash of the concatenated data
	hashResult := hash.Sum(nil)

	//Convert the hash into hexstring
	hashHex := hex.EncodeToString(hashResult)
	//register.l.Print(hashHex)

	//Convert the salt into hex
	saltHex := hex.EncodeToString(salt)
	register.l.Info(saltHex)

	//Create the object of the account structure that will hold the information abount the new account to be registered
	newAcc := &data.Account{Username: regAcc.Username, DisplayName: regAcc.DisplayName, Email: regAcc.Email, PasswordHash: hashHex, Salt: saltHex, IdentityPublicKey: regAcc.IdentityPublicKey, PreSignedPublicKey: regAcc.PreSignedPublicKey}
	//register.l.Print(newAcc)

	//Create specific errors to send back to the client
	err = register.dbConn.InsertAccount(newAcc)

	//If there was an internal error when interacting with the database server
	if err != nil && (err.Error() != "username already exists" && err.Error() != "email already exists") {
		register.l.Error("Error occured during interaction with the database server")
		return errors.New("account registration failed")
	}

	//The username or the email already exists in the database so the account cannot be registered
	if err != nil {
		register.l.Error("Cannot insert account,", err.Error())
		if err != nil {
			return err
		}
	}

	return nil
}

/*
 * This function will validate that the data received in the request body is a valid json
 * If the data is valid then nil will be returned
 * Else an error will be sent to the client and an error will be returned from the function
 */
func (register *Register) validateRequestData(rw http.ResponseWriter, r *http.Request) error {
	//
	return nil
}

/*
 * Function that will register a new account (internal function of the package)
 * This function will receive the data in the body as a parameter
 * Checks will be done on the input from the user
 * If there are no errors in the data received from the user then the account will be registered and status created will be sent to user
 * If the data received has errors then an error will be sent to the user specifing the problem with the data
 */
func (register *Register) RegisterAccount(rw http.ResponseWriter, r *http.Request) {
	//Log that the endpoint has been reached by a user
	register.l.Info("Endpoint /register reached (POST method)")

	//Get the data from the request body (POST request)
	data := &data.RegisterAccount{}

	//Decode the json data from the user
	err := data.FromJSON(r.Body)

	//If an error occured during the json decode then send error to user
	if err != nil {
		//Log the error and send it back to the user
		register.l.Error("JSON decode error: ", err)

		//Send the error back to the user
		rw.WriteHeader(http.StatusBadRequest)
		jsonError := &jsonerrors.JsonError{Message: "invalid json format"}
		jsonError.ToJSON(rw)
		return
	}

	//Validate the data from the user (using the validator package)
	err = data.Validate()

	//Check if the user input didn't raise any errors
	if err != nil {
		//The input has errors so send the error back to the user

		//Send only the first error occured back to the user (TO DO...send a custom html for the error)
		for _, e := range err.(validator.ValidationErrors) {

			rw.WriteHeader(http.StatusBadRequest)
			jsonError := &jsonerrors.JsonError{Message: e.Field() + " invalid format"}
			jsonError.ToJSON(rw)
			break
		}
		//Log the error(s)
		register.l.Error(err.Error())
		return
	}

	register.l.Info("Inserting the account into the database")
	//User input passed the checks so the account can be registered
	err = register.insertUser(data)
	if err != nil {
		rw.WriteHeader(http.StatusInternalServerError)
		jsonError := &jsonerrors.JsonError{Message: err.Error()}
		jsonError.ToJSON(rw)
		return
	}
	register.l.Info("Account has been added, username = " + data.Username)

	//Send the success message back to the client
	rw.WriteHeader(http.StatusOK)
	rw.Write([]byte("Account created!"))
}
