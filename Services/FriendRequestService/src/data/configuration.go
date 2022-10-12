package data

/*
 * This structure will hold the configuration data needed by this service to run
 * It will have the ip (or name ) and port of the database, the username and password
 * of the database server and the database name
 */
type Configuration struct {
	DbUser     string `json:"dbUsername" validate:"required"`
	DbPassword string `json:"dbPassword" validate:"required"`
	DbHost     string `json:"dbHost" validate:"required"`
	DbName     string `json:"dbName" validate:"required"`
}
