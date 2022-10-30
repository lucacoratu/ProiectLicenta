using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

        private FriendService friendService;

        public ObservableCollection<FriendModel> Friends { get; } = new();

        public MainViewModel(FriendService friendService)
        {
            this.friendService = friendService;
        }

        public async Task LoadData()
        {
            await GetFriendsAsync();
        }

        [RelayCommand]
        async Task Tap(FriendModel f)
        {
            await Shell.Current.GoToAsync(nameof(ChatPage), true, new Dictionary<string, object>
                {
                    {"friend", f},
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
                    Friends.Add(friend);
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

        [RelayCommand]
        async Task SelectAddFriend()
        {
            if (PendingSelected == true)
                PendingSelected = false;

            if (AddFriendSelected == true)
            {
                AddFriendSelected = false;
                return;
            }
            AddFriendSelected = true;
        }

        [RelayCommand]
        async Task SelectPending()
        {
            if (AddFriendSelected)
            {
                AddFriendSelected = false;
            }

            if (PendingSelected == true)
            {
                pendingSelected = false;
                return;
            }

            PendingSelected = true;
        }
    }
}
