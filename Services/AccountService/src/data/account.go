package data

import (
	"encoding/json"
	"io"
)

/*
 * This structure will be used for sharing public information abount an account
 */
type Account struct {
	ID           int    `json:"id"`
	Username     string `json:"-"`
	DisplayName  string `json:"displayName"`
	Email        string `json:"email"`
	PasswordHash string `json:"-"`
	Salt         string `json:"-"`
	LastOnline   string `json:"lastOnline"`
	Status       string `json:"status"`
	JoinDate     string `json:"joinDate"`
}

/*
 * This function will encode the structure to json with the field names specified
 * in the structure tags
 */
func (acc *Account) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(acc)
}

/*
 * This function will decode a json string into an Account structure
 */
func (accs *Account) FromJSON(r io.Reader) error {
	e := json.NewDecoder(r)
	return e.Decode(accs)
}
