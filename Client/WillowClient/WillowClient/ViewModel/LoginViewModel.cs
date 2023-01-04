using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WillowClient.Model;
using WillowClient.Services;
using WillowClient.Views;

namespace WillowClient.ViewModel;

public partial class LoginViewModel : BaseViewModel
{
    private LoginService m_LoginService;

    public LoginViewModel()
    {
        this.m_LoginService = new LoginService();
        this.LoginCommand = new Command(async () => await Login());
        this.GoToRegisterCommand = new Command(async () => await GoToRegister());
#if ANDROID
        this.phoneVisibility = true;
        this.WindowsVisibility = false;
#elif WINDOWS
        this.phoneVisibility = false;
        this.WindowsVisibility = true;
#endif
    }

    [ObservableProperty]
    private string username;

    [ObservableProperty]
    private string password;

    [ObservableProperty]
    private bool windowsVisibility;

    [ObservableProperty]
    private bool phoneVisibility;

    public Command LoginCommand { get; }
    public Command GoToRegisterCommand { get; }

    async Task GoToRegister()
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private bool VerifyUsernameInput(string? username)
    {
        //Check that the username inserted by the user has the right format
        if(username == null)
            return false;
        if (username.Length < 6 || username.Length > 20)
            return false;
        System.Text.RegularExpressions.Regex usernameRegex = new System.Text.RegularExpressions.Regex("^([a-zA-Z]+)([0-9|_|a-z|A-Z]*)$");
        var matches = usernameRegex.Matches(username);
        return matches.Count == 1;
    }

    private bool VerifyPasswordInput(string? password)
    {
        //Check that the password inserted by the user has the right format
        if (password == null)
            return false;
        if (password.Length < 6 || password.Length > 20)
            return false;
        System.Text.RegularExpressions.Regex passwordRegex = new System.Text.RegularExpressions.Regex("^([A-Z]+)([a-zA-Z0-9_!%$^]*)$");
        var matches = passwordRegex.Matches(password);
        return matches.Count == 1;
    }

    private async Task Login()
    {
        var model = new LoginModel();

        bool checkRes = this.VerifyUsernameInput(Username);
        if(checkRes == false)
        {
            //The username is not in the right format
            Error = "Invalid credentials";
            return;
        }

        checkRes = this.VerifyPasswordInput(Password);
        if(checkRes == false)
        {
            //Password is not in the right format
            Error = "Invalid credentials";
            return;
        }

        model.Username = Username;
        model.Password = Password;

        var res = await this.m_LoginService.LoginIntoAccount(model);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        ErrorModel? err = JsonSerializer.Deserialize<ErrorModel>(res, options);
        if (err != null) {
            if (err.Error != null)
            {
                Error = err.Error;
                return;
            }
        }

        AccountModel? accountModel = JsonSerializer.Deserialize<AccountModel>(res, options);
        if(accountModel != null)
        {
            if (accountModel.DisplayName != null)
            {
                //The account will be returned from the server
                //Redirect to the main page because the user logged in succesfully

                string session = this.m_LoginService.GetSessionCookie();
                //Error = session;
                string idHex = accountModel.Id.ToString("X");
                int lenPadding = 6 - idHex.Length;
                string hexID = "#";
                for(int i =0; i < lenPadding; i++)
                {
                    hexID += "0";
                }
                hexID += idHex;
#if ANDROID
                await Shell.Current.GoToAsync(nameof(MobileMainPage), true, new Dictionary<string, object>
                {
                    {"account", accountModel },
                    {"hexID", hexID},
                    {"session", session }
                });
#else
                await Shell.Current.GoToAsync(nameof(MainPage), true, new Dictionary<string, object>
                {
                    {"account", accountModel },
                    {"hexID", hexID},
                    {"session", session }
                });
#endif
            }
        }
    }
}
