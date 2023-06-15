package data

import (
	"encoding/json"
	"io"
)

type AreFriendsResponse struct {
	Message bool `json:"arefriends"`
}

func (afr *AreFriendsResponse) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(afr)
}

func (afr *AreFriendsResponse) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(afr)
}
