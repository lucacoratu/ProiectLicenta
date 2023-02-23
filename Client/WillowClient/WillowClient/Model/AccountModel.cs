using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class AccountModel : ObservableObject
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string LastOnline { get; set; }
        public string Status { get; set; }
        public string JoinDate { get; set; }

        private string profilePictureUrl;
        public string ProfilePictureUrl 
        {
            get => profilePictureUrl;
            set => SetProperty(ref profilePictureUrl, value);
        }
    }
}
