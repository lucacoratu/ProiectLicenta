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

        public ObservableCollection<MessageGroupModel> MessageGroups { get; } = new();

        private int roomId;

        private ChatService chatService;

        private DatabaseService databaseService;
        public ChatViewModel(ChatService chatService, DatabaseService databaseService)
        {
            this.chatService = chatService;
            this.databaseService = databaseService;
            this.chatService.RegisterReadCallback(MessageReceivedOnWebsocket);
            MessagingCenter.Subscribe<MainViewModel, PrivateMessageModel>(this, "Private message received", this.PrivateMessageReceived);
        }

        ~ChatViewModel() {
            MessagingCenter.Unsubscribe<MainViewModel, PrivateMessageModel>(this, "Private message received");
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
                    NoMessages = false;
                    return;
                }
                else {
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
                    NoMessages = false;
                    return;
                }
            }
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
                                if (Int32.Parse(messageModel.MessageId) == srm.messageId) {
                                    messageModel.Reactions.Add(new ReactionModel { Id = 0, Emoji = srm.emojiReaction, ReactionDate = DateTime.Now.ToString("dd MMMM yyyy"), SenderId = srm.senderId });
                                    return;
                                }
                            }
                        }
                        return;
                    }
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
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

                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageId = message.Id.ToString(), Text = message.Text, TimeStamp = msgDate };
                    //if (.Reactions != null) {
                    //    foreach (var reaction in historyMessage.Reactions)
                    //        msgModel.Reactions.Add(reaction);
                    //}

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


                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageId = message.Id.ToString(), Text = message.Text, TimeStamp = msgDate };
                    //if (historyMessage.Reactions != null) {
                    //    foreach (var reaction in historyMessage.Reactions)
                    //        msgModel.Reactions.Add(reaction);
                    //}

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
            System.Threading.Thread.Sleep(1000);
            await Shell.Current.Navigation.PopAsync(true);
        }

        [RelayCommand]
        public async void TakePhoto()
        {
            try {
                if (MediaPicker.Default.IsCaptureSupported) {
                    FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

                    if (photo != null) {
                        // save the file into local storage
                        string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

                        using Stream sourceStream = await photo.OpenReadAsync();
                        using FileStream localFileStream = File.OpenWrite(localFilePath);

                        await sourceStream.CopyToAsync(localFileStream);
                    }
                }
            } catch(Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        [RelayCommand]
        public async Task PickFile()
        {
            try {
                var res = await FilePicker.PickMultipleAsync(new PickOptions { PickerTitle = "Select file(s)", FileTypes = FilePickerFileType.Images });
                if (res == null)
                    return;
            } catch(Exception ex) { 
                Console.WriteLine(ex.ToString());
            }
        }

        [RelayCommand]
        public async Task SendMessage()
        {
            //Before sending the message check if the user has internet connection
            //Check if the application is connected to the chat service via websockets

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
            //Create the model object which will be sent as a json to the server
            SendReactionModel srm = new SendReactionModel { messageId = messageId, emojiReaction = emojiReaction, senderId = this.Account.Id, roomId = this.roomId };
            string jsonMessage = JsonSerializer.Serialize(srm);
            await this.chatService.SendMessageAsync(jsonMessage);
        }
    }
}
