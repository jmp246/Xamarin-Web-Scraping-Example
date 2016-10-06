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
using Android.Util;
using Android.Webkit;
using Android.Graphics;
using System.Net.Http;

namespace Geekzone
{
    [Activity(Label = "LoginActivity")]
    public class LoginActivity :MasterActivity
    {
        EditText usernameText;
        EditText passwordText;

        class MyWebViewClient : WebViewClient
        {
            string loginCookie = "";

            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                base.OnPageStarted(view, url, favicon);

                //Page load started look for login redirect
                Log.Debug("Page load: ", url);
            }
        }

        private async void login()
        {
            string username = "";
            string password = "";

            this.RunOnUiThread(() =>
            {
                username = usernameText.Text;
                password = passwordText.Text;
            });

            if (username.Equals("") || password.Equals(""))
            {
                var errorToast = Toast.MakeText(this, "Make sure to enter a username and password.", ToastLength.Short);
                errorToast.Show();
            }
            else
            {
                bool loggedIn = await login(username, password);
                //Log.Debug("LOGIN",usernameText.Text + " " + passwordText.Text);

                if (loggedIn)
                {
                    Log.Debug("LOGIN", "Worked");
                    this.RunOnUiThread(() =>
                    {
                        StartActivity(typeof(MainActivity));
                    });
                }
                else
                {
                    Log.Debug("Login", "Fail");
                    var errorToast = Toast.MakeText(this, "Make sure your username and password is correct.", ToastLength.Short);
                    errorToast.Show();
                }
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.Title = "Login";

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.login);

            usernameText = FindViewById<EditText>(Resource.Id.usernameEditText);
            passwordText = FindViewById<EditText>(Resource.Id.passwordEditText);

            Button loginButton = FindViewById<Button>(Resource.Id.loginButton);
            loginButton.Click += delegate
            {
                login();
            };
        }
    }
}