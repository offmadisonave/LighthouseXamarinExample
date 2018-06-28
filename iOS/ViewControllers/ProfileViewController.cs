using System;
using UIKit;
using System.Collections.Generic;
using LighthousePortableLibrary;

namespace LHPEXamarinSample.iOS.ViewControllers
{
    public partial class ProfileViewController : UIViewController
    {
        public ProfileViewController() : base("ProfileViewController", null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.EdgesForExtendedLayout = UIRectEdge.None;
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

        partial void SaveButton_TouchUpInside(UIButton sender)
        {
            Lighthouse.LHPEUser user = new Lighthouse.LHPEUser();
            user.Id = idText.Text;
            Dictionary<string, string> Profile = new Dictionary<string, string>();
            Profile.Add("first_name", firstNameText.Text);
            Profile.Add("last_name", lastNameText.Text);
            user.Profile = Profile;

            Lighthouse.SetUserProfile(user);
        }
    }
}

