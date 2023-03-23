using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.Services;
using WillowClient.Views;

namespace WillowClient.ViewModel {
    [QueryProperty(nameof(UserId), "userId")]
    [QueryProperty(nameof(Account), "account")]
    public partial class UserProfileViewModel : BaseViewModel {

        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private UserProfileModel userProfile;

        [ObservableProperty]
        private int userId;

        [ObservableProperty]
        private int numberCommonGroups = 0;

        [ObservableProperty]
        private string commonGroupsText;

        [ObservableProperty]
        private string participantsText;

        [ObservableProperty]
        private bool loadingCommonGroups;

        [ObservableProperty]
        private bool loadedCommonGroups;

        public ObservableCollection<CommonGroupWithParticipantsModel> CommonGroups { get; } = new();

        private ProfileService profileService;

        private ChatService chatService;
        
        public UserProfileViewModel(ProfileService ps, ChatService cs) {
            this.profileService = ps;
            this.chatService = cs;
            this.chatService.RegisterReadCallback(MessageReceivedOnWebsocket);
        }

        //TO DO ... Check if it works with this destructor
        //~UserProfileViewModel() {
        //    this.chatService.UnregisterReadCallback(MessageReceivedOnWebsocket);
        //}

        public async Task MessageReceivedOnWebsocket(string message) {
            //It is a message specifing the new status of a user
            if (message.IndexOf("Change status") != -1) {
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true,
                };

                //Parse the response
                try {
                    ChangeStatusWebsocketResponseModel resp = JsonSerializer.Deserialize<ChangeStatusWebsocketResponseModel>(message, options);
                    //Check if the account which changed its state is the one in the profile page
                    if (this.UserProfile.Id == resp.AccountId) {
                        //Change the color depending on which status it is set
                        if (resp.NewStatus == "Online") {
                            this.UserProfile.Status = "Online";
                            this.UserProfile.StatusBackgroundColor = Colors.Green;
                            this.UserProfile.StatusStrokeColor = Colors.DarkGreen;
                        }
                        else {
                            this.UserProfile.Status = "Offline";
                            this.UserProfile.StatusBackgroundColor = Colors.Gray;
                            this.UserProfile.StatusStrokeColor = Colors.DarkGray;
                        }
                    }
                    return;
                }
                catch(Exception ex) {
                    Console.WriteLine(ex.ToString());
                    return;
                }
            }
        }

        [RelayCommand]
        public async Task GoBack() {
            await Shell.Current.Navigation.PopAsync(true);
        }

        public async void PopulateCommonGroups() {
            if(this.CommonGroups.Count != 0)
                this.CommonGroups.Clear();

            LoadingCommonGroups = true;
            LoadedCommonGroups = false;
            var commonGroups = await this.chatService.GetCommonGroups(this.Account.Id, this.UserId, Globals.Session);
            foreach(var group in commonGroups) {
                CommonGroupWithParticipantsModel mod = new CommonGroupWithParticipantsModel{ CreationDate = group.CreationDate, CreatorId = group.CreatorId, GroupName = group.GroupName,  RoomId = group.RoomId, Participants = group.Participants, ParticipantNames = group.ParticipantNames };
                mod.ParticipantsText += "You, ";
                for(int i = 0; i < group.ParticipantNames.Count; i++) {
                    if(i != group.ParticipantNames.Count - 1) {
                        mod.ParticipantsText += group.ParticipantNames[i] + ", ";
                    } else {
                        mod.ParticipantsText += group.ParticipantNames[i];
                    }
                }
                if(group.GroupPictureUrl == null || group.GroupPictureUrl == "NULL") {
                    mod.GroupPictureUrl = Constants.defaultGroupPicture;
                } else {
                    mod.GroupPictureUrl = Constants.chatServerUrl + "chat/groups/static/" + group.GroupPictureUrl;
                }

                this.CommonGroups.Add(mod);
            }
            this.NumberCommonGroups = this.CommonGroups.Count;
            if(this.NumberCommonGroups == 0 || this.NumberCommonGroups == 1) {
                this.CommonGroupsText = "Group in Common";
            } else {
                this.CommonGroupsText = "Groups in Common";
            }
            LoadingCommonGroups = false;
            LoadedCommonGroups = true;
        }

        public async void PopulateProfileData() {
            var userAccount = await this.profileService.GetUserProfile(this.UserId, Globals.Session);
            string profilePicture = "";
            if (userAccount.ProfilePictureUrl != "NULL")
                profilePicture = Constants.serverURL + "/accounts/static/" + userAccount.ProfilePictureUrl;
            else
                profilePicture = Constants.defaultProfilePicture;

            Color statusBackgroundColor, statusStrokeColor;
            if(userAccount.Status == "Online") {
                statusBackgroundColor = Colors.Green;
                statusStrokeColor = Colors.DarkGreen;
            } else {
                statusBackgroundColor = Colors.Gray;
                statusStrokeColor = Colors.DarkGray;
            }

            userAccount.JoinDate = DateTime.Parse(userAccount.JoinDate).ToString("dd MMMM yyyy");

            //Get if the current account and the one in the profile are friends
            UserProfile = new UserProfileModel { Id = userAccount.Id, DisplayName = userAccount.DisplayName, Email = userAccount.Email, JoinDate = userAccount.JoinDate, Status = userAccount.Status, ProfilePictureUrl = profilePicture, AreFriends = true, StatusBackgroundColor = statusBackgroundColor, StatusStrokeColor = statusStrokeColor, About = userAccount.About };
            
            bool res = await this.profileService.AreUsersFriends(Account.Id, userAccount.Id, Globals.Session);
            UserProfile.AreFriends = res;
        }

        [RelayCommand]
        async Task Tap(CommonGroupWithParticipantsModel cgm) {
            if (cgm == null)
                return;

            GroupModel gm = new GroupModel { GroupName = cgm.GroupName, CreatorId = cgm.CreatorId, RoomId = cgm.RoomId, GroupPictureUrl = cgm.GroupPictureUrl, LastMessage = "", LastMessageTimestamp = "", LastMessageSender = 0, ParticipantNames = cgm.ParticipantNames, Participants = cgm.Participants };
            await Shell.Current.GoToAsync(nameof(GroupChatPage), true, new Dictionary<string, object>
            {
                {"group", gm},
                {"account", Account},
            });
        }
    }
}
