using System;
using UIKit;
using Foundation;
using Photos;
using GMImagePicker;
using System.Threading.Tasks;
using CoreGraphics;
using MobileCoreServices;

namespace GMPhotoPicker.Xamarin
{
	public partial class ViewController : UIViewController
	{
		public ViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			// Perform any additional setup after loading the view, typically from a nib.
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}

		private PHAsset[] _preselectedAssets;

		async partial void ShowGMImagePicker (NSObject sender)
		{
			var picker = new GMImagePickerController
			{
				Title = "Custom Title",
				CustomDoneButtonTitle = "Finished",
				CustomCancelButtonTitle = "Nope",
				CustomNavigationBarPrompt = "Take a new photo or select an existing one!",
				ColsInPortrait = 3,
				ColsInLandscape = 5,
				MinimumInteritemSpacing = 2.0f,
				DisplaySelectionInfoToolbar = true,
				AllowsMultipleSelection = true,
				ShowCameraButton = true,
				AutoSelectCameraImages = true,
				ModalPresentationStyle = UIModalPresentationStyle.Popover,
				MediaTypes = new[] { PHAssetMediaType.Image },

				// Other customizations to play with:
				//GridSortOrder = SortOrder.Descending,
				//ConfirmSingleSelection = true,
				//ConfirmSingleSelectionPrompt = "Do you want to select the image you have chosen?",
				//PickerBackgroundColor = UIColor.Black,
				//PickerTextColor = UIColor.White,
				//ToolbarBarTintColor = UIColor.Red,
				//ToolbarBackgroundColor = UIColor.Yellow,
				//ToolbarTextColor = UIColor.White,
				//ToolbarTintColor = UIColor.Red,
				//NavigationBarBackgroundColor = UIColor.DarkGray,
				//NavigationBarBarTintColor = UIColor.DarkGray,
				//NavigationBarTextColor = UIColor.White,
				//NavigationBarTintColor = UIColor.Red,
				//CameraButtonTintColor = UIColor.Red,
				//PickerFontName = "Verdana",
				//PickerBoldFontName = "Verdana-Bold",
				//PickerFontNormalSize = 14.0f,
				//PickerFontHeaderSize = 17.0f,
				//PickerStatusBarStyle = UIStatusBarStyle.LightContent,
				//UseCustomFontForNavigationBar = true,
			};

			// You can limit which galleries are available to browse through
			picker.CustomSmartCollections = new [] { 
				PHAssetCollectionSubtype.SmartAlbumUserLibrary, 
				PHAssetCollectionSubtype.AlbumRegular 
			};

			if (_preselectedAssets != null)
			{
				foreach (var asset in _preselectedAssets)
				{
					picker.SelectedAssets.Add(asset);
				}
			}

			// Event handling
			picker.FinishedPickingAssets += Picker_FinishedPickingAssets;
			picker.Canceled += Picker_Canceled;

            // Other events to implement in order to influence selection behavior:
            // Set EventArgs::Cancel flag to true in order to prevent the action from happening
            picker.ShouldDeselectAsset += (s, e) => { /* allow deselection of (mandatory) assets */ };
            picker.ShouldEnableAsset += (s, e) => { /* determine if a specific asset should be enabled */ };
            picker.ShouldHighlightAsset += (s, e) => { /* determine if a specific asset should be highlighted */ };
            picker.ShouldShowAsset += (s, e) => { /* determine if a specific asset should be displayed */ };
            picker.ShouldSelectAsset += (s, e) => { /* determine if a specific asset can be selected */ };

			picker.AssetSelected += (s, e) => { /* keep track of individual asset selection */ };
			picker.AssetDeselected += (s, e) => { /* keep track of individual asset de-selection */ };

			// GMImagePicker can be treated as a PopOver as well:
			var popPC = picker.PopoverPresentationController;
			popPC.PermittedArrowDirections = UIPopoverArrowDirection.Any;
			popPC.SourceView = gmImagePickerButton;
			popPC.SourceRect = gmImagePickerButton.Bounds;
			//popPC.BackgroundColor = UIColor.Black;

			await PresentViewControllerAsync(picker, true);
		}

		void Picker_Canceled (object sender, EventArgs e)
		{
			if (sender is UIImagePickerController) {
				((UIImagePickerController)sender).DismissViewController (true, null);
			}
			Console.WriteLine ("User canceled picking image.");
		}

		async void Picker_FinishedPickingAssets (object sender, MultiAssetEventArgs args)
		{
			PHImageManager imageManager = new PHImageManager();

			Console.WriteLine ("User finished picking assets. {0} items selected.", args.Assets.Length);

			_preselectedAssets = args.Assets;

			// For demo purposes: just show all chosen pictures in order every second
			foreach (var asset in args.Assets) {
				imagePreview.Image = null;

				imageManager.RequestImageForAsset (asset, 
					new CGSize(asset.PixelWidth, asset.PixelHeight), 
					PHImageContentMode.Default, 
					null, 
					(image, info) => {
						imagePreview.Image = image;
				});
				await Task.Delay (1000);
			}
		}

		partial void ShowUIImagePicker (NSObject sender)
		{
			var picker = new UIImagePickerController {
				SourceType = UIImagePickerControllerSourceType.PhotoLibrary,
				ModalPresentationStyle = UIModalPresentationStyle.Popover,
				MediaTypes = new string[] { UTType.Image },
			};

			picker.FinishedPickingMedia += Picker_FinishedPickingMedia;
			picker.FinishedPickingImage += Picker_FinishedPickingImage;
			picker.Canceled += Picker_Canceled;

			var popPC = picker.PopoverPresentationController;
			popPC.PermittedArrowDirections = UIPopoverArrowDirection.Any;
			popPC.SourceView = uiImagePickerButton;
			popPC.SourceRect = uiImagePickerButton.Bounds;
			
			ShowViewController (picker, this);
		}

		void Picker_FinishedPickingImage (object sender, UIImagePickerImagePickedEventArgs e)
		{
			((UIImagePickerController)sender).DismissViewController (true, null);

			Console.WriteLine ("UIImagePicker finished picking image");
			imagePreview.Image = e.Image;
		}

		void Picker_FinishedPickingMedia (object sender, UIImagePickerMediaPickedEventArgs e)
		{
			((UIImagePickerController)sender).DismissViewController (true, null);

			Console.WriteLine ("UIImagePicker finished picking media");
			imagePreview.Image = e.OriginalImage;
		}
	}
}

