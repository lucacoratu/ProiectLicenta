using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class MobileSettingsPage : ContentPage
{
	public MobileSettingsPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}