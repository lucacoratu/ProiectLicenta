package handlers

import (
	"net/http"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/jwt"
	"willow/accountservice/logging"
)

/*
 * This structure will hold the data necessary for the session validation
 * It will have a logger and the handle for the database connection
 */
type Authentication struct {
	l      logging.ILogger
	dbConn *database.Connection
}

/*
 * This function will create a new object of Authentication structure
 * The parameters that it needs are logger and database.Connection
 * Returns a new Authentication structure pointer
 */
func NewAuthentication(l logging.ILogger, db *database.Connection) *Authentication {
	return &Authentication{l: l, dbConn: db}
}

/*
 * This function will validate the session cookie
 * First it need to extract the cookie with the name session then the value of that cookie
 * Then the value should be validated (signature from the jwt is valid with the payload provided)
 * After that the account specified in the payload section should be checked if it exists
 * If all the requirements are met then call the next function as this will be used as a middleware for code reusability
 * An error response (JSON) will be sent back to the client if the session validation fails
 */
func (auth *Authentication) ValidateSessionCookie(next http.Handler) http.Handler {
	return http.HandlerFunc(func(rw http.ResponseWriter, r *http.Request) {
		//Validate the JWT token which is in the cookie with the name session
		//Extract the cookie with the name session
		auth.l.Info("Validating jwt token")
		cookie, err := r.Cookie("session")
		//Check if the session cookie exists
		if err != nil {
			//The session cookie does not exist so send an error message back to the client
			auth.l.Error(err.Error())
			jsonErr := jsonerrors.JsonError{Message: "Invalid session"}
			rw.WriteHeader(http.StatusBadRequest)
			jsonErr.ToJSON(rw)
			return
		}
		//Extract the cookie value of the session cookie
		jwtTokenString := cookie.Value
		//Check if the jwt is valid (signature is correct over the payload specified)
		claim, err := jwt.ValidateJWT(jwtTokenString)
		if err != nil {
			//The jwt is not valid so send an error message back to the client
			auth.l.Error(err.Error())
			jsonErr := jsonerrors.JsonError{Message: "Invalid session"}
			rw.WriteHeader(http.StatusBadRequest)
			jsonErr.ToJSON(rw)
			return
		}

		//Check if the payload is correct (ID, DisplayName and Password can be found in the database for an account)
		err = auth.dbConn.ValidateJWTPayload(int(claim.ID), claim.DisplayName)
		if err != nil {
			//The account does not exist in the database so the authentication failed
			auth.l.Error(err.Error())
			jsonErr := jsonerrors.JsonError{Message: "Invalid session"}
			rw.WriteHeader(http.StatusBadRequest)
			jsonErr.ToJSON(rw)
			return
		}

		//The session cookie is valid and the account exists so call the next function
		//ctx := context.WithValue(r.Context(), )
		next.ServeHTTP(rw, r)
	})
}
