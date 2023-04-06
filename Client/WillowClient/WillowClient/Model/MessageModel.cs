using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public enum MessageOwner {
        CurrentUser,
        OtherUser
    }

    public enum MessageType {
        Text,
        Photo,
        Video,
        File,
        Pdf
    }

    public partial class MessageModel : ObservableObject {
        public MessageOwner Owner { get; set; }
        private MessageType messageType;
        public MessageType MessageType {
            get => messageType;
            set => SetProperty(ref messageType, value);
        }

        public string MessageId { get; set; }
        public string TimeStamp { get; set; }
        public string Text { get; set; }
        public string SenderName { get; set; }
        private ImageSource mediaStream;
        public ImageSource MediaStream {
            get => mediaStream;
            set => SetProperty(ref mediaStream, value);
        }

        private bool isDownloaded;
        public bool IsDownloaded {
            get => isDownloaded;
            set => SetProperty(ref isDownloaded, value);
        }
        private bool isLoading;
        public bool IsLoading {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        private bool isDownloading;
        public bool IsDownloading {
            get => isDownloading;
            set => SetProperty(ref isDownloading, value);
        }

        public bool IsNotDownloading {
            get => !isDownloading;
        }

        private string filename;
        public string Filename {
            get => filename;
            set => SetProperty(ref filename, value);
        }

        public ObservableCollection<ReactionModel> Reactions { get; } = new();
    }
}
