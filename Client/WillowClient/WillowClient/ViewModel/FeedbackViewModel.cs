using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WillowClient.Model;
using WillowClient.Services;

namespace WillowClient.ViewModel
{
    [QueryProperty(nameof(Account), "account")]
    [QueryProperty(nameof(HexID), "hexID")]
    [QueryProperty(nameof(Session), "session")]
    public partial class FeedbackViewModel : BaseViewModel
    {
        [ObservableProperty]
        private AccountModel account;

        [ObservableProperty]
        private string session;

        [ObservableProperty]
        private string hexID;

        [ObservableProperty]
        private string selectedCategory;

        [ObservableProperty]
        private string bugReportDescription;

        public ObservableCollection<string> Categories { get; } = new ();

        FeedbackService feedbackService;

        public FeedbackViewModel(FeedbackService feedbackService)
        {
            this.feedbackService = feedbackService;
            this.PopulateCategories();
        }

        async void PopulateCategories()
        {
            this.Categories.Clear();
            var cats = await this.feedbackService.GetReportCategories();
            foreach(BugReportCategoryModel cat in cats)
            {
                this.Categories.Add(cat.name);
            }
        }

        [RelayCommand]
        async Task SubmitBugReport()
        {
            await this.feedbackService.AddBugReport(this.SelectedCategory, this.BugReportDescription, this.Account.Id, Session);
        }


        [RelayCommand]
        async Task ExitReportABug()
        {
            _ = await Shell.Current.Navigation.PopAsync();
        }
        
    }
}
