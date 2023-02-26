package data

import (
	"encoding/json"
	"io"
)

type CommonGroup struct {
	RoomId           int64    `json:"roomId"`
	GroupName        string   `json:"groupName"`
	CreatorId        int64    `json:"creatorId"`
	CreationDate     string   `json:"creationDate"`
	Participants     []int64  `json:"participants"`
	ParticipantNames []string `json:"participantNames"`
}

func (cg *CommonGroup) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(cg)
}

func (cg *CommonGroup) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(cg)
}

type CommonGroups []CommonGroup

func (cgs *CommonGroups) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(cgs)
}

func (cgs *CommonGroups) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(cgs)
}
