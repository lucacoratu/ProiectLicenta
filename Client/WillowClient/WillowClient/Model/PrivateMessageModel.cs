﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class PrivateMessageModel
    {
        public int RoomId { get; set; }
        public string Data { get; set; }
        public string MessageType { get; set; }
        public int SenderId { get; set; }
    }
}