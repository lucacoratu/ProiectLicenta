package handlers

import (
	"net/http"
	"willow/sessionservice/logging"
)

/*
 * This structure will hold the data neccessary during the creation of
 * a new JWT for a user that logged in
 */
type CreateJWT struct {
	logger logging.ILogger
}

/*
 * This function will create a new CreateJWT structure
 */
func NewCreateJWT(l logging.ILogger) *CreateJWT {
	return &CreateJWT{logger: l}
}

/*
 * This function will create a new JWT token that will be sent back
 */
func (newJWT *CreateJWT) CreateJWT(rw http.ResponseWriter, r *http.Request) {
	
}
