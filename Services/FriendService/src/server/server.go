package server

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"time"
	"willow/friendservice/database"
	"willow/friendservice/handlers"
	"willow/friendservice/logging"

	"github.com/gorilla/mux"
)

var serverLogger logging.ILogger
var server *http.Server = nil
var dbConn *database.Connection = nil

func InitServer(address string) error {
	//Check if the server has not been initialized before
	if server != nil {
		return errors.New("cannot initialize the server twice")
	}
	//Initialize the logger
	serverLogger = logging.NewLogger(log.New(os.Stdout, "[*] - Friend Service -", log.LstdFlags), "[INFO]", "[WARNINGS]", "[ERROR]")
	serverLogger.Info("Logger has been initialized")

	dbConn = database.NewConnection(serverLogger)
	err := dbConn.InitializeConnection()
	if err != nil {
		serverLogger.Error("Could not initialize the database connection")
		return err
	}
	serverLogger.Info("Initialized the database connection")

	err = dbConn.TestConnection()
	if err != nil {
		serverLogger.Error("Could not connect to the database")
		return err
	}
	serverLogger.Info("Tested database connection, it is active")

	//Create the handlers for the routes
	handlerFriendRequest := handlers.NewFriendRequest(serverLogger, dbConn)
	handlerFriend := handlers.NewFriend(serverLogger, dbConn)

	//Initialize the gorilla servemux
	serveMux := mux.NewRouter()
	//Get the subrouter for the POST method
	postSubrouter := serveMux.Methods(http.MethodPost).Subrouter()
	//Add the function for adding a friend request
	postSubrouter.HandleFunc("/friendrequest/add", handlerFriendRequest.AddFriendRequest)
	//Add the function for deleting a friend request
	postSubrouter.HandleFunc("/friendrequest/delete", handlerFriendRequest.DeleteFriendRequest)
	//Add the function for adding a new friend
	postSubrouter.HandleFunc("/friend/add", handlerFriend.AddFriend)
	//Add the function for deleting a friend
	postSubrouter.HandleFunc("/friend/delete", handlerFriend.DeleteFriend)

	//Get the subrouter for the GET method
	getSubrouter := serveMux.Methods(http.MethodGet).Subrouter()
	//Add the function to get the friend requests
	getSubrouter.HandleFunc("/friendrequest/view/{id:[0-9]+}", handlerFriendRequest.ViewFriendRequests)
	//Add the function to get the sent friend request
	getSubrouter.HandleFunc("/friendrequest/viewsent/{id:[0-9]+}", handlerFriendRequest.ViewSentFriendRequests)
	//Add the function to get the friends
	getSubrouter.HandleFunc("/friend/view/{id:[0-9]+}", handlerFriend.GetFriends)

	serverLogger.Info("Handlers have been added to the serve mux")

	server = &http.Server{
		Addr:         address,
		Handler:      serveMux,
		IdleTimeout:  120 * time.Second,
		WriteTimeout: 1 * time.Second,
		ReadTimeout:  1 * time.Second,
	}

	//Server finished initialization
	serverLogger.Info("Server finished initialization")
	return nil
}

func RunServer() error {
	//Check if the server was initialized before calling RunServer
	if server == nil || serverLogger == nil {
		return errors.New("server need to be initialized before running")
	}

	go func() {
		err := server.ListenAndServe()
		if err != nil {
			serverLogger.Error(err.Error())
			os.Exit(-1)
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
	serverLogger.Info("Received signal to terminate, exiting gracefully", sig)

	//Let the server finish the current connection in a timeout of 30 seconds then exit
	tc, _ := context.WithTimeout(context.Background(), 30*time.Second)
	server.Shutdown(tc)

	return nil
}
