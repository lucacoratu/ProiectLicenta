package main

import "willow/sessionservice/server"

func main() {
	err := server.InitServer(":8082")
	if err != nil {
		panic(err.Error())
	}

	err = server.RunServer()
	if err != nil {
		panic(err.Error())
	}
}
