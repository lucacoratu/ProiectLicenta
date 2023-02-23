using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.Views;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(Account), "account")]
    [QueryProperty(nameof(NumberFriends), "numberFriends")]
    [QueryProperty(nameof(NumberGroups), "numberGroups")]
    public partial class ProfileViewModel : BaseViewModel
    {
        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private int numberFriends;

        [ObservableProperty]
        private int numberGroups;

        public ProfileViewModel() { }

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
            }
        }
    }
}
