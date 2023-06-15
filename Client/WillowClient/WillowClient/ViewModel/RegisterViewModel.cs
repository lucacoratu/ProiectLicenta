using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
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

            //Show an prompt that after completing the registration this will be the main device and the account
            //cannot be used on another device
            var result = await Shell.Current.DisplayAlert("Important", "After finishing the register this will be the only device the account can be used on, are you sure?", "Ok", "Cancel");
            if (!result)
                return;

            //Generate the 2 keys necessary for the register
            var identityKey = Encryption.Utils.GenerateX25519Key();
            var preSignedKey = Encryption.Utils.GenerateX25519Key();

            //Add the public keys PEM encoded in the register model object
            var publicIdentityKey = identityKey.ExportSubjectPublicKeyInfoPem();
            var publicPreSignedKey = preSignedKey.ExportSubjectPublicKeyInfoPem();

            //Export the private keys in pem format
            var privateIdentityKey = identityKey.ExportECPrivateKeyPem();
            var privatePreSignedKey = preSignedKey.ExportECPrivateKeyPem();

            //First try to get the private keys from the secure storage
            //If the keys already exist then ask the user if he wants to override them
            try {
                var identityPrivateStorage = await SecureStorage.Default.GetAsync(Constants.identityKey);
                var preSignedPrivateStorage = await SecureStorage.Default.GetAsync(Constants.preSignedPrivate);

                bool noKeys = false;
                if (identityPrivateStorage == null || preSignedPrivateStorage == null)
                    noKeys = true;

                if (!noKeys) {
                    bool wantsOverride = await Shell.Current.DisplayAlert("Important", "Private keys for the application already exist, do you want to override", "Yes", "No");
                    if (!wantsOverride)
                        return;
                }

            } catch(Exception ex) {
                await Shell.Current.DisplayAlert("Error", "Could not get the identity private", "Ok");
                Console.WriteLine(ex.ToString());
            }

            //Save the private keys in the Secure storage in pem format
            try {
                await SecureStorage.Default.SetAsync(Constants.identityKey, privateIdentityKey);
                await SecureStorage.Default.SetAsync(Constants.preSignedPrivate, privatePreSignedKey);
            }
            catch (Exception ex) {
                await Shell.Current.DisplayAlert("Error", "Could not insert the private keys in the secure storage", "Ok");
                Console.WriteLine(ex.Message);
            }

            //Add the public keys to the model
            model.IdPubKey = publicIdentityKey;
            model.PreSignedKey = publicPreSignedKey;

            var res = await this.m_RegisterService.RegisterAccount(model);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            try {
                ErrorModel? err = JsonSerializer.Deserialize<ErrorModel>(res, options);
                if (err != null) {
                    Error = err.Error;
                    return;
                }
            }
            catch (Exception e) {
                //Invalid json format
                Console.WriteLine($"Error: {e}");
            }
            finally {
                if (res == "Account created!") {
#if ANDROID || IOS
                    await Shell.Current.DisplaySnackbar("Account has been registered", null, "Ok", TimeSpan.FromSeconds(2.0), null, default);
#elif WINDOWS
                    await Shell.Current.DisplayAlert("Success", "Account has been registered", "Ok");
#endif
                }
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
