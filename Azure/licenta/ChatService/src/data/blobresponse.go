package data

import (
	"encoding/json"
	"io"
)

type BlobResponse struct {
	BlobUuid string `json:"blobUuid"`
}

func (br *BlobResponse) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(br)
}

func (br *BlobResponse) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(br)
}
