using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.Services;

namespace WillowClient.ViewModel
{
    public partial class RegisterViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string confirmPassword;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string displayName;

        private RegisterService m_RegisterService;

        private async Task RegisterAccount()
        {
            if(Password != ConfirmPassword)
            {
                Error = "Passwords do not match!";
                return;
            }

            var model = new RegisterModel();
            model.Username = Username;
            model.Password = Password;
            model.Email = Email;
            model.DisplayName = DisplayName;

            var res = await this.m_RegisterService.RegisterAccount(model);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            try
            {
                ErrorModel? err = JsonSerializer.Deserialize<ErrorModel>(res, options);
                if (err != null)
                {
                    Error = err.Error;
                    return;
                }

                //The registration was successful
                Error = "Registration successful!";
            }
            catch(Exception e)
            {
                //Invalid json format
                
            }
        }

        public Command RegisterCommand { get; }

        public RegisterViewModel()
        {
            this.m_RegisterService = new RegisterService();
            this.RegisterCommand = new Command(async () => await RegisterAccount());
        }
    }
}
