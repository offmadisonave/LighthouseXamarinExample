// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace LHPEXamarinSample.iOS.ViewControllers
{
    [Register ("ProfileViewController")]
    partial class ProfileViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField firstNameText { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField idText { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField lastNameText { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton saveButton { get; set; }

        [Action ("SaveButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void SaveButton_TouchUpInside (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (firstNameText != null) {
                firstNameText.Dispose ();
                firstNameText = null;
            }

            if (idText != null) {
                idText.Dispose ();
                idText = null;
            }

            if (lastNameText != null) {
                lastNameText.Dispose ();
                lastNameText = null;
            }

            if (saveButton != null) {
                saveButton.Dispose ();
                saveButton = null;
            }
        }
    }
}