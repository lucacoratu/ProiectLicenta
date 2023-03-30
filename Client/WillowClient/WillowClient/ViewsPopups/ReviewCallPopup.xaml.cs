using Mopups.Pages;
using WillowClient.ViewModel;

namespace WillowClient.ViewsPopups;

public partial class ReviewCallPopup : PopupPage
{
	public ReviewCallPopup(CallFeedbackViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void PopupPage_Loaded(object sender, EventArgs e) {
		var vm = BindingContext as CallFeedbackViewModel;
		vm.LoadData();
    }
}