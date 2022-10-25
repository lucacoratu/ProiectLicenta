package handlers

import (
	"net/http"
	"willow/accountservice/database"
	"willow/accountservice/logging"
)

type Friends struct {
	l      logging.ILogger
	dbConn *database.Connection
}

func NewFriends(l logging.ILogger, db *database.Connection) *Friends {
	return &Friends{l: l, dbConn: db}
}

func AddFriend(rw http.ResponseWriter, r *http.Request) {

}
