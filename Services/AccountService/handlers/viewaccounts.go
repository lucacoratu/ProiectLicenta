package handlers

import (
	"log"
	"net/http"
	"strconv"
	"willow/accountservice/data"
	"willow/accountservice/errors"

	"github.com/gorilla/mux"
)

type ViewAccounts struct {
	l *log.Logger
}

func NewViewAccounts(l *log.Logger) *ViewAccounts {
	return &ViewAccounts{l}
}

func (viewAccounts *ViewAccounts) GetAccounts(rw http.ResponseWriter, r *http.Request) {
	if r.Method == http.MethodGet {
		accounts := data.GetAccounts()
		err := accounts.ToJSON(rw)
		if err != nil {
			http.Error(rw, err.Error(), http.StatusInternalServerError)
		}
		return
	}

	rw.WriteHeader(http.StatusMethodNotAllowed)
}

func (viewAccounts *ViewAccounts) GetAccount(rw http.ResponseWriter, r *http.Request) {
	if r.Method == http.MethodGet {
		vars := mux.Vars(r)
		id, err := strconv.Atoi(vars["id"])
		if err != nil {
			http.Error(rw, err.Error(), http.StatusInternalServerError)
			return
		}

		account := data.GetAccount(id)
		if account == nil {
			jsonError := &errors.JsonError{Message: "Invalid id"}
			rw.WriteHeader(http.StatusInternalServerError)
			jsonError.ToJSON(rw)
			return
		}

		err = account.ToJSON(rw)
		if err != nil {
			http.Error(rw, err.Error(), http.StatusInternalServerError)
		}
		return
	}

	rw.WriteHeader(http.StatusMethodNotAllowed)
}

func (viewAccounts *ViewAccounts) ServeHTTP(rw http.ResponseWriter, r *http.Request) {
	if r.Method == http.MethodGet {
		accounts := data.GetAccounts()
		err := accounts.ToJSON(rw)
		if err != nil {
			http.Error(rw, err.Error(), http.StatusInternalServerError)
		}
		return
	}

	rw.WriteHeader(http.StatusMethodNotAllowed)
}
