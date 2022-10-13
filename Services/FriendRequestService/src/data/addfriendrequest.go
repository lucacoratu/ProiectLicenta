package data

import (
	"encoding/json"
	"io"

	"github.com/go-playground/validator/v10"
)

type FriendRequest struct {
	AccID    int `json:"accountID" validate:"required,ID"` //this is the account that received the request
	SenderID int `json:"senderID" validate:"required,ID"`  //this is the account that sent the request
}

func (frReq *FriendRequest) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(frReq)
}

func (frReq *FriendRequest) FromJSON(r io.Reader) error {
	e := json.NewDecoder(r)
	return e.Decode(frReq)
}

func validateID(fl validator.FieldLevel) bool {
	//Check if the ID can be converted to an int
	if !fl.Field().CanInt() {
		return false
	}
	id := fl.Field().Int()
	//The ID should not be negative
	return id > 0
}

func (frReq *FriendRequest) Validate() error {
	validate := validator.New()
	validate.RegisterValidation("ID", validateID)
	return validate.Struct(frReq)
}
