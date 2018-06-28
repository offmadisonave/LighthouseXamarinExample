
using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Webkit;

using LighthousePortableLibrary;

namespace LHPEXamarinSample.Droid.Activities
{

    [Activity(Label = "SignalActivity")]
    public class SignalActivity : Activity
    {

        private WebView web_view;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            string SignalId = Intent.GetStringExtra("SignalId") ?? null;
            Uri uri = Lighthouse.GetUriForSignal(SignalId);

            SetContentView(Resource.Layout.signal_activity);

            web_view = FindViewById<WebView>(Resource.Id.webview);
            web_view.Settings.JavaScriptEnabled = true;
            web_view.SetWebViewClient(new SignalWebViewClient(Lighthouse.GetAuthorizationToken()));
            web_view.LoadUrl(uri.AbsoluteUri);

        }

        public class SignalWebViewClient : WebViewClient
        {

            private string Authorization;

            public SignalWebViewClient(string AuthorizationToken) : base()
            {
                this.Authorization = AuthorizationToken;
            }

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                view.LoadUrl(url);
                return false;
            }

            // For API level 24 and later
            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
            {
                view.LoadUrl(request.Url.ToString());
                return false;
            }

            public override WebResourceResponse ShouldInterceptRequest(WebView view, IWebResourceRequest request)
            {

                var client = new HttpClient();

                client.DefaultRequestHeaders.Add("Authorization", Authorization);

                var result = client.GetAsync(request.Url.ToString()).Result;
                var encoding = result.Content.Headers.ContentEncoding.GetEnumerator().Current;
                String contentType = result.Content.Headers.ContentType.ToString();
                var stream = result.Content.ReadAsStreamAsync().Result;

                return new WebResourceResponse("text/html", "charset=utf-8", stream);

            }

        }

    }

}
