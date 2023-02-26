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
        public GroupDetailsViewModel(ProfileService ps) {
            this.profileService = ps;
        }

        public async void PopulateGroupParticipants() {
            //Get all the profiles of the participants
            if (this.Participants.Count != 0)
                this.Participants.Clear();

            if (this.Account.Id == this.Group.CreatorId)
                this.Participants.Add(new GroupParticipantModel { Id = this.Account.Id, DisplayName = "You", Owner = "Owner", ProfilePictureUrl = this.Account.ProfilePictureUrl });
            else
                this.Participants.Add(new GroupParticipantModel { Id = this.Account.Id, DisplayName = "You", Owner = "", ProfilePictureUrl = this.Account.ProfilePictureUrl });

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
        public async void PrepareUI() {
            if (this.Account.Id == this.Group.CreatorId)
                this.IsGroupOwner = true;
            else
                this.IsGroupOwner = false;
        }

        [RelayCommand]
        async Task GoBack() {
            await Shell.Current.Navigation.PopAsync(true);
        }

    }
}
