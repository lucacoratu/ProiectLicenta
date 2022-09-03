package main

import (
	server "willow/accountservice/server"
)

func main() {
	err := server.InitServer(":8080")
	if err != nil {
		panic(err)
	}

	err = server.RunServer()
	if err != nil {
		panic(err)
	}
}
