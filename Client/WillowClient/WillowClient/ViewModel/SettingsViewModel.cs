using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.Views;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

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
        async Task Logout() {
            await Shell.Current.Navigation.PopToRootAsync();
        }

        [RelayCommand]
        async Task GoBack() {
            await Shell.Current.Navigation.PopAsync(true);
        }

        [RelayCommand]
        async Task OnlyEnglishSuported() {
            SnackbarOptions options = new SnackbarOptions { BackgroundColor = Color.FromRgb(0x50, 0x50, 0x50), TextColor = Colors.WhiteSmoke, ActionButtonTextColor = Colors.WhiteSmoke };
            await Shell.Current.DisplaySnackbar("The only supported language is english!", null, "Ok", TimeSpan.FromSeconds(3.0), options, default);
        }

        [RelayCommand]
        async Task GoToInformation() {
            await Shell.Current.GoToAsync(nameof(InformationPage), true);
        }

        [RelayCommand]
        async Task GoToSubmitedFeedback() {
            await Shell.Current.GoToAsync(nameof(SubmitedFeedbackPage), true, new Dictionary<string, object>
            {
                {"account", this.Account },
                {"hexID", HexID},
                {"session", Session }
            });
        }

        [RelayCommand]
        async Task GoToNewFeedback() {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Action action = async () => await Shell.Current.DisplayAlert("Sorry for the inconvenience", "Thank you for your understanding", "OK");
            SnackbarOptions options = new SnackbarOptions { BackgroundColor = Color.FromRgb(0x50, 0x50, 0x50), TextColor = Colors.WhiteSmoke, ActionButtonTextColor = Colors.WhiteSmoke };
            var snackbar = Snackbar.Make("This feature is disable on Windows as it is unstable!", action, "Ok", TimeSpan.FromSeconds(3.0), options);

            await snackbar.Show(cancellationTokenSource.Token);
            //await Shell.Current.DisplaySnackbar("This feature is disable on Windows as it is unstable!", null, "Ok", TimeSpan.FromSeconds(3.0), options, default);
            //await Shell.Current.GoToAsync(nameof(NewFeedbackPage), true);
        }
    }
}
