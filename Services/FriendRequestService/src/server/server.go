package server

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"time"
	"willow/friendrequestservice/data"
	"willow/friendrequestservice/database"
	"willow/friendrequestservice/handlers"
	"willow/friendrequestservice/logging"

	"github.com/gorilla/mux"
)

var serverLogger logging.ILogger
var server *http.Server = nil
var serverDb *database.Connection = nil

var configurationFile string = "test.conf"

func InitServer(address string) error {
	//Check if the server has not been initialized before
	if server != nil {
		return errors.New("cannot initialize the server twice")
	}

	//Get the configuration data from the configuration file
	file, err := os.OpenFile(configurationFile, os.O_RDWR, 0644)
	if err != nil {
		return err
	}
	defer file.Close()

	svc := data.Configuration{}
	err = svc.FromJSON(file)
	if err != nil {
		log.Println("Cannot initialize the configuration settings from file")
		return err
	}

	//Initialize the logger
	//serverLogger = logging.NewLogger(log.New(os.Stdout, "[*] - Friend Request Service - ", log.LstdFlags), "[INFO]", "[WARNING]", "[ERROR]")
	if svc.DebugEnabled {
		serverLogger = logging.NewDebugLogger(log.New(os.Stdout, "[*] - Friend Request Service - ", log.LstdFlags), svc.InfoPrefix, svc.WarningPrefix, svc.ErrorPrefix, svc.DebugPrefix)
	} else {
		serverLogger = logging.NewLogger(log.New(os.Stdout, "[*] - Friend Request Service - ", log.LstdFlags), svc.InfoPrefix, svc.WarningPrefix, svc.ErrorPrefix)
	}
	serverLogger.Info("Logger has been initialized")

	//Initialize the database connection
	serverDb = database.NewConnection(serverLogger)
	err = serverDb.InitializeConnection()
	if err != nil {
		return err
	}

	//The database connection is active
	serverLogger.Info("Database connection has been initialized")

	//Create the routes
	handlerFriendRequest := handlers.NewFriendRequest(serverLogger, serverDb)

	//Initialize the gorilla servemux
	serveMux := mux.NewRouter()
	//Create the subrouter that will handle POST methods
	postSubrouter := serveMux.Methods(http.MethodPost).Subrouter()
	//Add the handle function which will add a friend request in the database
	postSubrouter.HandleFunc("/add", handlerFriendRequest.AddFriendRequest)
	//Add the handle function which will delete a friend request from the database
	postSubrouter.HandleFunc("/delete", handlerFriendRequest.DeleteFriendRequest)
	//Add the handle function which will return if the accounts have a friend request
	postSubrouter.HandleFunc("/arefriends", handlerFriendRequest.ExistsFriendRequest)

	//Create the subrouter that will handle GET methods
	getSubrouter := serveMux.Methods(http.MethodGet).Subrouter()
	getSubrouter.HandleFunc("/view/{id:[0-9]+}", handlerFriendRequest.GetFriendRequests)
	//Add the function to view the friend requests sent by the user
	getSubrouter.HandleFunc("/viewsent/{id:[0-9]+}", handlerFriendRequest.GetSentFriendRequests)

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

	//Test the connection to the database
	err := serverDb.TestConnection()
	if err != nil {
		return err
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
