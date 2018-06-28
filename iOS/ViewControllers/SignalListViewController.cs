using System;
using UIKit;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using LighthousePortableLibrary;
using Foundation;
using System.Collections.Generic;

namespace LHPEXamarinSample.iOS.ViewControllers
{
    public partial class SignalListViewController : UITableViewController
    {

        private List<Lighthouse.LHPESignal> Signals = new List<Lighthouse.LHPESignal>();
        protected string cellIdentifier = "codeCell";
        private UIBarButtonItem UnreadBtn;

        public SignalListViewController() : base("SignalListViewController", null)
        {
        }

        public SignalListViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.NavigationItem.Title = "Notifications";

            // Toolbar

            var fontAttribute = new UITextAttributes();
            fontAttribute.Font = UIFont.SystemFontOfSize(14);

            UIBarButtonItem pushBtn = new UIBarButtonItem("Request Push", UIBarButtonItemStyle.Plain, delegate {
                GetPushPermission();
            });
            pushBtn.SetTitleTextAttributes(fontAttribute, UIControlState.Normal);
            pushBtn.SetTitleTextAttributes(fontAttribute, UIControlState.Highlighted);

            UnreadBtn = new UIBarButtonItem("Loading...", UIBarButtonItemStyle.Plain, delegate {
                Reload();
            });
            UnreadBtn.SetTitleTextAttributes(fontAttribute, UIControlState.Normal);
            UnreadBtn.SetTitleTextAttributes(fontAttribute, UIControlState.Highlighted);

            UIBarButtonItem spacer = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace);

            UIBarButtonItem locationBtn = new UIBarButtonItem("Request Location", UIBarButtonItemStyle.Plain, delegate {
                GetLocationPermission();
            });
            locationBtn.SetTitleTextAttributes(fontAttribute, UIControlState.Normal);
            locationBtn.SetTitleTextAttributes(fontAttribute, UIControlState.Highlighted);

            this.ToolbarItems = new UIBarButtonItem[] { UnreadBtn, spacer, pushBtn, locationBtn };

            // Nav Buttons
            UIBarButtonItem profileBtn = new UIBarButtonItem("Profile", UIBarButtonItemStyle.Plain, delegate {
                NavigationController.PushViewController(new ProfileViewController(), true);
            });
            this.NavigationItem.RightBarButtonItem = profileBtn;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            this.NavigationController.ToolbarHidden = false;

            Reload();

        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
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
            UnreadBtn.Title = UnreadCount + " Unread";
        }

        private async void GetSignals()
        {
            Signals = await Lighthouse.GetSignals();
            TableView.ReloadData();
        }

        private void GetPushPermission()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                var pushSettings = UIUserNotificationSettings.GetSettingsForTypes(
                                   UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound,
                                   new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(pushSettings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
            else
            {
                UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound;
                UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
            }
        }

        private async void GetLocationPermission()
        {
            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
            var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);
            if (results.ContainsKey(Permission.Location))
            {
                status = results[Permission.Location];
            }
            if (status == PermissionStatus.Granted)
            {
                Lighthouse.SetLocationAllowed(true);
                // TODO: Start foreground location tracking (currently in AppDelegate)
            }
            else if (status == PermissionStatus.Denied)
            {
                Lighthouse.SetLocationAllowed(false);
            }
        }

        // **** TABLE VIEW DATASOURCE ****
        public override nint RowsInSection(UITableView tableView, nint section)
        {
            return Signals.Count;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            // request a recycled cell to save memory
            UITableViewCell cell = tableView.DequeueReusableCell(cellIdentifier);

            // if there are no cells to reuse, create a new one
            if (cell == null)
                cell = new UITableViewCell(UITableViewCellStyle.Subtitle, cellIdentifier);

            Lighthouse.LHPESignal signal = Signals[indexPath.Row];

            cell.TextLabel.Text = signal.Title;
            cell.DetailTextLabel.Text = signal.Alert;

            if (signal.LastViewAt == null)
            {
                cell.TextLabel.Font = UIFont.BoldSystemFontOfSize(16);
                cell.DetailTextLabel.Font = UIFont.BoldSystemFontOfSize(14);
            }
            else
            {
                cell.TextLabel.Font = UIFont.SystemFontOfSize(16);
                cell.DetailTextLabel.Font = UIFont.SystemFontOfSize(14);
            }

            return cell;
        }

        // **** TABLE VIEW DELEGATE ****
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            Lighthouse.LHPESignal signal = Signals[indexPath.Row];
            SignalDetailViewController signalView = new SignalDetailViewController(signal.Id);
            NavigationController.PushViewController(signalView, true);
        }

        public override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
        {
            switch (editingStyle)
            {
                case UITableViewCellEditingStyle.Delete:
                    DeleteSignal(indexPath);
                    break;
            }
        }

        public override bool CanEditRow(UITableView tableView, NSIndexPath indexPath)
        {
            return true;
        }

        private async void DeleteSignal(NSIndexPath indexPath)
        {
            Lighthouse.LHPESignal signal = Signals[indexPath.Row];
            bool success = await Lighthouse.SetSignalDeleted(signal.Id);
            if (success)
            {
                TableView.DeleteRows(new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Fade);
                Reload();
            }
        }

    }
}

