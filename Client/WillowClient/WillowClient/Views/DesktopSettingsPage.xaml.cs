using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class DesktopSettingsPage : ContentPage
{
	public DesktopSettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}