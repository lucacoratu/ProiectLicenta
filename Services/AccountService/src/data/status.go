package data

import (
	"encoding/json"
	"io"
	"regexp"

	"github.com/go-playground/validator/v10"
)

type Status struct {
	NewStatus string `json:"status" validate:"required,status"`
}

func (status *Status) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(status)
}

func (status *Status) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(status)
}

func validateStatus(fl validator.FieldLevel) bool {
	//Check if the status contains only letters or space
	status := fl.Field().String()
	regex := regexp.MustCompile("^[A-Za-z ]+$")
	matches := regex.FindAllString(status, -1)
	return len(matches) != 1
}

func (status *Status) Validate() error {
	validate := validator.New()
	validate.RegisterValidation("status", validateStatus)
	return validate.Struct(status)
}
