package data

import (
	"encoding/json"
	"io"
)

type AddFriend struct {
	AccountID int `json:"accountID" validate:"required,id"`
	FriendID  int `json:"friendID" validate:"required,id"`
}

func (addFr *AddFriend) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(addFr)
}

func (addFr *AddFriend) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(addFr)
}
