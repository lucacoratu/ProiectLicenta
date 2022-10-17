package handlers

import (
	"willow/chatservice/logging"
)

/*
 * This structure will have the handler functions for all the chat endpoints
 * It contains a connection to the database structure.
 */
type Chat struct {
	dbConn database.IConnection
	logger logging.ILogger
}

/*
 * This function will create a new Chat structure given the logger parameter and the dbConn parameter
 */
func NewChat(dbConn database.IConnection, logger logging.ILogger) *Chat{
	return &Chat{dbConn: dbConn, logger: logger}
}

func (ch *Chat) GetRooms(rw http.ResponseWriter, r http.Request) {

} 