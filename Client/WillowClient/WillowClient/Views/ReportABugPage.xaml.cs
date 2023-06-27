using InputKit.Shared.Validations;
using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class ReportABugPage : ContentPage
{
	public ReportABugPage(FeedbackViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

		this.bugReportDescriptionEditor.Validations.Add(new RequiredValidation());
		this.selectCategoryPicker.Validations.Add(new RequiredValidation());
    }
}