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

        public ObservableCollection<FeedbackQuestionModel> FeedbackQuestions { get;  } = new ();

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

        async void PopulateFeedbackQuestions() {
            FeedbackQuestionModel feedbackQuestion = new FeedbackQuestionModel { QuestionIndex = "1", Question = "How satisfied are you when using the app?" };
            FeedbackQuestionModel feedbackQuestion2 = new FeedbackQuestionModel { QuestionIndex = "2", Question = "How satisfied are you with the chat?" };
            FeedbackQuestionModel feedbackQuestion3 = new FeedbackQuestionModel { QuestionIndex = "3", Question = "How satisfied are you with the group chat?" };
            FeedbackQuestionModel feedbackQuestion4 = new FeedbackQuestionModel { QuestionIndex = "4", Question = "How satisfied are you with the audio call?" };
            FeedbackQuestionModel feedbackQuestion5 = new FeedbackQuestionModel { QuestionIndex = "5", Question = "How satisfied are you with the video call?" };
            this.FeedbackQuestions.Add(feedbackQuestion);
            this.FeedbackQuestions.Add(feedbackQuestion2);
            this.FeedbackQuestions.Add(feedbackQuestion3);
            this.FeedbackQuestions.Add(feedbackQuestion4);
            this.FeedbackQuestions.Add(feedbackQuestion5);
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

        [RelayCommand]
        async Task GoBack() {
            await Shell.Current.Navigation.PopAsync();
        }
        
    }
}
