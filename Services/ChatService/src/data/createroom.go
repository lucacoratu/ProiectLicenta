package data

import (
	"encoding/json"
	"io"
)

/*
 * This structure will hold variables that will be used when creating a new private room
 */
type CreatePrivateRoom struct {
	SenderID int64 `json:"senderID"` //This is the id of the user that made the request to create a room
	ReceiverID int64 `json:"receiverID"` //This is the id of the user that will be added in the room
}

/*
 * This function will encode a CreatePrivateRoom into json format
 */
func (cpr *CreatePrivateRoom) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(cpr)
}

/*
 * This function will decode a json string into CreatePrivateRoom structure
 */
func (cpr *CreatePrivateRoom) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(cpr)
}