using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class MobileFriendsPage : ContentPage
{
	public MobileFriendsPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}