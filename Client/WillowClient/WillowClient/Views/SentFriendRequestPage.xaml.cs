using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class SentFriendRequestPage : ContentPage
{
	public SentFriendRequestPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}