package data

import (
	"encoding/json"
	"io"
)

type Account struct {
	ID          int    `json:"-"`
	Username    string `json:"username"`
	DisplayName string `json:"displayName"`
	Email       string `json:"email"`
	Password    string `json:"-"`
	Salt        string `json:"-"`
	LastOnline  string `json:"lastOnline"`
	Status      string `json:"status"`
	JoinDate    string `json:"joinDate"`
}

type Accounts []*Account

func (accs *Accounts) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(accs)
}

func (accs *Account) FromJSON(r io.Reader) error {
	e := json.NewDecoder(r)
	return e.Decode(accs)
}

func GetAccounts() Accounts {
	return accountsList
}

var accountsList = []*Account{
	{
		ID:         1,
		Username:   "lucacoratu",
		Password:   "1234",
		Email:      "lucacoratu@gmail.com",
		LastOnline: "1 hour ago",
		Status:     "Offline",
		JoinDate:   "24 August 2022",
	},

	{
		ID:         2,
		Username:   "lucacoratu2",
		Password:   "1234",
		Email:      "lucacoratu2@gmail.com",
		LastOnline: "0 seconds ago",
		Status:     "Online",
		JoinDate:   "25 August 2022",
	},
}
