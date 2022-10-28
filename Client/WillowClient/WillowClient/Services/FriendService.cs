using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;

namespace WillowClient.Services
{
    public class FriendService
    {
        private HttpClientHandler m_handler;
        private HttpClient m_httpClient;
        private CookieContainer m_CookieContainer;

        List<FriendModel> friendsList = new(); 
        public FriendService()
        {
            this.m_CookieContainer = new CookieContainer();
            this.m_handler = new HttpClientHandler();
            this.m_handler.CookieContainer = this.m_CookieContainer;
            this.m_httpClient = new HttpClient(this.m_handler);
        }

        public async Task<List<FriendModel>> GetFriends(int accountId, string session)
        {
            if (friendsList?.Count > 0)
                return friendsList;

            var url = Constants.serverURL + "/friend/view/" + accountId.ToString();
            var baseAddress = new Uri(url);
            this.m_CookieContainer = new CookieContainer();
            this.m_handler.CookieContainer = this.m_CookieContainer;
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
            
            var response = await this.m_httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                friendsList = await response.Content.ReadFromJsonAsync<List<FriendModel>>();
            }

            return friendsList;
        }
    }
}
