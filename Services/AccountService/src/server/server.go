package server

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"time"
	"willow/accountservice/database"
	"willow/accountservice/handlers"

	"github.com/gorilla/mux"
)

/* This variable will be used only locally in this package
 * It will be initialized with the handles that the server will support
 * It will have an idle timeout, read timeout and a write timeout that will be tweaked
 * It will initialize the logger that will be used in the application
 */
var server *http.Server = nil
var serverLogger *log.Logger = nil
var serverDbConn *database.Connection = nil

/*
 * The InitServer function will initialize the server with the address specified as a parameter to the function
 * It will also initialize the handlers for the paths supported by the HTTP server
 * It will initialize the logger that will be used across the HTTP server
 * Initializes the http.Server object with timeouts for Idle, Read and Write
 * It returns an error if something bad happens in the initialization process
 * Else returns nil
 */
func InitServer(address string) error {
	//Check if the server has not been already initialized
	if server != nil || serverLogger != nil {
		//The server has been already initialized so return an error
		return errors.New("server cannot be initialized more than once")
	}

	//Initialize the logger to be on stdout and with the correct prefix for this service
	serverLogger = log.New(os.Stdout, "[*] - Account Service - ", log.LstdFlags)
	serverLogger.Println("Logger initialized")

	//Create the connection object from the database package
	serverDbConn = database.NewConnection(serverLogger)
	err := serverDbConn.InitializeConnection()
	if err != nil {
		serverLogger.Println("Database connection initialization failed: " + err.Error())
		return err
	}

	//Create the handlers that the server will have (paths and functions to where the server will respond)
	//Create the /login handler
	handlerLogin := handlers.NewLogin(serverLogger, serverDbConn)

	//Create the /register handler and add the database handle to the struct
	handlerRegister := handlers.NewRegister(serverLogger, serverDbConn)

	//Create the serve mux where the handlers will be assigned so can then be used by the http.Server object
	//serveMuxServer := http.NewServeMux()
	serveMuxServer := mux.NewRouter()

	//Create the subrouter for the POST method which will have the handler functions for the POST requests to specific routes
	postRouter := serveMuxServer.Methods(http.MethodPost).Subrouter()
	postRouter.HandleFunc("/login", handlerLogin.LoginAccount)
	postRouter.HandleFunc("/register", handlerRegister.RegisterAccount)

	//Log that the handlers have been added
	serverLogger.Println("Handlers added to the serve mux of the server")

	//Initialize the http.Server object
	server = &http.Server{
		Addr:         address,
		Handler:      serveMuxServer,
		ReadTimeout:  1 * time.Second,
		WriteTimeout: 1 * time.Second,
		IdleTimeout:  120 * time.Second,
	}

	//Log that the server finished initialization
	serverLogger.Println("Server has been initialized")
	return nil
}

/*
 * This function will start the HTTP server, if the server has been initialized
 * If the server object has not been initialized then the function will return an error
 * The server will listen then it will create a channel for handling the shutdown
 * Sets a timeout for the shutdown, so the server can finish handling the connections it has before shutdown
 * To make the server shutdown a signal has to be received by it (interrupt or kill)
 */
func RunServer() error {
	//Check if the server has been initialized
	if server == nil || serverLogger == nil {
		//The server has not been initialized so return an error
		return errors.New("server has to be initialized before running")
	}

	serverLogger.Println("Testing the connection to the database server")
	//Try to ping the database server to check if the connection can be established
	errDb := serverDbConn.TestConnection()
	if errDb != nil {
		//The server should be able to connect to the database server in order to work
		serverLogger.Println(errDb.Error())
		return errors.New("server cannot connect to the database server")
	}
	//Close the connection when before this function returns
	defer serverDbConn.CloseConnection()
	//Log that the connection to the database server has been established
	serverLogger.Println("Database connection has been established")

	//Run the server in a go function so the channel for graceful exit can be created
	go func() {
		err := server.ListenAndServe()
		if err != nil {
			serverLogger.Fatal(err)
		}
	}()

	//Create the channel where the signals from the os will be notified
	chanSig := make(chan os.Signal)

	//Set the signals that we want to be received for graceful exit
	signal.Notify(chanSig, os.Kill)
	signal.Notify(chanSig, os.Interrupt)

	//Wait for the signal to be received then continue to shutdown
	sig := <-chanSig

	//Log that the server will shutdown
	serverLogger.Println("Received signal to terminate, exiting gracefully", sig)

	//Let the server finish the current connection in a timeout of 30 seconds then exit
	tc, _ := context.WithTimeout(context.Background(), 30*time.Second)
	server.Shutdown(tc)

	return nil
}
