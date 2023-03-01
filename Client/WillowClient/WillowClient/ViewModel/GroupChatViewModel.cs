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
using WillowClient.Views;

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
        //public ObservableCollection<MessageModel> Messages { get; } = new();

        private ChatService chatService;

        private ProfileService profileService;

        public List<MessageModel> Messages { get; } = new();

        public ObservableCollection<GroupParticipantModel> Participants { get; set; } = new();

        [ObservableProperty]
        private string participantNames;

        [ObservableProperty]
        private bool entryEnabled = true;

        public ObservableCollection<MessageGroupModel> MessageGroups { get; } = new();

        private int roomId;

        public GroupChatViewModel(ChatService chatService, ProfileService ps)
        {
            this.chatService = chatService;
            this.chatService.RegisterReadCallback(MessageReceivedOnWebsocket);
            this.profileService = ps;
        }

        public async Task MessageReceivedOnWebsocket(string message)
        {
            //Check if the message is from the user in the current conversation
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
                    //This is a private message received from another user
                    //Check if the sender is the current user from the private conversation
                    if (privMessageModel.SenderId != this.Account.Id)
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        public async void GetHistory()
        {
            //If there are any elements in the list then clear it
            if (Messages.Count != 0)
                Messages.Clear();

            Dictionary<string, List<MessageModel>> groupsAndMessages = new();

            int roomId = this.Group.RoomId;
            if (Group.LastMessageSender == 0 && Group.LastMessageTimestamp == null) {
                return;
            }
            var historyMessages = await this.chatService.GetMessageHistory(roomId);
            foreach (var historyMessage in historyMessages)
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

                    //Find the sender name of the message
                    int indexSender = this.Group.Participants.IndexOf(historyMessage.UserId);
                    if (indexSender != -1) {
                        groupsAndMessages[group].Add(new MessageModel { Owner = MessageOwner.OtherUser, Text = historyMessage.Data, TimeStamp = msgDate, SenderName = this.Group.ParticipantNames[indexSender] });
                    } else {
                        groupsAndMessages[group].Add(new MessageModel { Owner = MessageOwner.OtherUser, Text = historyMessage.Data, TimeStamp = msgDate, SenderName = "Unknown" });
                    }


                    if (indexSender != -1) {
                        this.Messages.Add(new MessageModel {
                            Owner = MessageOwner.OtherUser,
                            Text = historyMessage.Data,
                            TimeStamp = msgDate,
                            SenderName = this.Group.ParticipantNames[indexSender],
                        });
                    } else {
                        this.Messages.Add(new MessageModel {
                            Owner = MessageOwner.OtherUser,
                            Text = historyMessage.Data,
                            TimeStamp = msgDate,
                            SenderName = "Unknown",
                        });
                    }
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
                    groupsAndMessages[group].Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = historyMessage.Data, TimeStamp = msgDate, SenderName = "You" });

                    this.Messages.Add(new MessageModel
                    {
                        Owner = MessageOwner.CurrentUser,
                        Text = historyMessage.Data,
                        TimeStamp = msgDate,
                        SenderName = "You",
                    });
                }
            }
            //this.MessageGroups.Add(new MessageGroupModel("Today", this.Messages));
            foreach (var e in groupsAndMessages)
            {
                this.MessageGroups.Add(new MessageGroupModel(e.Key as string, e.Value as List<MessageModel>));
            }
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
            if (this.MessageText == null || this.MessageText == "")
                return;

            //Create the structure that will hold the data which will be json encoded and sent to the server
            SendPrivateMessageModel sendMessageModel = new SendPrivateMessageModel { roomId = this.Group.RoomId, data = this.MessageText, messageType = "Text" };
            string jsonMessage = JsonSerializer.Serialize(sendMessageModel);
            chatService.SendMessageAsync(jsonMessage);
            //Add the message into the collection view for the current user
            bool found = false;
            foreach (var e in this.MessageGroups)
            {
                if (e.Name == "Today")
                {
                    e.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = this.MessageText, TimeStamp = DateTime.Now.ToString("HH:mm"), SenderName = "You" });
                    //e.Name = "Today";
                    found = true;
                }
            }
            //If the group Today is not found (no messages were exchanges in current they) then create the group and add the message in the group
            if (!found)
            {
                List<MessageModel> messages = new();
                messages.Add(new MessageModel { Owner = MessageOwner.CurrentUser, Text = this.MessageText, TimeStamp = DateTime.Now.ToString("HH:mm"), SenderName = "You" });
                this.MessageGroups.Add(new MessageGroupModel("Today", messages));
            }
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
    }
}
