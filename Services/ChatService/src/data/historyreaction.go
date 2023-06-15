package data

import (
	"encoding/json"
	"io"
)

type HistoryReaction struct {
	MessageId    int64  `json:"messageId"`
	ReactionId   int64  `json:"reactionId"`
	SenderId     int64  `json:"senderId"`
	Emoji        string `json:"emoji"`
	ReactionDate string `json:"reactionDate"`
}

func (hr *HistoryReaction) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(hr)
}

func (hr *HistoryReaction) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(hr)
}

type HistoryReactions []HistoryReaction

func (hrs *HistoryReactions) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(hrs)
}

func (hrs *HistoryReactions) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(hrs)
}
