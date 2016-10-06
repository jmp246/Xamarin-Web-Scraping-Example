using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Webkit;
using System.Net.Http;
using System.Net;
using Android.Util;
using HtmlAgilityPack;

namespace Geekzone
{
    [Activity(Label = "WebViewForum")]
    public class WebViewForum : MasterActivity
    {
        private WebView webView;
        private Button replyButton;
        private EditText replyText;
        private CheckBox emailMeCheckBox;
        private LinearLayout buttons;

        private HttpClient client;
        //private CookieContainer httpClientCookies;
        private string URL;
        private string TITLE;

        public class MyCustomWebView : WebViewClient
        {
            WebViewForum activity;

            public MyCustomWebView(WebViewForum activity)
            {
                this.activity = activity;
            }

            public bool shouldOverrideUrlLoading(WebView view, String url)
            {
                return true;
            }
            public override void OnLoadResource(WebView view, string url)
            {
                //base.OnLoadResource(view, url);

                Log.Debug("Load", url);
                if (url.Contains("m.geekzone.co.nz"))
                {
                    Log.Debug("Load", "Contains");
                    activity.loadURL(url);
                }

            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.webViewForum);
            // Create your application here

            //http://m.geekzone.co.nz/Forums/75/Topic/196726
            URL = Intent.GetStringExtra("URL");
            Log.Debug("URL", URL);

            Title = Intent.GetStringExtra("TITLE");

            webView = FindViewById<WebView>(Resource.Id.threadWebView);
            webView.Settings.LoadWithOverviewMode = true;
            webView.Settings.BuiltInZoomControls = true;
            webView.Settings.DisplayZoomControls = false;
            //webView.Settings.UseWideViewPort = true;

            var customWebView = new MyCustomWebView(this);
            webView.SetWebViewClient(customWebView);


            //webView.Settings.UseWideViewPort = true;
            //webView.Settings.JavaScriptEnabled = true;
            //webView.LoadUrl(URL);
            loadURL(URL);

            replyButton = FindViewById<Button>(Resource.Id.postReplyButton);
            replyText = FindViewById<EditText>(Resource.Id.replyEditText);
            emailMeCheckBox = FindViewById<CheckBox>(Resource.Id.emailMeCheckBox);

            buttons = FindViewById<LinearLayout>(Resource.Id.buttons);

            replyButton.Click += delegate
            {
                reply();
            };
            
        }

        private async void reply()
        {
            await postReply(URL, replyText.Text, emailMeCheckBox.Checked);
        }

        private async void loadURL(string URL)
        {
            client = getHttpClient();
            Log.Debug("Cookie", "SETUPed");

            try
            {
                Log.Debug("Cookie", URL);

                var answer = await client.GetStringAsync(URL).ConfigureAwait(false);
                Log.Debug("Cookie", "Before Process");
                ProcessHtml(answer);
                Log.Debug("Cookie", "After Process");
            }
            catch (HttpRequestException htmlException)
            {
                /* Unfortunately, HttpRequestException doesn't allow us
                 * to access the original http status code */
                //if (!needsAuth)
                 //   needsAuth = htmlException.Message.Contains("302");
                //continue;

                Log.Error("RentalsGenericError", htmlException.ToString());
            }
            catch (Exception e)
            {
                Log.Error("RentalsGenericError", e.ToString());
                //break;
            }
        }

        private void ProcessHtml(string answer)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(answer); //Parse HTML

            var div = doc.GetElementbyId("main");
            Log.Debug("TABLE", div.InnerHtml);

            bool ableToPost = div.InnerText.Contains("Post A Reply");

            var divs = div.Elements("div");
            div.RemoveChild(divs.ElementAt(0)); //Remove top - Reply
            div.RemoveChild(divs.ElementAt(0)); //Remove bottom - Reply

            //Remove bottom footer as it has credit which would be confusing to ownership
            var tables = div.Elements("table").Last();
            div.RemoveChild(tables);

            this.RunOnUiThread(() =>
            {
                if (ableToPost)
                {
                    buttons.Visibility = ViewStates.Visible;
                }

                webView.LoadDataWithBaseURL(URL, div.InnerHtml, "text/html", "UTF-8", "");
            });
            
        }
    }
}