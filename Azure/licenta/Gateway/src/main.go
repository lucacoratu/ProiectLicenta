package main

import (
	"willow/gateway/server"
)

func main() {
	err := server.InitServer(":8080")
	if err != nil {
		panic(err.Error())
	}

	err = server.RunServer()
	if err != nil {
		panic(err.Error())
	}
}
