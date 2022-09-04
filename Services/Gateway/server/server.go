package server

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"time"

	"github.com/gorilla/mux"
)

/*
 * This variables are scoped to this package
 * They are the logger used by the server during its execution
 * And the http.Server object which has the functions to listen and serve the client
 * They are initialized as nil at the start and after calling the InitServer function
 * they will hold the addresses of the coresponding objects
 */
var serverLogger *log.Logger
var server *http.Server

/*
 * The InitServer function will initialize the server with the address specified as a parameter to the function
 * It will also initialize the handlers for the paths supported by the HTTP server
 * It will initialize the logger that will be used across the HTTP server
 * Initializes the http.Server object with timeouts for Idle, Read and Write
 * It returns an error if something bad happens in the initialization process
 * Else returns nil
 */
func InitServer(address string) error {
	//Check if the server has not been initialized before
	if server != nil || serverLogger != nil {
		//The server has been initialized before
		return errors.New("server cannot be initialized more than once")
	}

	//Initialize the logger that the server will use
	serverLogger = log.New(os.Stdout, "[*] - Gateway - ", log.LstdFlags)
	serverLogger.Println("Logger has been initialized")

	//Create the handlers that will be passed to the serverMux

	//Initialize the server serveMux which will hold the handlers to the paths handled by the service
	//serveMux := http.NewServeMux()
	serveMux := mux.NewRouter()
	getRouter := serveMux.Methods(http.MethodGet).Subrouter()
	getRouter.HandleFunc("/{id:[0-9]+}")

	//Add the handlers to the serveMux (path and callback function)

	/*
	 * Intialize the http.Server object which will have some timeout times for diferent events
	 * It also has the serveMux initialized above as the Handler
	 */
	server = &http.Server{
		Addr:         address,
		Handler:      serveMux,
		ReadTimeout:  1 * time.Second,
		WriteTimeout: 1 * time.Second,
		IdleTimeout:  30 * time.Second,
	}

	//Server finished initialization
	serverLogger.Println("server has been initialized")
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
		//The server has not been initialized so return an erorr
		return errors.New("server is not initialized, cannot run")
	}

	/*
	 * Start the server inside a go func so it runs on a different thread and signal handlers
	 * can be added for graceful exit
	 */
	go func() {
		err := server.ListenAndServe()
		if err != nil {
			serverLogger.Fatal(err.Error())
		}
	}()

	//Create a channel where the signals will be received from OS
	chanSig := make(chan os.Signal)
	//Set the signals that should be waited on the channel
	signal.Notify(chanSig, os.Kill)
	signal.Notify(chanSig, os.Interrupt)

	//Wait for a signal to occur
	sig := <-chanSig
	serverLogger.Println("Received signal to terminate, exiting gracefully", sig)

	//Let the server run for some time so it can finish with the clients then exit, if the timeout end then exit
	tc, _ := context.WithTimeout(context.Background(), 30*time.Second)
	server.Shutdown(tc)

	return nil
}
