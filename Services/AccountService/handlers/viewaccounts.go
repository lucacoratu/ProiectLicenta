package handlers

import (
	"log"
	"net/http"
	"willow/accountservice/data"
)

type ViewAccounts struct {
	l *log.Logger
}

func NewViewAccounts(l *log.Logger) *ViewAccounts {
	return &ViewAccounts{l}
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
