using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class MobileGroupsPage : ContentPage
{
	public MobileGroupsPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}