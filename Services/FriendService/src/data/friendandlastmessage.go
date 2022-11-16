package data

import (
	"encoding/json"
	"io"
)

type FriendAndLastMessage struct {
	AccountID    int    `json:"accountID"`
	FriendID     int    `json:"friendID"`
	RoomID       int64  `json:"roomID"`
	BefriendDate string `json:"befriendDate"`
	LastMessage  string `json:"lastMessage"`
}

func (f *FriendAndLastMessage) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(f)
}

func (f *FriendAndLastMessage) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(f)
}

type FriendsAndLastMessages []FriendAndLastMessage

func (f *FriendsAndLastMessages) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(f)
}

func (f *FriendsAndLastMessages) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(f)
}
