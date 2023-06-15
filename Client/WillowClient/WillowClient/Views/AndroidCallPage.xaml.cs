using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class AndroidCallPage : ContentPage
{
	public AndroidCallPage(CallViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        CallViewModel vm = BindingContext as CallViewModel;
        vm.InitializeCall(this.webView);
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        CallViewModel vm = BindingContext as CallViewModel;
        vm.TerminateCall(this.webView);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        CallViewModel vm = BindingContext as CallViewModel;
        vm.TerminateCall(this.webView);
    }
}