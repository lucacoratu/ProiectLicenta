package data

import (
	"encoding/json"
	"io"
)

type CanSendFriendRequest struct {
	CanSendRequest bool `json:"canSendRequest"`
}

func (can *CanSendFriendRequest) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(can)
}

func (can *CanSendFriendRequest) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(can)
}
