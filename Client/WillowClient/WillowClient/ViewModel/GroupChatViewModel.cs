using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WillowClient.Database;
using WillowClient.Model;
using WillowClient.Services;
using WillowClient.Views;
using CommunityToolkit.Maui.Views;
using System.Security.Cryptography;

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

        [ObservableProperty]
        private bool loadingMessages;

        [ObservableProperty]
        private bool noMessages;

        //public ObservableCollection<MessageModel> Messages { get; } = new();

        private ChatService chatService;

        private ProfileService profileService;

        private DatabaseService databaseService;

        public List<MessageModel> Messages { get; } = new();

        public ObservableCollection<GroupParticipantModel> Participants { get; set; } = new();

        [ObservableProperty]
        private string participantNames;

        [ObservableProperty]
        private bool entryEnabled = true;

        public ObservableCollection<MessageGroupModel> MessageGroups { get; } = new();

        private int roomId;

        bool isInConversation = true;

        public GroupChatViewModel(ChatService chatService, ProfileService ps, DatabaseService databaseService)
        {
            this.chatService = chatService;
            this.chatService.RegisterReadCallback(MessageReceivedOnWebsocket);
            this.profileService = ps;
            this.databaseService = databaseService;
        }

        public async Task MessageReceivedOnWebsocket(string message)
        {
            //Check if the message is from the user in the current conversation
            //Parse the JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            //Check if the message is new reaction
            if (message.IndexOf("emojiReaction") != -1) {
                try {
                    //Parse the JSON response
                    SendReactionModel srm = JsonSerializer.Deserialize<SendReactionModel>(message, options);
                    if (srm != null) {
                        //Add the reaction to the specific message
                        //Add a new reaction in the collection view
                        foreach (var group in this.MessageGroups) {
                            foreach (var messageModel in group) {
                                if (Int32.Parse(messageModel.MessageId) == srm.messageId)
                                    messageModel.Reactions.Add(new ReactionModel { Id = 0, Emoji = srm.emojiReaction, ReactionDate = DateTime.Now.ToString("dd MMMM yyyy"), SenderId = srm.senderId });
                            }
                        }
                        return;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e.ToString());
                    return;
                }
            }

            if (message.IndexOf("callee") != -1) {
                return;
            }

            if (message.IndexOf("data") != -1) {
                try {
                    //Parse the JSON body of the message
                    PrivateMessageModel? privMessageModel = JsonSerializer.Deserialize<PrivateMessageModel>(message, options);
                    if (privMessageModel != null) {
                        //This is a private message received from another user
                        //Check if the sender is the current user from the private conversation
                        if (privMessageModel.SenderId != this.Account.Id) {
                            //This is the friend that sent the message
                            //this.Messages.Add(new MessageModel
                            //{
                            //    Owner = MessageOwner.OtherUser,
                            //    Text = privMessageModel.Data,
                            //    TimeStamp = DateTime.Now.ToString("HH:mm")
                            //});
                            int senderIndex = this.Group.Participants.IndexOf(privMessageModel.SenderId);
                            MessageModel msgModel = new MessageModel { SenderName = this.Group.ParticipantNames[senderIndex] , Owner = MessageOwner.OtherUser, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), MessageId = privMessageModel.Id.ToString() };
                            bool added = false;
                            foreach (var e in this.MessageGroups) {
                                if (e.Name == "Today") {
                                    e.Add(msgModel);
                                    added = true;
                                    //e.Name = "Today";
                                }
                            }
                            if (!added) {
                                List<MessageModel> messageModels = new();
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
                                    e.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), SenderName = "You", MessageId = privMessageModel.Id.ToString() });
                                    //e.Name = "Today";
                                    found = true;
                                }
                            }
                            //If the group Today is not found (no messages were exchanges in current they) then create the group and add the message in the group
                            if (!found) {
                                List<MessageModel> messages = new();
                                messages.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = privMessageModel.Data, TimeStamp = DateTime.Now.ToString("HH:mm"), SenderName = "You", MessageId = privMessageModel.Id.ToString() });
                                this.MessageGroups.Add(new MessageGroupModel("Today", messages));
                            }
                            NoMessages = false;
                        }

                        if (isInConversation)
                            this.Group.SeenAllNewMessages();
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }

        }

        public async void LoadLocalMessages() {
            try {
                //If there are any elements in the list then clear it
                if (Messages.Count != 0)
                    Messages.Clear();

                //The user got all the new messages
                this.Group.SeenAllNewMessages();
                await this.databaseService.UpdateNewMessagesForGroup(this.Group.RoomId, int.Parse(this.Group.NumberNewMessages));

                Dictionary<string, List<MessageModel>> groupsAndMessages = new();

                await foreach (var message in this.databaseService.GetLocalMessagesInRoom(this.Group.RoomId)) {
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
                        foreach (var messageGroup in this.MessageGroups) {
                            if (messageGroup.Name == group) {
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
                        if (message.Type == "Text")
                            msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Text, MessageId = message.Id.ToString(), Text = message.Text, SenderName = message.SenderName , TimeStamp = msgDate };
                        if (message.Type == "Attachment") {
                            //Parse the message
                            var attachment = JsonSerializer.Deserialize<AttachmentModel>(message.Text);
                            var dbAttachment = await this.databaseService.GetAttachment(attachment.BlobUuid);
                            if (attachment.AttachmentType == "photo") {
                                if (dbAttachment != null) {
                                    //Show the message as photo if it is downloaded
                                    if (dbAttachment.Downloaded) {
                                        msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), SenderName = message.SenderName, TimeStamp = msgDate, IsDownloaded = true, MediaStream = ImageSource.FromFile(dbAttachment.LocalFilepath) };
                                    }
                                    else {
                                        //Else show the message as downloadable attachment
                                        msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), SenderName = message.SenderName, TimeStamp = msgDate, IsDownloaded = false };
                                    }
                                }
                            }
                            if (attachment.AttachmentType == "video") {
                                if (dbAttachment != null) {
                                    //Show the message as photo if it is downloaded
                                    if (dbAttachment.Downloaded) {
                                        msgModel = new MessageModel { SenderName = message.SenderName, FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.OtherUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true, VideoStream = MediaSource.FromFile(dbAttachment.LocalFilepath) };
                                    }
                                    else {
                                        //Else show the message as downloadable attachment
                                        msgModel = new MessageModel { SenderName = message.SenderName, FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.OtherUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                    }
                                }
                            }
                            if (attachment.AttachmentType == "file") {
                                if (dbAttachment != null) {
                                    if (dbAttachment.Downloaded) {
                                        msgModel = new MessageModel { SenderName = message.SenderName, FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.OtherUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                    }
                                    else {
                                        msgModel = new MessageModel { SenderName = message.SenderName, FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.OtherUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true };
                                    }
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
                        if (message.Type == "Text")
                            msgModel = new MessageModel { SenderName = "You", Owner = MessageOwner.CurrentUser, MessageType = MessageType.Text, MessageId = message.Id.ToString(), Text = message.Text, TimeStamp = msgDate };
                        if (message.Type == "Attachment") {
                            //Parse the message
                            var attachment = JsonSerializer.Deserialize<AttachmentModel>(message.Text);
                            var dbAttachment = await this.databaseService.GetAttachment(attachment.BlobUuid);
                            if (attachment.AttachmentType == "photo") {
                                if (dbAttachment != null) {
                                    //Show the message as photo if it is downloaded
                                    if (dbAttachment.Downloaded) {
                                        msgModel = new MessageModel { SenderName = "You", Owner = MessageOwner.CurrentUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true, MediaStream = ImageSource.FromFile(dbAttachment.LocalFilepath) };
                                    }
                                    else {
                                        //Else show the message as downloadable attachment
                                        msgModel = new MessageModel { SenderName = "You", Owner = MessageOwner.CurrentUser, MessageType = MessageType.Photo, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                    }
                                }
                            }
                            if (attachment.AttachmentType == "video") {
                                if (dbAttachment != null) {
                                    //Show the message as photo if it is downloaded
                                    if (dbAttachment.Downloaded) {
                                        msgModel = new MessageModel { SenderName = "You", FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.CurrentUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true, VideoStream = MediaSource.FromFile(dbAttachment.LocalFilepath) };
                                    }
                                    else {
                                        //Else show the message as downloadable attachment
                                        msgModel = new MessageModel { SenderName = "You", FileSizeString = attachment.FileSizeFormat, Owner = MessageOwner.CurrentUser, MessageType = MessageType.Video, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                    }
                                }
                            }
                            if (attachment.AttachmentType == "file") {
                                if (dbAttachment != null) {
                                    msgModel = new MessageModel { SenderName = "You", FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.CurrentUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = false };
                                }
                                else {
                                    msgModel = new MessageModel { SenderName = "You", FileSizeString = attachment.FileSizeFormat, Filename = attachment.Filename, Owner = MessageOwner.CurrentUser, MessageType = MessageType.File, MessageId = message.Id.ToString(), TimeStamp = msgDate, IsDownloaded = true };
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
            catch(Exception e) {
                await Shell.Current.DisplayAlert("Error", e.Message, "Ok");
                return;
            }
        }

        public async void GetHistory()
        {
            //If there are any elements in the list then clear it
            if (Messages.Count != 0)
                Messages.Clear();

            LoadingMessages = true;
            Dictionary<string, List<MessageModel>> groupsAndMessages = new();

            int roomId = this.Group.RoomId;
            if (Group.LastMessageSender == 0 && Group.LastMessageTimestamp == null) {
                LoadingMessages = false;
                NoMessages = true;
                return;
            }

            await foreach (var historyMessage in this.chatService.GetMessageHistoryAsync(roomId))
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

                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.OtherUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate, SenderName = "Unknown" };
                    if (historyMessage.Reactions != null) {
                        foreach (var reaction in historyMessage.Reactions)
                            msgModel.Reactions.Add(reaction);
                    }
                    

                    //Find the sender name of the message
                    int indexSender = this.Group.Participants.IndexOf(historyMessage.UserId);
                    if (indexSender != -1) {
                        msgModel.Owner = MessageOwner.OtherUser;
                        msgModel.SenderName = this.Group.ParticipantNames[indexSender];
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

                    MessageModel msgModel = new MessageModel { Owner = MessageOwner.CurrentUser, MessageId = historyMessage.Id.ToString(), Text = historyMessage.Data, TimeStamp = msgDate, SenderName = "You" };
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

            foreach (var e in groupsAndMessages)
            {
                this.MessageGroups.Add(new MessageGroupModel(e.Key as string, e.Value as List<MessageModel>));
            }
            LoadingMessages = false;
        }

        [RelayCommand]
        public async void CreateGroupParticipantsList() {
            if(this.ParticipantNames != "")
                this.ParticipantNames = "";
            if (this.ParticipantNames == null)
                this.ParticipantNames = "";

            this.ParticipantNames += "You, ";
            if (this.Group.ParticipantNames != null) {
                for (int i = 0; i < this.Group.ParticipantNames.Count; i++) {
                    if (i != this.Group.ParticipantNames.Count - 1)
                        this.ParticipantNames += this.Group.ParticipantNames[i] + ", ";
                    else
                        this.ParticipantNames += this.Group.ParticipantNames[i];
                }
            }
        }




        [RelayCommand]
        public async Task GoBack()
        {
            this.EntryEnabled = false;
            this.isInConversation = false;
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
            if (this.MessageText == null || this.MessageText == "")
                return;

            //If the message is the first sent message by this user
            //For every participant in the group
            //Generate the chain key and the signature key pair
            //Generate the message key used to encrypt the chain key and the signature key pair
            //Upload the encrypted chain key and the signature public key to the server
            //Derive the message key from the chain key
            //Encrypt the message data using the message key
            //Send the message with the encrypted chain key and the signed pub key

            //Get the number of messages send by the user in the group
            //int numberSentMessagesInGroup = await this.databaseService.GetNumberUserSentMessages(this.Group.RoomId, this.Account.Id);
            //if (numberSentMessagesInGroup == 0) {
            //    //Generate the chain key (32 bytes)
            //    Random random = new Random();
            //    byte[] chainKey = new byte[32];
            //    random.NextBytes(chainKey);
            //    var signatureKeyPair = Encryption.Utils.GenerateX25519Key();
            //    //Get the participants of the group
            //    var participants = await this.databaseService.GetGroupParticipants(this.Group.RoomId);
            //    foreach(var participant in participants) {
            //        //Get the participant keys
            //        var keys = await this.databaseService.GetParticipantKeysForGroup(this.Group.RoomId, participant.Id);
            //        if(keys == null) {
            //            //Get the keys from the server
            //        }
            //        var ephemeralKey = Encryption.Utils.GenerateX25519Key();
            //        string ephemeralPrivate = ephemeralKey.ExportECPrivateKeyPem();
            //        var ephemeralPublic = ephemeralKey.ExportSubjectPublicKeyInfoPem();
            //        var masterSecret = Encryption.Utils.ComputeSenderMasterSecret(ephemeralPrivate, keys.IdentityPublicKey, keys.PreSignedPublicKey);
            //        var rootChainKeys = Encryption.Utils.GenerateRootAndChainKeyFromMasterSecret(masterSecret);
            //        //Generate the message key
            //        var messageChainKey = Encryption.Utils.GenerateMessageKey(rootChainKeys.ChainKey);
            //        var encryptedChainKey = Encryption.Utils.EncryptMessage(System.Convert.ToBase64String(chainKey), messageChainKey.MessageKey);
            //        //System.Buffer.BlockCopy(messageChainKey.MessageKey, 0, messageKey, 0, messageChainKey.MessageKey.Length);
            //    }
            //}
            //else {

            //}
            //return;


            //Create the structure that will hold the data which will be json encoded and sent to the server
            SendPrivateMessageModel sendMessageModel = new SendPrivateMessageModel { roomId = this.Group.RoomId, data = this.MessageText, messageType = "Text" };
            string jsonMessage = JsonSerializer.Serialize(sendMessageModel);
            await this.chatService.SendMessageAsync(jsonMessage);
            //this.Messages.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = this.MessageText, TimeStamp = DateTime.Now.ToString("HH:mm") });
            //Clear the entry text
            this.MessageText = "";
            //Scroll to the end
        }

        [RelayCommand]
        public async Task GoToGroupDetails() {
            await Shell.Current.GoToAsync(nameof(GroupDetailsPage), true, new Dictionary<string, object> {
                {"group", this.Group },
                {"account", this.Account },
            });
        }

        [RelayCommand]
        public async Task GoToGroupCall() {
#if ANDROID
            await Shell.Current.GoToAsync(nameof(AndroidGroupCallPage), true, new Dictionary<string, object>
            {
                {"roomID", Group.RoomId },
                {"account", Account },
                {"friend", null},
                {"audio", true},
                {"video", true },
            });
#else
            await Shell.Current.GoToAsync(nameof(WindowsGroupCallPage), true, new Dictionary<string, object>
                {
                {"roomID", Group.RoomId },
                {"account", Account },
                {"friend", null},
                {"audio", true},
                {"video", true },
            });
#endif
        }

        public async void ReactToMessage(int messageId, string emojiReaction) {
            //Create the model object which will be sent as a json to the server
            SendReactionModel srm = new SendReactionModel { messageId = messageId, emojiReaction = emojiReaction, senderId = this.Account.Id, roomId = Group.RoomId };
            string jsonMessage = JsonSerializer.Serialize(srm);
            await this.chatService.SendMessageAsync(jsonMessage);
        }
    }
}
