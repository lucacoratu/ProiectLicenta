using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class NewFeedbackPage : ContentPage
{
	public NewFeedbackPage(FeedbackViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void ContentPage_Loaded(object sender, EventArgs e) {
		//FeedbackViewModel viewModel = BindingContext as FeedbackViewModel;
		//viewModel.PopulateFeedbackQuestions();
    }
}