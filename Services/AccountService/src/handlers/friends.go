package handlers

import (
	"log"
	"net/http"
	"willow/accountservice/database"
)

type Friends struct {
	l      *log.Logger
	dbConn *database.Connection
}

func NewFriends(l *log.Logger, db *database.Connection) *Friends {
	return &Friends{l: l, dbConn: db}
}

func AddFriend(rw http.ResponseWriter, r *http.Request) {

}
