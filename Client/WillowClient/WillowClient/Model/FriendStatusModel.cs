using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class FriendStatusModel : ObservableObject
    {
        public int FriendId { get; set; }
        public string DisplayName { get; set; }
        public string BefriendDate { get; set; }
        private string lastOnline;
        public string LastOnline
        {
            get => lastOnline;
            set => SetProperty(ref lastOnline, value);
        }
        public string Status { get; set; }
        public string JoinDate { get; set; }
        public int RoomID { get; set; }

        private Color statusBackgroundColor;
        private Color statusStrokeColor;

        public Color StatusBackgroundColor
        {
            get => statusBackgroundColor; 
            set => SetProperty(ref statusBackgroundColor, value);
        }

        public Color StatusStrokeColor
        {
            get => statusStrokeColor;
            set => SetProperty(ref statusStrokeColor, value);
        }

        private string lastMessageTimestamp;
        public string LastMessageTimestamp
        {
            get => lastMessageTimestamp;
            set => SetProperty(ref lastMessageTimestamp, value);
        }

        private string lastMessage;

        public string LastMessage
        {
            get => lastMessage;
            set => SetProperty(ref lastMessage, value);
        }

        public FriendStatusModel(FriendModel f, Color statusBackgroundColor, Color statusStrokeColor)
        {
            FriendId = f.FriendId;
            DisplayName = f.DisplayName;
            BefriendDate = f.BefriendDate;
            LastOnline = f.LastOnline;
            Status = f.Status;
            JoinDate = f.JoinDate;
            RoomID = f.RoomID;
            LastMessageTimestamp = f.LastMessageTimestamp;
            LastMessage = f.LastMessage;
            this.statusBackgroundColor = statusBackgroundColor;
            this.statusStrokeColor = statusStrokeColor;
        }
    }
}
