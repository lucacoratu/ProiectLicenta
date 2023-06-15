package data

import (
	"encoding/json"
	"io"
)

type AreFriends struct {
	Message bool `json:"arefriends"`
}

func (af *AreFriends) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(af)
}

func (af *AreFriends) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(af)
}
