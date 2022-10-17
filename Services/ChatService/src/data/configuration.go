package data

import (
	"encoding/json"
	"errors"
	"io"
)

/*
 * This structure will hold the configuration data for a service that this service will comunicate with
 * It will have the name of the service and the url it has.
 */
type ServiceConfiguration struct {
	Name string `json:"name" validate:"required"`
	URL  string `json:"url" validate:"required"`
}

/*
 * This function will convert the ServiceConfiguration structure to JSON format
 */
func (svc *ServiceConfiguration) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(svc)
}

/*
 * This function will convert a json to ServiceConfiguration structure
 */
func (svc *ServiceConfiguration) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(svc)
}

//TO DO...Add the validate function for this structure

/*
 * This structure will hold the configuration data needed by this service to run
 * It will have the ip (or name ) and port of the database, the username and password
 * of the database server and the database name
 */
type Configuration struct {
	//Address of the server
	Address string `json:"address" validate:"required"`
	//Timeout configuration settings
	IdleTimeout    int `json:"idleTimeout" validate:"required"`
	WriteTimeout   int `json:"writeTimeout" validate:"required"`
	ReadTimeout    int `json:"readTimeout" validate:"required"`
	ContextTimeout int `json:"contextTimeout" validate:"required"`
	//Database configuration settings
	UsesDatabase bool   `json:"usesDatabase" validate:"required"`
	DbUser       string `json:"dbUsername" validate:"required"`
	DbPassword   string `json:"dbPassword" validate:"required"`
	DbHost       string `json:"dbHost" validate:"required"`
	DbName       string `json:"dbName" validate:"required"`
	//Logger configuration settings
	DebugEnabled bool `json:"debugEnabled" validate:"required"`
	//Logger prefix configuration
	InfoPrefix    string `json:"infoPrefix"`
	WarningPrefix string `json:"warningPrefix"`
	ErrorPrefix   string `json:"errorPrefix"`
	DebugPrefix   string `json:"debugPrefix"`
	//The services it communicates with configuration
	Services []ServiceConfiguration `json:"services" validate:"required"`
}

/*
 * This function will convert the Configuration structure to JSON format
 */
func (conf *Configuration) ToJSON(w io.Writer) error {
	e := json.NewEncoder(w)
	return e.Encode(conf)
}

/*
 * This function will convert from JSON format to Configuration structure
 */
func (conf *Configuration) FromJSON(r io.Reader) error {
	d := json.NewDecoder(r)
	return d.Decode(conf)
}

/*
 * This function will get the url of a service based on the name received as a parameter
 */
func (conf *Configuration) GetServiceURL(name string) (string, error) {
	for _, service := range conf.Services {
		if service.Name == name {
			return service.URL, nil
		}
	}
	return "", errors.New("service name could not be found")
}
