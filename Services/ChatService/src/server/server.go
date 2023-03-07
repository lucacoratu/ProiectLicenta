package server

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"time"
	"willow/chatservice/data"
	"willow/chatservice/database"
	"willow/chatservice/handlers"
	"willow/chatservice/logging"
	"willow/chatservice/websocket"

	"github.com/gorilla/mux"
)

var serverLogger logging.ILogger
var server *http.Server = nil
var serverDb database.IConnection
var serverConfiguration *data.Configuration = nil

var configurationFile string = "chatservice.conf"

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
	serverConfiguration = &svc

	//Initialize the logger
	//serverLogger = logging.NewLogger(log.New(os.Stdout, "[*] - Friend Request Service - ", log.LstdFlags), "[INFO]", "[WARNING]", "[ERROR]")
	if svc.DebugEnabled {
		serverLogger = logging.NewDebugLogger(log.New(os.Stdout, "[*] - Chat Service - ", log.LstdFlags), svc.InfoPrefix, svc.WarningPrefix, svc.ErrorPrefix, svc.DebugPrefix)
	} else {
		serverLogger = logging.NewLogger(log.New(os.Stdout, "[*] - Chat Service - ", log.LstdFlags), svc.InfoPrefix, svc.WarningPrefix, svc.ErrorPrefix)
	}
	serverLogger.Info("Logger has been initialized")

	//Initialize the database connection
	serverDb = database.NewConnection(serverLogger, serverConfiguration)
	err = serverDb.InitializeConnection()
	if err != nil {
		return err
	}

	//The database connection is active
	serverLogger.Info("Database connection has been initialized")

	//Create the pool
	pool := websocket.NewPool(serverLogger, serverDb)
	go pool.Start()

	//Create the routes
	handlerChat := handlers.NewChat(serverDb, serverLogger)

	//Initialize the gorilla servemux
	serveMux := mux.NewRouter()
	serveMux.PathPrefix("/chat/groups/static/").Handler(http.StripPrefix("/chat/groups/static/", http.FileServer(http.Dir("./static/"))))
	//Create the subrouter that will handle POST methods
	getSubrouter := serveMux.Methods(http.MethodGet).Subrouter()
	getSubrouter.HandleFunc("/privaterooms/{id:[0-9]+}", handlerChat.GetPrivateRooms)
	getSubrouter.HandleFunc("/history/{id:[0-9]+}", handlerChat.GetRoomHistory)
	getSubrouter.HandleFunc("/chat/groups/{id:[0-9]+}", handlerChat.GetGroups)
	getSubrouter.HandleFunc("/history/{id:[0-9]+}/lastmessage", handlerChat.GetRoomLastMessage)
	getSubrouter.HandleFunc("/chat/commongroups/{idfirst:[0-9]+}/{idsecond:[0-9]+}", handlerChat.GetCommonGroups)
	serveMux.HandleFunc("/ws", func(rw http.ResponseWriter, r *http.Request) {
		handlerChat.ServeWs(pool, rw, r)
	})
	postSubrouter := serveMux.Methods(http.MethodPost).Subrouter()
	postSubrouter.HandleFunc("/privateroom/create", handlerChat.CreatePrivateRoom)
	postSubrouter.HandleFunc("/privateroom", handlerChat.GetRoomId)
	postSubrouter.HandleFunc("/chat/group/updatepicture", handlerChat.UpdateGroupPicture)

	serverLogger.Info("Handlers have been added to the serve mux")

	server = &http.Server{
		Addr:         svc.Address,
		Handler:      serveMux,
		IdleTimeout:  120 * time.Second,
		WriteTimeout: 60 * time.Second,
		ReadTimeout:  60 * time.Second,
	}

	//Server finished initialization
	serverLogger.Info("Server finished initialization - Listening on", svc.Address)
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
	serverLogger.Info("Service is connected to the database")

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
