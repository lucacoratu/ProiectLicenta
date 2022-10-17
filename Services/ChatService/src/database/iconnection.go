package database

/*
 * This will be the interface that will define the functions that can be called to interact with the database
 * This is needed because during the testing of the application mysql will be used because the cassandra nodes are expensive
 * In the rest of the code this interface will be used so when the exchange to cassandra has to be made, only the initialization
 * should be modified
 */
type IConnection interface {
	InitializeConnection() error
	TestConnection() error
	CloseConnection() error
}
