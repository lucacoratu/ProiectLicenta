using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.ViewModel;
using WillowClient.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WillowClient.Services;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(Friend), "friend")]
    [QueryProperty(nameof(Account), "account")]
    [QueryProperty(nameof(RoomId), "roomID")]
    [QueryProperty(nameof(Audio), "audio")]
    [QueryProperty(nameof(Video), "video")]
    public partial class CallViewModel : BaseViewModel
    {
        [ObservableProperty]
        private FriendModel friend;

        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private int roomId;

        [ObservableProperty]
        private bool audio;

        [ObservableProperty]
        private bool video;

        public CallViewModel()
        {

        }

        public void InitializeCall(WebView webView)
        {
            //TO DO... Send details about the call to the signaling service
            webView.Source = "http://localhost:8090/room?roomID=" + this.roomId.ToString() + "&audio=" + this.audio.ToString() + "&video=" + this.video.ToString();
        }
    }
}
