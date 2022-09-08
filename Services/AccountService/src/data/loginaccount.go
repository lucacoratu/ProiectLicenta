package data

import (
	"encoding/json"
	"io"
	"regexp"

	"github.com/go-playground/validator/v10"
)

/*
 * This structure will hold the data that comes from the user when
 * it wants to login into an account (the username and the password)
 */
type LoginAccount struct {
	Username string `json:"username" validate:"required"` //TO DO...Add username validation
	Password string `json:"password" validate:"required"` //TO DO...Add password validation
}

/*
 * This function will convert the LoginAccount structure to json
 * If the encoding fails then it will return an error
 * Else it will return nil
 */
func (logAcc *LoginAccount) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(logAcc)
}

/*
 * This function will convert an json string into an LoginAcount object
 * If the decoding fails then an error is returned
 * Else it will return nil
 */
func (logAcc *LoginAccount) FromJSON(r io.Reader) error {
	e := json.NewDecoder(r)
	return e.Decode(logAcc)
}

/*
 * This function will validate that the username has the right format
 * The right format for the username is: minimum 6 characters long, maximum 20 characters long
 * The username must start with a letter, it can contain numbers and _
 * The username cannot contain special characters
 */
func validateLoginUsername(fl validator.FieldLevel) bool {
	//Check if the username has the right length
	username := fl.Field().String()
	if len(username) < 6 || len(username) > 20 {
		return false
	}

	//Build the regex that will check the format
	r := regexp.MustCompile("^([a-zA-Z]+)([0-9|_|a-z|A-Z]*)$")

	//Find all the matches of the username regex
	matches := r.FindAllString(username, -1)

	//If the matches is nil then return false
	if matches == nil {
		return false
	}

	//If there is more than 1 match then return false (only one username can be passed)
	if len(matches) != 1 {
		return false
	}

	//There is only one match in the username string received
	return true
}

/*
 * This function will validate that the password has the right format
 * The password should be at least 8 characters long and maximum 20
 * The password should start with an uppercase letter and should contain number
 * The password can contain the following special characters: _, !, $, %, ^
 */
func validateLoginPassword(fl validator.FieldLevel) bool {
	//Check if the password has the right length
	password := fl.Field().String()
	if len(password) < 8 || len(password) > 20 {
		return false
	}

	//Create the regex that will test if the password has the right format
	r := regexp.MustCompile("^([A-Z]+)([a-zA-Z0-9_!%$^]*)$")

	//Check if the regex matches any substring in the password string
	matches := r.FindAllString(password, -1)

	//If matches is nil then return false
	if matches == nil {
		return false
	}

	//If there is more than one match then return false
	if len(matches) != 1 {
		return false
	}

	//Else return true
	return true
}

/*
 * This function will validate the data in the structure based on the requirements
 * specified in the struct tags
 */
func (logAcc *LoginAccount) Validate() error {
	validate := validator.New()
	validate.RegisterValidation("username", validateLoginUsername)
	validate.RegisterValidation("password", validateLoginPassword)
	return validate.Struct(logAcc)
}
