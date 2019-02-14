using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.Util;

using LighthousePortableLibrary;
using EstimoteSdk.Observation.Region.Beacon;
using EstimoteSdk.Service;

using Plugin.CurrentActivity;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;

using Java.Lang;
using Java.Util;
using Android.Content;

namespace LHPEXamarinSample.Droid
{
    //You can specify additional application information in this attribute
    [Application]
    public class MainApplication : Application, Application.IActivityLifecycleCallbacks, BeaconManager.IServiceReadyCallback
    {

        Lighthouse.Environment LHPEEnvironment = Lighthouse.Environment.STAGING;
        string LHPEAppId = "your-lhpe-app-id";
        string LHPEAppKey = "your-lhpe-app-key";
        string LHPEEstimoteId = "estimote-id";
        string LHPEEstimoteKey = "estimote-key";

        const long LOCATION_INTERVAL = 8 * 1000;
        const long LOCATION_FASTEST_INTERVAL = 1 * 1000;

        BeaconManager BeaconManager;
        List<BeaconRegion> BeaconRegions = new List<BeaconRegion>();
        bool BeaconServiceReady = false;

        FusedLocationProviderClient fusedLocationProviderClient;
        LocationCallback locationCallback;
        LocationRequest locationRequest;
        int locationFrequencySeconds = 60;

        public MainApplication(IntPtr handle, JniHandleOwnership transer)
        : base(handle, transer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            RegisterActivityLifecycleCallbacks(this);
            App.Initialize();

            CrossCurrentActivity.Current.Init(this);

            StartLighthouse();

        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            UnregisterActivityLifecycleCallbacks(this);
        }

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
            CrossCurrentActivity.Current.Activity = activity;
        }

        public void OnActivityDestroyed(Activity activity)
        {
        }

        public void OnActivityPaused(Activity activity)
        {

        }

