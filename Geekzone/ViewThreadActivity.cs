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
using static Geekzone.MainActivity;
using Android.Util;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net;

namespace Geekzone
{
    [Activity(Label = "ViewThreadActivity")]
    public class ViewThreadActivity : Activity
    {
        private HttpClient client;
        private CookieContainer httpClientCookies;
        private string URL;
        private string TITLE;
        private List<SubForumList> m_forums = new List<SubForumList>();
        private MyAdapter adapter;
        private ListView listView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.viewThread);

            // Create your application here
            listView = FindViewById<ListView>(Resource.Id.list);

            //col1
            var URL = Intent.GetStringExtra("URL") ?? "No Data";
            Log.Debug("URL", URL);
            this.URL = URL;

            var TITLE = Intent.GetStringExtra("TITLE") ?? "No Data";
            this.TITLE = TITLE;

            Title = TITLE;

            GetRentals();

        }

        private async void GetRentals()
        {
            //var cookieManager = CookieManager.Instance;

            bool needsAuth = false;
            client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = httpClientCookies,
                UseCookies = true
            });

            httpClientCookies = new CookieContainer();

            try
            {
                var answer = await client.GetStringAsync("http://www.geekzone.co.nz/" + URL).ConfigureAwait(false);
                ProcessHtml(answer);
            }
            catch (HttpRequestException htmlException)
            {
                /* Unfortunately, HttpRequestException doesn't allow us
                 * to access the original http status code */
                if (!needsAuth)
                    needsAuth = htmlException.Message.Contains("302");
                //continue;
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

            var div = doc.GetElementbyId("colmid");
            var table = div.Element("div").Element("div").Element("table").Elements("tr");

            Forum currentForum = new Forum();

            foreach (var row in table)
            {
                var subforumDescriptionAndTitle = row.Elements("td").ElementAt(2).Element("table");
                var text = subforumDescriptionAndTitle.Descendants("div").Where(x => x.Attributes["class"].Value == "forumMessage").Single();

                Log.Debug("ROW", text.InnerHtml);

                var aSubForum = new SubForum();

                aSubForum.title = text.InnerHtml;

                currentForum.subForums.Add(aSubForum);
            }


            //Populate forum list
            this.RunOnUiThread(() =>
            {
                //Finished loading into array

                foreach (var aSubForum in currentForum.subForums)
                {
                    var subForum = new SubForumList();
                    subForum.title = aSubForum.title;
                    subForum.forumTitle = TITLE;
                    subForum.url = aSubForum.url;
                    m_forums.Add(subForum);
                }

                adapter = new MyAdapter(this, m_forums);
                listView.Adapter = adapter;
                listView.ScrollingCacheEnabled = false;
                listView.DrawingCacheEnabled = false;
                Log.Debug("List", m_forums.ToString());
            });
        }
    }
}