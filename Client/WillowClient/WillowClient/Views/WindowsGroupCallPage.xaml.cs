using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class WindowsGroupCallPage : ContentPage
{
	public WindowsGroupCallPage(CallViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
    private void ContentPage_Loaded(object sender, EventArgs e) {
        CallViewModel vm = BindingContext as CallViewModel;
        vm.InitializeGroupCall(this.webView);
    }

    private void ImageButton_Clicked(object sender, EventArgs e) {
        //Call the leave function in the javascript
        CallViewModel vm = BindingContext as CallViewModel;
        vm.TerminateGroupCall(this.webView);
    }
}