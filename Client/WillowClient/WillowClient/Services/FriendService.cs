using Microsoft.VisualBasic.FileIO;
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
            //if (friendsList?.Count > 0)
            //    return friendsList;

            var url = Constants.serverURL + "/friend/view/" + accountId.ToString();
            var baseAddress = new Uri(url);
            //this.m_CookieContainer = new CookieContainer();
            //this.m_handler.CookieContainer = this.m_CookieContainer;
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
            
            var response = await this.m_httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                friendsList = await response.Content.ReadFromJsonAsync<List<FriendModel>>();
            }

            return friendsList;
        }

        public async Task<bool> SendFriendRequest(int accountID, int friendID, string session)
        {
            var url = Constants.serverURL + "/friendrequest/add";
            var baseAddress = new Uri(url);
            //this.m_CookieContainer = new CookieContainer();
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
            //this.m_handler.CookieContainer = this.m_CookieContainer;

            SendFriendRequestModel frm = new SendFriendRequestModel { accountId = friendID, friendId = accountID};
            var response = await this.m_httpClient.PostAsync(url, JsonContent.Create(frm));
            //Check if the response was an error
            if (response.StatusCode != HttpStatusCode.OK)
                return false;
            return true;
        }

        public async Task<List<FriendRequestModel>> GetFriendRequest(int accountID, string session)
        {
            var url = Constants.serverURL + "/friendrequest/view/" + accountID.ToString();
            var baseAddress = new Uri(url);
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
            var response = await this.m_httpClient.GetAsync(url);
            //Parse the response from server
            List<FriendRequestModel> friendRequests = new();
            if (response.IsSuccessStatusCode)
            {
                friendRequests = await response.Content.ReadFromJsonAsync<List<FriendRequestModel>>();
            }

            return friendRequests;
        }

        public async Task<List<FriendRequestModel>> GetSentFriendRequests(int accountID, string session)
        {
            var url = Constants.serverURL + "/friendrequest/viewsent/" + accountID.ToString();
            var baseAddress = new Uri(url);
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
            var response = await this.m_httpClient.GetAsync(url);
            //Parse the response from server
            List<FriendRequestModel> friendRequests = new();
            if (response.IsSuccessStatusCode)
            {
                friendRequests = await response.Content.ReadFromJsonAsync<List<FriendRequestModel>>();
            }

            return friendRequests;
        }

        public async Task<bool> AcceptFriendRequest(int accountID, int friendID, string session)
        {
            var url = Constants.serverURL + "/friend/add";
            var baseAddress = new Uri(url);
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
            AcceptFriendRequestModel acfrm = new AcceptFriendRequestModel { accountID = accountID, friendID = friendID };
            var response = await this.m_httpClient.PostAsync(url, JsonContent.Create(acfrm));
            if (response.IsSuccessStatusCode)
                return true;
            return false;
        }

        public async Task<bool> DeclineFriendRequest(int accountID, int friendID, string session)
        {
            var url = Constants.serverURL + "/friendrequest/delete";
            var baseAddress = new Uri(url);
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
            AcceptFriendRequestModel acfrm = new AcceptFriendRequestModel { accountID = accountID, friendID = friendID };
            var response = await this.m_httpClient.PostAsync(url, JsonContent.Create(acfrm));
            if (response.IsSuccessStatusCode)
                return true;
            return false;
        }
    }
}
