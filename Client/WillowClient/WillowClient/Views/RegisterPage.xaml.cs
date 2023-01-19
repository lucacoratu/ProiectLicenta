using InputKit.Shared.Controls;
using InputKit.Shared.Validations;
using WillowClient.ViewModel;

namespace WillowClient.Views;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		this.entryUsername.Validations.Add(new RequiredValidation());
		this.entryPassword.Validations.Add(new RequiredValidation());
		this.entryConfirmPassword.Validations.Add(new RequiredValidation());
		this.entryEmail.Validations.Add(new RequiredValidation());
		this.entryEmail.Validations.Add(new RegexValidation { Message = "Please type a valid email address", Pattern=AdvancedEntry.REGEX_EMAIL});
		this.entryDisplayName.Validations.Add(new RequiredValidation());
	}
}