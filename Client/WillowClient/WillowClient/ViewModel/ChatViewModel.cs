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
using WillowClient.Database;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;
using Mopups.Services;
using WillowClient.ViewsPopups;
using System.Security.Cryptography;
using Microsoft.Maui.Storage;
using System.Reflection.Metadata;
using CommunityToolkit.Maui.Views;

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

        [ObservableProperty]
        private bool isInPage = true;

        //public ObservableCollection<MessageModel> Messages { get; } = new();
        public List<MessageModel> Messages { get; } = new();

        [ObservableProperty]
        private bool entryEnabled = true;

        private byte[] lastMessageKey = new byte[80];

        //Members needed to update the last online timestamp
        private DateTime lastOnlineTimestamp = DateTime.Now;

        private int updateLastOnlineTimestampPeriod = 1000;

        Timer updateLastOnlineTimestampTimer = null;

        public ObservableCollection<MessageGroupModel> MessageGroups { get; } = new();

        [ObservableProperty]
        private MessageModel photoMessageToSend;

        private int roomId;

        private ChatService chatService;

        private DatabaseService databaseService;
        public ChatViewModel(ChatService chatService, DatabaseService databaseService)
        {
            this.chatService = chatService;
            this.databaseService = databaseService;
            this.chatService.RegisterReadCallback(MessageReceivedOnWebsocket);
            MessagingCenter.Subscribe<MainViewModel, PrivateMessageModel>(this, "Private message received", this.PrivateMessageReceived);
            MessagingCenter.Subscribe<MainViewModel, PrivateMessageModel>(this, "Private attachment received", this.PrivateAttachmentReceived);
            MessagingCenter.Subscribe<MainViewModel, SendReactionModel>(this, "New reaction", this.ReactionToMessageReceived);
        }

        ~ChatViewModel() {
            MessagingCenter.Unsubscribe<MainViewModel, PrivateMessageModel>(this, "Private message received");
            MessagingCenter.Unsubscribe<MainViewModel, PrivateMessageModel>(this, "Private attachment received");
            MessagingCenter.Unsubscribe<MainViewModel, SendReactionModel>(this, "New reaction");
        }

        public async void ReactionToMessageReceived(MainViewModel sender, SendReactionModel reaction) {
            if (reaction != null) {
                //Check if the reaction is from this page
                if (this.roomId != reaction.roomId)
                    return;

                if (!this.IsInPage)
                    return;

                //Add the reaction to the specific message
                //Add a new reaction in the collection view
                foreach (var group in this.MessageGroups) {
                    foreach (var messageModel in group) {
                        if (Int32.Parse(messageModel.MessageId) == reaction.messageId) {
                            messageModel.Reactions.Add(new ReactionModel { Id = 0, Emoji = reaction.emojiReaction, ReactionDate = DateTime.Now.ToString("dd MMMM yyyy"), SenderId = reaction.senderId });
                            return;
                        }
                    }
                }
                return;
            }
        }

        public async void PrivateAttachmentReceived(MainViewModel sender, PrivateMessageModel privateMessageModel) {
            if (privateMessageModel == null)
                return;
            if (privateMessageModel.RoomId != this.roomId)
                return;
            if (!this.IsInPage)
                return;

            this.Friend.SeenAllNewMessages();
            await this.databaseService.UpdateNewMessagesForFriend(this.Friend.FriendId, int.Parse(this.Friend.NumberNewMessages));

            //Handle the attachment message
            //If the user that sent the message is the current user
            var attachment = JsonSerializer.Deserialize<AttachmentModel>(privateMessageModel.Data);
            //if(privateMessageModel.SenderId == this.Account.Id) {
            //    MessageModel msgModel = null;
            //    if (attachment.AttachmentType == "photo") {
            //        msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageType = MessageType.Photo, IsDownloaded = false, Text = "", TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privateMessageModel.Id.ToString() };
            //    }

            //    if (msgModel != null) {
            //        bool added = false;
            //        foreach (var e in this.MessageGroups) {
            //            if (e.Name == "Today") {
            //                e.Add(msgModel);
            //                //e.Name = "Today";
            //                added = true;
            //            }
            //        }
            //        if (!added) {
            //            //Create the group named today and add the message to the group
            //            List<MessageModel> messageModels = new List<MessageModel>();
            //            messageModels.Add(msgModel);
            //            this.MessageGroups.Add(new MessageGroupModel("Today", messageModels));
            //        }
            //    }
            //}

            if(privateMessageModel.SenderId == this.Friend.FriendId) {
                MessageModel msgModel = null;
                if (attachment.AttachmentType == "photo") {
                    msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Photo, IsDownloaded = false, Text = "", TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privateMessageModel.Id.ToString() };
                }
                if(attachment.AttachmentType == "file") {
                    msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.File, IsDownloaded = false, Text = "", TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privateMessageModel.Id.ToString() };
                }
                if(attachment.AttachmentType == "video") {
                    msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Video, IsDownloaded = false, Text = "", TimeStamp = DateTime.Now.ToString("HH:mm"), FileSizeString = attachment.FileSizeFormat, MessageId = privateMessageModel.Id.ToString() };
                }

                if (msgModel != null) {
                    bool added = false;
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
                }
            }

        }

        public async void PrivateMessageReceived(MainViewModel sender, PrivateMessageModel privMessageModel) {
            if (privMessageModel.RoomId == this.roomId) {
                if (!this.IsInPage)
                    return;

                this.Friend.SeenAllNewMessages();
                await this.databaseService.UpdateNewMessagesForFriend(this.Friend.FriendId, int.Parse(this.Friend.NumberNewMessages));
                
                //This is a private message received from another user
                //Check if the sender is the current user from the private conversation
                if (privMessageModel.SenderId == this.friend.FriendId) {
                    //This is the friend that sent the message

                    bool added = false;
                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Text, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privMessageModel.Id.ToString() };
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
                    NoMessages = false;
                    return;
                }
                else {
                    //Add the message into the collection view for the current user
                    bool found = false;
                    foreach (var e in this.MessageGroups) {
                        if (e.Name == "Today") {
                            e.Add(new MessageModel { Owner = MessageOwner.CurrentUser, MessageType = MessageType.Text, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privMessageModel.Id.ToString() });
                            //e.Name = "Today";
                            found = true;
                        }
                    }
                    //If the group Today is not found (no messages were exchanges in current they) then create the group and add the message in the group
                    if (!found) {
                        List<MessageModel> messages = new();
                        messages.Add(new MessageModel { Owner = MessageOwner.CurrentUser, MessageType = MessageType.Text, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privMessageModel.Id.ToString() });
                        this.MessageGroups.Add(new MessageGroupModel("Today", messages));
                    }
                    NoMessages = false;
                    return;
                }
            }
        }

        private void UpdateLastOnlineTimestamp(object objectInfo) {
            //Determine the difference
            DateTime currentTimestamp = DateTime.Now;
            double seconds = (currentTimestamp - this.lastOnlineTimestamp).Seconds;
            double minutes = (currentTimestamp - this.lastOnlineTimestamp).Minutes;
            double hours = (currentTimestamp - this.lastOnlineTimestamp).Hours;

            //If he was online in the last minute
            //if(seconds < 60.0 && minutes < 1.0 && hours < 1.0) {
            //    this.Friend.LastOnline = $"{(int)seconds} seconds ago";
            //    return;
            //}

            if(minutes >= 1.0 && hours < 1.0) {
                this.Friend.LastOnline = $"{(int)minutes} minutes ago";
                //Update the period of the callback if it was not previously updated
                //if(this.updateLastOnlineTimestampPeriod == 1000) {
                //    this.updateLastOnlineTimestampTimer.Change(1000, 1000 * 60);
                //}
                return;
            }

            if(hours >= 1.0) {
                this.Friend.LastOnline = $"{(int)hours} hours ago";
                //Update the period of the callback if it was not previously updated
                if (this.updateLastOnlineTimestampPeriod == 1000) {
                    this.updateLastOnlineTimestampTimer.Change(1000, 1000 * 60 * 60);
                }

                return;
            }

            this.Friend.LastOnline = this.lastOnlineTimestamp.ToString("d Y");
        }

        private void OnlineCallback(object objectInfo) {
            return;
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
                ChangeStatusWebsocketResponseModel resp = null;
                try {
                    resp = JsonSerializer.Deserialize<ChangeStatusWebsocketResponseModel>(message, options1);
                } catch(Exception ex) {
                    Console.WriteLine(ex.ToString());
                    return;
                }
                //Search through the friend accounts and see if the message that the account involved is in the list of friends
                //Change the color depending on which status it is set
                if (this.Friend.FriendId == resp.AccountId)
                {
                    if (resp.NewStatus == "Online")
                    {
                        this.LastOnlineText = "Online";
                        this.Friend.LastOnline = "";

                        TimerCallback callback = OnlineCallback;
                        this.updateLastOnlineTimestampTimer.Dispose();
                        this.updateLastOnlineTimestampTimer = null;
                    }
                    else
                    {
                        this.LastOnlineText = "Last Online: ";
                        this.Friend.LastOnline = "1 minute ago";

                        //Save the moment the user became offline
                        this.lastOnlineTimestamp = DateTime.Now;

                        //Create a callback function which will be called every second to update the last time the user was online
                        TimerCallback callback = UpdateLastOnlineTimestamp;
                        this.updateLastOnlineTimestampTimer = new Timer(callback, "State", 1000 * 60, 1000 * 60);
                    }
                }
                return;
            }

            if (message.IndexOf("callee") != -1) {
                return;
            }

            //Parse the JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

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
            foreach(var e in groupsAndMessages) {
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

        public async Task GetLocalMessages() {
            //If there are any elements in the list then clear it
            if (Messages.Count != 0)
                Messages.Clear();

            //The user got all the new messages
            this.Friend.SeenAllNewMessages();
            await this.databaseService.UpdateNewMessagesForFriend(this.Friend.FriendId, int.Parse(this.Friend.NumberNewMessages));

            Dictionary<string, List<MessageModel>> groupsAndMessages = new();

            await foreach(var message in this.databaseService.GetLocalMessagesInRoom(this.roomId)) {
                //Create the MessageModel list
                if (message.Owner != this.Account.Id) {
                    //Convert date to a cleaner format
                    var messageDateString = message.TimeStamp;
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

                    bool found = false;
                    foreach(var messageGroup in this.MessageGroups) {
                        if(messageGroup.Name == group) {
                            found = true;
                        }
                    }
                    if (!found) {
                        //Create the group 
                        this.MessageGroups.Add(new MessageGroupModel(group, new List<MessageModel>()));
                    }

                    //if (message.Text.Length == 23)
                    //    message.Text += " ";

                    MessageModel msgModel = null;
                    if(message.Type == "Text")
                        msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Text, MessageId = message.Id.ToString(), Text = message.Text, TimeStamp = msgDate };
                    if(message.Type == "Attachment") {
                        //Parse the message
                        var attachment = JsonSerializer.Deserialize<AttachmentModel>(message.Text);
                        var dbAttachment = await this.databaseService.GetAttachment(attachment.BlobUuid);
                        if(dbAttachment == null) {
                            //Store the attachment in the database
                            _ = await this.databaseService.SaveUndownloadedAttachment(attachment, message.Id);
                        }
                        if (attachment.AttachmentType == "photo") {
                            if (dbAttachment != null) {
                                //Show the message as photo if it is downloaded
                                if (dbAttachment.Downloaded) {
                                    msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true, MediaStream = ImageSource.FromFile(dbAttachment.LocalFilepath) };
                                }
                                else {
                                    //Else show the message as downloadable attachment
                                    msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                }
                            } else {
                                //Else show the message as downloadable attachment
                                msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                            }
                        }
                        if(attachment.AttachmentType == "video") {
                            if (dbAttachment != null) {
                                //Show the message as photo if it is downloaded
                                if (dbAttachment.Downloaded) {
                                    msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.OtherUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true, VideoStream = MediaSource.FromFile(dbAttachment.LocalFilepath) };
                                }
                                else {
                                    //Else show the message as downloadable attachment
                                    msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.OtherUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                }
                            } else {
                                //Else show the message as downloadable attachment
                                msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.OtherUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                            }
                        }
                        if (attachment.AttachmentType == "file") {
                            if (dbAttachment != null) {
                                if (dbAttachment.Downloaded) {
                                    msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.OtherUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                }
                                else {
                                    msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.OtherUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true };
                                }
                            }
                            else {
                                msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.OtherUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true };
                            }
                        }
                    }

                    //Get the number of reactions for this model
                    int numberReactions = await this.databaseService.GetNumberReactionsOfMessage(message.Id);
                    if(numberReactions != 0) {
                        await foreach(var reaction in this.databaseService.GetMessageReactions(message.Id)) {
                            msgModel.Reactions.Add(new ReactionModel { Id = reaction.Id, Emoji = reaction.Emoji, ReactionDate = reaction.ReactionDate.ToString(), SenderId = reaction.SenderId });
                        }
                    }

                    //Add the message to the new group created
                    foreach(var messageGroup in this.MessageGroups) {
                        if(messageGroup.Name == group) {
                            messageGroup.Add(msgModel);
                        }
                    }

                    this.Messages.Add(msgModel);
                }
                else {
                    var messageDateString = message.TimeStamp;
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

                    bool found = false;
                    foreach (var messageGroup in this.MessageGroups) {
                        if (messageGroup.Name == group) {
                            found = true;
                        }
                    }
                    if (!found) {
                        //Create the group 
                        this.MessageGroups.Add(new MessageGroupModel(group, new List<MessageModel>()));
                    }

                    MessageModel msgModel = null;
                    if(message.Type == "Text")
                        msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageType = MessageType.Text, MessageId = message.Id.ToString(), Text = message.Text, TimeStamp = msgDate };
                    if(message.Type == "Attachment") {
                        //Parse the message
                        var attachment = JsonSerializer.Deserialize<AttachmentModel>(message.Text);
                        var dbAttachment = await this.databaseService.GetAttachment(attachment.BlobUuid);
                        if (attachment.AttachmentType == "photo") {
                            if (dbAttachment != null) {
                                //Show the message as photo if it is downloaded
                                if (dbAttachment.Downloaded) {
                                    msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true, MediaStream = ImageSource.FromFile(dbAttachment.LocalFilepath) };
                                }
                                else {
                                    //Else show the message as downloadable attachment
                                    msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                }
                            }
                        }
                        if (attachment.AttachmentType == "video") {
                            if (dbAttachment != null) {
                                //Show the message as photo if it is downloaded
                                if (dbAttachment.Downloaded) {
                                    msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.CurrentUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true, VideoStream = MediaSource.FromFile(dbAttachment.LocalFilepath) };
                                }
                                else {
                                    //Else show the message as downloadable attachment
                                    msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.CurrentUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                }
                            }
                        }
                        if (attachment.AttachmentType == "file") {
                            if (dbAttachment != null) {
                                msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.CurrentUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                            }
                            else {
                                msgModel = new MessageModel { FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.CurrentUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true };
                            }
                        }
                    }

                    //Get the number of reactions for this model
                    int numberReactions = await this.databaseService.GetNumberReactionsOfMessage(message.Id);
                    if (numberReactions != 0) {
                        await foreach (var reaction in this.databaseService.GetMessageReactions(message.Id)) {
                            msgModel.Reactions.Add(new ReactionModel { Id = reaction.Id, Emoji = reaction.Emoji, ReactionDate = reaction.ReactionDate.ToString(), SenderId = reaction.SenderId });
                        }
                    }

                    //Add the message to the new group created
                    foreach (var messageGroup in this.MessageGroups) {
                        if (messageGroup.Name == group) {
                            messageGroup.Add(msgModel);
                        }
                    }

                    this.Messages.Add(msgModel);
                }

                if (this.MessageGroups.Count == 0)
                    NoMessages = true;
            }
        }

        private string FormatLastOnlineTimestamp(string lastOnline) {
            try {
                DateTime lastOnlineTimestamp = DateTime.Parse(lastOnline);
                double secondsDiff = (DateTime.Now - lastOnlineTimestamp).Seconds;
                double minutesDiff = (DateTime.Now - lastOnlineTimestamp).Minutes;
                double hoursDiff = (DateTime.Now - lastOnlineTimestamp).Hours;

                string returnString = "";
                if (minutesDiff < 1.0 && hoursDiff < 1.0) {
                    returnString = $"{((int)secondsDiff)} seconds ago";
                }

                if (hoursDiff < 1.0 && minutesDiff >= 1.0) {
                    returnString = $"{(int)minutesDiff} minutes ago";
                }

                if (hoursDiff >= 1.0 && hoursDiff < 24.0) {
                    returnString = $"{(int)hoursDiff} hours ago";
                }

                if (hoursDiff >= 24.0) {
                    returnString = lastOnlineTimestamp.ToString("d Y");
                }

                return returnString;
            } catch(Exception e) {
                Console.WriteLine(e.ToString());
                return lastOnline;
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
            } else {
                this.LastOnlineText = "Last Online: ";
                //this.Friend.LastOnline = "";
                this.Friend.LastOnline = FormatLastOnlineTimestamp(this.Friend.LastOnline);
            }

            _ = await this.databaseService.UpdateFriendRoomId(this.friend.FriendId, this.roomId);
            this.friend.RoomID = this.roomId;

            this.isInPage = true;
            LoadingMessages = true;
            //await this.GetHistory();
            await this.GetLocalMessages();
            //await this.GetHistoryWithCache();
            LoadingMessages = false;
        }

        [RelayCommand]
        public async Task GoBack()
        {
            this.EntryEnabled = false;
            this.IsInPage = false;
            //this.updateLastOnlineTimestampTimer.Dispose();
            System.Threading.Thread.Sleep(1000);
            await Shell.Current.Navigation.PopAsync(true);
        }

        private async Task<Encryption.MessageEphemeralPublicKeys> GetCurrentMessageKey() {
            //If this is the first message in the conversation then generate ephemeral key
            //Then generate the master secret
            //From the master secret generate the root key and the chain key
            //From the chain key generate the message key
            //Update the chain key
            //Encrypt the message and add the public identity key and public ephemeral key to the message model
            int numberMessagesInRoom = await this.databaseService.GetNumberMessagesInRoom(roomId: roomId);
            byte[] messageKey = new byte[80];
            string ephemeralPublic = "";
            if (numberMessagesInRoom == 0) {
                var ephemeralKey = Encryption.Utils.GenerateX25519Key();
                string ephemeralPrivate = ephemeralKey.ExportECPrivateKeyPem();
                ephemeralPublic = ephemeralKey.ExportSubjectPublicKeyInfoPem();
                var masterSecret = Encryption.Utils.ComputeSenderMasterSecret(ephemeralPrivate, this.Friend.IdentityPublicKey, this.Friend.PreSignedPublicKey);
                var rootChainKeys = Encryption.Utils.GenerateRootAndChainKeyFromMasterSecret(masterSecret);
                //Generate the message key
                var messageChainKey = Encryption.Utils.GenerateMessageKey(rootChainKeys.ChainKey);
                System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, messageKey, 0, messageChainKey.MessageKey.Length);
                System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, this.lastMessageKey, 0, messageChainKey.MessageKey.Length);
                _ = await this.databaseService.SaveKeysForFriend(this.Friend.FriendId, masterSecret, messageChainKey.ChainKey, rootChainKeys.RootKey, ephemeralPrivate, ephemeralPublic);
            }
            else {
                //Get the last message in this conversation
                var lastMessage = await this.databaseService.GetLastMessageInRoom(this.roomId);
                //Check if the last message was sent by this user or by the friend
                //If the last message was sent by the current user
                if (lastMessage.Owner == this.Account.Id) {
                    //Get the ephemeral public key from the database
                    ephemeralPublic = await this.databaseService.GetEphemeralPublicKey(this.Friend.FriendId);
                    //Get the chain and root keys from the database
                    var chainKey = await this.databaseService.GetChainKeyForFriend(this.Friend.FriendId);
                    //Compute the message key from the chain key
                    var messageChainKey = Encryption.Utils.GenerateMessageKey(chainKey);
                    //Save the message keys in the buffers
                    System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, messageKey, 0, messageChainKey.MessageKey.Length);
                    System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, this.lastMessageKey, 0, messageChainKey.MessageKey.Length);
                    //Update the keys in the database for the friend
                    _ = await this.databaseService.UpdateChainKeyForFriend(this.Friend.FriendId, messageChainKey.ChainKey);
                }

                //If the last message was sent by the friend
                if (lastMessage.Owner == this.Friend.FriendId) {
                    //Generate a new ephemeral key
                    var ephemeralPrivateKey = Encryption.Utils.GenerateX25519Key();
                    ephemeralPublic = ephemeralPrivateKey.ExportSubjectPublicKeyInfoPem();
                    var ephemeralPrivatePem = ephemeralPrivateKey.ExportECPrivateKeyPem();
                    //Get the party ephemeral public key from the last message
                    var partyEphemeralKey = Encryption.Utils.GenerateX25519Key();
                    partyEphemeralKey.ImportFromPem(lastMessage.EphemeralPublic);
                    //Compute the ephemeral secret
                    var ephemeralSecret = Encryption.Utils.ComputeEphemeralSecret(ephemeralPrivateKey, partyEphemeralKey);
                    //Compute the root and chain key from the ephemeral secret
                    //Get the previous root key from the database
                    var previousRootKey = await this.databaseService.GetRootKeyForFriend(this.Friend.FriendId);
                    var rootChainKeys = Encryption.Utils.ComputeRootChainFromEphemeralSecret(ephemeralSecret, previousRootKey);
                    //Compute the message key
                    var messageChainKey = Encryption.Utils.GenerateMessageKey(rootChainKeys.ChainKey);
                    System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, messageKey, 0, messageChainKey.MessageKey.Length);
                    System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, this.lastMessageKey, 0, messageChainKey.MessageKey.Length);
                    //Update the keys in the database for the friend (root, chain, ephemeral secret)
                    _ = await this.databaseService.SaveEphemeralRootChainForFriend(this.Friend.FriendId, ephemeralSecret, messageChainKey.ChainKey, rootChainKeys.RootKey, ephemeralPrivatePem, ephemeralPublic);
                }
            }

            return new Encryption.MessageEphemeralPublicKeys { EphemeralPublic = ephemeralPublic, MessageKey = messageKey };
        }

        [RelayCommand]
        public async Task TakePhoto() {
            if (MediaPicker.Default.IsCaptureSupported) {
                FileResult photo = null;
                try {
                    photo = await MediaPicker.Default.CapturePhotoAsync();
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    return;
                }


                if (photo != null) {
                    // save the file into local storage
                    string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

                    Stream sourceStream = await photo.OpenReadAsync();

                    using (var fileStream = File.Create(localFilePath)) {
                        sourceStream.CopyTo(fileStream);
                    }

                    //Create a new message with the template for photo sent message
                    MessageModel msgModel = new MessageModel { MessageId = "0", MessageType = MessageType.Photo, IsDownloaded = true, Text = "", MediaStream = ImageSource.FromFile(localFilePath), IsLoading = true, Owner = MessageOwner.CurrentUser, TimeStamp = DateTime.Now.ToString("HH:mm") };

                    //await MopupService.Instance.PushAsync(new AddCaptionToMessagePopup(), true);

                    //Add the message into the today group
                    bool found = false;
                    foreach (var messageGroup in this.MessageGroups) {
                        if (messageGroup.Name == "Today") {
                            messageGroup.Add(msgModel);
                            found = true;
                        }
                    }
                    if (!found) {
                        this.MessageGroups.Add(new MessageGroupModel("Today", new List<MessageModel>() { msgModel }));
                    }

                    sourceStream.Seek(0, SeekOrigin.Begin);
                    //Generate a AES 256 key and iv
                    byte[] aesKey = new byte[32];
                    byte[] aesIv = new byte[16];
                    byte[] hmacKey = new byte[32];
                    aesKey = RandomNumberGenerator.GetBytes(32);
                    aesIv = RandomNumberGenerator.GetBytes(16);
                    hmacKey = RandomNumberGenerator.GetBytes(32);

                    //Encrypt the stream using the key and the iv
                    var encryptedData = Encryption.Utils.EncryptBlobData(sourceStream, aesKey, aesIv);
                    var hmac = Encryption.Utils.AutheticateBlobData(encryptedData, hmacKey);

                    //Create the blob with the hmac
                    byte[] encBlob = new byte[encryptedData.Length + hmac.Length];
                    System.Buffer.BlockCopy(encryptedData, 0, encBlob, 0, encryptedData.Length);
                    System.Buffer.BlockCopy(hmac,0, encBlob, encryptedData.Length, hmac.Length);

                    //Generate the sha256 of the ciphertext
                    var hash = Encryption.Utils.HashBlobCipherText(encryptedData);

                    var blobResponse = await this.chatService.UploadDataToBlobStorage(encBlob);
                    //If the photo could not have been uploaded to the blob then show a popup to the user
                    if(blobResponse == null) {
                        await Shell.Current.DisplayAlert("Error", "Could not send photo", "Ok");
                        return;
                    }

                    //Craft the message that will be sent to the user
                    AttachmentModel attachment = new AttachmentModel { Caption = "",AesIvBase64 = System.Convert.ToBase64String(aesIv), AesKeyBase64 = System.Convert.ToBase64String(aesKey), AttachmentType = "photo", BlobUuid = blobResponse.blobUuid, HashBase64 = System.Convert.ToBase64String(hash), HMACKeyBase64 = System.Convert.ToBase64String(hmacKey)};
                    string messageBody = JsonSerializer.Serialize(attachment);

                    //Get the current message key
                    var messageEphemeral = await this.GetCurrentMessageKey();

                    //Encrypt the data
                    var encryptedMessageData = Encryption.Utils.EncryptMessage(messageBody, messageEphemeral.MessageKey);

                    //Create the structure that will hold the data which will be json encoded and sent to the server
                    SendPrivateMessageModel sendMessageModel = new SendPrivateMessageModel { roomId = this.roomId, data = encryptedMessageData, messageType = "Attachment", ephemeralPublicKey = messageEphemeral.EphemeralPublic, identityPublicKey = Globals.identityKey.ExportSubjectPublicKeyInfoPem() };

                    string jsonMessage = JsonSerializer.Serialize(sendMessageModel);
                    await chatService.SendMessageAsync(jsonMessage);

                    this.Friend.LastMessage = messageBody;

                    //Upload the encrypted stream to the blob
                    msgModel.IsLoading = false;

                    //Save the attachment details in the database
                    _ = await this.databaseService.SaveMessageAttachment(attachment, localFilePath, sourceStream.Length);
                }
            }
        }

        private readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        private string FormatSize(Int64 bytes) {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1) {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        private int getNumberOfPdfPages(string fileName) {
            using (StreamReader sr = new StreamReader(File.OpenRead(fileName))) {
                Regex regex = new Regex(@"/Type\s*/Page[^s]");
                MatchCollection matches = regex.Matches(sr.ReadToEnd());

                return matches.Count;
            }
        }

        [RelayCommand]
        public async Task PickFile()
        {
            try {
                var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.my.comic.extension" } }, // UTType values
                    { DevicePlatform.Android, new[] { "*/*" } }, // MIME type
                    { DevicePlatform.WinUI, new[] { ".txt", ".pdf", ".h5", ".png", ".jpg", ".jfif", ".jpeg", ".mp4", ".webm" } }, // file extension
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "cbr", "cbz" } }, // UTType values
                });

                var pickedFiles = await FilePicker.PickMultipleAsync(new PickOptions { PickerTitle = "Select file(s)", FileTypes = customFileType });
                if (pickedFiles == null)
                    return;

                List<MessageModel> messagesToSend = new List<MessageModel>();
                foreach(var file in pickedFiles) {
                    if (file == null)
                        continue;

                    // save the file into local storage
                    string localFilePath = Path.Combine(FileSystem.CacheDirectory, file.FileName);
                    Stream sourceStream = await file.OpenReadAsync();
                    using (var fileStream = File.Create(localFilePath)) {
                        sourceStream.CopyTo(fileStream);
                    }

                    //Check if the file is a pdf
                    var parts = file.FileName.Split(".");
                    var extension = parts[parts.Length - 1];
                    var previewPath = Path.Combine(FileSystem.CacheDirectory, parts[0]);

                    string sizeFormat = this.FormatSize(sourceStream.Length);

                    MessageModel msgModel = null;
                    if (extension == "pdf") {
                        //Create a message with template for pdf file
                        //Get the data from the file
                        sourceStream.Seek(0, SeekOrigin.Begin);

                        //var html = $"""
                        //    <!DOCTYPE html>
                        //    <html>
                        //      <body>
                        //        <iframe src="file://{localFilePath}#toolbar=0"></iframe>
                        //      </body>
                        //    </html>
                        //    """;

                        msgModel = new MessageModel { Filename = file.FileName, FileSizeString = sizeFormat, IsLoading = true, NumberPages = this.getNumberOfPdfPages(localFilePath), MessageId = "0", MessageType = MessageType.Pdf, Owner = MessageOwner.CurrentUser, TimeStamp = DateTime.Now.ToString("HH:mm") };
                    }

                    if (extension == "png" || extension == "jpg" || extension == "jfif") {
                        //Create a new message with the template for file sent message
                        msgModel = new MessageModel { MediaStream = ImageSource.FromFile(localFilePath), MessageId = "0", MessageType = MessageType.Photo, IsDownloaded = true, Text = "", IsLoading = true, Owner = MessageOwner.CurrentUser, TimeStamp = DateTime.Now.ToString("HH:mm") };
                    }

                    if(extension == "webm" || extension == "mp4") {
                        msgModel = new MessageModel { VideoStream = MediaSource.FromFile(localFilePath), MessageId = "0", MessageType = MessageType.Video, IsDownloaded = true, IsLoading = true, Owner = MessageOwner.CurrentUser, TimeStamp = DateTime.Now.ToString("HH:mm") };
                    }

                    if (extension != "png" && extension != "jpg" && extension != "jfif" && extension != "pdf" && extension != "webm" && extension != "mp4") {
                        msgModel = new MessageModel { FileSizeString = sizeFormat, LocalPath = localFilePath, Filename = file.FileName, IsDownloaded = true, MessageId = "0", MessageType = MessageType.File, Owner = MessageOwner.CurrentUser, TimeStamp = DateTime.Now.ToString("HH:mm"), Progress = 0, IsLoading = true };
                    }

                    //Add the message into the today group
                    bool found = false;
                    foreach (var messageGroup in this.MessageGroups) {
                        if (messageGroup.Name == "Today") {
                            messageGroup.Add(msgModel);
                            found = true;
                        }
                    }
                    if (!found) {
                        this.MessageGroups.Add(new MessageGroupModel("Today", new List<MessageModel>() { msgModel }));
                    }

                    messagesToSend.Add(msgModel);
                }

                foreach(var message in messagesToSend) {
                    //Generate the keys for encrypting and authenticating the blob
                    var aesKey = RandomNumberGenerator.GetBytes(32);
                    var aesIv = RandomNumberGenerator.GetBytes(16);
                    var hmacKey = RandomNumberGenerator.GetBytes(32);

                    message.Progress = 0.1;
                    FileImageSource fileImageSource = message.MediaStream as FileImageSource;
                    FileMediaSource fileMediaSource = message.VideoStream as FileMediaSource;

                    string filename = "";
                    if(fileImageSource == null) {
                        if(fileMediaSource == null) {
                            filename = message.LocalPath;
                        } else {
                            filename = fileMediaSource.Path;
                        }
                    } else {
                        filename = fileImageSource.File;
                    }
                    var sourceStream = File.OpenRead(filename);
                    if(sourceStream == null) {
                        sourceStream = File.OpenRead(message.LocalPath);
                    }

                    message.Progress = 0.2;
                    var sizeFormat = this.FormatSize(sourceStream.Length);

                    //Encrypt the stream using the key and the iv
                    var encryptedData = Encryption.Utils.EncryptBlobData(sourceStream, aesKey, aesIv);
                    message.Progress = 0.3;
                    var hmac = Encryption.Utils.AutheticateBlobData(encryptedData, hmacKey);
                    message.Progress = 0.4;

                    //Create the blob with the hmac
                    byte[] encBlob = new byte[encryptedData.Length + hmac.Length];
                    System.Buffer.BlockCopy(encryptedData, 0, encBlob, 0, encryptedData.Length);
                    System.Buffer.BlockCopy(hmac, 0, encBlob, encryptedData.Length, hmac.Length);

                    //Generate the sha256 of the ciphertext
                    var hash = Encryption.Utils.HashBlobCipherText(encryptedData);
                    message.Progress = 0.5;

                    var blobResponse = await this.chatService.UploadDataToBlobStorage(encBlob);
                    //If the photo could not have been uploaded to the blob then show a popup to the user
                    if (blobResponse == null) {
                        message.Progress = 0.0;
                        await Shell.Current.DisplayAlert("Error", "Could not send photo", "Ok");
                        return;
                    }
                    message.Progress = 0.6;

                    //Craft the message that will be sent to the user
                    AttachmentModel attachment = null;
                    if (message.MessageType == MessageType.Photo) {
                        attachment = new AttachmentModel { Caption = "", AesIvBase64 = System.Convert.ToBase64String(aesIv), AesKeyBase64 = System.Convert.ToBase64String(aesKey), AttachmentType = "photo", BlobUuid = blobResponse.blobUuid, HashBase64 = System.Convert.ToBase64String(hash), HMACKeyBase64 = System.Convert.ToBase64String(hmacKey) };
                    }
                    if (message.MessageType == MessageType.File) {
                        attachment = new AttachmentModel { Filename = message.Filename, FileSizeFormat = sizeFormat, Caption = "", AesIvBase64 = System.Convert.ToBase64String(aesIv), AesKeyBase64 = System.Convert.ToBase64String(aesKey), AttachmentType = "file", BlobUuid = blobResponse.blobUuid, HashBase64 = System.Convert.ToBase64String(hash), HMACKeyBase64 = System.Convert.ToBase64String(hmacKey) };
                    }
                    if(message.MessageType == MessageType.Pdf) {
                        attachment = new AttachmentModel { Filename = message.Filename, FileSizeFormat = sizeFormat, NumberPages = this.getNumberOfPdfPages(fileImageSource.File), Caption = "", AesIvBase64 = System.Convert.ToBase64String(aesIv), AesKeyBase64 = System.Convert.ToBase64String(aesKey), AttachmentType = "pdf", BlobUuid = blobResponse.blobUuid, HashBase64 = System.Convert.ToBase64String(hash), HMACKeyBase64 = System.Convert.ToBase64String(hmacKey) };
                    }
                    if(message.MessageType == MessageType.Video) {
                        attachment = new AttachmentModel { Filename = message.Filename, FileSizeFormat = sizeFormat, Caption = "", AesIvBase64 = System.Convert.ToBase64String(aesIv), AesKeyBase64 = System.Convert.ToBase64String(aesKey), AttachmentType = "video", BlobUuid = blobResponse.blobUuid, HashBase64 = System.Convert.ToBase64String(hash), HMACKeyBase64 = System.Convert.ToBase64String(hmacKey) };
                    }
                    message.Progress = 0.7;

                    string messageBody = JsonSerializer.Serialize(attachment);

                    //Get the current message key
                    var messageEphemeral = await this.GetCurrentMessageKey();

                    //Encrypt the data
                    var encryptedMessageData = Encryption.Utils.EncryptMessage(messageBody, messageEphemeral.MessageKey);
                    message.Progress = 0.8;

                    //Create the structure that will hold the data which will be json encoded and sent to the server
                    SendPrivateMessageModel sendMessageModel = new SendPrivateMessageModel { roomId = this.roomId, data = encryptedMessageData, messageType = "Attachment", ephemeralPublicKey = messageEphemeral.EphemeralPublic, identityPublicKey = Globals.identityKey.ExportSubjectPublicKeyInfoPem() };

                    string jsonMessage = JsonSerializer.Serialize(sendMessageModel);
                    await chatService.SendMessageAsync(jsonMessage);
                    message.Progress = 0.9;

                    this.Friend.LastMessage = messageBody;

                    //Upload the encrypted stream to the blob
                    message.IsLoading = false;

                    //Save the attachment details in the database
                    _ = await this.databaseService.SaveMessageAttachment(attachment, filename, sourceStream.Length);
                    message.Progress = 1.0;
                }

            } catch(Exception ex) { 
                Console.WriteLine(ex.ToString());
            }
        }

        [RelayCommand]
        public async Task SendMessage()
        {
            //Before sending the message check if the user has internet connection
            //Check if the application is connected to the chat service via websockets
            if (this.MessageText == null)
                return;

            if (this.MessageText == "")
                return;

            NetworkAccess accessType = Connectivity.Current.NetworkAccess;
            if (accessType != NetworkAccess.Internet)
                return;

            if (!this.chatService.IsConnectedToWebsocket())
                return;

            //If this is the first message in the conversation then generate ephemeral key
            //Then generate the master secret
            //From the master secret generate the root key and the chain key
            //From the chain key generate the message key
            //Update the chain key
            //Encrypt the message and add the public identity key and public ephemeral key to the message model
            int numberMessagesInRoom = await this.databaseService.GetNumberMessagesInRoom(roomId: roomId);
            byte[] messageKey = new byte[80];
            string ephemeralPublic = "";
            if(numberMessagesInRoom == 0) {
                var ephemeralKey = Encryption.Utils.GenerateX25519Key();
                string ephemeralPrivate = ephemeralKey.ExportECPrivateKeyPem();
                ephemeralPublic = ephemeralKey.ExportSubjectPublicKeyInfoPem();
                var masterSecret = Encryption.Utils.ComputeSenderMasterSecret(ephemeralPrivate, this.Friend.IdentityPublicKey, this.Friend.PreSignedPublicKey);
                var rootChainKeys = Encryption.Utils.GenerateRootAndChainKeyFromMasterSecret(masterSecret);
                //Generate the message key
                var messageChainKey = Encryption.Utils.GenerateMessageKey(rootChainKeys.ChainKey);
                System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, messageKey, 0, messageChainKey.MessageKey.Length);
                System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, this.lastMessageKey, 0, messageChainKey.MessageKey.Length);
                _ = await this.databaseService.SaveKeysForFriend(this.Friend.FriendId, masterSecret, messageChainKey.ChainKey, rootChainKeys.RootKey, ephemeralPrivate, ephemeralPublic);
            } else {
                //Get the last message in this conversation
                var lastMessage = await this.databaseService.GetLastMessageInRoom(this.roomId);
                //Check if the last message was sent by this user or by the friend
                //If the last message was sent by the current user
                if(lastMessage.Owner == this.Account.Id) {
                    //Get the ephemeral public key from the database
                    ephemeralPublic = await this.databaseService.GetEphemeralPublicKey(this.Friend.FriendId);
                    //Get the chain and root keys from the database
                    var chainKey = await this.databaseService.GetChainKeyForFriend(this.Friend.FriendId);
                    //Compute the message key from the chain key
                    var messageChainKey = Encryption.Utils.GenerateMessageKey(chainKey);
                    //Save the message keys in the buffers
                    System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, messageKey, 0, messageChainKey.MessageKey.Length);
                    System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, this.lastMessageKey, 0, messageChainKey.MessageKey.Length);
                    //Update the keys in the database for the friend
                    _ = await this.databaseService.UpdateChainKeyForFriend(this.Friend.FriendId, messageChainKey.ChainKey);
                }

                //If the last message was sent by the friend
                if(lastMessage.Owner == this.Friend.FriendId) {
                    //Generate a new ephemeral key
                    var ephemeralPrivateKey = Encryption.Utils.GenerateX25519Key();
                    ephemeralPublic = ephemeralPrivateKey.ExportSubjectPublicKeyInfoPem();
                    var ephemeralPrivatePem = ephemeralPrivateKey.ExportECPrivateKeyPem();
                    //Get the party ephemeral public key from the last message
                    var partyEphemeralKey = Encryption.Utils.GenerateX25519Key();
                    partyEphemeralKey.ImportFromPem(lastMessage.EphemeralPublic);
                    //Compute the ephemeral secret
                    var ephemeralSecret = Encryption.Utils.ComputeEphemeralSecret(ephemeralPrivateKey, partyEphemeralKey);
                    //Compute the root and chain key from the ephemeral secret
                    //Get the previous root key from the database
                    var previousRootKey = await this.databaseService.GetRootKeyForFriend(this.Friend.FriendId);
                    var rootChainKeys = Encryption.Utils.ComputeRootChainFromEphemeralSecret(ephemeralSecret, previousRootKey);
                    //Compute the message key
                    var messageChainKey = Encryption.Utils.GenerateMessageKey(rootChainKeys.ChainKey);
                    System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, messageKey, 0, messageChainKey.MessageKey.Length);
                    System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, this.lastMessageKey, 0, messageChainKey.MessageKey.Length);
                    //Update the keys in the database for the friend (root, chain, ephemeral secret)
                    _ = await this.databaseService.SaveEphemeralRootChainForFriend(this.Friend.FriendId, ephemeralSecret, messageChainKey.ChainKey, rootChainKeys.RootKey, ephemeralPrivatePem, ephemeralPublic);
                }
            }

            this.Friend.LastMessage = this.MessageText;
            //Encrypt the data
            var encryptedMessageData = Encryption.Utils.EncryptMessage(this.MessageText, messageKey);

            //Create the structure that will hold the data which will be json encoded and sent to the server
            SendPrivateMessageModel sendMessageModel = new SendPrivateMessageModel { roomId = this.roomId, data = encryptedMessageData, messageType = "Text", ephemeralPublicKey = ephemeralPublic, identityPublicKey = Globals.identityKey.ExportSubjectPublicKeyInfoPem()};

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
                {"video", true },
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
            if (emojiReaction == null)
                return;

            //Create the model object which will be sent as a json to the server
            SendReactionModel srm = new SendReactionModel { messageId = messageId, emojiReaction = emojiReaction, senderId = this.Account.Id, roomId = this.roomId };
            string jsonMessage = JsonSerializer.Serialize(srm);
            await this.chatService.SendMessageAsync(jsonMessage); 
        }

        [RelayCommand]
        async Task PhotoMessageTapped(MessageModel message) {
            //Check if the file is downloaded
            if (message.IsDownloaded == false)
                return;
            //Get the filename of the media stream
            var fileMediaStream = message.MediaStream as FileImageSource;
            if (fileMediaStream == null)
                return;
            string filename = fileMediaStream.File;
            await Shell.Current.GoToAsync(nameof(ZoomImagePage), true, new Dictionary<string, object>
            {
                {"imageFilename", filename},
            });
        }

        [RelayCommand]
        async Task DownloadAttachment(MessageModel message) {
            message.IsDownloading = true;

            //Download the blob data
            var attachment = await this.databaseService.GetAttachment(int.Parse(message.MessageId));
            var blobData = await this.chatService.DownloadDataFromBlobStorage(attachment.BlobUuid);
            var msgDb = await this.databaseService.GetMessage(int.Parse(message.MessageId));
            AttachmentModel att = JsonSerializer.Deserialize<AttachmentModel>(msgDb.Text);
            var key = System.Convert.FromBase64String(att.AesKeyBase64);
            var iv = System.Convert.FromBase64String(att.AesIvBase64);
            var decrypted = Encryption.Utils.DecryptBlobData(blobData, key, iv);
            //Save the data on the disk with a guid name
            var guid = System.Guid.NewGuid();
            string filename = guid.ToString();
            string localFilePath = Path.Combine(FileSystem.CacheDirectory, filename);
            
            var stream = File.Create(localFilePath);
            stream.Write(decrypted, 0, decrypted.Length);
            stream.Close();

            //Update the attachment
            attachment.Downloaded = true;
            attachment.LocalFilepath = localFilePath;
            _ = await this.databaseService.UpdateAttachment(attachment);

            message.IsDownloading = false;

            //Update the ui
            foreach (var group in this.MessageGroups) {
                for (int i = 0; i < group.Count; i++) {
                    if (group[i] == message) {
                        MessageModel newMessage = null;
                        if (message.MessageType == MessageType.Photo) {
                            var source = ImageSource.FromFile(localFilePath);
                            newMessage = new MessageModel { IsDownloaded = true, IsDownloading = false, IsLoading = false, MediaStream = source, MessageId = message.MessageId, MessageType = message.MessageType, Owner = message.Owner, SenderName = message.SenderName, TimeStamp = message.TimeStamp };
                        }
                        if(message.MessageType == MessageType.Video) {
                            var source = MediaSource.FromFile(localFilePath);
                            newMessage = new MessageModel { IsDownloaded = true, IsDownloading = false, IsLoading = false, VideoStream = source, MessageId = message.MessageId, MessageType = message.MessageType, Owner = message.Owner, SenderName = message.SenderName, TimeStamp = message.TimeStamp };
                        }
                        foreach (var reaction in message.Reactions)
                            newMessage.Reactions.Add(reaction);
                        group[i] = newMessage;
                        return;
                    }
                }
            }

            Shell.Current.CurrentPage.ForceLayout();
        }
    }
}
