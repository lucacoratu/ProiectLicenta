package data

import (
	"encoding/json"
	"io"
)

type SendReact struct {
	Id            int64  `json:"reactionId"`
	MessageId     int64  `json:"messageId"`
	EmojiReaction string `json:"emojiReaction"`
	SenderId      int64  `json:"senderId"`
	RoomId        int64  `json:"roomId"`
}

func (sr *SendReact) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(sr)
}

func (sr *SendReact) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(sr)
}
