package data

import (
	"encoding/json"
	"io"
)

/*
 * This structure will hold the data for a create group request
 */
type CreateGroup struct {
	CreatorId    int64   `json:"creatorID"`
	GroupName    string  `json:"groupName"`
	Participants []int64 `json:"participants"`
}

func (cg *CreateGroup) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(cg)
}

func (cg *CreateGroup) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(cg)
}

type CreateGroupResponse struct {
	GroupId int64 `json:"groupId"`
}

func (cgr *CreateGroupResponse) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(cgr)
}

func (cgr *CreateGroupResponse) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(cgr)
}
