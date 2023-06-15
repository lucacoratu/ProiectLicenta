package data

import (
	"encoding/json"
	"io"
)

type ReportCategory struct {
	Name string `json:"name"`
}

func (r *ReportCategory) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(r)
}

func (r *ReportCategory) FromJSON(reader io.Reader) error {
	d := json.NewDecoder(reader)
	return d.Decode(r)
}

type ReportCategories []ReportCategory

func (rc *ReportCategories) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(rc)
}

func (rc *ReportCategories) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(rc)
}
