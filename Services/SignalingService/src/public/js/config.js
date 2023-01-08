turnConfig = {
    iceServers: [
        { 
            urls: ["stun:fr-turn1.xirsys.com"] 
        },
        { 
            username: "t3cT5hbSE1SYPBHFkUTM28EF4Ep4j4C_sVyxdQ0sqS0DeyLxOBQATvSoPZ8XMaaYAAAAAGNiEaNsdWNhY29yYXR1", 
            credential: "b3ac54b0-5a79-11ed-864b-0242ac120004", 
            urls: ["turn:fr-turn1.xirsys.com:80?transport=udp", "turn:fr-turn1.xirsys.com:3478?transport=udp", "turn:fr-turn1.xirsys.com:80?transport=tcp", "turn:fr-turn1.xirsys.com:3478?transport=tcp", "turns:fr-turn1.xirsys.com:443?transport=tcp", "turns:fr-turn1.xirsys.com:5349?transport=tcp"] 
        }
    ]
}