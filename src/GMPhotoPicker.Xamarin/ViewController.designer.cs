// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace GMPhotoPicker.Xamarin
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		UIKit.UIButton gmImagePickerButton { get; set; }

		[Outlet]
		UIKit.UIImageView imagePreview { get; set; }

		[Outlet]
		UIKit.UIButton uiImagePickerButton { get; set; }

		[Action ("ShowGMImagePicker:")]
		partial void ShowGMImagePicker (Foundation.NSObject sender);

		[Action ("ShowUIImagePicker:")]
		partial void ShowUIImagePicker (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (gmImagePickerButton != null) {
				gmImagePickerButton.Dispose ();
				gmImagePickerButton = null;
			}

			if (uiImagePickerButton != null) {
				uiImagePickerButton.Dispose ();
				uiImagePickerButton = null;
			}

			if (imagePreview != null) {
				imagePreview.Dispose ();
				imagePreview = null;
			}
		}
	}
}
