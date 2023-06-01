package data

import (
	"encoding/json"
	"io"
)

type Friend struct {
	ID	     int    `json:"id"`
	AccountID    int    `json:"accountID"`
	FriendID     int    `json:"friendID"`
	BefriendDate string `json:"befriendDate"`
}

func (fr *Friend) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(fr)
}

type Friends []Friend

func (frs *Friends) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(frs)
}
