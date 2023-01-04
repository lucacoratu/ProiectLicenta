using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class AddFriendPage : ContentPage
{
	public AddFriendPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}