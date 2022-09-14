package handlers

import (
	"net/http"
	"willow/sessionservice/logging"
)

type VerifyJWT struct {
	logger logging.ILogger
}

func NewVerifyJWT(l logging.ILogger) *VerifyJWT {
	return &VerifyJWT{logger: l}
}

func (ver *VerifyJWT) VerifyJWT(rw http.ResponseWriter, r *http.Request) {

}
