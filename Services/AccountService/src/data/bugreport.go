package data

import (
	"encoding/json"
	"io"
)

type BugReport struct {
	ID          int64  `json:"id"`
	Category    string `json:"category"`
	Description string `json:"description"`
	ReportedBy  string `json:"reportedBy"`
	Timestamp   string `json:"timestamp"`
}

func (br *BugReport) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(br)
}

func (br *BugReport) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(br)
}

type BugReports []BugReport

func (brs *BugReports) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(brs)
}

func (brs *BugReports) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(brs)
}
