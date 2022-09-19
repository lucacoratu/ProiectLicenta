package jwt

import (
	"errors"
	"time"

	"github.com/dgrijalva/jwt-go"
)

// Key that will be used to sign the jwt tokens
// TO DO... move it to environment variable (NOT SECURE)
var jwtKey = []byte("accountsupersecretjwtkey")

/*
 * This structure will hold the data inside the JWT Token
 * TO DO...use a salt to create different JWT tokens for a user
 */
type JWTClaim struct {
	ID          uint64 `json:"id"`
	DisplayName string `json:"displayName"`
	jwt.StandardClaims
}

/*
 * This function will generate a JWT token that will be valid for 60 minutes
 * If an error occurs during the JWT generation then this function will return an error
 * Else the function will return nil for error
 */
func GenerateJWT(id uint64, displayName string) (string, error) {
	//Set the expiration time to be in an hour from the time of generation
	expirationTime := time.Now().Add(time.Hour * 1).Unix()
	//Create the data of the JWT
	claims := &JWTClaim{
		ID:          id,
		DisplayName: displayName,
		StandardClaims: jwt.StandardClaims{
			ExpiresAt: expirationTime,
		},
	}
	//Set the HMAC_SHA256 algorithm to be used with a secret password
	token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	//Sign the token
	return token.SignedString(jwtKey)
}

/*
 * This function will validate the JWT token
 */
func ValidateJWT(signedToken string) (*JWTClaim, error) {
	//Try to parse the token received (general format)
	token, err := jwt.ParseWithClaims(
		signedToken,
		&JWTClaim{},
		func(t *jwt.Token) (interface{}, error) {
			return []byte(jwtKey), nil
		},
	)
	//Check if an erorr occured during parsing
	if err != nil {
		//An error occured (invalid format of the JWT)
		return nil, err
	}

	//Try to convert the data in the JWT to the JWTClaim structure
	claims, ok := token.Claims.(*JWTClaim)
	//Check if the convertion was ok
	if !ok {
		//The conversion was no ok
		return nil, errors.New("couldn't parse the claims")
	}

	//Check if the token is still valid
	if claims.ExpiresAt < time.Now().Local().Unix() {
		//The token expired
		return nil, errors.New("token expired")
	}

	//The token is valid
	return claims, nil
}

func ExtractClaims(signedToken string) (*JWTClaim, error) {
	//Try to parse the token received (general format)
	token, err := jwt.ParseWithClaims(
		signedToken,
		&JWTClaim{},
		func(t *jwt.Token) (interface{}, error) {
			return []byte(jwtKey), nil
		},
	)
	//Check if an erorr occured during parsing
	if err != nil {
		//An error occured (invalid format of the JWT)
		return nil, err
	}

	//Try to convert the data in the JWT to the JWTClaim structure
	claims, ok := token.Claims.(*JWTClaim)
	//Check if the convertion was ok
	if !ok {
		//The conversion was no ok
		return nil, errors.New("couldn't parse the claims")
	}

	return claims, nil
}
