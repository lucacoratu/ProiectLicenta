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

        [ObservableProperty]
        private string addFriendEntryText;

        [ObservableProperty]
        private string statusBackgroundColor;

        [ObservableProperty]
        private string statusStrokeColor;

        private FriendService friendService;
        private ChatService chatService;
        private SignalingService signalingService;
        public ObservableCollection<FriendStatusModel> Friends { get; } = new();

        public ObservableCollection<FriendStatusModel> CreateGroupSelectedFriends { get; set; } = new();

        public ObservableCollection<GroupModel> Groups { get; } = new();

        public ObservableCollection<FriendRequestModel> FriendRequests { get; } = new();

        public ObservableCollection<FriendRequestModel> SentFriendRequests { get; } = new();

        public MainViewModel(FriendService friendService, ChatService chatService, SignalingService signalingService)
        {
            this.friendService = friendService;
            this.chatService = chatService;
            this.chatService.RegisterReadCallback(MessageReceivedOnWebsocket);
            this.signalingService = signalingService;
            this.signalingService.RegisterReadCallback(MessageReceivedOnSignalingWebsocket);
        }

        public async Task MessageReceivedOnSignalingWebsocket(string message)
        {
            int i = 0;
        }

        //This function will be a callback for when a message will be received on the websocket
        public async Task MessageReceivedOnWebsocket(string message)
        {
            //TO DO...Change the format of the messages for better validation of data received!!!

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

            //It is a message specifing the new status of a user
            if (message.IndexOf("Change status") != -1)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                //Parse the response
                ChangeStatusWebsocketResponseModel resp = JsonSerializer.Deserialize<ChangeStatusWebsocketResponseModel>(message, options);
                //Search through the friend accounts and see if the message that the account involved is in the list of friends
                for(int i =0; i < this.Friends.Count; i++)
                {
                    if (this.Friends[i].FriendId == resp.AccountId)
                    {
                        //Change the color depending on which status it is set
                        if(resp.NewStatus == "Online")
                        {
                            this.Friends[i].Status = "Online";
                            this.Friends[i].StatusBackgroundColor = Colors.Green;
                            this.Friends[i].StatusStrokeColor = Colors.DarkGreen;
                        }
                        else
                        {
                            this.Friends[i].Status = "Offline";
                            this.Friends[i].StatusBackgroundColor = Colors.Gray;
                            this.Friends[i].StatusStrokeColor = Colors.DarkGray;
                        }
                    }
                }
                return;
            }

            //Some friend is calling
            if(message.IndexOf("callee") != -1) {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                //Parse the response
                CallFriendModel cfm = JsonSerializer.Deserialize<CallFriendModel>(message, options);
                //Search through all the friends and find the one that is calling
                if(cfm != null) {
                    if (cfm.option == "Call")
                    {
                        for (int i = 0; i < this.Friends.Count; i++)
                        {
                            if (this.Friends[i].FriendId == cfm.caller)
                            {
                                //Go to the CalleePage
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                    await Shell.Current.GoToAsync(nameof(CalleePage), true, new Dictionary<string, object>
                                    {
                                    {"roomID", cfm.roomId},
                                    {"account", this.Account},
                                    {"friend", this.Friends[i]},
                                    {"audio", false },
                                    {"video", false },
                                    })
                                );
                                break;
                            }
                        }
                    }
                    if (cfm.option == "Cancel")
                    {
                        //Go back to where the application was before the call
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                            await Shell.Current.Navigation.PopAsync()
                        );
                    }
                    if(cfm.option == "Answer")
                    {
                        //The friend answered the call
                        for (int i = 0; i < this.Friends.Count; i++)
                        {
                            if (this.Friends[i].FriendId == cfm.caller)
                            {
#if ANDROID
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                    await Shell.Current.GoToAsync(nameof(AndroidCallPage), true, new Dictionary<string, object>
                                    {
                                        { "roomID", cfm.roomId },
                                        { "account", account },
                                        { "friend", this.Friends[i] },
                                        { "audio", true },
                                        { "video", true },
                                    }));
#else
                                await MainThread.InvokeOnMainThreadAsync(async () => 
                                    await Shell.Current.GoToAsync(nameof(WindowsCallPage), true, new Dictionary<string, object>
                                    {
                                        {"roomID", cfm.roomId },
                                        {"account", account },
                                        {"friend", this.Friends[i] },
                                        {"audio", true},
                                        {"video", true },
                                    }));
#endif
                                break;
                            }
                        }
                    }
                    if(cfm.option == "Deny")
                    {
                        //The friend denied the call
                        //Go back to where the application was before the call
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                            await Shell.Current.Navigation.PopAsync()
                        );
                    }
                }
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
                return;
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
                                List<FriendStatusModel> CopyFriends = new();
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
                        //Check if the message is from a group
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
        async Task Tap(FriendStatusModel f)
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
                    //Update the date format to show only the hour if the message is from Today
                    if (friend.LastMessageTimestamp != "")
                    {
                        //string messageTimestamp = DateTime.Parse(friend.LastMessageTimestamp).ToString("HH:mm");
                        //friend.LastMessageTimestamp = messageTimestamp;
                        DateTime messageDate = DateTime.Parse(friend.LastMessageTimestamp);
                        double diffDays = (DateTime.Now - messageDate).TotalDays;
                        if (diffDays <= 1.0 && diffDays >= 0.0)
                        {
                            string messageTimestamp = messageDate.ToString("HH:mm");
                            friend.LastMessageTimestamp = messageTimestamp;
                        }
                        else if (diffDays > 1.0 && diffDays <= 2.0)
                        {
                            friend.LastMessageTimestamp = "Yesterday";
                        }
                        else
                        {
                            friend.LastMessageTimestamp = messageDate.ToString("dddd");
                        }
                    }

                    
                    if (friend.Status == "Online")
                    {
                        FriendStatusModel friendStatusModel = new FriendStatusModel(friend, Colors.Green, Colors.DarkGreen);
                        Friends.Add(friendStatusModel);
                    } else
                    {
                        FriendStatusModel friendStatusModel = new FriendStatusModel(friend, Colors.Gray, Colors.DarkGray);
                        Friends.Add(friendStatusModel);
                    }

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
                {
                    if(group.LastMessageTimestamp != "")
                    {
                        DateTime messageDate = DateTime.Parse(group.LastMessageTimestamp);
                        double diffDays = (DateTime.Now - messageDate).TotalDays;
                        if (diffDays <= 1.0 && diffDays >= 0.0)
                        {
                            string messageTimestamp = messageDate.ToString("HH:mm");
                            group.LastMessageTimestamp = messageTimestamp;
                        }
                        else if(diffDays > 1.0 && diffDays <= 2.0)
                        {
                            group.LastMessageTimestamp = "Yesterday";
                        }
                        else 
                        {
                            group.LastMessageTimestamp = messageDate.ToString("dddd");
                        }
                    }
                    Groups.Add(group);
                }
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
            //Get all the sent friend requests of the account
            this.SentFriendRequests.Clear();
            var sentRequests = await this.friendService.GetSentFriendRequests(this.Account.Id, this.Session);
            foreach (var sentRequest in sentRequests)
                this.SentFriendRequests.Add(sentRequest);

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

            var requests = await this.friendService.GetFriendRequest(this.Account.Id, Session);
            this.FriendRequests.Clear();
            foreach(var request in requests)
                this.FriendRequests.Add(request);

            PendingSelected = true;
        }

        private int ConvertHexToFriendId(string input)
        {
            try
            {
                string hexString = "";
                for (int i = 1; i < input.Length; i++)
                    hexString += input[i];
                int friendID = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                return friendID;
            } catch (Exception ex)
            {
                //Return an invalid id if the conversion failed
                return -1;            
            }   
        }

        private bool ValidateAddFriendInput(string input) 
        {
            //Split the input
            string[] fields = input.Split(" ");
            //Check if there are 2 fields in the input (display name and id in hex)
            if(fields.Length != 2)
            {
                return false;
            }
            //Check if the id is in the correct format # 6 hex characters
            int friendID = ConvertHexToFriendId(fields[1]);
            return (friendID == -1) ? false : true;
        }

        [RelayCommand]
        async Task AddFriend()
        {
            //Send a friend request to another user
            //Get the data from the entry
            string input = this.AddFriendEntryText;
            //Validate the input before sending it
            bool res = ValidateAddFriendInput(input);
            if (res)
            {
                //Send the friend request to the specified account
                string[] fields = input.Split(' ');
                int friendId = ConvertHexToFriendId(fields[1]);
                res = await this.friendService.SendFriendRequest(this.Account.Id, friendId, this.Session);
                //Check if the request was successful
                if (res)
                {
                    //The friend request has been sent
                    await Shell.Current.DisplayAlert("FriendRequest", "FriendRequest has been sent", "Ok");
                } else
                {
                    //The friend request couldn't be sent
                    await Shell.Current.DisplayAlert("FriendRequest", "FriendRequest couldn't be sent!", "Ok");
                }
            }
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

        public void CreateGroupSelectionChanged(List<FriendStatusModel> newList)
        {
            if (newList == null)
                return;
            //If the friend already exists in the list then remove it
            if (this.CreateGroupSelectedFriends.Count != 0)
                this.CreateGroupSelectedFriends.Clear();

            foreach(FriendStatusModel f in newList) {
                this.CreateGroupSelectedFriends.Add(f);
            }
        }

        [RelayCommand]
        async Task CreateGroup()
        {
            //Create the message to create a group
            CreateGroupMessageModel createGroupMessageModel = new CreateGroupMessageModel();
            if (this.GroupName == "" || this.GroupName == null)
                return;
            createGroupMessageModel.groupName = this.GroupName;
            createGroupMessageModel.creatorID = this.account.Id;
            createGroupMessageModel.participants = new();
            foreach(FriendStatusModel f in this.CreateGroupSelectedFriends)
            {
                createGroupMessageModel.participants.Add(f.FriendId);
            }

            string jsonMessage = JsonSerializer.Serialize(createGroupMessageModel);
            this.chatService.SendMessageAsync(jsonMessage);
        }

        [RelayCommand]
        async Task CreateGroupMobile()
        {
            await Shell.Current.GoToAsync(nameof(CreateGroupPage), true);
        }

        [RelayCommand]
        async Task ExitCreateGroup()
        {
            await Shell.Current.Navigation.PopAsync();
        }

        [RelayCommand]
        async Task AddFriendMobile()
        {
            await Shell.Current.GoToAsync(nameof(AddFriendPage), true);
        }

        [RelayCommand]
        async Task ExitAddFriend()
        {
            await Shell.Current.Navigation.PopAsync();
        }


        [RelayCommand]
        async Task FriendRequestMobile()
        {
            await Shell.Current.GoToAsync(nameof(FriendRequestPage), true);
            var requests = await this.friendService.GetFriendRequest(this.Account.Id, Session);
            this.FriendRequests.Clear();
            foreach (var request in requests)
                this.FriendRequests.Add(request);
        }

        [RelayCommand]
        async Task ExitFriendRequests()
        {
            _ = await Shell.Current.Navigation.PopAsync();
        }

        [RelayCommand]
        async Task AcceptFriendRequest(FriendRequestModel f)
        {
            //Accept the friend request from the user
            int myId = this.Account.Id;
            int friendId = f.AccountID;
            bool res = await this.friendService.AcceptFriendRequest(myId, friendId, Session);
            //Check if the request was successful
            if (res)
            {
                //Remove the friend request from the list of pending friend requests
                this.FriendRequests.Remove(f);
                //Update the list of friends
                //var friends = await friendService.GetFriends(myId, Session);
                //if (this.Friends.Count != 0)
                //{
                //    this.Friends.Clear();
                //}

                //foreach (var friend in friends)
                //{
                //    //Update the date format to show only the hour if the message is from Today
                //    if (friend.LastMessageTimestamp != "")
                //    {
                //        string messageTimestamp = DateTime.Parse(friend.LastMessageTimestamp).ToString("HH:mm");
                //        friend.LastMessageTimestamp = messageTimestamp;
                //    }
                //    this.Friends.Add(friend);
                //}
                await GetFriendsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Cannot accept the friend request", "Ok");
            }
        }

        [RelayCommand]
        async Task DeclineFriendRequest(FriendRequestModel f)
        {
            //Decline the friend request
            int myId = this.Account.Id;
            int friendId = f.AccountID;
            bool res = await this.friendService.DeclineFriendRequest(myId, friendId, Session);
            //Remove the friend request from the list of pending friend requests
            if (res)
            {
                this.FriendRequests.Remove(f);
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Cannot decline the friend request", "Ok");
            }
        }

        [RelayCommand]
        async Task GoToReportABug()
        {
            await Shell.Current.GoToAsync(nameof(ReportABugPage), true);
        }
    }
}
