package data

import (
	"io"
	"encoding/json"
)

type FriendAccount struct {
	FriendID int64 `json:"friendID"`
	DisplayName string `json:"displayName"`
	BefriendDate string `json:"befriendDate"`
	LastOnline string `json:"lastOnline"`
	JoinDate     string `json:"joinDate"`
}

func (fracc *FriendAccount) ToJSON(w io.Writer) error{
	e := json.NewEncoder(w)
	return e.Encode(fracc)
}

func (fracc *FriendAccount) FromJSON(r io.Reader) error{
	d := json.NewDecoder(r)
	return d.Decode(fracc)
}

type FriendAccounts []FriendAccount

func (fraccs *FriendAccounts) ToJSON(w io.Writer) error{
	e := json.NewEncoder(w)
	return e.Encode(fraccs)
}

func (fraccs *FriendAccounts) FromJSON(r io.Reader) error{
	d := json.NewDecoder(r)
	return d.Decode(fraccs)
}