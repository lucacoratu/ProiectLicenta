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
    }
}
