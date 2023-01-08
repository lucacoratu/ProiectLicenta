using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class CalleePage : ContentPage
{
	public CalleePage(CallViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}