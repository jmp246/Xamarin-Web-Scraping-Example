using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Util;
using HtmlAgilityPack;
using System.Linq;
using Android.Webkit;
using Android.Graphics;

namespace Geekzone
{
    [Activity(Label = "Geekzone", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : MasterActivity
    {
        int count = 1;

        private HttpClient client;
        private CookieContainer httpClientCookies;
        private List<SubForumList> m_forums = new List<SubForumList>();
        private MyAdapter adapter;
        private ListView listView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var data = new ListItemCollection<ListItemValue>()
            {
                new ListItemValue("1"),
                new ListItemValue("2")
            };

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.main);

            // Get our button from the layout resource,
            // and attach an event to it
            GetData();

            listView = FindViewById<ListView>(Resource.Id.list);

            listView.ItemClick += (sender, e) =>
            {
                if (e.Position >= 0)
                {
                    var item = m_forums.ElementAt(e.Position);

                    this.RunOnUiThread(() =>
                    {
                        var startActivityIntent = new Intent(this, typeof(ViewForumActivity));
                        startActivityIntent.PutExtra("URL", item.url);
                        startActivityIntent.PutExtra("TITLE", item.title);
                        StartActivity(startActivityIntent);
                        Log.Debug("TOU", item.title);
                    });
                }
            };

        }

        public class Rental
        {
            public int id;
            public string text;
        }

        private async void GetData()
        {
            bool isLoggedIn = await testLoginCookie();

            if(isLoggedIn)
            {
                Log.Debug("LOGIN", "Logged In");
                await getForums();
            }
            else
            {
                //Show login page
                StartActivity(typeof(LoginActivity));
            }
        }

        private async Task<Rental[]> getForums()
        {
            //var cookieManager = CookieManager.Instance;
            
            bool needsAuth = false;
            client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = httpClientCookies,
                UseCookies = true
            });

            httpClientCookies = new CookieContainer(); //Cookies not actually used for this screen

            try
            {
                var answer = await client.GetStringAsync("http://www.geekzone.co.nz/forums.asp").ConfigureAwait(false);
                return ProcessHtml(answer);
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

            return new Rental[] { };
        }

        public class Forum
        {
            public Forum()
            {
                subForums = new List<SubForum>();
            }

            public string title { get; set; }
            public string url { get; set; }
            public List<SubForum> subForums { get; set; }
        }

        public class SubForum
        {
            public string title { get; set; }
            public string description { get; set; }
            public string url { get; set; }
            public string pinned { get; set; }
        }

        public class SubForumList
        {
            public string forumTitle { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public string url { get; set; }
        }

        private Rental[] ProcessHtml(string answer)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(answer); //Parse HTML

            var div = doc.GetElementbyId("colmid");
            var table = div.Element("div").Element("div").Element("table").Elements("tr");

            List<Forum> forumList = new List<Forum>();

            //int count = 0;
            foreach (var row in table)
            {
                try
                {
                    if (row.Element("td").GetAttributeValue("class", "") == "subHeading") //Forum
                    {
                        var currentForum = new Forum();
                        currentForum.title = row.Element("td").InnerText.Trim();
                        Log.Debug("ROW", "Forum: " + currentForum.title);

                        forumList.Add(currentForum);
                    }
                    else
                    {
                        var aSubForum = new SubForum();

                        var rowData = row.Elements("td");

                        Log.Debug("Forum", rowData.ElementAt(0).InnerHtml);

                        if (rowData.ElementAt(0).InnerHtml.Contains("forum_newstar.png")) //Can't tell as user isn't logged into desktop site
                        {
                            aSubForum.pinned = "Hot";
                        }
                        else
                        {
                            aSubForum.pinned = "";
                        }

                        //Get sub forum title
                        var subforumDescriptionAndTitle = rowData.ElementAt(1);
                        var subforumTitle = subforumDescriptionAndTitle.Element("a");

                        aSubForum.title = subforumTitle.InnerText.Trim();
                        aSubForum.url = subforumTitle.GetAttributeValue("href", "");

                        //Get sub forum description by removing title
                        subforumDescriptionAndTitle.RemoveChild(subforumTitle);

                        aSubForum.description = subforumDescriptionAndTitle.InnerText.Trim();

                        Log.Debug("ROW", "Sub Forum Title: " + aSubForum.title);
                        Log.Debug("ROW", "Sub Forum Description: " + aSubForum.description);
                        Log.Debug("ROW", "Sub Forum URL: " + aSubForum.url);

                        var currentForum = forumList.Last();
                        currentForum.subForums.Add(aSubForum);

                    }
                }
                catch (Exception e)
                {
                    Log.Debug("ERROR", row.InnerHtml);
                    Log.Debug("ERROR", e.ToString());
                }
            }

            //Populate forum list
            this.RunOnUiThread(() =>
            {
                //Add array
                var latestSubForum = new SubForumList();
                latestSubForum.title = "Latest Posts";
                latestSubForum.forumTitle = "Latest";
                latestSubForum.url = "https://live.geekzone.co.nz/LoadForums.asp";
                m_forums.Add(latestSubForum);

                //Finished loading into array
                foreach (var aForum in forumList)
                {
                    var currentForumTitle = aForum.title;

                    foreach (var aSubForum in aForum.subForums)
                    {
                        var subForum = new SubForumList();
                        subForum.title = aSubForum.title;
                        subForum.forumTitle = currentForumTitle + " " + aSubForum.pinned;
                        subForum.url = aSubForum.url;
                        m_forums.Add(subForum);
                    }
                }

                //Check Login
                adapter = new MyAdapter(this, m_forums);
                listView.Adapter = adapter;
                adapter.NotifyDataSetChanged();
                //listView.DeferNotifyDataSetChanged();
                Log.Debug("List", m_forums.ToString());
            });

            return new Rental[] { };
        }
    }
}

