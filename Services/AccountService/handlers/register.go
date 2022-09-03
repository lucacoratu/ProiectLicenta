package handlers

import (
	"log"
	"net/http"
	"willow/accountservice/data"
	"willow/accountservice/errors"

	"github.com/go-playground/validator/v10"
)

/*
 * This structure is the placeholder for all the data
 * that will be used by the register functionality of the account
 * service
 */
type Register struct {
	l *log.Logger
}

/*
 * The NewRegister function will create a new object of the Register struct
 * Factory method for creating an object of Register easily
 */
func NewRegister(l *log.Logger) *Register {
	return &Register{l}
}

/*
 * Function that will register a new account (internal function of the package)
 * This function will receive the data in the body as a parameter
 * Checks will be done on the input from the user
 * If there are no errors in the data received from the user then the account will be registered and status created will be sent to user
 * If the data received has errors then an error will be sent to the user specifing the problem with the data
 */
func (register *Register) registerAccount(rw http.ResponseWriter, r *http.Request) {
	//Log that the endpoint has been reached by a user
	register.l.Println("Endpoint /register reached (POST method)")

	//Get the data from the request body (POST request)
	data := &data.RegisterAccount{}

	//Decode the json data from the user
	err := data.FromJSON(r.Body)

	//If an error occured during the json decode then send error to user
	if err != nil {
		//Log the error and send it back to the user
		register.l.Println("JSON decode error: ", err)

		//Send the error back to the user
		rw.WriteHeader(http.StatusBadRequest)
		jsonError := &errors.JsonError{Message: "invalid json format"}
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
			jsonError := &errors.JsonError{Message: e.Field() + " " + e.Tag()}
			jsonError.ToJSON(rw)
			break
		}

		//Log all the errors occured
		register.l.Println(err)
		return
	}

	//User input passed the checks so the account can be registered
	rw.WriteHeader(http.StatusCreated)
	rw.Write([]byte("User created!"))
}

/*
 * The ServeHTTP function will be a method of the Register struct
 * The idea with this method is that Register struct will be "inherited"
 * from the Http.Handler interface so now this Request struct can be used
 * as a handler for the http server (making a custom handler that holds data)
 * so the functionality of the handler can be extended
 */
func (register *Register) ServeHTTP(rw http.ResponseWriter, r *http.Request) {
	//Here will be the code for what the server will do if the path is /register

	//Check if the method is post, else send back to the user that the method is not allowed
	if r.Method == http.MethodPost {
		//Register a new account with the data specified by the user in the body of the request
		register.registerAccount(rw, r)

		//Return because the request has been handled
		return
	}

	//If the method is not POST the return method not allowed
	rw.WriteHeader(http.StatusMethodNotAllowed)
}
