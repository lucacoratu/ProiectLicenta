package data

import (
	"encoding/json"
	"io"
)

type ViewFriendRequest struct {
	AccID       int    `json:"accID"`
	SenderID    int    `json:"senderID"`
	RequestDate string `json:"requestDate"`
}

func (frReq *ViewFriendRequest) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(frReq)
}

type ViewFriendRequests []ViewFriendRequest

func (frReqs *ViewFriendRequests) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(frReqs)
}
