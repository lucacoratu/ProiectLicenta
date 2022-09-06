package handlers

import (
	"log"
	"net/http"
)

/*
 * This structure is the placeholder for all the data
 * that will be used by the login functionality of the account
 * service
 */
type Login struct {
	l *log.Logger
}

/*
 * The NewLogin function will create a new object of the Register struct
 * Factory method for creating an object of Login easily
 */
func NewLogin(l *log.Logger) *Login {
	return &Login{l}
}

func (login *Login) LoginAccount(rw http.ResponseWriter, r *http.Request) {
}

/*
 * The ServeHTTP function will be a method of the Register struct
 * The idea with this method is that Register struct will be "inherited"
 * from the Http.Handler interface so now this Request struct can be used
 * as a handler for the http server (making a custom handler that holds data)
 * so the functionality of the handler can be extended
 */
func (login *Login) ServeHTTP(rw http.ResponseWriter, r *http.Request) {
	//The code for the login handler will be here
}
