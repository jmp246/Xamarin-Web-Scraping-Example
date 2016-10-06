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
using HtmlAgilityPack;
using Android.Util;
using System.Net.Http;
using System.Net;
using static Geekzone.MainActivity;

namespace Geekzone
{
    [Activity(Label = "ViewForumActivity")]
    public class ViewForumActivity : Activity
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

            SetContentView(Resource.Layout.viewForum);
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

            listView.ItemClick += (sender, e) =>
            {
                if (e.Position >= 0)
                {
                    var item = m_forums.ElementAt(e.Position);

                    this.RunOnUiThread(() =>
                    {
                        var startActivityIntent = new Intent(this, typeof(WebViewForum));


                        var parameters = item.url.Split('?')[1].Split('&');
                        string forumID = parameters[0].Split('=')[1];
                        string topicID = parameters[1].Split('=')[1];

                        Log.Debug("TOU", "Forum: " + forumID);
                        Log.Debug("TOU", "Topic: " + topicID);

                        //http://m.geekzone.co.nz/Forums/75/Topic/196726
                        //string mobileURL = item.url;

                        //string 

                        startActivityIntent.PutExtra("URL", "http://m.geekzone.co.nz/Forums/" + forumID + "/Topic/" + topicID);
                        startActivityIntent.PutExtra("TITLE", item.title);
                        StartActivity(startActivityIntent);
                        Log.Debug("TOU", item.title);
                    });
                }
            };
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
                if (URL.Equals("https://live.geekzone.co.nz/LoadForums.asp")) //Latest Posts
                {
                    var answer = await client.GetStringAsync(URL).ConfigureAwait(false);
                    processLatestForums(answer);
                }
                else
                {
                    var answer = await client.GetStringAsync("http://www.geekzone.co.nz/" + URL).ConfigureAwait(false);
                    ProcessHtml(answer);
                }
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
            //}

            //return new Rental[] { };
        }

        private void processLatestForums(string answer)
        {
            //Single parse and extra <a> then <span>
            //or split via "<br /><br />" then parse

            //Log.Debug("Latest", "Split");

            Forum currentForum = new Forum();
            currentForum.title = "Latest";

            var splitStringArray = new string[] { "<br /><br />" };
            var latestPosts = answer.Split(splitStringArray, StringSplitOptions.None);

            for (int post = 1; post < latestPosts.Length-1; post++) //Note missing last entry
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(latestPosts[post]); //Parse HTML

                Log.Debug("Latest", "---");
                Log.Debug("Latest", doc.DocumentNode.InnerHtml);

                

                var aSubForum = new SubForum();

                var titleNode = doc.DocumentNode.Elements("a").ElementAt(0);

                aSubForum.title = titleNode.InnerText;
                Log.Debug("Latest", "Title: " + titleNode.InnerText);
                aSubForum.url = titleNode.GetAttributeValue("href", "").Substring(25);

                aSubForum.description = doc.DocumentNode.Elements("span").ElementAt(0).InnerText;
                

                currentForum.subForums.Add(aSubForum);
            }

            //Populate forum list
            this.RunOnUiThread(() =>
            {
                //Finished loading into array
                foreach (var addSubForum in currentForum.subForums)
                {
                    var subForum = new SubForumList();
                    subForum.title = addSubForum.title;
                    subForum.forumTitle = "";
                    subForum.url = addSubForum.url;
                    m_forums.Add(subForum);
                }

                adapter = new MyAdapter(this, m_forums);
                listView.Adapter = adapter;
                listView.ScrollingCacheEnabled = false;
                listView.DrawingCacheEnabled = false;
                Log.Debug("List", m_forums.ToString());
            });
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
                if (row.Element("td").GetAttributeValue("colspan", "").Equals("4")) //First Row
                {
                    Log.Debug("ROW", "First Column");
                    currentForum.title = "Title";
                }
                else
                {
                    var aSubForum = new SubForum();

                    var rowData = row.Elements("td");
                    var status = rowData.ElementAt(0).GetAttributeValue("class", "");
                    if (status.Equals("forumRowLightPinned") || status.Equals("forumRowPinned"))
                    {
                        aSubForum.pinned = "Pinned";
                    }
                    else if (status.Equals("forumRowHot"))
                    {
                        aSubForum.pinned = "Hot";
                    }
                    else
                    {
                        aSubForum.pinned = "";
                    }

                    var subforumDescriptionAndTitle = rowData.ElementAt(1);
                    var subforumTitle = subforumDescriptionAndTitle.Element("a");

                    var subforumNotes = rowData.ElementAt(2);

                    aSubForum.title = subforumTitle.InnerText.Trim() + subforumNotes.InnerText.Trim();
                    aSubForum.url = subforumTitle.GetAttributeValue("href", "");

                    //Get sub forum description by removing title
                    subforumDescriptionAndTitle.RemoveChild(subforumTitle);

                    aSubForum.description = subforumDescriptionAndTitle.InnerText.Trim();

                    Log.Debug("ROW", "Sub Forum Title: " + aSubForum.title);
                    Log.Debug("ROW", "Sub Forum Description: " + aSubForum.description);
                    Log.Debug("ROW", "Sub Forum URL: " + aSubForum.url);

                    currentForum.subForums.Add(aSubForum);
                    //Log.Debug("ROW", subforumTitle.InnerHtml);
                }

                Log.Debug("ROW", row.InnerHtml);
            }


            //Populate forum list
            this.RunOnUiThread(() =>
            {
                //Finished loading into array

                foreach (var aSubForum in currentForum.subForums)
                {
                    var subForum = new SubForumList();
                    subForum.title = aSubForum.title;
                    subForum.forumTitle = aSubForum.pinned;
                    subForum.url = aSubForum.url;
                    m_forums.Add(subForum);
                }

                adapter = new MyAdapter(this, m_forums);
                listView.Adapter = adapter;
                listView.ScrollingCacheEnabled = false;
                listView.DrawingCacheEnabled = false;
                Log.Debug("List", m_forums.ToString());
            });

            //return new Rental[] { };
        }
    }
}