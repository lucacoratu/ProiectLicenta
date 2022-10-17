package data

/*
 * This structure will hold the data of the message received from the client
 * It will have information about the sender of the message and the receiver
 * The from and to values will represent id's (from will always be an id of a user, but to can be an user id or a group id)
 * The AccountService will complete the data associated with the from (because that service will validate the JWT token after login)
 * 
 */
type Message struct {
	From string `json:"from" validate:"required"`
	To string `json:"to" validate:"required"`

}