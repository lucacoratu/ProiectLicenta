using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class AndroidGroupCallPage : ContentPage
{
	public AndroidGroupCallPage(CallViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
    private void ContentPage_Loaded(object sender, EventArgs e) {
        CallViewModel vm = BindingContext as CallViewModel;
        vm.InitializeGroupCall(this.webView);
    }
    private void ImageButton_Clicked(object sender, EventArgs e) {
        CallViewModel vm = BindingContext as CallViewModel;
        vm.TerminateGroupCall(this.webView);
    }
}