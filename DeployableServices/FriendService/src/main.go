package main

import "willow/friendservice/server"

func main() {
	err := server.InitServer(":8084")
	if err != nil {
		panic(err.Error())
	}

	err = server.RunServer()
	if err != nil {
		panic(err.Error())
	}
}
