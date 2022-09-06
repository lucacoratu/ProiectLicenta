package data

import (
	"encoding/json"
	"io"
	"regexp"

	"github.com/go-playground/validator/v10"
)

/*
 * This data structure will be used when a user wants to register an account
 * It will have less data than the structure used for sending the account details back to the user
 * It has less data because some fields are not required during registration (Id, Password, Salt, LastOnline, Status, JoinDate)
 */
type RegisterAccount struct {
	Username    string `json:"username" validate:"required,username"`       //Account username (TO DO...Set a minimum and a maximum length)
	Password    string `json:"password" validate:"required,password"`       //Account password (TO DO... Set a minimum and a maximum length (maybe casing and numbers))
	DisplayName string `json:"displayName" validate:"required,displayname"` //Account name that will be available publicly (TO DO... Set a minimum and a maximum length)
	Email       string `json:"email" validate:"required,email"`             //Account email
}

/*
 * This function converts the RegisterAccount struct to json with the names of the
 * fields specified by the struct tags in the struct declaration
 * Returns the encoded data if the operation succeded, else it returns an error
 */
func (regAcc *RegisterAccount) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(regAcc)
}

/*
 * This function tries to convert the json data in the io.Reader to the struct RegisterAccount
 * It returns the structure with the fields initialized with the values in the json if the fields
 * specified by the user are correct
 * Returns an error if the decoding failed
 */
func (regAcc *RegisterAccount) FromJSON(r io.Reader) error {
	e := json.NewDecoder(r)
	return e.Decode(regAcc)
}

/*
 * This function will validate that the username has the right format
 * The right format for the username is: minimum 6 characters long, maximum 20 characters long
 * The username must start with a letter, it can contain numbers and _
 * The username cannot contain special characters
 */
func validateUsername(fl validator.FieldLevel) bool {
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
func validatePassword(fl validator.FieldLevel) bool {
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
 * This function will validate if the display name has the right format
 * The Display Name should be at least 6 characters long and maximum 20
 * The display name can only contain letters, numbers and _
 */
func validateDisplayName(fl validator.FieldLevel) bool {
	//Check if the Display Name has the right length
	displayName := fl.Field().String()
	if len(displayName) < 6 || len(displayName) > 20 {
		return false
	}

	//Create the regex that will test if the Display Name has the right format
	r := regexp.MustCompile("^([a-zA-Z]+)([0-9|_|a-z|A-Z]*)$")

	//Check if the regex matches any substring in the password string
	matches := r.FindAllString(displayName, -1)

	//If matches is nil then return false
	if matches == nil {
		return false
	}

	//If there is more than one match in the string then return false
	if len(matches) != 1 {
		return false
	}

	//Else return true
	return true
}

/*
 * This function will validate the data in the structure after the json decoding
 * The validate tags in the struct tags will specify the requirments that the field
 * needs to pass the validation
 * If the validation fails then the function will return an error dictionary (validator package)
 */
func (regAcc *RegisterAccount) Validate() error {
	validate := validator.New()
	validate.RegisterValidation("username", validateUsername)
	validate.RegisterValidation("password", validatePassword)
	validate.RegisterValidation("displayname", validateDisplayName)
	return validate.Struct(regAcc)
}
