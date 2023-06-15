using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class ZoomImagePage : ContentPage
{
	public ZoomImagePage(ZoomImageViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}