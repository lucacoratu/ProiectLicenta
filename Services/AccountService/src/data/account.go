package data

import (
	"encoding/json"
	"io"
	"time"
)

type Account struct {
	ID           int    `json:"-"`
	Username     string `json:"-"`
	DisplayName  string `json:"displayName"`
	Email        string `json:"email"`
	PasswordHash string `json:"-"`
	Salt         string `json:"-"`
	LastOnline   string `json:"lastOnline"`
	Status       string `json:"status"`
	JoinDate     string `json:"joinDate"`
}

func (acc *Account) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(acc)
}

func (accs *Account) FromJSON(r io.Reader) error {
	e := json.NewDecoder(r)
	return e.Decode(accs)
}

type Accounts []*Account

func (accs *Accounts) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(accs)
}

func GetAccounts() Accounts {
	return accountsList
}

func GetAccount(id int) *Account {
	for _, acc := range accountsList {
		if acc.ID == id {
			return acc
		}
	}

	return nil
}

func AddAccount(acc *Account) error {
	//Generate the id
	acc.ID = accountsList[len(accountsList)-1].ID + 1
	acc.JoinDate = time.Now().UTC().Format(time.RFC822)
	acc.Status = "Offline"
	acc.LastOnline = "never"

	accountsList = append(accountsList, acc)
	return nil
}

var accountsList = []*Account{
	{
		ID:           1,
		Username:     "lucacoratu",
		DisplayName:  "LucaCoratu",
		PasswordHash: "1234",
		Email:        "lucacoratu@gmail.com",
		LastOnline:   "1 hour ago",
		Status:       "Offline",
		JoinDate:     "24 August 2022",
	},

	{
		ID:           2,
		Username:     "lucacoratu2",
		DisplayName:  "LucaCoratu2",
		PasswordHash: "1234",
		Email:        "lucacoratu2@gmail.com",
		LastOnline:   "0 seconds ago",
		Status:       "Online",
		JoinDate:     "25 August 2022",
	},
}
