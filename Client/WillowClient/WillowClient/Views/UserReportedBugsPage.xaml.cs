using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class UserReportedBugsPage : ContentPage
{
	public UserReportedBugsPage(FeedbackViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void ContentPage_Loaded(object sender, EventArgs e) {
		FeedbackViewModel vm = BindingContext as FeedbackViewModel;
		if (vm != null) {
			vm.PopulateBugReports();
        }
    }
}