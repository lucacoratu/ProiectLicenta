using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.Services;
using WillowClient.Views;

namespace WillowClient.ViewModel {

    [QueryProperty(nameof(Group), "group")]
    [QueryProperty(nameof(Account), "account")]
    public partial class GroupDetailsViewModel : BaseViewModel {

        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private GroupModel group;

        [ObservableProperty]
        private int numberParticipants;

        [ObservableProperty]
        bool isGroupOwner = false;

        public ObservableCollection<GroupParticipantModel> Participants { get; set; } = new();

        private ProfileService profileService;
        private ChatService chatService;
        public GroupDetailsViewModel(ProfileService ps, ChatService cs) {
            this.profileService = ps;
            this.chatService = cs;
        }

        public async void PopulateGroupParticipants() {
            //Get all the profiles of the participants
            if (this.Participants.Count != 0)
                this.Participants.Clear();

            if (this.Account.Id == this.Group.CreatorId)
                this.Participants.Add(new GroupParticipantModel { Id = this.Account.Id, DisplayName = "You", Owner = "Owner", ProfilePictureUrl = this.Account.ProfilePictureUrl, About = this.Account.About });
            else
                this.Participants.Add(new GroupParticipantModel { Id = this.Account.Id, DisplayName = "You", Owner = "", ProfilePictureUrl = this.Account.ProfilePictureUrl, About = this.Account.About });

            var auxParticipants = await profileService.GetGroupParticipantProfiles(this.Group.Participants, Globals.Session);
            for (int i = 0; i < auxParticipants.Count; i++) {
                if (auxParticipants[i].Id == this.Group.CreatorId) {
                    auxParticipants[i].Owner = "Owner";
                }
                else {
                    auxParticipants[i].Owner = "";
                }
                if (auxParticipants[i].ProfilePictureUrl != "NULL") {
                    auxParticipants[i].ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + auxParticipants[i].ProfilePictureUrl;
                }
                else {
                    auxParticipants[i].ProfilePictureUrl = Constants.defaultProfilePicture;
                }
            }

            foreach (var auxParticipant in auxParticipants) {
                this.Participants.Add(auxParticipant);
            }

            this.NumberParticipants = this.Participants.Count;
        }

        [RelayCommand]
        public async Task ChangeGroupPicture() {
            List<string> actions = new List<string>
{
                "Take photo",
                "Upload photo"
            };
            string res = await Shell.Current.DisplayActionSheet("Change photo", "Cancel", null, actions.ToArray());
            if (res == actions[0]) {
                try {
                    if (MediaPicker.Default.IsCaptureSupported) {
                        FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
                        if (photo != null) {
                            Stream photoStream = await photo.OpenReadAsync();
                            bool uploadedResult = await this.chatService.UpdateGroupPicture(this.Group.RoomId, photoStream, Globals.Session);
                            if (uploadedResult) {
                                //Send a messsage to all the other clients that the profile picture has been changed
                                //UpdateUserProfilePictureForAllUsers();
                                this.Group.GroupPictureUrl = "";
                                this.Group.GroupPictureUrl = Constants.chatServerUrl + "chat/groups/static/" + this.Group.RoomId + ".png";

                                await Shell.Current.DisplayAlert("Group picture", "Group picture has been updated", "Ok");
                            }
                        }
                    }
                } catch(Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if(res == actions[1]) {
                try {
                    FileResult photo = await MediaPicker.Default.PickPhotoAsync();
                    if (photo != null) {
                        Stream photoStream = await photo.OpenReadAsync();
                        bool uploadedResult = await this.chatService.UpdateGroupPicture(this.Group.RoomId, photoStream, Globals.Session);
                        if (uploadedResult) {
                            //Send a messsage to all the other clients that the profile picture has been changed
                            //UpdateUserProfilePictureForAllUsers();
                            this.Group.GroupPictureUrl = "";
                            this.Group.GroupPictureUrl = Constants.chatServerUrl + "chat/groups/static/" + this.Group.RoomId + ".png";

                            await Shell.Current.DisplayAlert("Group picture", "Group picture has been updated", "Ok");
                        }
                    }
                } catch(Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public void PrepareUI() {
            if (this.Account.Id == this.Group.CreatorId)
                this.IsGroupOwner = true;
            else
                this.IsGroupOwner = false;
        }

        [RelayCommand] 
        public async Task ParticipantTap (GroupParticipantModel participant) {
            if (participant.Id == this.Account.Id)
                return;

            await Shell.Current.GoToAsync(nameof(UserProfilePage), true, new Dictionary<string, object> {
                { "userId", participant.Id },
                { "account", this.Account },
            });
        }

        [RelayCommand]
        async Task GoBack() {
            await Shell.Current.Navigation.PopAsync(true);
        }

    }
}
