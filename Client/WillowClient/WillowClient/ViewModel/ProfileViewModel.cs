using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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

        [ObservableProperty]
        private string session;

        ProfileService profileService;

        ChatService chatService;
        public ProfileViewModel(ProfileService ps, ChatService chatService) {
            this.profileService = ps;
            this.chatService = chatService;
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.Navigation.PopAsync();
        }

        [RelayCommand]
        async Task GoToEditProfile()
        {
            await Shell.Current.GoToAsync(nameof(EditProfilePage), true, new Dictionary<string, object>
                {
                    {"account", Account}
                });
        }

        void UpdateUserProfilePictureForAllUsers() {
            UpdateProfilePictureModel upm = new UpdateProfilePictureModel {
                id = this.Account.Id,
                newPhoto = "newPhoto",
            };
            this.chatService.SendMessageAsync(JsonSerializer.Serialize(upm));
        }

        [RelayCommand]
        async Task ChangeProfilePicture()
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
                        int i = 0;
                    }
                }
            } else {
                FileResult photo = await MediaPicker.Default.PickPhotoAsync();
                if(photo != null) {
                    Stream photoStream = await photo.OpenReadAsync();
                    bool uploadedResult = await this.profileService.ChangeProfilePicture(photoStream, this.Account.Id, this.Session);
                    if(uploadedResult) {
                        //Send a messsage to all the other clients that the profile picture has been changed
                        UpdateUserProfilePictureForAllUsers();
                        await Shell.Current.DisplayAlert("Profile picture", "Your profile picture has been updated", "Ok");
                        //Update the picture in the box
                        this.Account.ProfilePictureUrl = "";
                        this.Account.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + this.Account.Id + ".png";
                    }
                }
            }
        }
    }
}
