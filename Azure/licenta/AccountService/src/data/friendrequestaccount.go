package data

import (
	"encoding/json"
	"io"
)

type FriendRequestAccount struct {
	AccountID         int64  `json:"accountID"`
	DisplayName       string `json:"displayName"`
	RequestDate       string `json:"requestDate"`
	LastOnline        string `json:"lastOnline"`
	JoinDate          string `json:"joinDate"`
	ProfilePictureUrl string `json:"profilePictureUrl"`
	About             string `json:"about"`
}

func (frReqAcc *FriendRequestAccount) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(frReqAcc)
}

func (frReqAcc *FriendRequestAccount) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(frReqAcc)
}

type FriendRequestAccounts []FriendRequestAccount

func (frReqAccs *FriendRequestAccounts) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(frReqAccs)
}

func (frReqAccs *FriendRequestAccounts) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(frReqAccs)
}
