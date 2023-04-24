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
	"willow/accountservice/logging"

	"github.com/gorilla/mux"
)

/* This variable will be used only locally in this package
 * It will be initialized with the handles that the server will support
 * It will have an idle timeout, read timeout and a write timeout that will be tweaked
 * It will initialize the logger that will be used in the application
 */
var server *http.Server = nil
var serverLogger logging.ILogger = nil
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
	//serverLogger = log.New(os.Stdout, "[*] - Account Service - ", log.LstdFlags)
	//serverLogger.Info("Logger initialized")

	//Initialize the logger
	serverLogger = logging.NewDebugLogger(log.New(os.Stdout, "[*] - Account Service - ", log.LstdFlags), "[INFO]", "[WARNINGS]", "[ERROR]", "[DEBUG]")
	serverLogger.Info("Logger has been initialized")

	//Create the connection object from the database package
	serverDbConn = database.NewConnection(serverLogger)
	err := serverDbConn.InitializeConnection()
	if err != nil {
		serverLogger.Info("Database connection initialization failed: " + err.Error())
		return err
	}

	//Create the handlers that the server will have (paths and functions to where the server will respond)
	//Create the /login handler
	handlerLogin := handlers.NewLogin(serverLogger, serverDbConn)

	//Create the /register handler and add the database handle to the struct
	handlerRegister := handlers.NewRegister(serverLogger, serverDbConn)

	//Create the /profile handler and add the database handle to the struct
	handlerProfile := handlers.NewProfile(serverLogger, serverDbConn)

	//Create the handler for friendrequests
	handlerFriendRequests := handlers.NewFriendRequest(serverLogger, serverDbConn)

	//Create the handler for friends
	handlerFriends := handlers.NewFriends(serverLogger, serverDbConn)

	//Create the feedback handler
	handlerFeedback := handlers.NewReportBug(serverLogger, serverDbConn)

	//Create the authentication middleware
	handlerAuth := handlers.NewAuthentication(serverLogger, serverDbConn)

	//Create the chat handler
	handlerChat := handlers.NewChat(serverLogger, serverDbConn)

	//Create the serve mux where the handlers will be assigned so can then be used by the http.Server object
	//serveMuxServer := http.NewServeMux()
	serveMuxServer := mux.NewRouter()

	//Create the methods that will handle login and register (this methods do not use authentication middleware)
	serveMuxServer.HandleFunc("/login", handlerLogin.LoginAccount).Methods("POST")
	serveMuxServer.HandleFunc("/register", handlerRegister.RegisterAccount).Methods("POST")
	//Add the function to get all the report bug categories
	serveMuxServer.HandleFunc("/accounts/reportcategories", handlerFeedback.GetBugReportCategories).Methods("GET")
	serveMuxServer.PathPrefix("/accounts/static/").Handler(http.StripPrefix("/accounts/static/", http.FileServer(http.Dir("./static/"))))
	serveMuxServer.PathPrefix("/chat/groups/static").HandlerFunc(handlerChat.GetGroupPicture)

	//Create the subrouter for the GET method which will use the Authentication middleware
	getRouter := serveMuxServer.Methods(http.MethodGet).Subrouter()
	getRouter.HandleFunc("/profile/{id:[0-9]+}", handlerProfile.ViewProfile)
	//Add the function to get the account status
	getRouter.HandleFunc("/status/{userId:[0-9]+}", handlerProfile.GetUserStatus)
	//Add the function to get the friend requests
	getRouter.HandleFunc("/friendrequest/view/{id:[0-9]+}", handlerFriendRequests.ViewFriendRequests)
	//Add the function to get the sent friend request
	getRouter.HandleFunc("/friendrequest/viewsent/{id:[0-9]+}", handlerFriendRequests.ViewSentFriendRequests)
	//Add the function to get the friendships
	getRouter.HandleFunc("/friend/view/{id:[0-9]+}", handlerFriends.GetFriends)
	//Add the function to get the friends with id greater than specified
	getRouter.HandleFunc("/friend/viewnew/{accountId:[0-9]+}/{lastId:[0-9]+}", handlerFriends.GetNewerFriends)
	//Add the function to get all the groups of the user
	getRouter.HandleFunc("/chat/groups/{id:[0-9]+}", handlerChat.GetGroups)
	//Add the function to get all the common groups of 2 users
	getRouter.HandleFunc("/chat/commongroups/{idFirst:[0-9]+}/{idSecond:[0-9]+}", handlerChat.GetCommonGroups)
	//Add the function to get the recommendations of a user for friends
	getRouter.HandleFunc("/account/{id:[0-9]+}/friendrecommendations", handlerFriendRequests.GetFriendsRecommendations)
	//Add the function to get the groups with room id greater than one specified
	getRouter.HandleFunc("/chat/groups/{id:[0-9]+}/{lastGroupId:[0-9]+}", handlerChat.GetGroupsWithId)

	getRouter.Use(handlerAuth.ValidateSessionCookie)

	//Create the subrouter for the PUT method which will use the Authentication middleware and will handle updates on the account data
	putRouter := serveMuxServer.Methods(http.MethodPut).Subrouter()
	putRouter.HandleFunc("/status", handlerProfile.UpdateStatusUnauthenticated)
	//putRouter.Use(handlerAuth.ValidateSessionCookie)

	//Create the subrouter for the POST method which will have the handler functions for the POST requests to specific routes
	postRouter := serveMuxServer.Methods(http.MethodPost).Subrouter()
	postRouter.HandleFunc("/friendrequest/add", handlerFriendRequests.AddFriendRequest)
	postRouter.HandleFunc("/friendrequest/delete", handlerFriendRequests.DeleteFriendRequest)
	postRouter.HandleFunc("/friend/add", handlerFriends.AddFriend)
	postRouter.HandleFunc("/friend/delete", handlerFriends.DeleteFriends)
	postRouter.HandleFunc("/accounts/reportbug", handlerFeedback.AddBugReport)
	postRouter.HandleFunc("/accounts/picture", handlerProfile.UpdateProfilePicture)
	postRouter.HandleFunc("/account/update/about", handlerProfile.UpdateAboutMessage)
	postRouter.HandleFunc("/chat/group/updatepicture", handlerChat.UpdateGroupPicture)
	postRouter.HandleFunc("/account/cansend/friendrequest", handlerFriends.CanSendFriendRequest)
	postRouter.Use(handlerAuth.ValidateSessionCookie)

	//Log that the handlers have been added
	//serverLogger.Info("Handlers added to the serve mux of the server")
	serverLogger.Info("Handlers added to the serve mux of the server")

	//Initialize the http.Server object
	server = &http.Server{
		Addr:         address,
		Handler:      serveMuxServer,
		ReadTimeout:  60 * time.Second,
		WriteTimeout: 60 * time.Second,
		IdleTimeout:  120 * time.Second,
	}

	//Log that the server finished initialization
	serverLogger.Info("Server has been initialized")
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

	serverLogger.Info("Testing the connection to the database server")
	//Try to ping the database server to check if the connection can be established
	errDb := serverDbConn.TestConnection()
	if errDb != nil {
		//The server should be able to connect to the database server in order to work
		serverLogger.Error(errDb.Error())
		return errors.New("server cannot connect to the database server")
	}
	//Close the connection when before this function returns
	defer serverDbConn.CloseConnection()
	//Log that the connection to the database server has been established
	serverLogger.Info("Database connection has been established")

	//Run the server in a go function so the channel for graceful exit can be created
	go func() {
		err := server.ListenAndServe()
		if err != nil {
			serverLogger.Error(err)
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
