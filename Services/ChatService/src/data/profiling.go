package data

import (
	"encoding/json"
	"io"
)

type ProfilingInformation struct {
	DeviceInfo         string    `json:"deviceInfo"`
	FrameRates         []float64 `json:"frameRates"`
	NumberParticipants int64     `json:"numberParticipants"`
}

func (profInfo *ProfilingInformation) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(profInfo)
}

func (profInfo *ProfilingInformation) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(profInfo)
}
