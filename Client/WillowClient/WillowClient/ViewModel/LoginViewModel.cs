#define CLEAR_MESSAGES

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Maui.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WillowClient.Database;
using WillowClient.Model;
using WillowClient.Services;
using WillowClient.Views;
using CommunityToolkit.Maui.Core;
using System.Security.Cryptography;

namespace WillowClient.ViewModel;

public partial class LoginViewModel : BaseViewModel
{
    private LoginService m_LoginService;
    private DatabaseService databaseService;

    public LoginViewModel(DatabaseService databaseService)
    {
        this.m_LoginService = new LoginService();
        this.databaseService = databaseService;

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

    [ObservableProperty]
    private bool rememberMe = true;

    public Command LoginCommand { get; }
    public Command GoToRegisterCommand { get; }

    async Task GoToRegister()
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    async Task LoginFromDatabase(string username, string password) {
        var model = new LoginModel { Username = username, Password = password };
        var res = await this.m_LoginService.LoginIntoAccount(model);
        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
        };

        ErrorModel? err = JsonSerializer.Deserialize<ErrorModel>(res, options);
        if (err != null) {
            if (err.Error != null) {
                Error = err.Error;
                return;
            }
        }

        AccountModel? accountModel = JsonSerializer.Deserialize<AccountModel>(res, options);
        if (accountModel != null) {
            if (accountModel.DisplayName != null) {
                //The account will be returned from the server
                //Redirect to the main page because the user logged in succesfully

                string session = this.m_LoginService.GetSessionCookie();
                //Error = session;
                string idHex = accountModel.Id.ToString("X");
                int lenPadding = 6 - idHex.Length;
                string hexID = "#";
                for (int i = 0; i < lenPadding; i++) {
                    hexID += "0";
                }
                hexID += idHex;

                String joinDate = DateTime.Parse(accountModel.JoinDate).ToString("dd MMMM yyyy");
                accountModel.JoinDate = joinDate;

                if (accountModel.ProfilePictureUrl == "NULL") {
                    accountModel.ProfilePictureUrl = "https://raw.githubusercontent.com/jamesmontemagno/app-monkeys/master/baboon.jpg";
                }
                else {
                    accountModel.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + accountModel.ProfilePictureUrl;
                }

                Globals.Session = session;
#if ANDROID
                await Shell.Current.GoToAsync(nameof(MobileMainPage), true, new Dictionary<string, object>
                {
                    {"account", accountModel },
                    {"hexID", hexID},
                    {"session", session }
                });
                //await Shell.Current.GoToAsync(nameof(MobileTabviewMainPage), true);
#else
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                string text = "Account has been registered, go to login";
                ToastDuration duration = ToastDuration.Short;
                double fontSize = 14;
                var toast = Toast.Make(text, duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);

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

    public async void AutoLoginUser() {
        bool hasAccountRemembered = await this.databaseService.HasAccountRemembered();
        if(hasAccountRemembered) {
            //Get the credentials from the database
            //Login into the account
            var account = await this.databaseService.GetAccount();
            if (account == null)
                return;

            Username = account.Username;
            Password = account.Password;
            RememberMe = true;
            await this.LoginFromDatabase(account.Username, account.Password);
        }
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

        //#if CLEAR_MESSAGES
        //_ = await databaseService.DeleteLocalFriends();
        //_ = await databaseService.DeleteMessages();
        //_ = await databaseService.DeleteAllReactions();
        //_ = await databaseService.DeleteAllGroups();
        //#endif


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

        ECDiffieHellman identityKey = ECDiffieHellman.Create();
        ECDiffieHellman preSignedKey = ECDiffieHellman.Create();
        string privateIdentityKey = null;
        string privatePreSignedKey = null;

        try {
            //Get the key from the secure storage
            privateIdentityKey = await SecureStorage.Default.GetAsync(Constants.identityKey);
            privatePreSignedKey = await SecureStorage.Default.GetAsync(Constants.preSignedPrivate);
            if (privateIdentityKey == null || privatePreSignedKey == null) {
                await Shell.Current.DisplayAlert("Information", "You don't have a registered account using this device", "Ok");
                return;
            }

            identityKey.ImportFromPem(privateIdentityKey);
            preSignedKey.ImportFromPem(privatePreSignedKey);
        } catch(Exception ex) {
            await Shell.Current.DisplayAlert("Error", "Could not get the private keys", "Ok");
            Console.WriteLine(ex.ToString());
            return;
        }

        //Sign the username using the private key in the secure storage
        var signature = Encryption.Utils.ECDSASignData(Username, privateIdentityKey);
        model.Signature = signature;

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
                await Shell.Current.DisplayAlert("Error", Error, "Ok");
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

                String joinDate = DateTime.Parse(accountModel.JoinDate).ToString("dd MMMM yyyy");
                accountModel.JoinDate = joinDate;

                if (accountModel.ProfilePictureUrl == "NULL") {
                    accountModel.ProfilePictureUrl = Constants.defaultProfilePicture;
                }
                else {
                    accountModel.ProfilePictureUrl = Constants.serverURL + "/accounts/static/" + accountModel.ProfilePictureUrl;
                }

                //Save the account details into the database
                if(RememberMe) {
                    _ = await this.databaseService.SaveAccount(accountModel.Id, Username, Password, true);
                }

                Globals.Session = session;
                Globals.identityKey = identityKey;
                Globals.preSignedKey = preSignedKey;
#if ANDROID
                await Shell.Current.GoToAsync(nameof(MobileMainPage), true, new Dictionary<string, object>
                {
                    {"account", accountModel },
                    {"hexID", hexID},
                    {"session", session }
                });
                //await Shell.Current.GoToAsync(nameof(MobileTabviewMainPage), true);
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
