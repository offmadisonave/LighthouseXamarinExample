using Foundation;
using UIKit;
using LighthousePortableLibrary;
using CoreLocation;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Estimote;
using LHPEXamarinSample.iOS.ViewControllers;

namespace LHPEXamarinSample.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        // class-level declarations
        CLLocationManager LocationManager;
        SecureBeaconManager BeaconManager;
        bool FirstBeaconing = true;

        // Your LHPE

        Lighthouse.Environment LHPEEnvironment = Lighthouse.Environment.STAGING;
        string LHPEAppId = "your-lhpe-app-id";
        string LHPEAppKey = "your-lhpe-app-key";
        string LHPEEstimoteId = "estimote-id";
        string LHPEEstimoteKey = "estimote-key";

        public override UIWindow Window
        {
            get;
            set;
        }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            App.Initialize();

            StartLighthouse();

            return true;
        }

        public override void OnResignActivation(UIApplication application)
        {
            // Invoked when the application is about to move from active to inactive state.
            // This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
            // or when the user quits the application and it begins the transition to the background state.
            // Games should use this method to pause the game.
        }

        public override void DidEnterBackground(UIApplication application)
        {
            // Use this method to release shared resources, save user data, invalidate timers and store the application state.
            // If your application supports background exection this method is called instead of WillTerminate when the user quits.
            this.StartBackgroundLocationTracking();
        }

        public override void WillEnterForeground(UIApplication application)
        {
            // Called as part of the transiton from background to active state.
            // Here you can undo many of the changes made on entering the background.
            this.StopBackgroundLocationTracking();
        }

        public override void OnActivated(UIApplication application)
        {
            // Restart any tasks that were paused (or not yet started) while the application was inactive. 
            // If the application was previously in the background, optionally refresh the user interface.
            this.StartForegroundLocationTracking();
        }

        public override void WillTerminate(UIApplication application)
        {
            // Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
        }


        // Lighthouse

        private async void StartLighthouse()
        {
            // create location manager
            this.LocationManager = new CLLocationManager();
            this.LocationManager.AllowsBackgroundLocationUpdates = true;
            this.LocationManager.DesiredAccuracy = CLLocation.AccuracyNearestTenMeters;
            this.LocationManager.DistanceFilter = 100;
            this.LocationManager.PausesLocationUpdatesAutomatically = true;
            this.LocationManager.Delegate = new LocationManagerDelegate();

            // initialize lighhouse
            await Lighthouse.Start(LHPEEnvironment, LHPEAppId, LHPEAppKey);  

            // Change root view controller
            var window = UIApplication.SharedApplication.KeyWindow;
            var storyboard = UIStoryboard.FromName("Main", null);
            var root = storyboard.InstantiateViewController("SignalList");
            window.RootViewController = root;

        }

        private async void StartForegroundLocationTracking()
        {
            var LocAlwaysStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.LocationAlways);
            var LocStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
            if (CLLocationManager.LocationServicesEnabled && (LocAlwaysStatus == PermissionStatus.Granted || LocStatus == PermissionStatus.Granted))
            {
                this.StartBeaconing();
                // start updating location
                this.LocationManager.StartUpdatingLocation();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Foreground Tracking could not start because permissions are not granted or location services are not enabled");
            }
        }

        private void StopBackgroundLocationTracking()
        {
            this.LocationManager.StopMonitoringSignificantLocationChanges();
        }

        private async void StartBackgroundLocationTracking()
        {
            this.LocationManager.StopUpdatingLocation();
            var LocStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.LocationAlways);
            if (CLLocationManager.LocationServicesEnabled && LocStatus == PermissionStatus.Granted)
            {
                this.LocationManager.StartMonitoringSignificantLocationChanges();
            }
        }

        private async void StartBeaconing()
        {

            if (this.BeaconManager == null)
            {
                Estimote.Config.Setup(LHPEEstimoteId, LHPEEstimoteKey); // TODO: Remove from source before distributing
                this.BeaconManager = new Estimote.SecureBeaconManager();
                this.BeaconManager.Delegate = new BeaconManagerDelegate();
            }

            this.StopBeaconing();

            // fetch beacon regions
            var BeaconRegions = await Lighthouse.GetBeaconRegions();

            if (FirstBeaconing)
            {
                this.GetBeaconRegionsState();
            }
            else
            {
                // start monitoring on each region
                foreach (Lighthouse.LHPEBeaconRegion region in BeaconRegions)
                {
                    CLBeaconRegion BeaconRegion = null;
                    if (region.Minor != null)
                    {
                        BeaconRegion = new CLBeaconRegion(new NSUuid(region.UUID), (ushort)region.Major, (ushort)region.Minor, region.Id);
                    }
                    else if (region.Major != null)
                    {
                        BeaconRegion = new CLBeaconRegion(new NSUuid(region.UUID), (ushort)region.Major, region.Id);
                    }
                    else
                    {
                        BeaconRegion = new CLBeaconRegion(new NSUuid(region.UUID), region.Id);
                    }
                    this.BeaconManager.StartMonitoring(BeaconRegion);
                }
            }

        }

        private void GetBeaconRegionsState()
        {

            FirstBeaconing = false;

            foreach (CLBeaconRegion region in this.BeaconManager.MonitoredRegions)
            {
                this.BeaconManager.RequestState(region);
            }

            this.StopBeaconing();
            this.StartBeaconing();

        }

        private void StopBeaconing()
        {
            // clean up Estimote
            foreach (CLBeaconRegion region in this.BeaconManager.MonitoredRegions)
            {
                this.BeaconManager.StopMonitoring(region);
            }

        }

        // Location Manager Delegate
        public class LocationManagerDelegate : CLLocationManagerDelegate
        {

            public override void UpdatedLocation(CLLocationManager manager, CLLocation newLocation, CLLocation oldLocation)
            {
                Lighthouse.SendGPSLocationUpdate(newLocation.Coordinate.Latitude, newLocation.Coordinate.Longitude, newLocation.Altitude, newLocation.HorizontalAccuracy, newLocation.VerticalAccuracy);
            }

            public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
            {
                foreach (CLLocation newLocation in locations)
                {
                    Lighthouse.SendGPSLocationUpdate(newLocation.Coordinate.Latitude, newLocation.Coordinate.Longitude, newLocation.Altitude, newLocation.HorizontalAccuracy, newLocation.VerticalAccuracy);
                }
            }


        }

        // Beacon Manager Delegate
        public class BeaconManagerDelegate : SecureBeaconManagerDelegate
        {

            public override void DidEnterRegion(NSObject manager, CLBeaconRegion region)
            {
                // Using DidDetermineState
            }

            public override void DidExitRegion(NSObject manager, CLBeaconRegion region)
            {
                // Using DidDetermineState
            }

            public override void DidDetermineState(NSObject manager, CLRegionState state, CLBeaconRegion region)
            {
                if (state == CLRegionState.Inside)
                {
                    Lighthouse.SendBeaconRegionLocationUpdate(Lighthouse.BeaconRegionLocationPhase.ENTER, region.ProximityUuid.ToString(), region.Major.UInt16Value, region.Minor.UInt16Value, region.Identifier);
                }
                else
                {
                    Lighthouse.SendBeaconRegionLocationUpdate(Lighthouse.BeaconRegionLocationPhase.EXIT, region.ProximityUuid.ToString(), region.Major.UInt16Value, region.Minor.UInt16Value, region.Identifier);
                }
            }

        }

        // Push Notifications

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            // Get current device token
            var DeviceToken = deviceToken.Description;
            if (!string.IsNullOrWhiteSpace(DeviceToken))
            {
                DeviceToken = DeviceToken.Trim('<').Trim('>');
            }

            // Get previous device token
            var oldDeviceToken = NSUserDefaults.StandardUserDefaults.StringForKey("PushDeviceToken");

            // Has the token changed?
            if (string.IsNullOrEmpty(oldDeviceToken) || !oldDeviceToken.Equals(DeviceToken))
            {
                Lighthouse.SetPushToken(DeviceToken);
                Lighthouse.SetPushAllowed(true);
            }

            // Save new device token
            NSUserDefaults.StandardUserDefaults.SetString(DeviceToken, "PushDeviceToken");
        }

    }
}
