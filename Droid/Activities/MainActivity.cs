using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Runtime;
using Android.Support.V7;
using Toolbar = Android.Support.V7.Widget.Toolbar;

using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Android.Views;

using Java.Lang;
using LighthousePortableLibrary;

using System.Collections.Generic;

namespace LHPEXamarinSample.Droid
{

    [Activity(Label = "LighthousePE", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Android.Support.V7.App.AppCompatActivity
    {

        private ListView ListView;
        private List<Lighthouse.LHPESignal> Signals = new List<Lighthouse.LHPESignal>();
        private SignalsAdapter ListAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);

            //Toolbar will now take on default actionbar characteristics
            SetSupportActionBar(toolbar);

            SupportActionBar.Title = "Lighthouse PE";

            ListView = FindViewById<ListView>(Resource.Id.listView);

            ListView.ItemClick += delegate (object sender, AdapterView.ItemClickEventArgs args)
            {
                Lighthouse.LHPESignal signal = Signals[args.Position];
                var intent = new Intent(this, typeof(LHPEXamarinSample.Droid.Activities.SignalActivity));
                intent.PutExtra("SignalId", signal.Id);
                StartActivity(intent);
            };

        }

        private void Reload()
        {
            // Note: These typically wouldn't be used at the same time
            GetUnreadSignalCount();
            GetSignals();
        }

        private async void GetUnreadSignalCount()
        {
            int UnreadCount = await Lighthouse.GetUnreadSignalCount();
            //UnreadBtn.Title = UnreadCount + " Unread";
        }

        private async void GetSignals()
        {
            Signals = await Lighthouse.GetSignals();
            ListAdapter = new SignalsAdapter(this, Signals);
            ListView.Adapter = ListAdapter;
        }

        protected override void OnResume()
        {
            GetLocationPermission();
            Reload();
            base.OnResume();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.set_user:

                    LayoutInflater layoutInflater = LayoutInflater.From(this);
                    View view = layoutInflater.Inflate(Resource.Layout.user_form, null);

                    Android.Support.V7.App.AlertDialog.Builder alertbuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
                    alertbuilder.SetView(view);

                    var userid = view.FindViewById<EditText>(Resource.Id.userIdText);
                    var firstName = view.FindViewById<EditText>(Resource.Id.firstNameText);
                    var lastName = view.FindViewById<EditText>(Resource.Id.lastNameText);

                    alertbuilder.SetCancelable(false)
                    .SetPositiveButton("Submit", delegate
                    {
                        System.Diagnostics.Debug.WriteLine("Setting User: " + userid.Text + ", " + firstName.Text + " " + lastName.Text);

                        Lighthouse.LHPEUser user = new Lighthouse.LHPEUser();
                        user.Id = userid.Text;
                        Dictionary<string, string> Profile = new Dictionary<string, string>();
                        Profile.Add("first_name", firstName.Text);
                        Profile.Add("last_name", lastName.Text);
                        user.Profile = Profile;

                        Lighthouse.SetUserProfile(user);
                    })
                    .SetNegativeButton("Cancel", delegate
                    {
                        alertbuilder.Dispose();
                    });

                    Android.Support.V7.App.AlertDialog dialog = alertbuilder.Create();

                    dialog.Show();

                    return true;
            }

            return base.OnOptionsItemSelected(item);

        }

        private async void GetLocationPermission()
        {
            System.Diagnostics.Debug.WriteLine("GetLocationPermission");

            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Location))
                    {
                        //await DisplayAlert("Need location", "Gunna need that location", "OK");
                        System.Diagnostics.Debug.WriteLine("Need Location");
                    }
                    var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);
                    //Best practice to always check that the key exists
                    if (results.ContainsKey(Permission.Location))
                    {
                        status = results[Permission.Location];
                    }
                }

                if (status == PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("Location Granted");
                    await Lighthouse.SetLocationAllowed(true);
                    ((MainApplication)Application).StartForegroundLocationTracking();
                }
                else if (status != PermissionStatus.Unknown)
                {
                    System.Diagnostics.Debug.WriteLine("Location Denied");
                    await Lighthouse.SetLocationAllowed(false);
                    //await DisplayAlert("Location Denied", "Can not continue, try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.LocalizedMessage);
            }

        }

    }

    public class SignalsAdapter : BaseAdapter<Lighthouse.LHPESignal>
    {

        List<Lighthouse.LHPESignal> items;
        Activity context;

        public SignalsAdapter(Activity context, List<Lighthouse.LHPESignal> items) : base()
        {
            this.context = context;
            this.items = items;
        }

        public override long GetItemId(int position)
        {
            return position;
        }
        public override Lighthouse.LHPESignal this[int position]
        {
            get { return items[position]; }
        }
        public override int Count
        {
            get { return items.Count; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
                view = context.LayoutInflater.Inflate(Resource.Layout.signal_item, null);

            view.FindViewById<TextView>(Resource.Id.titleText).Text = items[position].Title;
            view.FindViewById<TextView>(Resource.Id.descriptionText).Text = items[position].Alert;

            return view;
        }

    }

}
