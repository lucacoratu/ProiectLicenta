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
