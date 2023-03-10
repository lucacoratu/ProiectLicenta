package data

import (
	"encoding/json"
	"io"
)

type Reaction struct {
	Id           int64  `json:"id"`
	Emoji        string `json:"emoji"`
	SenderId     int64  `json:"senderId"`
	ReactionDate string `json:"reactionDate"`
}

func (react *Reaction) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(react)
}

func (react *Reaction) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(react)
}
