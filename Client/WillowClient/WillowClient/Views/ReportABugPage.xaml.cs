using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class ReportABugPage : ContentPage
{
	public ReportABugPage(FeedbackViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}