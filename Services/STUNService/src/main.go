package main

import (
	"flag"
	"log"
	"net"

	"github.com/pion/stun"
)

func main() {
	realm := flag.String("realm", "pion.ly", "STUN server realm")
	flag.Parse()

	server := &stun.Server{Realm: *realm}

	l, err := net.Listen("tcp", ":3478")
	if err != nil {
		log.Fatal(err)
	}
	defer l.Close()
	log.Printf("STUN server started on %v", l.Addr())

	for {
		conn, err := l.Accept()
		if err != nil {
			log.Fatal(err)
		}

		go func(conn net.Conn) {
			if err := server.Serve(conn); err != nil {
				log.Printf("Failed to serve connection: %v", err)
			}
		}(conn)
	}
}
