﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class FriendModel : ObservableObject
    {
        public int FriendId { get; set; }
        public string DisplayName { get; set; }
        public string BefriendDate { get; set; }
        public string LastOnline { get; set; }
        public string JoinDate { get; set; }
        public int RoomID { get; set; }
        private string lastMessage;
        public string LastMessage
        {
            get => lastMessage;
            set => SetProperty(ref lastMessage, value);
        }
    }
}
