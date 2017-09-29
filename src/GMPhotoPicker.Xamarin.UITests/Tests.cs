using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.iOS;
using Xamarin.UITest.Queries;

namespace GMPhotoPicker.Xamarin.UITests
{
	[TestFixture]
	public class Tests
	{
		iOSApp app;

		[SetUp]
		public void BeforeEachTest ()
		{
			app = ConfigureApp.iOS.StartApp ();
		}

		[Test]
		public void ViewIsDisplayed ()
		{
			AppResult[] results = app.WaitForElement (c => c.Child ("UIView"));
			app.Screenshot ("First screen.");

			Assert.IsTrue (results.Any ());
		}

		[Test]
		public void UseDefaultUIPicker()
		{
			app.Tap (x => x.Text ("UIImagePicker"));
			app.Screenshot ("Tapped on view UIButtonLabel with Text: 'UIImagePicker'");
			app.Tap (x => x.Class ("PUAlbumListCellContentView").Marked ("Moments"));
			app.Screenshot ("Tapped on view PUAlbumListCellContentView");
			app.Tap (x => x.Class ("PUPhotoView").Index (1));
			app.Screenshot ("Tapped on view PUPhotoView");
		}

		[Test]
		public void SelectMultipleImages ()
		{
			app.Tap (x => x.Text ("GMImagePicker"));
			app.Screenshot ("Tapped on view UIButtonLabel with Text: 'GMImagePicker'");
			app.Tap (x => x.Marked ("Camera Roll"));
			app.Screenshot ("Tapped on view UITableViewLabel with Text: 'Camera Roll'");
			app.Tap (x => x.Class ("UIView").Index (33));
			app.Screenshot ("Tapped on view UIView");
			app.Tap (x => x.Class ("UIView").Index (32));
			app.Screenshot ("Tapped on view UIView");
			app.Tap (x => x.Class ("UIView").Index (37));
			app.Screenshot ("Tapped on view UIView");
			app.Tap (x => x.Text ("Finished"));
			app.Screenshot ("Tapped on view UIButtonLabel with Text: 'Finished'");
		}

		[Test]
		public void TryCamera()
		{
			app.Tap(x => x.Text("GMImagePicker"));
			app.Screenshot("Tapped on view UIButtonLabel with Text: 'GMImagePicker'");
			app.Tap(x => x.Marked("Camera"));
			app.Screenshot("Tapped on view UIToolbarButton");
			app.Tap(x => x.Marked("OK"));
			app.Screenshot("Tapped on view _UIAlertControllerActionView");
			app.Tap(x => x.Text("Nope"));
			app.Screenshot("Tapped on view UIButtonLabel with Text: 'Nope'");
		}
	}
}


