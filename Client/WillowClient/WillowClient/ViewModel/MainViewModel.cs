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
    [QueryProperty(nameof(Account), "account")]
    [QueryProperty(nameof(HexID), "hexID")]
    [QueryProperty(nameof(Session), "session")]
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private string session;

        [ObservableProperty]
        private string hexID;

        [ObservableProperty]
        private bool addFriendSelected;

        [ObservableProperty]
        private bool pendingSelected;

        [ObservableProperty]
        private bool createGroupSelected;

        [ObservableProperty]
        private string groupName;

        private FriendService friendService;
        private ChatService chatService;
        public ObservableCollection<FriendModel> Friends { get; } = new();

        public ObservableCollection<FriendModel> CreateGroupSelectedFriends { get; set; } = new();

        public ObservableCollection<GroupModel> Groups { get; } = new();

        public MainViewModel(FriendService friendService, ChatService chatService)
        {
            this.friendService = friendService;
            this.chatService = chatService;
            this.chatService.RegisterReadCallback(MessageReceivedOnWebsocket);
        }

        //This function will be a callback for when a message will be received on the websocket
        public async Task MessageReceivedOnWebsocket(string message)
        {
            //Check if the message received is the first message that the server sends
            if (message.IndexOf("New User") != -1)
            {
                //It is the first message received from the server, so set the accountId of the connection
                string hexString = "";
                for (int i = 1; i < hexID.Length; i++)
                    hexString += hexID[i];
                var accountId = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                string setAccountIdMessage = "{\"setAccountId\":" + accountId.ToString() + "}";
                this.chatService.SendMessageAsync(setAccountIdMessage);
                return;
            }

            //It is a message from another user
            if(message.IndexOf("groupName") != -1)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                //Parse the response
                CreateGroupResponseModel resp = JsonSerializer.Deserialize<CreateGroupResponseModel>(message, options);

                //Notify that the group has been created (move the client to the group page)
                //await Shell.Current.DisplayAlert("Group created!", "Group" + resp.GroupName + "has been created", "Ok");

                GroupModel gm = new GroupModel();
                gm.CreatorId = resp.CreatorId;
                gm.GroupName = resp.GroupName;
                gm.RoomId = resp.RoomId;
                //gm.Participants = resp.Participants as List<int>;

                this.Groups.Insert(0, gm);

                //Clear the friends selected for the group
                this.CreateGroupSelectedFriends.Clear();
                this.CreateGroupSelected = false;
            }

            if (message.IndexOf("data") != -1)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    //Parse the JSON body of the message
                    PrivateMessageModel? privMessageModel = JsonSerializer.Deserialize<PrivateMessageModel>(message, options);
                    if (privMessageModel != null)
                    {
                        //This is a private message received from another user
                        //Check if the sender is the current user from the private conversation
                        for (int i = 0; i < this.Friends.Count; i++)
                        {
                            if (privMessageModel.RoomId == this.Friends[i].RoomID)
                            {
                                //Create the hour format from the date the message was sent
                                var timestamp = DateTime.Now.ToString("HH:mm");
                                if (privMessageModel.SenderId == this.Account.Id)
                                {
                                    //Update the last message sent in the conversation
                                    this.Friends[i].LastMessage = "You: " + privMessageModel.Data;
                                    this.Friends[i].LastMessageTimestamp = timestamp;
                                }
                                else
                                {
                                    this.Friends[i].LastMessage = privMessageModel.Data;
                                    this.Friends[i].LastMessageTimestamp = timestamp;
                                }
                                //Move the conversation to the top
                                List<FriendModel> CopyFriends = new();
                                CopyFriends.Add(this.Friends[i]);
                                for(int index = 0; index < this.Friends.Count; index++)
                                {
                                    if(index != i)
                                        CopyFriends.Add(this.Friends[index]);
                                }
                                //this.Friends.Clear();
                                //System.Threading.Thread.Sleep(100);
                                for(int index =0; index < CopyFriends.Count; index++)
                                    this.Friends[index] = CopyFriends[index];

                                return;
                            }
                        }
                        //Check if the message if from a group
                        for(int i =0; i < this.Groups.Count; i++)
                        {
                            if(privMessageModel.RoomId == this.Groups[i].RoomId)
                            {
                                if(privMessageModel.SenderId == this.Account.Id)
                                {
                                    this.Groups[i].LastMessage = "You: " + privMessageModel.Data;
                                }
                                else
                                {
                                    this.Groups[i].LastMessage = privMessageModel.Data;
                                }
                                //Move the conversation to the top
                                List<GroupModel> CopyGroups = new();
                                CopyGroups.Add(this.Groups[i]);
                                for (int index = 0; index < this.Groups.Count; index++)
                                {
                                    if (index != i)
                                        CopyGroups.Add(this.Groups[index]);
                                }
                                //this.Friends.Clear();
                                //System.Threading.Thread.Sleep(100);
                                for (int index = 0; index < CopyGroups.Count; index++)
                                    this.Groups[index] = CopyGroups[index];

                                return;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public async void LoadData()
        {
            await GetFriendsAsync();
            await GetGroupsAsync();
        }

        [RelayCommand]
        async Task Tap(FriendModel f)
        {
            await Shell.Current.GoToAsync(nameof(ChatPage), true, new Dictionary<string, object>
                {
                    {"friend", f},
                    {"account", Account},
                });
        }

        [RelayCommand]
        async Task TapGroup(GroupModel group)
        {
            await Shell.Current.GoToAsync(nameof(GroupChatPage), true, new Dictionary<string, object>
                {
                    {"group", group},
                    {"account", Account},
                });
        }

        [RelayCommand]
        async Task GetFriendsAsync()
        {
            try
            {
                string hexString = "";
                for(int i = 1; i < hexID.Length; i++)
                    hexString += hexID[i];
                var friends = await friendService.GetFriends(int.Parse(hexString, System.Globalization.NumberStyles.HexNumber), Session);
                if (Friends.Count != 0)
                {
                    Friends.Clear();
                }

                foreach (var friend in friends)
                {
                    //Update the date format to show only the hour
                    if (friend.LastMessageTimestamp != "")
                    {
                        string messageTimestamp = DateTime.Parse(friend.LastMessageTimestamp).ToString("HH:mm");
                        friend.LastMessageTimestamp = messageTimestamp;
                    }
                    Friends.Add(friend);
                }
            }
            catch(Exception e)
            {
                //Debug.WriteLine(e);
                await Shell.Current.DisplayAlert("Error!", $"Unable to get friends: {e.Message}", "OK");
            }
            finally
            {

            }
        }

        async Task GetGroupsAsync()
        {
            try
            {
                string hexString = "";
                for (int i = 1; i < hexID.Length; i++)
                    hexString += hexID[i];
                var groups = await chatService.GetGroups(int.Parse(hexString, System.Globalization.NumberStyles.HexNumber), Session);
                if (Groups.Count != 0)
                {
                    Groups.Clear();
                }

                foreach (var group in groups)
                    Groups.Add(group);
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
                await Shell.Current.DisplayAlert("Error!", $"Unable to get groups: {e.Message}", "OK");
            }
            finally
            {

            }
        }

        [RelayCommand]
        async Task SelectAddFriend()
        {
            if (PendingSelected == true)
                PendingSelected = false;

            if (CreateGroupSelected == true)
                CreateGroupSelected = false;

            if (AddFriendSelected == true)
            {
                AddFriendSelected = false;
                return;
            }
            AddFriendSelected = true;
        }

        [RelayCommand]
        async Task SelectPending()
        {
            if (AddFriendSelected)
                AddFriendSelected = false;

            if (CreateGroupSelected == true)
                CreateGroupSelected = false;

            if (PendingSelected == true)
            {
                PendingSelected = false;
                return;
            }

            PendingSelected = true;
        }

        [RelayCommand]
        async Task SelectCreateGroup()
        {
            if (AddFriendSelected)
                AddFriendSelected = false;

            if (PendingSelected == true)
                PendingSelected = false;

            if(CreateGroupSelected == true)
            {
                CreateGroupSelected = false;
                return;
            }

            CreateGroupSelected = true;
        }

        public void CreateGroupSelectionChanged(List<FriendModel> newList)
        {
            if (newList == null)
                return;
            //If the friend already exists in the list then remove it
            if (this.CreateGroupSelectedFriends.Count != 0)
                this.CreateGroupSelectedFriends.Clear();

            foreach(FriendModel f in newList) {
                this.CreateGroupSelectedFriends.Add(f);
            }
        }

        [RelayCommand]
        async Task CreateGroup()
        {
            //Create the message to create a group
            CreateGroupMessageModel createGroupMessageModel = new CreateGroupMessageModel();
            if (this.GroupName == "")
                return;
            createGroupMessageModel.groupName = this.GroupName;
            createGroupMessageModel.creatorID = this.account.Id;
            createGroupMessageModel.participants = new();
            foreach(FriendModel f in this.CreateGroupSelectedFriends)
            {
                createGroupMessageModel.participants.Add(f.FriendId);
            }

            string jsonMessage = JsonSerializer.Serialize(createGroupMessageModel);
            this.chatService.SendMessageAsync(jsonMessage);
        }
    }
}
