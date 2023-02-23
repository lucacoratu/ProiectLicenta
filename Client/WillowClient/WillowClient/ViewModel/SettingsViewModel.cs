using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.Views;

namespace WillowClient.ViewModel {

    [QueryProperty(nameof(Account), "account")]
    [QueryProperty(nameof(HexID), "hexID")]
    [QueryProperty(nameof(Session), "session")]
    [QueryProperty(nameof(NumberFriends), "numberFriends")]
    [QueryProperty(nameof(NumberGroups), "numberGroups")]
    public partial class SettingsViewModel : BaseViewModel {

        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private string session;

        [ObservableProperty]
        private string hexID;

        [ObservableProperty]
        private int numberFriends;

        [ObservableProperty]
        private int numberGroups;

        public SettingsViewModel() {

        }

        [RelayCommand]
        async Task GoToReportABug() {
            await Shell.Current.GoToAsync(nameof(ReportABugPage), true, new Dictionary<string, object>
            {
                {"account", this.Account },
                {"hexID", HexID},
                {"session", Session }
            });
        }

        [RelayCommand]
        async Task GoToProfile() {
            await Shell.Current.GoToAsync(nameof(ProfilePage), true, new Dictionary<string, object>
                {
                    {"account", this.Account},
                    {"numberFriends",  this.NumberFriends},
                    {"numberGroups",  this.NumberGroups},
                    {"session",  this.Session},
                });
        }

        [RelayCommand]
        async Task GoBack() {
            await Shell.Current.Navigation.PopAsync(true);
        }
    }
}
