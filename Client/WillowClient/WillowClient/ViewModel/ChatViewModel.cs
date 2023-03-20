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
        private bool loadingMessages;

        [ObservableProperty]
        private bool noMessages;

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

            //Check if the message is new reaction
            if(message.IndexOf("emojiReaction") != -1) {
                var options1 = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true,
                };
                try {
                    //Parse the JSON response
                    SendReactionModel srm = JsonSerializer.Deserialize<SendReactionModel>(message, options1);
                    if (srm != null) {
                        //Add the reaction to the specific message
                        //Add a new reaction in the collection view
                        foreach (var group in this.MessageGroups) {
                            foreach (var messageModel in group) {
                                if (Int32.Parse(messageModel.MessageId) == srm.messageId)
                                    messageModel.Reactions.Add(new ReactionModel { Id = 0, Emoji = srm.emojiReaction, ReactionDate = DateTime.Now.ToString("dd MMMM yyyy"), SenderId = srm.senderId});
                            }
                        }
                        return;
                    }
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }

            //Parse the JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            try
            {
                //Parse the JSON body of the message
                PrivateMessageModel? privMessageModel = JsonSerializer.Deserialize<PrivateMessageModel>(message, options);
                if (privMessageModel != null)
                {
                    if (privMessageModel.RoomId == this.roomId) {
                        //This is a private message received from another user
                        //Check if the sender is the current user from the private conversation
                        if (privMessageModel.SenderId == this.friend.FriendId) {
                            //This is the friend that sent the message
                            //this.Messages.Add(new MessageModel
                            //{
                            //    Owner = MessageOwner.OtherUser,
                            //    Text = privMessageModel.Data,
                            //    TimeStamp = DateTime.Now.ToString("HH:mm")
                            //});
                            bool added = false;
                            MessageModel msgModel = new MessageModel { Owner = MessageOwner.OtherUser, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privMessageModel.Id.ToString() };
                            foreach (var e in this.MessageGroups) {
                                if (e.Name == "Today") {
                                    e.Add(msgModel);
                                    //e.Name = "Today";
                                    added = true;
                                }
                            }
                            if (!added) {
                                //Create the group named today and add the message to the group
                                List<MessageModel> messageModels = new List<MessageModel>();
                                messageModels.Add(msgModel);
                                this.MessageGroups.Add(new MessageGroupModel("Today", messageModels));
                            }
                            return;
                        } else {
                            ////He is the one that sent the message so update the message id
                            //foreach(var e in this.MessageGroups) {
                            //    if(e.Name == "Today") {
                            //        foreach(var m in e) {
                            //            if(m.MessageId == "-1" && m.Text == privMessageModel.Data) {
                            //                m.MessageId = privMessageModel.Id.ToString();
                            //            }
                            //        }
                            //    }
                            //}
                            //Add the message into the collection view for the current user
                            bool found = false;
                            foreach (var e in this.MessageGroups) {
                                if (e.Name == "Today") {
                                    e.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privMessageModel.Id.ToString() });
                                    //e.Name = "Today";
                                    found = true;
                                }
                            }
                            //If the group Today is not found (no messages were exchanges in current they) then create the group and add the message in the group
                            if (!found) {
                                List<MessageModel> messages = new();
                                messages.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privMessageModel.Id.ToString() });
                                this.MessageGroups.Add(new MessageGroupModel("Today", messages));
                            }
                            return;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return;
        }
        
        public async Task GetHistory()
        {
            //If there are any elements in the list then clear it
            if (Messages.Count != 0)
                Messages.Clear();

            Dictionary<string, List<MessageModel>> groupsAndMessages = new();

            await foreach(var historyMessage in this.chatService.GetMessageHistoryAsync(this.roomId))
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
                    //groupsAndMessages[group].Add(new MessageModel { Owner = MessageOwner.OtherUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate, Reactions = historyMessage.Reactions });
                    
                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate };
                    if (historyMessage.Reactions != null) {
                        foreach (var reaction in historyMessage.Reactions)
                            msgModel.Reactions.Add(reaction);
                    }

                    groupsAndMessages[group].Add(msgModel);
                    this.Messages.Add(msgModel);
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

                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate };
                    if (historyMessage.Reactions != null) {
                        foreach (var reaction in historyMessage.Reactions)
                            msgModel.Reactions.Add(reaction);
                    }

                    groupsAndMessages[group].Add(msgModel);

                    this.Messages.Add(msgModel);
                }
            }

            if (groupsAndMessages.Count == 0)
                NoMessages = true;

            //this.MessageGroups.Add(new MessageGroupModel("Today", this.Messages));
            foreach(var e in groupsAndMessages)
            {
                this.MessageGroups.Add(new MessageGroupModel(e.Key as string, e.Value as List<MessageModel>));
            }
        }

        public async Task GetHistoryWithCache() {
            //If there are any elements in the list then clear it
            if (Messages.Count != 0)
                Messages.Clear();

            int lastId = 0;
            Dictionary<string, List<MessageModel>> groupsAndMessages = new();

            await foreach(var historyMessage in this.chatService.GetMessageHistoryWithCache(this.roomId)) {
                //Create the MessageModel list
                if (historyMessage.UserId != this.Account.Id) {
                    //Convert date to a cleaner format
                    var messageDateString = DateTime.Parse(historyMessage.SendDate);
                    double diffDays = (DateTime.Now - messageDateString).TotalDays;
                    var msgDate = messageDateString.ToString("HH:mm");
                    string group = "";
                    if (diffDays < 1.0)
                        group = "Today";
                    else {
                        if (diffDays >= 1.0 && diffDays < 2.0)
                            group = "Yesterday";
                        else {
                            group = messageDateString.ToString("dd MMMM yyyy");
                        }
                    }

                    //TODO...Check if the group exists, if not then create it and add the message to it, else add the message to the existing group
                    if (!groupsAndMessages.ContainsKey(group))
                        groupsAndMessages.Add(group, new List<MessageModel>());
                    //groupsAndMessages[group].Add(new MessageModel { Owner = MessageOwner.OtherUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate, Reactions = historyMessage.Reactions });

                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate };
                    if (historyMessage.Reactions != null) {
                        foreach (var reaction in historyMessage.Reactions)
                            msgModel.Reactions.Add(reaction);
                    }

                    this.Messages.Add(msgModel);

                    if (this.Messages.Count < 20)
                        groupsAndMessages[group].Add(msgModel);             
                }
                else {
                    var messageDateString = DateTime.Parse(historyMessage.SendDate);
                    double diffDays = (DateTime.Now - messageDateString).TotalDays;
                    var msgDate = messageDateString.ToString("HH:mm");
                    string group = "";
                    if (diffDays < 1.0)
                        group = "Today";
                    else {
                        if (diffDays >= 1.0 && diffDays < 2.0)
                            group = "Yesterday";
                        else {
                            group = messageDateString.ToString("dd MMMM yyyy");
                        }
                    }
                    //TODO...Check if the group exists, if not then create it and add the message to it, else add the message to the existing group
                    if (!groupsAndMessages.ContainsKey(group))
                        groupsAndMessages.Add(group, new List<MessageModel>());

                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate };
                    if (historyMessage.Reactions != null) {
                        foreach (var reaction in historyMessage.Reactions)
                            msgModel.Reactions.Add(reaction);
                    }

                    this.Messages.Add(msgModel);

                    if (this.Messages.Count < 20)
                        groupsAndMessages[group].Add(msgModel);
                }

                lastId = historyMessage.Id;
            }

            await foreach(var newMessage in this.chatService.GetMessagesWithIdGreater(this.roomId, lastId)) {
                //Create the MessageModel list
                if (newMessage.UserId != this.Account.Id) {
                    //Convert date to a cleaner format
                    var messageDateString = DateTime.Parse(newMessage.SendDate);
                    double diffDays = (DateTime.Now - messageDateString).TotalDays;
                    var msgDate = messageDateString.ToString("HH:mm");
                    string group = "";
                    if (diffDays < 1.0)
                        group = "Today";
                    else {
                        if (diffDays >= 1.0 && diffDays < 2.0)
                            group = "Yesterday";
                        else {
                            group = messageDateString.ToString("dd MMMM yyyy");
                        }
                    }

                    //TODO...Check if the group exists, if not then create it and add the message to it, else add the message to the existing group
                    if (!groupsAndMessages.ContainsKey(group))
                        groupsAndMessages.Add(group, new List<MessageModel>());
                    //groupsAndMessages[group].Add(new MessageModel { Owner = MessageOwner.OtherUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate, Reactions = historyMessage.Reactions });

                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageId = newMessage.Id.ToString(), Text = newMessage.Data, TimeStamp = msgDate };
                    if (newMessage.Reactions != null) {
                        foreach (var reaction in newMessage.Reactions)
                            msgModel.Reactions.Add(reaction);
                    }

                    groupsAndMessages[group].Add(msgModel);
                }
                else {
                    var messageDateString = DateTime.Parse(newMessage.SendDate);
                    double diffDays = (DateTime.Now - messageDateString).TotalDays;
                    var msgDate = messageDateString.ToString("HH:mm");
                    string group = "";
                    if (diffDays < 1.0)
                        group = "Today";
                    else {
                        if (diffDays >= 1.0 && diffDays < 2.0)
                            group = "Yesterday";
                        else {
                            group = messageDateString.ToString("dd MMMM yyyy");
                        }
                    }
                    //TODO...Check if the group exists, if not then create it and add the message to it, else add the message to the existing group
                    if (!groupsAndMessages.ContainsKey(group))
                        groupsAndMessages.Add(group, new List<MessageModel>());

                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageId = newMessage.Id.ToString(), Text = newMessage.Data, TimeStamp = msgDate };
                    if (newMessage.Reactions != null) {
                        foreach (var reaction in newMessage.Reactions)
                            msgModel.Reactions.Add(reaction);
                    }

                    groupsAndMessages[group].Add(msgModel);

                    this.Messages.Add(msgModel);
                }
            }

            if (groupsAndMessages.Count == 0)
                NoMessages = true;

            foreach (var e in groupsAndMessages) {
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

            LoadingMessages = true;
            await this.GetHistory();
            //await this.GetHistoryWithCache();
            LoadingMessages = false;
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
            await chatService.SendMessageAsync(jsonMessage);

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

        public async void ReactToMessage(int messageId, string emojiReaction) {
            //Create the model object which will be sent as a json to the server
            SendReactionModel srm = new SendReactionModel { messageId = messageId, emojiReaction = emojiReaction, senderId = this.Account.Id, roomId = this.roomId };
            string jsonMessage = JsonSerializer.Serialize(srm);
            await this.chatService.SendMessageAsync(jsonMessage);
        }
    }
}
