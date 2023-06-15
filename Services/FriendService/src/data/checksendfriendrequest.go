package data

import (
	"encoding/json"
	"io"
)

type CheckSendFriendRequest struct {
	SenderId int64 `json:"senderId"`
	UserId   int64 `json:"userId"`
}

func (check *CheckSendFriendRequest) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(check)
}

func (check *CheckSendFriendRequest) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(check)
}
