package jsonerrors

import (
	"encoding/json"
	"io"
)

/*
 * This struct will be used by the service to return errors to the client
 * It will contain a stirng which will be the error message as text
 */
type JsonError struct {
	Message string `json:"error"`
}

/*
 * This method will encode the error message in json format
 * If in an error occurs during the encoding then the error will be returned
 * Else returns the json encoded error message
 */
func (je *JsonError) ToJSON(w io.Writer) error {
	jEncoder := json.NewEncoder(w)
	return jEncoder.Encode(je)
}

/*
 * This method will decode a json string to a JsonError object
 * If any error occurs during the decoding then an error is returned
 * Else the function returns the decoded JsonError struct
 */
func (je *JsonError) FromJSON(r io.Reader) error {
	jDecoder := json.NewDecoder(r)
	return jDecoder.Decode(je)
}
