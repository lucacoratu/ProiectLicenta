using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class FriendRequestPage : ContentPage
{
	public FriendRequestPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}