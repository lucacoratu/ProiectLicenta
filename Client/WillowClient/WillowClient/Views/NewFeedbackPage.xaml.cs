using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class NewFeedbackPage : ContentPage
{
	public NewFeedbackPage(FeedbackViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}