
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

namespace LHPEXamarinSample.Droid
{
    [Activity(Label = "LoadingActivity", MainLauncher = true, Icon = "@mipmap/icon")]
    public class LoadingActivity : Android.Support.V7.App.AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.loading);

            SupportActionBar.Title = "Lighthouse PE";

        }
    }
}
