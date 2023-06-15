using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;

namespace WillowClient.Services
{
    public class FeedbackService
    {
        private HttpClientHandler m_handler;
        private HttpClient m_httpClient;
        private CookieContainer m_CookieContainer;
        public FeedbackService() 
        {
            this.m_CookieContainer = new CookieContainer();
            this.m_handler = new HttpClientHandler();
            this.m_handler.CookieContainer = this.m_CookieContainer;
            this.m_httpClient = new HttpClient(this.m_handler);
        }

        public async Task<bool> AddBugReport(string category, string description, int accountId, string session)
        {
            var url = Constants.serverURL + "/accounts/reportbug";
            var baseAddress = new Uri(url);
           
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
            AddBugReportModel addModel = new AddBugReportModel { category=category, description=description, reportedBy=accountId };
            var res = await this.m_httpClient.PostAsync(baseAddress, JsonContent.Create(addModel));
            if(res.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

        public async Task<List<BugReportCategoryModel>> GetReportCategories()
        {
            var url = Constants.serverURL + "/accounts/reportcategories";
            var baseAddress = new Uri(url);

            var res = await this.m_httpClient.GetAsync(baseAddress);
            List<BugReportCategoryModel> categories = new List<BugReportCategoryModel>();
            if(res.StatusCode == HttpStatusCode.OK)
            {
                categories = await res.Content.ReadFromJsonAsync<List<BugReportCategoryModel>>();
            }
            return categories;
        }
    }
}
