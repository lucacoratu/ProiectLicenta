using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Database.Model;

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
        public string About { get; set; }

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

        private string profilePictureUrl;
        public string ProfilePictureUrl {
            get => profilePictureUrl;
            set => SetProperty(ref profilePictureUrl, value);
        }

        private int numberNewMessages = 0;
        public string NumberNewMessages {
            get => numberNewMessages.ToString();
            set => SetProperty(ref numberNewMessages, int.Parse(value));
        }

        private bool hasNewMessages = false;

        public bool HasNewMessages {
            get => hasNewMessages;
            set => SetProperty(ref hasNewMessages, value);
        }

        public void IncrementNumberNewMessages() {
            int newMessages = numberNewMessages + 1;
            NumberNewMessages = newMessages.ToString();
            HasNewMessages = true;
        }

        public void SeenAllNewMessages() {
            NumberNewMessages = "0";
            HasNewMessages = false;
        }

        public string IdentityPublicKey { get; set; }
        public string PreSignedPublicKey { get; set; }

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
            ProfilePictureUrl = f.ProfilePictureUrl;
            IdentityPublicKey = f.IdentityPublicKey;
            PreSignedPublicKey = f.PreSignedPublicKey;
            this.statusBackgroundColor = statusBackgroundColor;
            this.statusStrokeColor = statusStrokeColor;
        }

        public FriendStatusModel(Friend f, Color statusBackgroundColor, Color statusStrokeColor) {
            FriendId = f.Id;
            DisplayName = f.DisplayName;
            BefriendDate = f.BefriendDate.ToString();
            LastOnline = f.LastOnline.ToString();
            Status = f.Status;
            JoinDate = f.JoinDate.ToString();
            RoomID = f.RoomID;
            LastMessageTimestamp = f.LastMessageTimestamp.ToString();
            LastMessage = f.LastMessage;
            ProfilePictureUrl = f.ProfilePictureUrl;
            IdentityPublicKey = f.IdentityPublicKey;
            PreSignedPublicKey = f.PreSignedPublicKey;
            this.statusBackgroundColor = statusBackgroundColor;
            this.statusStrokeColor = statusStrokeColor;
        }
    }
}
