package data

import (
	"encoding/json"
	"io"
)

type LastMessage struct {
	MessageText      string `json:"messageText"`
	MessageTimestamp string `json:"messageTimestamp"`
}

func (lm *LastMessage) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(lm)
}

func (lm *LastMessage) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(lm)
}
