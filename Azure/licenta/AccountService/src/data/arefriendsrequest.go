package data

import (
	"encoding/json"
	"io"
)

type AreFriendsRequest struct {
	AccID    int `json:"accountId"` //this is the account that wants to send the request
	SenderID int `json:"friendId"`  //this is the account that should receive the request
}

func (afr *AreFriendsRequest) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(afr)
}

func (afr *AreFriendsRequest) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(afr)
}
