using Android.App;
using Android.Content;
using Android.Util;
using Android.Webkit;
using Java.Net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public class MasterActivity : Activity
{
    private const string mobileWebsiteURL = "m.geekzone.co.nz";
    private const string mobileURIWebsiteURL = "http://" + mobileWebsiteURL;

    private HttpClient httpClient;
    private CookieContainer httpClientCookies = new CookieContainer();

    private bool loggedIn = false;

    public MasterActivity()
    {
        loadCookie();
    }

    private void loadHttpClient()
    {
        httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            CookieContainer = httpClientCookies,
            UseCookies = true
        });

        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; U; Android 4.0.4; en-gb; GT-I9300 Build/IMM76D) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30");
    }

    private void loadCookie()
    {
        loadHttpClient();

        var prefs = Application.Context.GetSharedPreferences("Geekzone", FileCreationMode.Private);

        var loginCookieValue = prefs.GetString("MobileLoginCookie", "");

        if (!loginCookieValue.Equals("")) //Not equal to nothing
        {
            httpClientCookies.Add(new Uri(mobileURIWebsiteURL), new Cookie(".GeekzoneMobileAuth", loginCookieValue, "/", mobileWebsiteURL));
        }
    }

    public HttpClient getHttpClient()
    {
        return httpClient;
    }

    //Load Cookie
    public async Task<bool> testLoginCookie()
    {
        loadHttpClient();

        var prefs = Application.Context.GetSharedPreferences("Geekzone", FileCreationMode.Private);

        var loginCookieValue = prefs.GetString("MobileLoginCookie", "");

        if (!loginCookieValue.Equals("")) //Not equal to nothing
        {
            httpClientCookies.Add(new Uri(mobileURIWebsiteURL), new Cookie(".GeekzoneMobileAuth", loginCookieValue, "/", mobileWebsiteURL));

            try
            {
                var answer = await httpClient.GetStringAsync(mobileURIWebsiteURL).ConfigureAwait(false);
                Log.Debug("LOGIN", answer);
                return answer.Contains("Welcome back");
            }
            catch (Exception e)
            {
                Log.Debug("HTTP", e.ToString()); //Display Error

                return false; //Error
            }
        }
        else
        {
            return false; //No Cookie
        }
    }

    //Save Cookie
    public void saveLoginCookie()
    {
        string loginCookie = "";

        var cookies = httpClientCookies.GetCookies(new Uri(mobileURIWebsiteURL));

        foreach (Cookie cookie in cookies)
        {
            if (cookie.Name.Equals(".GeekzoneMobileAuth"))
            {
                loginCookie = cookie.Value;
                Log.Debug("Cookie", "LOGIN Cookie: " + loginCookie);
            }
        }

        if (!loginCookie.Equals("")) //Check cookie generated
        {
            var prefs = Application.Context.GetSharedPreferences("Geekzone", FileCreationMode.Private);
            var prefEditor = prefs.Edit();
            prefEditor.PutString("MobileLoginCookie", loginCookie);
            prefEditor.Commit();
        }
    }

    public async Task<bool> login(string username, string password)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"username",username},
            {"password",password},
            {"rememberMe","true" }
        });

        var login = await httpClient.PostAsync("http://m.geekzone.co.nz/Home/LogOn", content).ConfigureAwait(false);

        if (login.StatusCode == HttpStatusCode.Found)
        {
            loggedIn = true;
            saveLoginCookie();

            return true;
        }
        else
        {
            loggedIn = false;

            return false;
        }
    }

    public async Task<bool> postReply(string topicURL, string message, bool emailReplies)
    {
        string emailRepliesCheckBox = "false";
        if (emailReplies)
        {
            emailRepliesCheckBox = "true";
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
                {"Message", message},
                {"NotifyEnabled",emailRepliesCheckBox}
        });

        var login = await httpClient.PostAsync(topicURL, content).ConfigureAwait(false);

        if (login.StatusCode == HttpStatusCode.Found)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}