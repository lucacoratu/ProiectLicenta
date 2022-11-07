package data

import (
	"encoding/json"
	"io"
)

type GetGroup struct {
	RoomId       int64   `json:"roomId"`
	GroupName    string  `json:"groupName"`
	CreatorId    int64   `json:"creatorId"`
	CreationDate string  `json:"creationDate"`
	Participants []int64 `json:"participants"`
}

func (gg *GetGroup) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(gg)
}

func (gg *GetGroup) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(gg)
}

type GetGroups []GetGroup

func (ggs *GetGroups) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(ggs)
}

func (ggs *GetGroups) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(ggs)
}
