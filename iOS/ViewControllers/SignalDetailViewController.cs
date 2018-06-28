using System;
using LighthousePortableLibrary;
using UIKit;
using Foundation;

namespace LHPEXamarinSample.iOS.ViewControllers
{
    public partial class SignalDetailViewController : UIViewController
    {

        private string _signalId;
        private UIWebView _webView;

        public SignalDetailViewController() : base("SignalDetailViewController", null)
        {
        }

        public SignalDetailViewController(IntPtr handle) : base(handle)
        {
        }

        public SignalDetailViewController(string SignalId) : base("SignalDetailViewController", null)
        {
            this._signalId = SignalId;
        }

        public override void ViewDidLoad()
        {

            _webView = new UIWebView(View.Bounds);
            _webView.ScalesPageToFit = true;
            View.AddSubview(_webView);

            Uri uri = Lighthouse.GetUriForSignal(_signalId);
            string Authorization = Lighthouse.GetAuthorizationToken();

            NSMutableUrlRequest UrlRequest = new Foundation.NSMutableUrlRequest(uri);
            var keys = new object[] { "Authorization" };
            var values = new object[] { Authorization };
            var dict = NSDictionary.FromObjectsAndKeys(values, keys);
            UrlRequest.Headers = dict;
            _webView.LoadRequest(UrlRequest);

            base.ViewDidLoad();

        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            this.NavigationController.ToolbarHidden = true;

        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}

