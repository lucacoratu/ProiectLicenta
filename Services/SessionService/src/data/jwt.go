package jwt

import (
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
	Id          uint64 `json:"id"`
	DisplayName string `json:"displayName"`
	Email       string `json:"email"`
	jwt.StandardClaims
}

/*
 * This function will generate a JWT token that will be valid for 60 minutes
 * If an error occurs during the JWT generation then this function will return an error
 * Else the function will return nil for error
 */
func GenerateJWT(id uint64, displayName string, email string) (string, error) {
	expirationTime := time.Now().Add(time.Hour * 1).Unix()
	claims := &JWTClaim{
		ID:          id,
		DisplayName: displayName,
		Email:       email,
		StandardClaims: jwt.StandardClaims{
			ExpiresAt: expirationTime,
		},
	}
	token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	return token.SignedString(jwtKey)
}

/*
 * This function will generate
 */
