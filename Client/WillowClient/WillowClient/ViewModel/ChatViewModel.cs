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
using System.Collections;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(Friend), "friend")]
    [QueryProperty(nameof(Account), "account")]
    public partial class ChatViewModel : BaseViewModel
    {
        [ObservableProperty]
        private FriendStatusModel friend;

        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private string messageText;

        [ObservableProperty]
        private string lastOnlineText = "Last Online:";

        //public ObservableCollection<MessageModel> Messages { get; } = new();
        public List<MessageModel> Messages { get; } = new();

        [ObservableProperty]
        private bool entryEnabled = true;

        public ObservableCollection<MessageGroupModel> MessageGroups { get; } = new();

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
            //It is a message specifing the new status of a user
            if (message.IndexOf("Change status") != -1)
            {
                var options1 = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                //Parse the response
                ChangeStatusWebsocketResponseModel resp = JsonSerializer.Deserialize<ChangeStatusWebsocketResponseModel>(message, options1);
                //Search through the friend accounts and see if the message that the account involved is in the list of friends
                //Change the color depending on which status it is set
                if (this.Friend.FriendId == resp.AccountId)
                {
                    if (resp.NewStatus == "Online")
                    {
                        this.LastOnlineText = "Online";
                        this.Friend.LastOnline = "";
                    }
                    else
                    {
                        this.LastOnlineText = "Last Online: ";
                        this.Friend.LastOnline = DateTime.Now.ToString("f");
                    }
                }
                return;
            }

            //Some friend is calling
            if (message.IndexOf("callee") != -1)
            {
                //var options1 = new JsonSerializerOptions
                //{
                //    PropertyNameCaseInsensitive = true,
                //};

                ////Parse the response
                //CallFriendModel cfm = JsonSerializer.Deserialize<CallFriendModel>(message, options1);
                ////Search through all the friends and find the one that is calling
                //if (cfm != null)
                //{
                //    if (cfm.option == "Call")
                //    {
                //        if (this.Friend.FriendId == cfm.caller)
                //        {
                //            //Go to the CalleePage
                //            await MainThread.InvokeOnMainThreadAsync(async () =>
                //                await Shell.Current.GoToAsync(nameof(CalleePage), true, new Dictionary<string, object>
                //                {
                //                {"roomID", this.Friend.RoomID},
                //                {"account", this.Account},
                //                {"friend", this.Friend},
                //                {"audio", false },
                //                {"video", false },
                //                })
                //            );
                //        }
                //    }
                //}
                return;
            }

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
                            //this.Messages.Add(new MessageModel
                            //{
                            //    Owner = MessageOwner.OtherUser,
                            //    Text = privMessageModel.Data,
                            //    TimeStamp = DateTime.Now.ToString("HH:mm")
                            //});
                            foreach (var e in this.MessageGroups)
                            {
                                if (e.Name == "Today")
                                {
                                    e.Add(new MessageModel { Owner = MessageOwner.OtherUser, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm") });
                                    //e.Name = "Today";
                                }
                            }
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

            Dictionary<string, List<MessageModel>> groupsAndMessages = new();

            var historyMessages = await this.chatService.GetMessageHistory(this.roomId);
            foreach(var historyMessage in historyMessages)
            {
                //Create the MessageModel list
                if (historyMessage.UserId != this.Account.Id)
                {
                    //Convert date to a cleaner format
                    var messageDateString = DateTime.Parse(historyMessage.SendDate);
                    double diffDays = (DateTime.Now - messageDateString).TotalDays;
                    var msgDate = messageDateString.ToString("HH:mm");
                    string group = "";
                    if (diffDays < 1.0)
                        group = "Today";
                    else
                    {
                        if (diffDays >= 1.0 && diffDays < 2.0)
                            group = "Yesterday";
                        else
                        {
                            group = messageDateString.ToString("dd MMMM yyyy");
                        }
                    }

                    //TODO...Check if the group exists, if not then create it and add the message to it, else add the message to the existing group
                    if (!groupsAndMessages.ContainsKey(group))
                        groupsAndMessages.Add(group, new List<MessageModel>());
                    groupsAndMessages[group].Add(new MessageModel { Owner = MessageOwner.OtherUser, Text = historyMessage.Data, TimeStamp = msgDate });
                    
                    this.Messages.Add(new MessageModel
                    {
                        Owner = MessageOwner.OtherUser,
                        Text = historyMessage.Data,
                        TimeStamp = msgDate,
                    });

                }
                else
                {
                    var messageDateString = DateTime.Parse(historyMessage.SendDate);
                    double diffDays = (DateTime.Now - messageDateString).TotalDays;
                    var msgDate = messageDateString.ToString("HH:mm");
                    string group = "";
                    if (diffDays < 1.0)
                        group = "Today";
                    else
                    {
                        if (diffDays >= 1.0 && diffDays < 2.0)
                            group = "Yesterday";
                        else
                        {
                            group = messageDateString.ToString("dd MMMM yyyy");
                        }
                    }
                    //TODO...Check if the group exists, if not then create it and add the message to it, else add the message to the existing group
                    if (!groupsAndMessages.ContainsKey(group))
                        groupsAndMessages.Add(group, new List<MessageModel>());
                    groupsAndMessages[group].Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = historyMessage.Data, TimeStamp = msgDate });

                    this.Messages.Add(new MessageModel
                    {
                        Owner = MessageOwner.CurrentUser,
                        Text = historyMessage.Data,
                        TimeStamp = msgDate,
                    });
                }
            }
            //this.MessageGroups.Add(new MessageGroupModel("Today", this.Messages));
            foreach(var e in groupsAndMessages)
            {
                this.MessageGroups.Add(new MessageGroupModel(e.Key as string, e.Value as List<MessageModel>));
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

            //Initialize the last online text
            if(this.friend.Status == "Online")
            {
                this.LastOnlineText = "Online";
                this.Friend.LastOnline = "";
            }
            this.GetHistory();
        }

        [RelayCommand]
        public async Task GoBack()
        {
            this.EntryEnabled = false;
            System.Threading.Thread.Sleep(1000);
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
            chatService.SendMessageAsync(jsonMessage);
            //Add the message into the collection view for the current user
            bool found = false;
            foreach(var e in this.MessageGroups)
            {
                if (e.Name == "Today")
                {
                    e.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = this.MessageText, TimeStamp = DateTime.Now.ToString("HH:mm") });
                    //e.Name = "Today";
                    found = true;
                }
            }
            //If the group Today is not found (no messages were exchanges in current they) then create the group and add the message in the group
            if (!found)
            {
                List<MessageModel> messages = new();
                messages.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = this.MessageText, TimeStamp = DateTime.Now.ToString("HH:mm") });
                this.MessageGroups.Add(new MessageGroupModel("Today", messages));
            }
            //this.Messages.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = this.MessageText, TimeStamp = DateTime.Now.ToString("HH:mm") });
            //Clear the entry text
            this.MessageText = "";
            //Scroll to the end
        }

        [RelayCommand]
        public async Task CallPeer()
        {
            await Shell.Current.GoToAsync(nameof(CallerPage), true, new Dictionary<string, object>
            {
                {"roomID", roomId },
                {"account", account },
                {"friend", friend},
                {"audio", true},
                {"video", false },
            });

//#if ANDROID
//            await Shell.Current.GoToAsync(nameof(AndroidCallPage), true, new Dictionary<string, object>
//                {
//                    {"roomID", roomId },
//                    {"account", account },
//                    {"friend", friend},
//                    {"audio", true},
//                    {"video", false },
//                });
//#else
//            await Shell.Current.GoToAsync(nameof(WindowsCallPage), true, new Dictionary<string, object>
//                {
//                    {"roomID", roomId },
//                    {"account", account },
//                    {"friend", friend},
//                    {"audio", true},
//                    {"video", false },
//                });
//#endif
        }

        [RelayCommand]
        public async Task VideoCallPeer()
        {
            await Shell.Current.GoToAsync(nameof(CallerPage), true, new Dictionary<string, object>
            {
                {"roomID", roomId },
                {"account", account },
                {"friend", friend},
                {"audio", true},
                {"video", false },
            });

            //#if ANDROID
            //            await Shell.Current.GoToAsync(nameof(AndroidCallPage), true, new Dictionary<string, object>
            //                {
            //                    {"roomID", roomId },
            //                    {"account", account },
            //                    {"friend", friend},
            //                    {"audio", true},
            //                    {"video", true },
            //                });
            //#else
            //            await Shell.Current.GoToAsync(nameof(WindowsCallPage), true, new Dictionary<string, object>
            //                {
            //                    {"roomID", roomId },
            //                    {"account", account },
            //                    {"friend", friend},
            //                    {"audio", true},
            //                    {"video", true },
            //                });
            //#endif
        }

        [RelayCommand]
        public async Task GoToUserProfile() {
            await Shell.Current.GoToAsync(nameof(UserProfilePage), true, new Dictionary<string, object> {
                { "userId", this.Friend.FriendId },
                { "account", account }
            });
        }
    }
}
