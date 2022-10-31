package data

import (
	"encoding/json"
	"io"
)

/*
 * This structure will hold the room details which will be
 * - roomId - the id of the room
 * - isGroup - a boolean value which will define if the room is a group or not
 * - creationDate - a string representation of the date the room was created
 * - groupName - which in case the isGroup flag is true this will have a non NULL value
 */
type RoomDetails struct {
	RoomId       int64  `json:"roomID"`
	IsGroup      bool   `json:"isGroup"`
	CreationDate string `json:"creationDate"`
	GroupName    string `json:"groupName"`
}

/*
 * This structure will embed the room details structure
 * It will also have a list of participants (without the current account that requested the list of rooms)
 */
type Room struct {
	RoomDet      RoomDetails `json:"details"`
	Participants []int64     `json:"participants"`
}

func (ro *Room) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(ro)
}

func (ro *Room) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(ro)
}

type Rooms []Room

func (ros *Rooms) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(ros)
}
