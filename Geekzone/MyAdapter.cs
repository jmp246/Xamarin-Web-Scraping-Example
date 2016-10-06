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
using com.refractored.components.stickylistheaders;
using Java.Lang;
using static Geekzone.MainActivity;
using Android.Text;
using static Android.Text.Html;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Graphics;
using System.Threading.Tasks;

namespace Geekzone
{
    public class HeaderViewHolder : Java.Lang.Object
    {
        public TextView Text1 { get; set; }
    }

    public class ViewHolder : Java.Lang.Object
    {
        public TextView Text { get; set; }

        public static explicit operator TextView(ViewHolder v)
        {
            throw new NotImplementedException();
        }
    }


    public class MyAdapter : BaseAdapter, IStickyListHeadersAdapter, ISectionIndexer
    {
        //private string[] m_Countries;
        private List<SubForumList> m_forums;
        private LayoutInflater m_Inflater;
        private Context m_Context;

        public MyAdapter(Context context, List<SubForumList> m_forums)
        {
            m_Context = context;
            m_Inflater = LayoutInflater.From(context);

            this.m_forums = m_forums;
        }

        public override int Count
        {
            get { return m_forums.Count; }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return m_forums.ElementAt(position).title;
        }

        public override long GetItemId(int position)
        {
            return position;//unique
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder = null;
            if (convertView == null)
            {
                holder = new ViewHolder();
                convertView = m_Inflater.Inflate(Resource.Layout.forumItem, parent, false);
                holder.Text = convertView.FindViewById<TextView>(Resource.Id.text);
                convertView.Tag = holder;
            }
            else
            {
                holder = convertView.Tag as ViewHolder;
            }

            //Tried loading images inside posts but it would take a lot of work to make it look good.
            //MyTask loadTask = new MyTask(this,holder, m_forums.ElementAt(position).title);
            //loadTask.Execute();
            //holder.Text.TextFormatted = Html.FromHtml(m_forums.ElementAt(position).title, new ImageGetter(), null);
            //GetImages(holder,m_forums.ElementAt(position).title);//Html.FromHtml(m_forums.ElementAt(position).title, new ImageGetter(), null);

            holder.Text.Text = m_forums.ElementAt(position).title;

            return convertView;

        }

        public class MyTask : AsyncTask
        {
            ViewHolder textView;
            string htmlString;
            ISpanned html;
            MyAdapter myAdapter;

            public MyTask(MyAdapter myAdapter, ViewHolder textView, string htmlString)
            {
                this.textView = textView;
                this.htmlString = htmlString;
                this.myAdapter = myAdapter;
            }

            protected override void OnPreExecute()
            {
            }


            protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
            {
                html = Html.FromHtml(htmlString, new ImageGetter(), null);

                Action action = delegate
                {
                    textView.Text.TextFormatted = html;
                    textView.Text.Visibility = ViewStates.Invisible;
                    textView.Text.Visibility = ViewStates.Visible;
                    //textView.Text.PostInvalidate();
                    //((TextView)textView).SetText()
                };
                textView.Text.Post(action);
                

                return null;
            }

            protected override void OnPostExecute(Java.Lang.Object result)
            {
            }
        }

        public class ImageGetter : Java.Lang.Object, Html.IImageGetter
        {
            public Drawable GetDrawable(string source)
            {
                Drawable drawable;
                Bitmap bitMap;
                BitmapFactory.Options bitMapOption;
                try
                {
                    Log.Debug("URL: ", source);

                    bitMapOption = new BitmapFactory.Options();
                    bitMapOption.InJustDecodeBounds = false;
                    bitMapOption.InPreferredConfig = Bitmap.Config.Argb4444;
                    bitMapOption.InPurgeable = true;
                    bitMapOption.InInputShareable = true;
                    var url = new Java.Net.URL(source);

                    bitMap = BitmapFactory.DecodeStream(url.OpenStream(), null, bitMapOption);
                    drawable = new BitmapDrawable(bitMap);

                }
                catch (Java.Lang.Exception e)
                {
                    Log.Debug("IMAGE URL ERR: ", e.ToString());
                    return null;
                }

                drawable.SetBounds(0, 0, bitMapOption.OutWidth, bitMapOption.OutHeight);
                return drawable;

            }

            public new IntPtr Handle
            {
                get { return base.Handle; }
            }

            public new void Dispose()
            {
                base.Dispose();
            }
    }

        public View GetHeaderView(int position, View convertView, ViewGroup parent)
        {
            HeaderViewHolder holder = null;
            if (convertView == null)
            {
                holder = new HeaderViewHolder();
                convertView = m_Inflater.Inflate(Resource.Layout.listViewSection, parent, false);
                holder.Text1 = convertView.FindViewById<TextView>(Resource.Id.text1);
                convertView.Tag = holder;
            }
            else
            {
                holder = convertView.Tag as HeaderViewHolder;
            }

            var headerChar = m_forums.ElementAt(position).forumTitle;
            string headerText = headerChar.ToString();
            holder.Text1.Text = headerText;
            return convertView;
        }

        public long GetHeaderId(int position)
        {
            return m_forums.ElementAt(position).forumTitle.GetHashCode();
        }

        public int GetPositionForSection(int sectionIndex)
        {
            throw new NotImplementedException();
        }

        public int GetSectionForPosition(int position)
        {
            throw new NotImplementedException();
        }

        public Java.Lang.Object[] GetSections()
        {
            throw new NotImplementedException();
        }
    }
}