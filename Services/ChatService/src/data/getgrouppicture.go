package data

import (
	"encoding/json"
	"io"
)

type GetGroupPicture struct {
	GroupPicture string `json:"groupPicture"`
}

func (ggp *GetGroupPicture) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(ggp)
}

func (ggp *GetGroupPicture) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(ggp)
}
