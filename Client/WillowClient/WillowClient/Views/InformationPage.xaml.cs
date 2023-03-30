using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class InformationPage : ContentPage
{
	public InformationPage(InformationViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}