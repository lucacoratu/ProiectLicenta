using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public partial class GroupModel : ObservableObject
    {
        public int RoomId { get; set; }
	    public int CreatorId { get; set; }
	    public string GroupName { get; set; }
        private string lastMessage;
        public string LastMessage
        {
            get => lastMessage;
            set => SetProperty(ref lastMessage, value);
        }

        private string lastMessageTimestamp;
        public string LastMessageTimestamp
        {
            get => lastMessageTimestamp;
            set => SetProperty(ref lastMessageTimestamp, value);
        }
        public List<int> Participants { get; set; }
        public int LastMessageSender { get; set; }

        public List<string> ParticipantNames { get; set; }
        private string groupPictureUrl;
        public string GroupPictureUrl {
            get => groupPictureUrl;
            set => SetProperty(ref groupPictureUrl, value);
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
    }
}
