package data

import (
	"encoding/json"
	"io"
)

type UpdateGroupPicture struct {
	RoomId          int64  `json:"roomId"`
	NewGroupPicture string `json:"newGroupPicture"`
}

func (ugp *UpdateGroupPicture) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(ugp)
}

func (ugp *UpdateGroupPicture) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(ugp)
}
