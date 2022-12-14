using System.Net.Http.Json;
using WillowClient.ViewModel;
using Microsoft.Maui.ApplicationModel;

namespace WillowClient.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        this.loginImage.WidthRequest = width;
        this.loginImage.HeightRequest = 200; //given that image is 411 x 191
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
#if ANDROID
      this.entryUsername.IsEnabled = false;
      this.entryUsername.IsEnabled = true;
      this.entryPassword.IsEnabled = false;
      this.entryPassword.IsEnabled = true;
#endif
    }

    private void entryUsername_Unfocused(object sender, FocusEventArgs e)
    {
#if ANDROID
        this.entryUsername.IsEnabled = false;
        this.entryUsername.IsEnabled = true;
#endif
    }

    private void entryPassword_Unfocused(object sender, FocusEventArgs e)
    {
#if ANDROID
        this.entryPassword.IsEnabled = false;
        this.entryPassword.IsEnabled = true;
#endif
    }
}