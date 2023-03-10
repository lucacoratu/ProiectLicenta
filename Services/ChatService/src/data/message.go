package data

import (
	"encoding/json"
	"io"
)

type Message struct {
	Id        int64      `json:"id"`
	TypeID    int64      `json:"typeId"`
	Data      string     `json:"data"`
	SendDate  string     `json:"sendDate"`
	UserId    int64      `json:"userId"`
	Reactions []Reaction `json:"reactions"`
}

func (m *Message) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(m)
}

func (m *Message) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(m)
}

type Messages []Message

func (ms *Messages) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(ms)
}

func (ms *Messages) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(ms)
}
