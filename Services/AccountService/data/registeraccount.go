package data

import (
	"encoding/json"
	"io"

	"github.com/go-playground/validator/v10"
)

/*
 * This data structure will be used when a user wants to register an account
 * It will have less data than the structure used for sending the account details back to the user
 * It has less data because some fields are not required during registration (Id, Password, Salt, LastOnline, Status, JoinDate)
 */
type RegisterAccount struct {
	Username    string `json:"username" validate:"required"`    //Account username (TO DO...Set a minimum and a maximum length)
	Password    string `json:"password" validate:"required"`    //Account password (TO DO... Set a minimum and a maximum length (maybe casing and numbers))
	DisplayName string `json:"displayName" validate:"required"` //Account name that will be available publicly (TO DO... Set a minimum and a maximum length)
	Email       string `json:"email" validate:"required,email"` //Account email
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
 * This function will validate the data in the structure after the json decoding
 * The validate tags in the struct tags will specify the requirments that the field
 * needs to pass the validation
 * If the validation fails then the function will return an error dictionary (validator package)
 */
func (regAcc *RegisterAccount) Validate() error {
	validate := validator.New()
	return validate.Struct(regAcc)
}
