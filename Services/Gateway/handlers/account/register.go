package handlers

import (
	"log"
	"net/http"
)

type AccountRegister struct {
	l *log.Logger
}

func NewAccountRegister(l *log.Logger) *AccountRegister {
	return &AccountRegister{l}
}

func (accRegister *AccountRegister) ServeHTTP(rw http.ResponseWriter, r *http.Request) {
	//This function will hold the code for forwarding the request to AccountService
}
