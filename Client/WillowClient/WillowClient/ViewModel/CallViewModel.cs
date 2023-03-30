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
using System.Text.Json;
using WillowClient.Services;
using Mopups.Services;
using WillowClient.ViewsPopups;

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
        private FriendStatusModel friend;

        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private int roomId;

        [ObservableProperty]
        private bool audio;

        [ObservableProperty]
        private bool video;

        private ChatService chatService;
        public CallViewModel(ChatService chat)
        {
            this.chatService = chat;
            //this.chatService.RegisterReadCallback(MessageReceivedOnCallWebSocket);
        }

        ~CallViewModel()
        {
            this.chatService.UnregisterReadCallback(MessageReceivedOnCallWebSocket);
        }

        private void DeleteCallback()
        {
            this.chatService.UnregisterReadCallback(MessageReceivedOnCallWebSocket);
        }

        public async Task MessageReceivedOnCallWebSocket(string message)
        {
            //This is where the message that the friend answered/canceled the call will be handled
            //The call has been closed by the caller

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            //Parse the message
            CallFriendModel cfm = JsonSerializer.Deserialize<CallFriendModel>(message, options);
            if (cfm != null)
            {
                if (cfm.option == "Cancel" && cfm.callee == this.Account.Id && cfm.caller == this.Friend.FriendId)
                {
                    //Go back to where the application was before the call
                    //Go to the CalleePage
                    this.DeleteCallback();
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                        await Shell.Current.Navigation.PopAsync()
                    );
                }
            }
        }

        public async void StartCalling()
        {
            if (this.IsBusy)
                return;

            this.IsBusy = true;
            CallFriendModel cfm = new CallFriendModel
            {
                roomId = this.friend.RoomID,
                caller = this.account.Id,
                callee = this.friend.FriendId,
                option = "Call"
            };
            string jsonMessage = JsonSerializer.Serialize(cfm);
            await this.chatService.SendMessageAsync(jsonMessage);
            this.IsBusy = false;
        }

        [RelayCommand]
        public async Task AnswerCall()
        {
            if (this.IsBusy)
                return;

            this.IsBusy = true;
            CallFriendModel cfm = new CallFriendModel
            {
                roomId = this.roomId,
                caller = this.account.Id,
                callee = this.friend.FriendId,
                option = "Answer"
            };
            string jsonMessage = JsonSerializer.Serialize(cfm);
            await this.chatService.SendMessageAsync(jsonMessage);
#if ANDROID
            await Shell.Current.GoToAsync(nameof(AndroidCallPage), true, new Dictionary<string, object>
            {
                {"roomID", cfm.roomId },
                {"account", account },
                {"friend", friend},
                {"audio", true},
                {"video", true },
            });
#else
        await Shell.Current.GoToAsync(nameof(WindowsCallPage), true, new Dictionary<string, object>
            {
                {"roomID", cfm.roomId },
                {"account", account },
                {"friend", friend},
                {"audio", true},
                {"video", true },
            });
#endif
        }

        [RelayCommand]
        public async Task DenyCall()
        {
            if (this.IsBusy)
                return;

            this.IsBusy = true;
            CallFriendModel cfm = new CallFriendModel
            {
                roomId = this.friend.RoomID,
                caller = this.account.Id,
                callee = this.friend.FriendId,
                option = "Deny"
            };
            string jsonMessage = JsonSerializer.Serialize(cfm);
            await this.chatService.SendMessageAsync(jsonMessage);
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Shell.Current.Navigation.PopAsync()
            );
            this.IsBusy = false;
        }

        [RelayCommand]
        public async Task EndCall()
        {
            if (this.IsBusy)
                return;

            this.IsBusy = true;
            CallFriendModel cfm = new CallFriendModel
            {
                caller = this.account.Id,
                callee = this.friend.FriendId,
                option = "Cancel"
            };
            string jsonMessage = JsonSerializer.Serialize(cfm);
            await this.chatService.SendMessageAsync(jsonMessage);
            //Send a notification through the websocket to close the call
            await Shell.Current.Navigation.PopAsync();
            this.IsBusy = false;
        }

        public async void TerminateCall(WebView webView)
        {
            if (this.IsBusy)
                return;

            this.IsBusy = true;
            //Invoke javascript code to leave the call
#if WINDOWS
            string result = await webView.EvaluateJavaScriptAsync("leaveCall()");
#else
            webView.Eval("leaveCall()");
#endif
            //Shell.Current.Navigation.RemovePage(Shell.Current.Navigation.NavigationStack[Shell.Current.Navigation.NavigationStack.Count - 2]);
            await Shell.Current.Navigation.PopAsync();
            await Shell.Current.Navigation.PopAsync();
            this.IsBusy = false;

            //Ask the user for feedback
#if ANDROID
            await MopupService.Instance.PushAsync(new ReviewCallPopup(new CallFeedbackViewModel()), true);
#endif
        }

        public async void TerminateGroupCall(WebView webView) {
            if (this.IsBusy)
                return;

            this.IsBusy = true;
            //Invoke javascript code to leave the call

            var script = """
               console.log(ws);
               """;
#if WINDOWS
            //string result = "null";
            //while(result == "null")
            await webView.EvaluateJavaScriptAsync("leaveCall()");
#else
            webView.Eval(script);
#endif
            await Shell.Current.Navigation.PopAsync();
            this.IsBusy = false;
        }

        public void InitializeCall(WebView webView)
        {
            //TO DO... Send details about the call to the signaling service
#if ANDROID
            webView.Source = Constants.signalingServerURL + "room?roomID=" + this.roomId.ToString() + "&audio=" + this.audio.ToString() + "&video=" + this.video.ToString() + "&platform=android";
#else
            webView.Source = Constants.signalingServerURL + "room?roomID=" + this.roomId.ToString() + "&audio=" + this.audio.ToString() + "&video=" + this.video.ToString() + "&platform=windows";  
#endif
        }

        public void InitializeGroupCall(WebView webView) {
            //TO DO... Send details about the call to the signaling service
#if ANDROID
            webView.Source = Constants.signalingServerURL + "group?roomID=" + this.roomId.ToString() + "&audio=" + this.audio.ToString() + "&video=" + this.video.ToString() + "&platform=android" + "&accountID=" + this.Account.Id.ToString();
#else
            webView.Source = Constants.signalingServerURL + "group?roomID=" + this.roomId.ToString() + "&audio=" + this.audio.ToString() + "&video=" + this.video.ToString() + "&platform=windows" + "&accountID=" + this.Account.Id.ToString();
#endif
        }
    }
}
