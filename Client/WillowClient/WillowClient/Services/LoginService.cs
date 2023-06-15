using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json;
using WillowClient.Model;
using System.Net;

namespace WillowClient.Services;

public class LoginService
{
    private CookieContainer m_cookieContainer;
    private HttpClientHandler m_handler;
    private HttpClient m_httpClient;

    public LoginService()
    {
        this.m_cookieContainer = new CookieContainer();
        this.m_handler = new HttpClientHandler();
        this.m_handler.CookieContainer = this.m_cookieContainer;
        this.m_httpClient = new HttpClient(this.m_handler);
    }

    public async Task<string> LoginIntoAccount(LoginModel loginData)
    {
        var url = Constants.serverURL + "/accounts/login";
        var response = await this.m_httpClient.PostAsync(url, JsonContent.Create(loginData));
        return await response.Content.ReadAsStringAsync();
    }

    public string GetSessionCookie()
    {
        Uri uri = new Uri(Constants.serverURL + "/accounts/login");
        IEnumerable<Cookie> responseCookies = this.m_cookieContainer.GetCookies(uri).Cast<Cookie>();
        foreach (Cookie cookie in responseCookies)
            if(cookie.Name == "session")
                return cookie.Value;
        return null;
    }

    public CookieContainer GetCookieContainer()
    {
        return this.m_cookieContainer;
    }
}
