package data

import (
	"encoding/json"
	"io"
)

type UpdateAbout struct {
	AccountId int64  `json:"accountId"`
	NewAbout  string `json:"newAbout"`
}

func (ua *UpdateAbout) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(ua)
}

func (ua *UpdateAbout) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(ua)
}
