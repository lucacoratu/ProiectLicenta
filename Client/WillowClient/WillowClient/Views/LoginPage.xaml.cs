using System.Net.Http.Json;
using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}