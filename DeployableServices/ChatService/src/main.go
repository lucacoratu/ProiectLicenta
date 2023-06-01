package main

import "willow/chatservice/server"

func main() {
	err := server.InitServer(":8083")
	if err != nil {
		panic(err.Error())
	}

	err = server.RunServer()
	if err != nil {
		panic(err.Error())
	}
}
