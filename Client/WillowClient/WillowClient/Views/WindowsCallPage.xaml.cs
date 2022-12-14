using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class WindowsCallPage : ContentPage
{
	public WindowsCallPage(CallViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

	private void ContentPage_Loaded(object sender, EventArgs e)
	{
		CallViewModel vm = BindingContext as CallViewModel;
		vm.InitializeCall(this.webView);
	}
}