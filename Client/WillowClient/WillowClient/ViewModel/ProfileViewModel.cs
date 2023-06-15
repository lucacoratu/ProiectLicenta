using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
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
    [QueryProperty(nameof(NumberFriends), "numberFriends")]
    [QueryProperty(nameof(NumberGroups), "numberGroups")]
    [QueryProperty(nameof(Session), "session")]
    public partial class ProfileViewModel : BaseViewModel
    {
        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private int numberFriends;

        [ObservableProperty]
        private int numberGroups;

        private string lastSetStatus;

        [ObservableProperty]
        private string session;

        private DefaultStatusModel selectedStatusModel;

        public ObservableCollection<DefaultStatusModel> StatusModels { get; } = new();

        ProfileService profileService;

        ChatService chatService;

        private Stream profilePictureStream;
        public ProfileViewModel(ProfileService ps, ChatService chatService) {
            this.profileService = ps;
            this.chatService = chatService;
        }

        [RelayCommand]
        async Task GoBack()
        {
            //Change the account status to the previous one
            if(this.lastSetStatus != null)
                Account.About = this.lastSetStatus;
            await Shell.Current.Navigation.PopAsync();
        }

        public void CreateDefaultStatusModels() {
            StatusModels.Add(new DefaultStatusModel { Text = Account.About, IsSelected = true, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "Hello", IsSelected = false, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "Busy", IsSelected = false, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "Available", IsSelected = false, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "Currently unavailable", IsSelected = false, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "On the road", IsSelected = false, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "Having fun", IsSelected = false, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "At school", IsSelected = false, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "Battery about to die", IsSelected = false, IsLineVisible = true });
            StatusModels.Add(new DefaultStatusModel { Text = "Can't talk, message only", IsSelected = false, IsLineVisible = false });
        }

        public void GetLastSavedStatus() {
            this.lastSetStatus = Account.About;
        }

        public void StatusSelectionChanged(DefaultStatusModel model) {
            foreach (var item in StatusModels) {
                if(item.IsSelected)
                    item.IsSelected = false;
            }
            if(model != null)
                this.selectedStatusModel = model;
        }

        [RelayCommand]
        async Task GoToEditProfile()
        {
            await Shell.Current.GoToAsync(nameof(EditProfilePage), true, new Dictionary<string, object>
                {
                    {"account", Account}
                });
        }

        [RelayCommand]
        async Task GoToEditStatus() {
            //Load the default statuses

            await Shell.Current.GoToAsync(nameof(EditStatusPage), true, new Dictionary<string, object> {
                { "account", Account },
            });
        }

        async void UpdateUserProfilePictureForAllUsers() {
            UpdateProfilePictureModel upm = new UpdateProfilePictureModel {
                id = this.Account.Id,
                newPhoto = "newPhoto",
            };
            await this.chatService.SendMessageAsync(JsonSerializer.Serialize(upm));
        }

        [RelayCommand]
        async Task DisplayChangeStatusActionSheet() {
            string result = await Shell.Current.DisplayPromptAsync(null, "Set a description for other users", maxLength: 100, keyboard: Keyboard.Chat);
            if (result != null) {
                //Update the account status with this one and the first model in default status models
                if(Account != null)
                    Account.About = result;
                if (StatusModels != null)
                    StatusModels[0].Text = result;
                foreach(var statusModel in StatusModels) {
                    if (statusModel.IsSelected)
                        statusModel.IsSelected = false;
                }
                this.selectedStatusModel = StatusModels[0];
                StatusModels[0].IsSelected = true;
            }
        }

        [RelayCommand]
        async Task SaveChanges() {
            if (this.selectedStatusModel == null)
                return;

            if (this.selectedStatusModel.Text == this.lastSetStatus)
                return;

            bool res = await this.profileService.ChangeAboutMessage(this.selectedStatusModel.Text, Account.Id, Globals.Session);
            if (res) {
                //Update the UI
                this.lastSetStatus = this.selectedStatusModel.Text;
                Account.About = this.selectedStatusModel.Text;
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                string text = "About message has been updated";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
            } else {
                await Shell.Current.DisplayAlert("Update about message error","About message couldn't be updated", "OK");
            }

        }

        public async void ChangeProfilePicture(AvatarView profileAvatar)
        {
            List<string> actions = new List<string>
            {
                "Take photo",
                "Upload photo"
            };
            string res = await Shell.Current.DisplayActionSheet("Change photo", "Cancel", null, actions.ToArray());
            if(res == actions[0])
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
                    if(photo != null)
                    {
                        Stream photoStream = await photo.OpenReadAsync();
                        //this.profilePictureStream = await photo.OpenReadAsync();
                        bool uploadedResult = await this.profileService.ChangeProfilePicture(photoStream, this.Account.Id, this.Session);
                        if (uploadedResult) {
                            //Send a messsage to all the other clients that the profile picture has been changed
                            UpdateUserProfilePictureForAllUsers();
                            await Shell.Current.DisplayAlert("Profile picture", "Your profile picture has been updated", "Ok");
                            //Update the picture in the box
                            this.Account.ProfilePictureUrl = "";
                            this.Account.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + this.Account.Id + ".png";
                            //profileAvatar.ImageSource = ImageSource.FromStream(() => this.profilePictureStream);
                        }
                    }
                }
            } else if (res == actions[1]) {
                FileResult photo = await MediaPicker.Default.PickPhotoAsync();
                if(photo != null) {
                    Stream photoStream = await photo.OpenReadAsync();
                    //this.profilePictureStream = await photo.OpenReadAsync();
                    bool uploadedResult = await this.profileService.ChangeProfilePicture(photoStream, this.Account.Id, this.Session);
                    if(uploadedResult) {
                        //Send a messsage to all the other clients that the profile picture has been changed
                        UpdateUserProfilePictureForAllUsers();
                        await Shell.Current.DisplayAlert("Profile picture", "Your profile picture has been updated", "Ok");
                        //Update the picture in the box
                        this.Account.ProfilePictureUrl = "";
                        this.Account.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + this.Account.Id + ".png";
                        //profileAvatar.ImageSource = ImageSource.FromStream(() => this.profilePictureStream);
                    }
                }
            }
        }
    }
}
