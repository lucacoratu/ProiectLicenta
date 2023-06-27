using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WillowClient.Model;
using WillowClient.Services;
using WillowClient.Views;

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

        public ObservableCollection<FeedbackQuestionModel> FeedbackQuestions { get;  } = new ();

        [ObservableProperty]
        public bool hasNoBugReports = true;

        [ObservableProperty]
        public bool loadingBugReports = false;

        public ObservableCollection<BugReportModel> BugReports { get; } = new ();

        [ObservableProperty]
        public bool hasNoSubmittedFeedback = true;

        FeedbackService feedbackService;

        public FeedbackViewModel(FeedbackService feedbackService)
        {
            this.feedbackService = feedbackService;
            this.PopulateCategories();
            this.PopulateFeedbackQuestions();
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

        public async void PopulateBugReports() {
            LoadingBugReports = true;

            var bugReports = await this.feedbackService.GetUserBugReports(this.Account.Id, Session);

            if (this.BugReports.Count != 0)
                this.BugReports.Clear();

            foreach(var bugReport in bugReports) {
                //Format the date
                DateTime submitDate = DateTime.Parse(bugReport.Timestamp);
                bugReport.Timestamp = submitDate.ToString("f");
                this.BugReports.Add(bugReport);
            }

            if(this.BugReports.Count != 0)
                HasNoBugReports = false;

            LoadingBugReports = false;
        }

        public async void PopulateFeedbackQuestions() {
            FeedbackQuestionModel feedbackQuestion = new FeedbackQuestionModel { QuestionIndex = "1", Question = "How satisfied are you when using the app?" };
            FeedbackQuestionModel feedbackQuestion2 = new FeedbackQuestionModel { QuestionIndex = "2", Question = "How satisfied are you with the chat?" };
            FeedbackQuestionModel feedbackQuestion3 = new FeedbackQuestionModel { QuestionIndex = "3", Question = "How satisfied are you with the group chat?" };
            FeedbackQuestionModel feedbackQuestion4 = new FeedbackQuestionModel { QuestionIndex = "4", Question = "How satisfied are you with the audio call?" };
            FeedbackQuestionModel feedbackQuestion5 = new FeedbackQuestionModel { QuestionIndex = "5", Question = "How satisfied are you with the video call?" };

            //Clear the previous data
            //this.FeedbackQuestions.Clear();

            //Add the questions
            this.FeedbackQuestions.Add(feedbackQuestion);
            this.FeedbackQuestions.Add(feedbackQuestion2);
            this.FeedbackQuestions.Add(feedbackQuestion3);
            this.FeedbackQuestions.Add(feedbackQuestion4);
            this.FeedbackQuestions.Add(feedbackQuestion5);
        }

        public async void ClearFeedbackQuestions() {
            this.FeedbackQuestions.Clear();
        }

        [RelayCommand]
        async Task SubmitBugReport()
        {
            //Check if the category and the description has been completed
            if (this.SelectedCategory == null || this.BugReportDescription == null)
                return;

            if (this.SelectedCategory == "" || this.BugReportDescription == "")
                return;

            //Check if the length of the bug report description is shorter than 500 characters
            if(this.BugReportDescription.Length > 500) {
                await Shell.Current.DisplaySnackbar("The description should be shorter than 500 characters", null, "Ok", TimeSpan.FromSeconds(3.0));
                return;
            }

            var res = await this.feedbackService.AddBugReport(this.SelectedCategory, this.BugReportDescription, this.Account.Id, Session);
            if(res == true) {
                await Shell.Current.DisplaySnackbar("Bug report has been submitted!", null, "Ok", TimeSpan.FromSeconds(3.0));
                //Clear the inputs
                SelectedCategory = null;
                BugReportDescription = "";
            }
            else {
                await Shell.Current.DisplayAlert("Error", "Could not add the bug report!", "Ok");
            }
        }

        [RelayCommand]
        async Task GoToSubmittedBugReports() {

            await Shell.Current.GoToAsync(nameof(UserReportedBugsPage), true, new Dictionary<string, object>
            {
                {"account", this.Account },
                {"hexID", HexID},
                {"session", Session }
            });
        }

        [RelayCommand]
        async Task ExitReportABug()
        {
            await Shell.Current.Navigation.PopAsync();
        }

        [RelayCommand]
        async Task GoBack() {
            await Shell.Current.Navigation.PopAsync();
        }
        
    }
}
