package data

import (
	"encoding/json"
	"io"
)

type FriendResponse struct {
	AccountID    int64  `json:"accountID"`
	FriendID     int64  `json:"friendID"`
	RoomID       int64  `json:"roomID"`
	BefriendDate string `json:"befriendDate"`
	LastMessage  string `json:"lastMessage"`
}

func (fr *FriendResponse) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(fr)
}

func (fr *FriendResponse) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(fr)
}

type FriendResponses []FriendResponse

func (frs *FriendResponses) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(frs)
}

func (frs *FriendResponses) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(frs)
}
