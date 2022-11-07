using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.ViewModel;
using WillowClient.Views;
using Microsoft.Maui.Media;
using WillowClient.Services;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(Friend), "friend")]
    [QueryProperty(nameof(Account), "account")]
    public partial class ChatViewModel : BaseViewModel
    {
        [ObservableProperty]
        private FriendModel friend;

        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private string messageText;

        public ObservableCollection<MessageModel> Messages { get; } = new();

        private int roomId;

        private ChatService chatService;
        public ChatViewModel(ChatService chatService)
        {
            this.chatService = chatService;
            this.chatService.RegisterReadCallback(MessageReceivedOnWebsocket);
        }

        public async Task MessageReceivedOnWebsocket(string message)
        {
            //Check if the message is from the user in the current conversation
            //Parse the JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            WebSocketMessageModel? websocketMessage = JsonSerializer.Deserialize<WebSocketMessageModel>(message, options);
            if (websocketMessage != null)
            {
                try
                {
                    //Parse the JSON body of the message
                    PrivateMessageModel? privMessageModel = JsonSerializer.Deserialize<PrivateMessageModel>(message, options);
                    if (privMessageModel != null)
                    {
                        //This is a private message received from another user
                        //Check if the sender is the current user from the private conversation
                        if (privMessageModel.SenderId == this.friend.FriendId)
                        {
                            //This is the friend that sent the message
                            this.Messages.Add(new MessageModel
                            {
                                Owner = MessageOwner.OtherUser,
                                Text = privMessageModel.Data,
                                TimeStamp = DateTime.Now.ToString("HH:mm:ss tt")
                            });
                            return;
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }
            return;
        }
        
        public async void GetHistory()
        {
            //If there are any elements in the list then clear it
            if (Messages.Count != 0)
                Messages.Clear();

            var historyMessages = await this.chatService.GetMessageHistory(this.roomId);
            foreach(var historyMessage in historyMessages)
            {
                //Create the MessageModel list
                if (historyMessage.UserId != this.Account.Id)
                    this.Messages.Add(new MessageModel 
                    { 
                        Owner = MessageOwner.OtherUser,
                        Text = historyMessage.Data,
                        TimeStamp = historyMessage.SendDate 
                    });
                else
                    this.Messages.Add(new MessageModel 
                    {
                        Owner = MessageOwner.CurrentUser,
                        Text = historyMessage.Data,
                        TimeStamp = historyMessage.SendDate
                    });
            }
        }

        public async void GetRoomId()
        {
            //Get the id of the room knowing the id of the account and the friend id
            var getRoomModel = new GetRoomIdModel { AccountId = account.Id, FriendId = friend.FriendId};
            var res = await this.chatService.GetRoomId(getRoomModel);
            //Parse the JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            GetRoomIdResponseModel? roomIdModel = JsonSerializer.Deserialize<GetRoomIdResponseModel>(res, options);
            if (roomIdModel != null)
            {
                this.roomId = roomIdModel.RoomId;
            }

            this.GetHistory();
        }

        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.Navigation.PopAsync(true);
        }

        [RelayCommand]
        public async void TakePhoto()
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

                if (photo != null)
                {
                    // save the file into local storage
                    string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

                    using Stream sourceStream = await photo.OpenReadAsync();
                    using FileStream localFileStream = File.OpenWrite(localFilePath);

                    await sourceStream.CopyToAsync(localFileStream);
                }
            }
        }

        [RelayCommand]
        public async Task PickFile()
        {
            var res = await FilePicker.PickMultipleAsync(new PickOptions { PickerTitle = "Select file(s)", FileTypes = FilePickerFileType.Images });
            if (res == null)
                return;
        }

        [RelayCommand]
        public async Task SendMessage()
        {
            //Create the structure that will hold the data which will be json encoded and sent to the server
            SendPrivateMessageModel sendMessageModel = new SendPrivateMessageModel { roomId = this.roomId, data = this.MessageText, messageType = "Text"};
            string jsonMessage = JsonSerializer.Serialize(sendMessageModel);
            this.chatService.SendMessageAsync(jsonMessage);
            //Add the message into the collection view for the current user
            this.Messages.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = this.MessageText, TimeStamp = DateTime.Now.ToString("HH:mm:ss tt") });
            //Clear the entry text
            this.MessageText = "";
            //Scroll to the end
        }
    }
}
