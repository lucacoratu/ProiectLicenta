package data

import (
	"io"
	"encoding/json"
)

type FriendRequestResponse struct {
	AccID int64 `json:"accID"`
	SenderID int64 `json:"senderID"`
	RequestDate string `json:"requestDate"`
}

func (frReqRes *FriendRequestResponse) ToJSON(w io.Writer) error{
	e := json.NewEncoder(w)
	return e.Encode(frReqRes)
}

func (frReqRes *FriendRequestResponse) FromJSON(r io.Reader) error{
	d := json.NewDecoder(r)
	return d.Decode(frReqRes)
}

type FriendRequestResponses []FriendRequestResponse

func (frReqResps *FriendRequestResponses) ToJSON(w io.Writer) error{
	e := json.NewEncoder(w)
	return e.Encode(frReqResps)
}

func (frReqResps *FriendRequestResponses) FromJSON(r io.Reader) error{
	d := json.NewDecoder(r)
	return d.Decode(frReqResps)
}