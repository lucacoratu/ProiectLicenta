package handlers

import (
	"log"
	"net/http"
)

type AccountLogin struct {
	l *log.Logger
}

func NewAccountLogin(l *log.Logger) *AccountLogin {
	return &AccountLogin{l}
}

func (accLogin *AccountLogin) ServeHTTP(rw http.ResponseWriter, r *http.Request) {
	//This function will hold the code for forwarding the request to AccountService
}
