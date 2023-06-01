package data

import (
	"encoding/json"
	"io"
)

type AddBugReport struct {
	Category    string `json:"category"`
	Description string `json:"description"`
	AccountId   int64  `json:"reportedBy"`
}

func (fracc *AddBugReport) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(fracc)
}

func (fracc *AddBugReport) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(fracc)
}
