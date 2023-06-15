package data

import (
	"encoding/json"
	"io"
)

type GetRoomId struct {
	AccountID int64 `json:"accountId"`
	FriendID  int64 `json:"friendID"`
}

func (gri *GetRoomId) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(gri)
}

func (gri *GetRoomId) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(gri)
}

type RoomIdResponse struct {
	RoomID int64 `json:"roomID"`
}

func (ri *RoomIdResponse) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(ri)
}
