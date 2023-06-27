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

type EncryptionProfiling struct {
	DeviceInfo         string `json:"deviceInfo"`
	ElapsedMiliseconds int64  `json:"elapsedMiliseconds"`
	MessageSize        int64  `json:"messageSize"`
}

func (encProf *EncryptionProfiling) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(encProf)
}

func (encProf *EncryptionProfiling) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(encProf)
}