        public void OnActivityResumed(Activity activity)
        {
            CrossCurrentActivity.Current.Activity = activity;
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {

        }

        public void OnActivityStarted(Activity activity)
        {
            CrossCurrentActivity.Current.Activity = activity;
        }

        public void OnActivityStopped(Activity activity)
        {

        }

        //

        public bool IsPlayServicesAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                    System.Diagnostics.Debug.WriteLine("Google Play Services: " + GoogleApiAvailability.Instance.GetErrorString(resultCode));
                else
                {
                    System.Diagnostics.Debug.WriteLine("Google Play Services: This device is not supported");
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        // Lighthouse

        private async void StartLighthouse()
        {

            System.Diagnostics.Debug.WriteLine("StartLighthouse");

            // Initialize Estimote (TODO: Remove values when distributing)
            EstimoteSdk.Common.Config.Estimote.EnableDebugLogging(true);
            EstimoteSdk.Common.Config.Estimote.Initialize(Application.Context, LHPEEstimoteId, LHPEEstimoteKey);

            // Initialize GPS Location
            if (IsPlayServicesAvailable())
            {
                locationRequest = new LocationRequest()
                                  .SetPriority(LocationRequest.PriorityBalancedPowerAccuracy)
                                  .SetInterval(1000)
                                  .SetFastestInterval(locationFrequencySeconds * 1000);
                locationCallback = new FusedLocationProviderCallback((MainActivity)CrossCurrentActivity.Current.Activity);

                fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
            }

            // Initialize lhpe
            await Lighthouse.Start(LHPEEnvironment, LHPEAppId, LHPEAppKey);

            // Stop Loading Activity, Start Main Activity
            var intent = new Intent(this, typeof(LHPEXamarinSample.Droid.MainActivity));
            StartActivity(intent);

        }

        public async void StartForegroundLocationTracking()
        {
            var LocStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
            if (LocStatus == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
            {

                SetupBeaconing();

                if (fusedLocationProviderClient != null)
                {
                    await fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
                }

            }
        }

        private async void StartBackgroundLocationTracking()
        {
            System.Diagnostics.Debug.WriteLine("StartBackgroundLocationTracking");

            var LocStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
            if (LocStatus == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
            {
                if (fusedLocationProviderClient != null)
                {
                    await fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
                }
            }

            System.Diagnostics.Debug.WriteLine("StartBackgroundLocationTracking Finshed");
        }

        private async void StopBackgroundLocationTracking()
        {
            if (fusedLocationProviderClient != null)
            {
                await fusedLocationProviderClient.RemoveLocationUpdatesAsync(locationCallback);
            }
        }

        public void OnServiceReady()
        {
            System.Diagnostics.Debug.WriteLine("Estimote OnServiceReady");
            BeaconServiceReady = true;
            this.StartBeaconing();
        }

        private async void SetupBeaconing()
        {
            /*
            if(!BeaconServiceReady){
                System.Diagnostics.Debug.WriteLine("Cannot start beaconing: service not ready");
                return;
            }
            */

            if (this.BeaconManager == null)
            {
                this.BeaconManager = new EstimoteSdk.Service.BeaconManager(this);
                this.BeaconManager.SetBackgroundScanPeriod(1000, 8000);
                //this.BeaconManager.SetBeaconMonitoringListener(new MonitoringListener(){});
                this.BeaconManager.BeaconEnteredRegion += (sender, e) => {
                    Lighthouse.SendBeaconRegionLocationUpdate(Lighthouse.BeaconRegionLocationPhase.ENTER, e.Region.ProximityUUID.ToString(), (ushort)e.Region.Major, (ushort)e.Region.Minor, e.Region.Identifier);
                };
                this.BeaconManager.BeaconExitedRegion += (sender, e) => {
                    Lighthouse.SendBeaconRegionLocationUpdate(Lighthouse.BeaconRegionLocationPhase.EXIT, e.Region.ProximityUUID.ToString(), (ushort)e.Region.Major, (ushort)e.Region.Minor, e.Region.Identifier);
                };
                this.BeaconManager.Connect(this);
            }

        }

        private async void StartBeaconing()
        {

            if (!this.BeaconServiceReady)
            {
                System.Diagnostics.Debug.WriteLine("Beaconing cannot be started.  Service is not ready.");
                return;
            }

            this.StopBeaconing();

            // fetch beacon regions
            var ServerBeaconRegions = await Lighthouse.GetBeaconRegions();

            // start monitoring on each region
            foreach (Lighthouse.LHPEBeaconRegion region in ServerBeaconRegions)
            {
                SecureBeaconRegion BeaconRegion = null;
                if (region.Minor != null)
                {
                    BeaconRegion = new SecureBeaconRegion(region.Id, UUID.FromString(region.UUID), new Integer(Convert.ToInt32(region.Major)), new Integer(Convert.ToInt32(region.Minor)));
                }
                else if (region.Major != null)
                {
                    BeaconRegion = new SecureBeaconRegion(region.Id, UUID.FromString(region.UUID), new Integer(Convert.ToInt32(region.Major)), null);
                }
                else
                {
                    BeaconRegion = new SecureBeaconRegion(region.Id, UUID.FromString(region.UUID), null, null);
                }
                BeaconRegions.Add(BeaconRegion);

                this.BeaconManager.StartMonitoring(BeaconRegion);
            }

        }

        private void StopBeaconing()
        {
            // clean up Estimote
            foreach (BeaconRegion region in BeaconRegions)
            {
                this.BeaconManager.StopMonitoring(region.Identifier);
            }

            // clear BeaconRegions
            BeaconRegions.Clear();
        }

        public class FusedLocationProviderCallback : LocationCallback
        {
            readonly MainActivity activity;

            public FusedLocationProviderCallback(MainActivity activity)
            {
                this.activity = activity;
            }

            public override void OnLocationAvailability(LocationAvailability locationAvailability)
            {
                Log.Debug("FusedLocationProvider", "IsLocationAvailable: {0}", locationAvailability.IsLocationAvailable);
            }


            public override void OnLocationResult(LocationResult result)
            {
                if (result.Locations.Any())
                {
                    var location = result.Locations.First();
                    Lighthouse.SendGPSLocationUpdate(location.Latitude, location.Longitude, location.Altitude, location.Accuracy, location.Accuracy);
                }
            }
        }

    }
}
