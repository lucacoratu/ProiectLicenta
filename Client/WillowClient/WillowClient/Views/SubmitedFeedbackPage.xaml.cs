using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class SubmitedFeedbackPage : ContentPage
{
	public SubmitedFeedbackPage(FeedbackViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}