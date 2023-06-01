package data

import (
	"encoding/json"
	"io"
)

type UpdateStatus struct {
	AccountID int    `json:"accountId"`
	NewStatus string `json:"newStatus"`
}

func (us *UpdateStatus) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(us)
}

func (us *UpdateStatus) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(us)
}
