using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(Account), "account")]
    [QueryProperty(nameof(HexID), "hexID")]
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private string session;

        [ObservableProperty]
        private string hexID;

        public MainViewModel()
        {

        }

    }
}
