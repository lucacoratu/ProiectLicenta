using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.Services;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(Group), "group")]
    [QueryProperty(nameof(Account), "account")]
    public partial class GroupChatViewModel : BaseViewModel
    {
        [ObservableProperty]
        private GroupModel group;

        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private string messageText;
        public ObservableCollection<MessageModel> Messages { get; } = new();

        private ChatService chatService;

        public GroupChatViewModel(ChatService chatService)
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
                        if (privMessageModel.SenderId != this.Account.Id)
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
                catch (Exception e)
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

            var historyMessages = await this.chatService.GetMessageHistory(this.Group.RoomId);
            foreach (var historyMessage in historyMessages)
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
            SendPrivateMessageModel sendMessageModel = new SendPrivateMessageModel { roomId = this.Group.RoomId, data = this.MessageText, messageType = "Text" };
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
