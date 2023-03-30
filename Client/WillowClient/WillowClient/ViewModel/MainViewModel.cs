using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
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
using WillowClient.Database.Model;
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

        [ObservableProperty]
        private bool loadingGroups;

        [ObservableProperty]
        private bool noGroups;

        [ObservableProperty]
        private bool loadingFriends;

        [ObservableProperty]
        private bool noFriends;

        [ObservableProperty]
        private bool hasFriends;

        [ObservableProperty]
        private bool noFriendRequests;

        [ObservableProperty]
        private bool noSentFriendRequests;

        private FriendService friendService;
        private ChatService chatService;
        private SignalingService signalingService;
        private NotificationService notificationService;
        private DatabaseService databaseService;
        private ProfileService profileService;
        public ObservableCollection<FriendStatusModel> Friends { get; } = new();
        public ObservableCollection<FriendStatusModel> CreateGroupSearchResults { get;  } = new();
        public ObservableCollection<FriendStatusModel> FriendsSearchResults { get; } = new();
        public ObservableCollection<FriendRecommendationModel> FriendRecommendations { get; } = new();
        public ObservableCollection<FriendRecommendationModel> FriendRecommendationSearchResults { get; } = new();
        private List<FriendRecommendationModel> selectedFriendRecommendations { get; } = new();
        private Stream CreateGroupPhoto { get; set; }
        public ObservableCollection<FriendStatusModel> CreateGroupSelectedFriends { get; set; } = new();
        public ObservableCollection<GroupModel> Groups { get; } = new();
        public ObservableCollection<GroupModel> GroupsSearchResults { get; } = new();   

        public ObservableCollection<FriendRequestModel> FriendRequests { get; } = new();

        public ObservableCollection<FriendRequestModel> SentFriendRequests { get; } = new();

        public MainViewModel(FriendService friendService, ProfileService profileService ,ChatService chatService, SignalingService signalingService, NotificationService notificationService, DatabaseService databaseService)
        {
            this.friendService = friendService;
            this.profileService = profileService;
            this.chatService = chatService;
            this.notificationService = notificationService;
            this.databaseService = databaseService;
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
                await this.chatService.SendMessageAsync(setAccountIdMessage);
                return;
            }

            //It is a message specifing the new status of a user
            if (message.IndexOf("Change status") != -1)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                try {
                    //Parse the response
                    ChangeStatusWebsocketResponseModel resp = JsonSerializer.Deserialize<ChangeStatusWebsocketResponseModel>(message, options);
                    if (resp != null) {
                        //Search through the friend accounts and see if the message that the account involved is in the list of friends
                        for (int i = 0; i < this.Friends.Count; i++) {
                            if (this.Friends[i].FriendId == resp.AccountId) {
                                //Change the color depending on which status it is set
                                if (resp.NewStatus == "Online") {
                                    this.Friends[i].Status = "Online";
                                    this.Friends[i].StatusBackgroundColor = Colors.Green;
                                    this.Friends[i].StatusStrokeColor = Colors.DarkGreen;
                                }
                                else {
                                    this.Friends[i].Status = "Offline";
                                    this.Friends[i].StatusBackgroundColor = Colors.Gray;
                                    this.Friends[i].StatusStrokeColor = Colors.DarkGray;
                                }
                            }
                        }
                    }
                    return;
                } catch(Exception ex) {
                    Console.WriteLine(ex.ToString());    
                }
            }

            //Friend request has been accepted
            if(message.IndexOf("friendId") != -1) {
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true,
                };
                try {
                    AcceptFriendRequestUpdateModel afrm = JsonSerializer.Deserialize<AcceptFriendRequestUpdateModel>(message, options);
                    if(afrm != null) {
                        //Get the profile of the user
                        var profileId = afrm.accountID == this.Account.Id ? afrm.friendID : afrm.accountID;
                        AccountModel newFriend = await this.profileService.GetUserProfile(profileId, Globals.Session);
                        var newFriendModel = new FriendModel { 
                            FriendId = newFriend.Id, 
                            About = newFriend.About,
                            BefriendDate = DateTime.Now.ToString(),
                            DisplayName = newFriend.DisplayName,
                            JoinDate = newFriend.JoinDate,
                            Status = newFriend.Status,
                            LastMessage = "Start conversation",
                            LastMessageTimestamp = null,
                            ProfilePictureUrl = newFriend.ProfilePictureUrl,
                            LastOnline = newFriend.LastOnline,
                            RoomID = afrm.roomID,
                        };
                        if (newFriendModel.ProfilePictureUrl == "NULL") {
                            newFriendModel.ProfilePictureUrl = Constants.defaultProfilePicture;
                        }
                        else {
                            newFriendModel.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + newFriendModel.ProfilePictureUrl;
                        }

                        FriendStatusModel newFriendStatusModel = null;
                        if (newFriendModel.Status == "Online") {
                            newFriendStatusModel = new FriendStatusModel(newFriendModel, Colors.Green, Colors.DarkGreen);
                        } else {
                            newFriendStatusModel = new FriendStatusModel(newFriendModel, Colors.Gray, Colors.DarkGray);
                        }
                        this.Friends.Insert(0,newFriendStatusModel);
                        this.FriendsSearchResults.Insert(0, newFriendStatusModel);
                    }
                }catch(Exception ex) { 
                    Console.WriteLine(ex.ToString());
                }
            }

            if (message.IndexOf("newPhoto") != -1) {
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true,
                };

                try {
                    UpdateProfilePictureModel upm = JsonSerializer.Deserialize<UpdateProfilePictureModel>(message, options);
                    if (upm != null) {
                        for (int i = 0; i < this.Friends.Count; i++) {
                            if (this.Friends[i].FriendId == upm.id) {
                                this.Friends[i].ProfilePictureUrl = "";
                                this.Friends[i].ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + upm.id.ToString() + ".png";
                            }
                        }
                        return;
                    }
                } catch(Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            }

            //Some friend is calling
            if(message.IndexOf("callee") != -1) {
                try {
                    var options = new JsonSerializerOptions {
                        PropertyNameCaseInsensitive = true,
                    };

                    //Parse the response
                    CallFriendModel cfm = JsonSerializer.Deserialize<CallFriendModel>(message, options);
                    //Search through all the friends and find the one that is calling
                    if (cfm != null) {
                        if (cfm.option == "Call") {
                            for (int i = 0; i < this.Friends.Count; i++) {
                                if (this.Friends[i].FriendId == cfm.caller) {
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
                        if (cfm.option == "Cancel") {
                            //Go back to where the application was before the call
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                                await Shell.Current.Navigation.PopAsync()
                            );
                        }
                        if (cfm.option == "Answer") {
                            //The friend answered the call
                            for (int i = 0; i < this.Friends.Count; i++) {
                                if (this.Friends[i].FriendId == cfm.caller) {
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
                        if (cfm.option == "Deny") {
                            //The friend denied the call
                            //Go back to where the application was before the call
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                                await Shell.Current.Navigation.PopAsync()
                            );
                        }
                        return;
                    }
                } catch(Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            }

            //It is a message from another user
            if(message.IndexOf("groupName") != -1)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                //Parse the response
                try {
                    CreateGroupResponseModel resp = JsonSerializer.Deserialize<CreateGroupResponseModel>(message, options);

                    //Notify that the group has been created (move the client to the group page)
                    //await Shell.Current.DisplayAlert("Group created!", "Group" + resp.GroupName + "has been created", "Ok");
                    if (resp != null) {

                        GroupModel gm = new GroupModel();
                        gm.CreatorId = resp.CreatorId;
                        gm.GroupName = resp.GroupName;
                        gm.RoomId = resp.RoomId;
                        gm.Participants = new List<int>();
                        gm.ParticipantNames = new List<string>();
                        gm.LastMessage = "Start conversation";
                        gm.GroupPictureUrl = Constants.defaultGroupPicture;
                        foreach (var participantId in resp.Participants) {
                            gm.Participants.Add(participantId);
                            foreach (var friend in Friends) {
                                if (participantId == friend.FriendId)
                                    gm.ParticipantNames.Add(friend.DisplayName);
                            }
                        }
                        //gm.Participants = resp.Participants as List<int>;

                        this.Groups.Insert(0, gm);
                        this.GroupsSearchResults.Insert(0, gm);

                        //Clear the friends selected for the group
                        this.CreateGroupSelectedFriends.Clear();
                        this.CreateGroupSelected = false;

                        //Remove the no groups icon
                        NoGroups = false;

                        return;
                    }
                } catch(Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
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

                                    //Send a push notification that a new message has been received from the friend
                                    this.notificationService.SendPrivateChatNotification(this.Friends[i].DisplayName, privMessageModel.Data);
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
                                //Create the hour format from the date the message was sent
                                var timestamp = DateTime.Now.ToString("HH:mm");
                                if (privMessageModel.SenderId == this.Account.Id)
                                {
                                    this.Groups[i].LastMessage = "You: " + privMessageModel.Data;
                                    this.Groups[i].LastMessageTimestamp = timestamp;
                                }
                                else
                                {
                                    //Search through the participants and get the name of the sender
                                    int senderIndex = -1;
                                    for(int j =0; j < this.Groups[i].Participants.Count; j++)
                                        if (this.Groups[i].Participants[j] == privMessageModel.SenderId) {
                                            senderIndex = j;
                                        }
                                    this.Groups[i].LastMessage = this.Groups[i].ParticipantNames[senderIndex] + ": " + privMessageModel.Data;
                                    this.Groups[i].LastMessageTimestamp = timestamp;
                                    this.notificationService.SendGroupChatNotification(this.Groups[i].GroupName, this.Groups[i].LastMessage);
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

        public async Task LoadData()
        {
            //await GetGroupsAsync();
            //Get the groups without the last message updated from the local database
            LoadingGroups = true;
            NoGroups = false;
            //await GetGroupsWithCacheAsync();
            //await GetGroupsAsync();
            await GetGroupsAsyncEnumerable();
            LoadingGroups = false;
            if (Groups.Count == 0)
                NoGroups = true;

            LoadingFriends = true;
            await GetFriendsAsync();
            LoadingFriends = false;
            if (Friends.Count == 0)
                NoFriends = true;
            HasFriends = !NoFriends;

            //Update the last message text and last message timestamp for groups
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

        async Task UpdateLocalFriendsWithRemote(List<FriendModel> remoteFriends) {
            //Go through all the remote friends and if there is a new one update the caching database
            //Update the search results as well because that are the ones that are displayed
            foreach(var remoteFriend in remoteFriends) {
                bool found = false;
                for(int i =0; i < this.Friends.Count; i++) {
                    if (this.Friends[i].FriendId == remoteFriend.FriendId) {
                        found = true;
                        //Update the data that changes often
                        this.Friends[i].LastMessage = remoteFriend.LastMessage;
                        //this.CreateGroupSearchResults[i].LastMessage = remoteFriend.LastMessage;
                        //this.FriendsSearchResults[i].LastMessage = remoteFriend.LastMessage;
                        //Update last message timestamp
                        //Update the date format to show only the hour if the message is from Today
                        if (this.Friends[i].LastMessageTimestamp != "") {
                            DateTime messageDate = DateTime.Parse(remoteFriend.LastMessageTimestamp);
                            double diffDays = (DateTime.Now - messageDate).TotalDays;
                            if (diffDays <= 1.0 && diffDays >= 0.0) {
                                string messageTimestamp = messageDate.ToString("HH:mm");
                                this.Friends[i].LastMessageTimestamp = messageTimestamp;
                                //this.CreateGroupSearchResults[i].LastMessageTimestamp = messageTimestamp;
                                //this.FriendsSearchResults[i].LastMessageTimestamp = messageTimestamp;
                            }
                            else if (diffDays > 1.0 && diffDays <= 2.0) {
                                this.Friends[i].LastMessageTimestamp = "Yesterday";
                                //this.CreateGroupSearchResults[i].LastMessageTimestamp = "Yesterday";
                                //this.FriendsSearchResults[i].LastMessageTimestamp = "Yesterday";
                            }
                            else {
                                this.Friends[i].LastMessageTimestamp = messageDate.ToString("dddd");
                                //this.CreateGroupSearchResults[i].LastMessageTimestamp = messageDate.ToString("dddd");
                                //this.FriendsSearchResults[i].LastMessageTimestamp = messageDate.ToString("dddd");
                            }
                        }

                        if (remoteFriend.ProfilePictureUrl == "NULL") {
                            this.Friends[i].ProfilePictureUrl = Constants.defaultProfilePicture;
                            //this.CreateGroupSearchResults[i].ProfilePictureUrl = Constants.defaultProfilePicture;
                            //this.FriendsSearchResults[i].ProfilePictureUrl = Constants.defaultProfilePicture;
                        } else {
                            this.Friends[i].ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + remoteFriend.ProfilePictureUrl;
                            //this.CreateGroupSearchResults[i].ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + remoteFriend.ProfilePictureUrl;
                            //this.FriendsSearchResults[i].ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + remoteFriend.ProfilePictureUrl;
                        }

                        if (remoteFriend.Status == "Online") {
                            this.Friends[i].StatusBackgroundColor = Colors.Green;
                            this.Friends[i].StatusStrokeColor = Colors.DarkGreen;

                            //this.CreateGroupSearchResults[i].StatusBackgroundColor = Colors.Green;
                            //this.CreateGroupSearchResults[i].StatusStrokeColor = Colors.DarkGreen;

                            //this.FriendsSearchResults[i].StatusBackgroundColor = Colors.Green;
                            //this.FriendsSearchResults[i].StatusStrokeColor = Colors.DarkGreen;
                        } else {
                            this.Friends[i].StatusBackgroundColor = Colors.Gray;
                            this.Friends[i].StatusStrokeColor = Colors.DarkGray;

                            //this.CreateGroupSearchResults[i].StatusBackgroundColor = Colors.Gray;
                            //this.CreateGroupSearchResults[i].StatusStrokeColor = Colors.DarkGray;

                            //this.FriendsSearchResults[i].StatusBackgroundColor = Colors.Gray;
                            //this.FriendsSearchResults[i].StatusStrokeColor = Colors.DarkGray;
                        }
                        break;
                    }
                }
                if (!found) { 
                    FriendStatusModel f = new FriendStatusModel(remoteFriend, Colors.Gray, Colors.DarkGray);
                    // Update the status
                    if(remoteFriend.Status == "Online") {
                        f.StatusBackgroundColor = Colors.Green;
                        f.StatusStrokeColor = Colors.DarkGreen;
                    }
                    Friends.Add(f);
                    FriendsSearchResults.Add(f);
                    CreateGroupSearchResults.Add(f);
                }
            }

            //Update the friends in the database
            await this.databaseService.SaveFriends(remoteFriends);
        }

        [RelayCommand]
        async Task GetFriendsAsync()
        {
            try
            {
                if (Friends.Count != 0)
                    Friends.Clear();

                if (CreateGroupSearchResults.Count != 0)
                    CreateGroupSearchResults.Clear();

                if (FriendsSearchResults.Count != 0)
                    FriendsSearchResults.Clear();
                //Try the local database to see if the friends are cached
                //If there are friends cached in the local database then show them and then search for remote friends and update the details if necessary
                //var localFriends = await this.databaseService.GetLocalFriends();

                //////If the local list is not empty display this list then update it with the remote one where is necessary
                //if (localFriends != null) {
                //    foreach (var localFriend in localFriends) {
                //        FriendStatusModel f = new FriendStatusModel(localFriend, Colors.Gray, Colors.DarkGray);
                //        Friends.Add(f);
                //        CreateGroupSearchResults.Add(f);
                //        FriendsSearchResults.Add(f);
                //    }
                //}

                //Get the remote list of friends
                string hexString = "";
                for(int i = 1; i < hexID.Length; i++)
                    hexString += hexID[i];
                var friends = await friendService.GetFriends(int.Parse(hexString, System.Globalization.NumberStyles.HexNumber), Session);

                //await this.UpdateLocalFriendsWithRemote(friends);

                //_ = await this.databaseService.SaveFriends(friends);

                foreach (var friend in friends) {
                    //Update the date format to show only the hour if the message is from Today
                    if (friend.LastMessageTimestamp != "") {
                        //string messageTimestamp = DateTime.Parse(friend.LastMessageTimestamp).ToString("HH:mm");
                        //friend.LastMessageTimestamp = messageTimestamp;
                        DateTime messageDate = DateTime.Parse(friend.LastMessageTimestamp);
                        double diffDays = (DateTime.Now - messageDate).TotalDays;
                        if (diffDays <= 1.0 && diffDays >= 0.0) {
                            string messageTimestamp = messageDate.ToString("HH:mm");
                            friend.LastMessageTimestamp = messageTimestamp;
                        }
                        else if (diffDays > 1.0 && diffDays <= 2.0) {
                            friend.LastMessageTimestamp = "Yesterday";
                        }
                        else {
                            friend.LastMessageTimestamp = messageDate.ToString("dddd");
                        }
                    }

                    if (friend.ProfilePictureUrl == "NULL") {
                        friend.ProfilePictureUrl = Constants.defaultProfilePicture;
                    }
                    else {
                        friend.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + friend.ProfilePictureUrl;
                    }

                    if (friend.Status == "Online") {
                        FriendStatusModel friendStatusModel = new FriendStatusModel(friend, Colors.Green, Colors.DarkGreen);
                        Friends.Add(friendStatusModel);
                        CreateGroupSearchResults.Add(friendStatusModel);
                        FriendsSearchResults.Add(friendStatusModel);
                    }
                    else {
                        FriendStatusModel friendStatusModel = new FriendStatusModel(friend, Colors.Gray, Colors.DarkGray);
                        Friends.Add(friendStatusModel);
                        CreateGroupSearchResults.Add(friendStatusModel);
                        FriendsSearchResults.Add(friendStatusModel);
                    }

                }
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
                await Shell.Current.DisplayAlert("Error!", $"Unable to get friends: {e.Message}", "OK");
            }
            finally
            {

            }
        }

        public void SearchbarFriendsTextChanged(string newText) {
            if(this.FriendsSearchResults.Count != 0)
                this.FriendsSearchResults.Clear();

            //If the new text is empty string then add all the friends in the search results
            if (newText == null || newText == "") {
                foreach (var friend in this.Friends)
                    this.FriendsSearchResults.Add(friend);
                return;
            }

            //Search in the friends list all the friends that contain the newText in their display name and add them in the search results
            foreach (var friend in this.Friends) {
                if (friend != null && (friend.DisplayName.ToLower().Contains(newText.ToLower()) || friend.LastMessage.ToLower().Contains(newText.ToLower()) )) {
                    this.FriendsSearchResults.Add(friend);
                }
            }
        }

        async Task GetGroupsWithCacheAsync() {
            try {
                if (this.Groups.Count != 0)
                    this.Groups.Clear();

                if (GroupsSearchResults.Count != 0)
                    GroupsSearchResults.Clear();

                await foreach (var group in this.chatService.GetGroupsWithCache(this.Account.Id, Globals.Session)) {
                    //Show group one by one
                    Console.WriteLine("Got group in ui function");
                    if (group.LastMessageTimestamp != "") {
                        DateTime messageDate;
                        if (DateTime.TryParse(group.LastMessageTimestamp, out messageDate)) {
                            double diffDays = (DateTime.Now - messageDate).TotalDays;
                            if (diffDays <= 1.0 && diffDays >= 0.0) {
                                string messageTimestamp = messageDate.ToString("HH:mm");
                                group.LastMessageTimestamp = messageTimestamp;
                            }
                            else if (diffDays > 1.0 && diffDays <= 2.0) {
                                group.LastMessageTimestamp = "Yesterday";
                            }
                            else {
                                group.LastMessageTimestamp = messageDate.ToString("dddd");
                            }
                        }
                    }

                    if (group.GroupPictureUrl == "NULL" || group.GroupPictureUrl == null || group.GroupPictureUrl.Contains("default")) {
                        group.GroupPictureUrl = Constants.defaultGroupPicture;
                    }
                    else {
                        if(!group.GroupPictureUrl.Contains(Constants.chatServerUrl + "chat/groups/static/"))
                            group.GroupPictureUrl = Constants.chatServerUrl + "chat/groups/static/" + group.GroupPictureUrl;
                    }

                    Groups.Add(group);
                    this.GroupsSearchResults.Add(group);
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return;
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

                if (GroupsSearchResults.Count != 0)
                    GroupsSearchResults.Clear();

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
                    if(group.GroupPictureUrl == "NULL" || group.GroupPictureUrl == null) {
                        group.GroupPictureUrl = Constants.defaultGroupPicture;
                    } else {
                        group.GroupPictureUrl = Constants.chatServerUrl + "chat/groups/static/" + group.GroupPictureUrl;
                    }

                    Groups.Add(group);
                    this.GroupsSearchResults.Add(group);
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

        async Task GetGroupsAsyncEnumerable() {
            string hexString = "";
            for (int i = 1; i < hexID.Length; i++)
                hexString += hexID[i];
            
            if (Groups.Count != 0) {
                Groups.Clear();
            }

            if (GroupsSearchResults.Count != 0)
                GroupsSearchResults.Clear();

            try { 
                await foreach (var group in this.chatService.GetGroupsAsyncEnumerable(int.Parse(hexString, System.Globalization.NumberStyles.HexNumber), Globals.Session)) {
                    if (group.LastMessageTimestamp != "") {
                        DateTime messageDate = DateTime.Parse(group.LastMessageTimestamp);
                        double diffDays = (DateTime.Now - messageDate).TotalDays;
                        if (diffDays <= 1.0 && diffDays >= 0.0) {
                            string messageTimestamp = messageDate.ToString("HH:mm");
                            group.LastMessageTimestamp = messageTimestamp;
                        }
                        else if (diffDays > 1.0 && diffDays <= 2.0) {
                            group.LastMessageTimestamp = "Yesterday";
                        }
                        else {
                            group.LastMessageTimestamp = messageDate.ToString("dddd");
                        }
                    }
                    if (group.GroupPictureUrl == "NULL" || group.GroupPictureUrl == null) {
                        group.GroupPictureUrl = Constants.defaultGroupPicture;
                    }
                    else {
                        group.GroupPictureUrl = Constants.chatServerUrl + "chat/groups/static/" + group.GroupPictureUrl;
                    }

                    Groups.Add(group);
                    this.GroupsSearchResults.Add(group);
                }

                //Sort the groups by last message timestamp

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

        public void SearchbarGroupsTextChanged(string newText) {
            if (this.GroupsSearchResults.Count != 0)
                this.GroupsSearchResults.Clear();

            //If the new text is empty string then add all the friends in the search results
            if (newText == null || newText == "") {
                foreach (var group in this.Groups)
                    this.GroupsSearchResults.Add(group);
                return;
            }

            //Search in the friends list all the friends that contain the newText in their display name and add them in the search results
            foreach (var group in this.Groups) {
                if (group != null && (group.GroupName.ToLower().Contains(newText.ToLower()) || group.LastMessage.ToLower().Contains(newText.ToLower()))) {
                    this.GroupsSearchResults.Add(group);
                }
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
            if (res) {
                //Send the friend request to the specified account
                string[] fields = input.Split(' ');
                int friendId = ConvertHexToFriendId(fields[1]);
                res = await this.friendService.SendFriendRequest(this.Account.Id, friendId, this.Session);
                //Check if the request was successful
                if (res)
                {
                    //The friend request has been sent
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    string text = "Friend request has been sent!";
                    ToastDuration duration = ToastDuration.Short;
                    double fontSize = 14;
                    var toast = Toast.Make(text, duration, fontSize);
                    await toast.Show(cancellationTokenSource.Token);
                } else
                {
                    //The friend request couldn't be sent
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    string text = "Couldn't send the friend request!";
                    ToastDuration duration = ToastDuration.Short;
                    double fontSize = 14;
                    var toast = Toast.Make(text, duration, fontSize);
                    await toast.Show(cancellationTokenSource.Token);
                }
            } else {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                string text = "Invalid username and id format!";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
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
            await this.chatService.SendMessageAsync(jsonMessage);

            //Notify the user a group has been created
            //CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            string text = "Group has been created!";
            //ToastDuration duration = ToastDuration.Long;
            //double fontSize = 14;
            //var toast = Toast.Make(text, duration, fontSize);
            //await toast.Show();

            SnackbarOptions opt = new SnackbarOptions();
            opt.CornerRadius = 20;
            await Shell.Current.DisplaySnackbar(text, visualOptions: opt); 
        }

        [RelayCommand]
        async Task CreateGroupMobile()
        {
            await Shell.Current.GoToAsync(nameof(CreateGroupPage), true);
        }

        public void SearchbarCreateGroupTextChanged(string newText) {
            //Clear the previous list results
           if(this.CreateGroupSearchResults.Count != 0)
                this.CreateGroupSearchResults.Clear();

            //If the new text is empty string then add all the friends in the search results
            if(newText == null || newText == "") {
                foreach (var friend in this.Friends)
                    this.CreateGroupSearchResults.Add(friend);
                return;
            }

            //Search in the friends list all the friends that contain the newText in their display name and add them in the search results
            foreach(var friend in this.Friends) {
                if(friend != null && friend.DisplayName.Contains(newText)) {
                    this.CreateGroupSearchResults.Add(friend);
                }
            }
        }

        public async void SelectImageForNewGroup(ImageButton imageButton) {
            List<string> actions = new List<string>
            {
                "Take photo",
                "Upload photo"
            };
            string res = await Shell.Current.DisplayActionSheet("Change photo", "Cancel", null, actions.ToArray());
            if (res == actions[0]) {
                if (MediaPicker.Default.IsCaptureSupported) {
                    FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null) {
                        int i = 0;
                    }
                }
            }
            else {
                FileResult photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null) {
                    this.CreateGroupPhoto = await photo.OpenReadAsync();
                    //Change the source of the image in the group icon
                    imageButton.Source = ImageSource.FromStream(() => this.CreateGroupPhoto);
                }
            }
        }

        [RelayCommand]
        async Task GoToInformation() {
            await Shell.Current.GoToAsync(nameof(InformationPage), true);
        }

        [RelayCommand]
        async Task GoToSubmitedFeedback() {
            await Shell.Current.GoToAsync(nameof(SubmitedFeedbackPage), true);
        }

        [RelayCommand]
        async Task GoToNewFeedback() {
            await Shell.Current.GoToAsync(nameof(NewFeedbackPage), true);
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
            if (requests != null) {
                foreach (var request in requests) {
                    if (request.ProfilePictureUrl == "NULL")
                        request.ProfilePictureUrl = Constants.defaultProfilePicture;
                    else
                        request.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + request.ProfilePictureUrl;
                    this.FriendRequests.Add(request);
                }
            }

            NoFriendRequests = false;
            if (FriendRequests.Count == 0)
                NoFriendRequests = true;
        }

        [RelayCommand]
        async Task ExitFriendRequests()
        {
            _ = await Shell.Current.Navigation.PopAsync();
        }

        [RelayCommand]
        async Task GoToSentFriendRequestsMobile() {
            await Shell.Current.GoToAsync(nameof(SentFriendRequestPage), true);
            var requests = await this.friendService.GetSentFriendRequests(this.Account.Id, Session);
            this.SentFriendRequests.Clear();
            if (requests != null) {
                foreach (var request in requests) {
                    if (request.ProfilePictureUrl == "NULL")
                        request.ProfilePictureUrl = Constants.defaultProfilePicture;
                    else
                        request.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + request.ProfilePictureUrl;
                    this.SentFriendRequests.Add(request);
                }
            }

            NoSentFriendRequests = false;
            if (SentFriendRequests.Count == 0)
                NoSentFriendRequests = true;
        }

        [RelayCommand]
        async Task ExitSentFriendRequests() {
            _ = await Shell.Current.Navigation.PopAsync(true);
        }

        public void FriendRecommendationSelectionChanged(List<FriendRecommendationModel> newSelectedItems) {
            //Clear the previous selection
            if (this.selectedFriendRecommendations.Count > 0)
                this.selectedFriendRecommendations.Clear();
            //Return if the new selected items list is null
            if (newSelectedItems == null)
                return;
            //Return if there is no item selected now
            if (newSelectedItems.Count == 0)
                return;
            //Update the selected items list
            foreach(var item in newSelectedItems)
                this.selectedFriendRecommendations.Add(item);
        }

        public void SearchbarFriendRecommendationsTextChanged(string newText) {
            //Clear the current selected items
            if (FriendRecommendationSearchResults.Count > 0)
                FriendRecommendationSearchResults.Clear();
            //Add the recommendations that match the newText
            foreach(var recommendation in FriendRecommendations) {
                if(recommendation.DisplayName.Contains(newText))
                    FriendRecommendationSearchResults.Add(recommendation);
            }
        }

        public async void GetFriendRequestRecommendations() {
            //Clear the list of friend recommendations
            if (FriendRecommendations.Count != 0)
                FriendRecommendations.Clear();
            //Clear the list of search results
            if (FriendRecommendationSearchResults.Count != 0)
                FriendRecommendationSearchResults.Clear();

            //Get the recommendations from the server
            var friendRecommendations = await this.friendService.GetFriendRecommendations(Account.Id, Globals.Session);
            if (friendRecommendations != null) {
                foreach (var friendRecommendation in friendRecommendations) {
                    //Check if the friend recommendation is not a friend of the user
                    bool isFriend = false;
                    foreach(var friend in Friends) {
                        if (friend.FriendId == friendRecommendation.Id) {
                            isFriend = true;
                            break;
                        }
                    }
                    if (!isFriend) {
                        var joinDate = DateTime.Parse(friendRecommendation.JoinDate);
                        friendRecommendation.JoinDate = joinDate.ToString("D");
                        if (friendRecommendation.ProfilePictureUrl == "NULL")
                            friendRecommendation.ProfilePictureUrl = Constants.defaultProfilePicture;
                        else
                            friendRecommendation.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + friendRecommendation.ProfilePictureUrl;
                        FriendRecommendations.Add(friendRecommendation);
                        FriendRecommendationSearchResults.Add(friendRecommendation);
                    }
                }
            }
        }

        [RelayCommand]
        async Task SendFriendRequestsToSelectedRecommendations() {
            bool allFriendRequestsSent = true;
            foreach(var selectedRecommendation in this.selectedFriendRecommendations) {
                bool res = await this.friendService.SendFriendRequest(Account.Id, selectedRecommendation.Id, Globals.Session);
                if (!res)
                    allFriendRequestsSent = false;
            }

            if (allFriendRequestsSent) {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                string text = "Friend requests have been sent";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
            }
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

                NoFriendRequests = false;
                if (this.FriendRequests.Count == 0)
                    NoFriendRequests = true;

                //Update the list of friends
                //await GetFriendsAsync();

                //int roomId = 0;
                //foreach(var friend in this.Friends) {
                //    if(friend.FriendId == friendId) {
                //        roomId = friend.RoomID;
                //        break;
                //    }
                //}

                //Send update for the user real time
                await this.chatService.SendMessageAsync(JsonSerializer.Serialize(new AcceptFriendRequestModel { accountID = Account.Id, friendID = friendId }));
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
                NoFriendRequests = false;
                if (this.FriendRequests.Count == 0)
                    NoFriendRequests = true;
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Cannot decline the friend request", "Ok");
            }
        }

        [RelayCommand]
        async Task GoToSettingsWindows() 
        {
            await Shell.Current.GoToAsync(nameof(DesktopSettingsPage), true, new Dictionary<string, object> {
                {"account", this.Account },
                {"hexID", HexID},
                {"session", Session },
                {"numberFriends", this.Friends.Count },
                {"numberGroups", this.Groups.Count}
            });
        }

        [RelayCommand]
        async Task Logout() {
            await Shell.Current.Navigation.PopToRootAsync();
        }

        [RelayCommand]
        async Task GoToReportABug()
        {
            await Shell.Current.GoToAsync(nameof(ReportABugPage), true, new Dictionary<string, object>
            {
                {"account", this.Account },
                {"hexID", HexID},
                {"session", Session }
            });
        }

        [RelayCommand]
        async Task GoToProfile()
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage), true, new Dictionary<string, object>
                {
                    {"account", this.Account},
                    {"numberFriends",  this.Friends.Count},
                    {"numberGroups",  this.Groups.Count},
                    {"session",  this.Session},
                });
        }

    }
}
