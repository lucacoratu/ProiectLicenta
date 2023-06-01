package handlers

import (
	"crypto"
	"crypto/ecdsa"
	"crypto/sha256"
	"crypto/x509"
	"encoding/base64"
	"encoding/hex"
	"encoding/pem"
	"math/big"
	"net/http"
	"willow/accountservice/data"
	"willow/accountservice/database"
	jsonerrors "willow/accountservice/errors"
	"willow/accountservice/jwt"
	"willow/accountservice/logging"

	"github.com/go-playground/validator/v10"
)

/*
 * This structure is the placeholder for all the data
 * that will be used by the login functionality of the account
 * service
 */
type Login struct {
	l             logging.ILogger
	dbConn        *database.Connection
	configuration data.Configuration
}

/*
 * The NewLogin function will create a new object of the Register struct
 * Factory method for creating an object of Login easily
 */
func NewLogin(l logging.ILogger, dbConn *database.Connection, conf data.Configuration) *Login {
	return &Login{l: l, dbConn: dbConn, configuration: conf}
}

func (login *Login) verifyLoginSignature(username string, signature string) bool {
	//Get the public key of the username
	identityPem, err := login.dbConn.GetAccountIdentityPublicKey(username)
	//Check if an error occured
	if err != nil {
		return false
	}
	//Load the public key
	login.l.Debug(identityPem)
	pemBlock, _ := pem.Decode([]byte(identityPem))
	key, err := x509.ParsePKIXPublicKey(pemBlock.Bytes)
	if err != nil {
		login.l.Error("Error occured when loading the public key", err.Error())
		return false
	}
	dsaKey := key.(*ecdsa.PublicKey)
	login.l.Debug(dsaKey.X, dsaKey.Y, dsaKey.Curve)

	//Convert from base64 the signature
	login.l.Debug(signature)
	signatureDecoded, err := base64.StdEncoding.DecodeString(signature)
	//Check if an error occured when decoding the signature from base64
	if err != nil {
		login.l.Error("Error occured when decoding the base64 signature", err.Error())
		return false
	}

	//Hash the username value using SHA-256
	hasher := crypto.Hash.New(crypto.SHA256)
	_, err = hasher.Write([]byte(username))
	//Check if the has could have been computed
	if err != nil {
		login.l.Error("Error occured when hashing the username", err.Error())
		return false
	}

	//Get the r value from the signature
	r := new(big.Int).SetBytes(signatureDecoded[:len(signatureDecoded)/2])
	//Get the s value from the signature
	s := new(big.Int).SetBytes(signatureDecoded[len(signatureDecoded)/2:])

	//Verify the signature
	signatureVerified := ecdsa.Verify(dsaKey, hasher.Sum(nil), r, s)
	login.l.Debug("Signature verified", signatureVerified)

	return signatureVerified
}

/*
 * This function will retrieve the login credentials from the HTTP POST request body and
 * will check if the credentials are corrent. If the credentials are correct then
 */
func (login *Login) LoginAccount(rw http.ResponseWriter, r *http.Request) {
	login.l.Info("Endpoint /login reached (POST method)")
	data := &data.LoginAccount{}
	//Convert the data in the request body from json to the LoginAcount structure
	err := data.FromJSON(r.Body)
	//Check if an error occured during decoding
	if err != nil {
		//An error occured so return a json error to the client
		jsonErr := jsonerrors.JsonError{Message: "Invalid json format"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}

	//Validate the fields in the structure
	err = data.Validate()
	//Check if there are any field erorrs after validation
	if err != nil {
		//Return only the first error to the client in a pretty format
		for _, e := range err.(validator.ValidationErrors) {
			jsonErr := jsonerrors.JsonError{Message: e.Field() + " Invalid format"}
			rw.WriteHeader(http.StatusBadRequest)
			jsonErr.ToJSON(rw)
			return
		}
	}

	//Get the salt of the username
	saltHex, err := login.dbConn.GetSalt(data.Username)
	if err != nil {
		//The login failed because the salt could not bet retrieved
		jsonErr := jsonerrors.JsonError{Message: "Invalid credentials"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}
	//Salt was retrieved so compute the hash of the password received from the user
	//Decode the hash from hex
	salt, err := hex.DecodeString(saltHex)
	if err != nil {
		login.l.Error("Error occured during decoding the salt from hex")
		jsonErr := jsonerrors.JsonError{Message: "Invalid credentials"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
	}

	//Concatenate the salt to the password
	hashdata := []byte(data.Password)
	hashdata = append(hashdata, salt...)

	//Compute the sha2-256 hash of the password concatenated with the salt
	hash := sha256.New()
	_, err = hash.Write(hashdata)
	if err != nil {
		login.l.Error("Cannot compute the hash for the password", err.Error())
		jsonErr := jsonerrors.JsonError{Message: "Invalid credentials"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}
	//Compute the hash of the concatenated data
	hashPassword := hash.Sum(nil)

	//Convert the hash into hexstring
	hashHex := hex.EncodeToString(hashPassword)
	acc, err := login.dbConn.LoginIntoAccount(data.Username, hashHex)
	//Check if the login failed (error is not nil)
	if err != nil {
		login.l.Error("Login failed for username ", data.Username)
		jsonErr := jsonerrors.JsonError{Message: "Invalid credentials"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}

	//Verify signature
	signatureVerified := login.verifyLoginSignature(data.Username, data.Signature)
	if !signatureVerified {
		login.l.Info("Login failed, invalid signature of username", data.Username)
		jsonErr := jsonerrors.JsonError{Message: "Invalid signature"}
		rw.WriteHeader(http.StatusBadRequest)
		jsonErr.ToJSON(rw)
		return
	}

	//Generate the JWT token for the session of the client
	token, err := jwt.GenerateJWT(uint64(acc.ID), acc.DisplayName)
	if err != nil {
		login.l.Error("JWT Token generation failed!")
		jsonErr := jsonerrors.JsonError{Message: "Something happened"}
		rw.WriteHeader(http.StatusInternalServerError)
		jsonErr.ToJSON(rw)
		return
	}

	login.l.Info("Token:", token)

	//The login succeded so return the account data to the client
	login.l.Info("Login succeded, username", acc.Username)
	rw.Header().Add("Set-Cookie", "session="+token)
	rw.WriteHeader(http.StatusOK)
	acc.ToJSON(rw)
}
