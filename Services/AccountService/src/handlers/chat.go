package handlers

import (
	"net/http"
	"willow/accountservice/database"
	"willow/accountservice/logging"
)

type Chat struct {
	logger logging.ILogger
	dbConn *database.Connection
}

func NewChat(logger logging.ILogger, dbConn *database.Connection) *Chat {
	return &Chat{logger: logger, dbConn: dbConn}
}

func (ch *Chat) GetGroups(rw http.ResponseWriter, r *http.Request) {
	ch.logger.Info("Endpoint /chat/groups/{id:[0-9]+} (GET request)")

}
