//
//  GMImagePickerController.cs
//  GMPhotoPicker.Xamarin
//
//  Created by Roy Cornelissen on 23/03/16.
//  Based on original GMImagePicker implementation by Guillermo Muntaner Perelló.
//  https://github.com/guillermomuntaner/GMImagePicker
//

using System;
using UIKit;
using Photos;
using CoreGraphics;
using System.Collections.Generic;
using MobileCoreServices;
using System.Linq;
using Foundation;
using AssetsLibrary;
using System.Threading.Tasks;
using AVFoundation;

namespace GMImagePicker
{
    /// <summary>
    /// Enum to specify the sort order of the images in the gallery.
    /// Images will be sorted by creation date. The default behavior is Ascending.
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Sort images in ascending order, i.e. the oldest images first.
        /// </summary>
        Ascending,
        /// <summary>
        /// Sort images in descending order, i.e. the newest images first.
        /// </summary>
        Descending
    }

	public class GMImagePickerController: UIViewController
	{
		//This is the default image picker size!
		//public const CGSize PopoverContentSize = new CoreGraphics.CGSize(320, 480);
		//static CGSize const kPopoverContentSize = {320, 480};
		//However, the iPad is 1024x768 so it can allow popups up to 768!
		public static readonly CGSize PopoverContentSize = new CGSize(480, 720);

		internal UINavigationController _navigationController;
		private GMAlbumsViewController _albumsViewController;

		/// <summary>
		/// Contains the selected 'PHAsset' objects. The order of the objects is the selection order.
		/// You can add assets before presenting the picker to show the user some preselected assets.
		/// </summary>
		/// <value>The selected assets.</value>
		private List<PHAsset> _selectedAssets;

		#region Grid customizations 

		public IList<PHAsset> SelectedAssets
		{
			get { return _selectedAssets; }
		}

		/// <summary>
		/// Number of columns in portrait mode (3 by default)
		/// </summary>
		public int ColsInPortrait { get; set; }

		/// <summary>
		/// Number of columns in landscape (5 by default).
		/// </summary>
		public int ColsInLandscape { get; set; }

		/// <summary>
		/// Horizontal and vertical minimum space between grid cells (2.0 by default)
		/// </summary>
		public nfloat MinimumInteritemSpacing { get; set; }

		#endregion

		#region UI Customizations

		/// <summary>
		/// Determines with smart collections are displayed (array of PHAssetCollectionSubtype)
		/// The default smart collections are:
		/// - Favorites
		/// - RecentlyAdded
		/// - Videos
		/// - SlomoVideos
		/// - Timelapses
		/// - Bursts
		/// - Panoramas
		/// </summary>
		/// <value>The custom smart collections.</value>
		public PHAssetCollectionSubtype[] CustomSmartCollections { get; set; }

		/// <summary>
		/// Determines which media types are allowed (array of PHAssetMediaType)
		/// </summary>
		/// <remarks>
		/// This defaults to all media types (view, audio and images)
		/// This can override CustomSmartCollections behavior (ie, remove video-only smart collections)</remarks>
		/// <value>The media types.</value>
		public PHAssetMediaType[] MediaTypes { get; set; }

		/// <summary>
		/// If set, it displays this string instead of the localised default of "Done" on the Done button. 
		/// </summary>
		/// <remarks>
		/// Note also that this is not used when a single selection is active since the selection of the chosen photo 
		/// closes the VS thus rendering the button pointless.
		/// </remarks>
		/// <value>The custom done button title.</value>
		public string CustomDoneButtonTitle { get; set; }

		/// <summary>
		/// If set, it displays this string instead of the localised default of "Cancel" on the cancel button.
		/// </summary>
		/// <value>The custom cancel button title.</value>
		public string CustomCancelButtonTitle { get; set; }

		/// <summary>
		/// If set, it displays a prompt in the navigation bar.
		/// </summary>
		/// <value>The custom navigation bar prompt.</value>
		public string CustomNavigationBarPrompt { get; set; }

		/// <summary>
		/// If set, it displays a UIAlert with this title when the user has denied access to photos.
		/// </summary>
		public string CustomPhotosAccessDeniedErrorTitle { get; set; }

		/// <summary>
		/// If set, it displays a this error message when the user has denied access to photos.
		/// </summary>
		public string CustomPhotosAccessDeniedErrorMessage { get; set; }

		/// <summary>
		/// If set, it displays a UIAlert with this title when the user has denied access to the camera.
		/// </summary>
		public string CustomCameraAccessDeniedErrorTitle { get; set; }

		/// <summary>
		/// If set, it displays a UIAlert with this title when the user has denied access to the camera.
		/// </summary>
		public string CustomCameraAccessDeniedErrorMessage { get; set; }

		/// <summary>
		/// Determines whether or not a toolbar with info about user selection is shown.
		/// </summary>
		/// <remarks>
		/// The InfoToolbar is visible by default.
		/// </remarks>
		/// <value><c>true</c> if display selection info toolbar; otherwise, <c>false</c>.</value>
		public bool DisplaySelectionInfoToolbar { get; set; }

		/// <summary>
		/// Determines whether or not the number of assets is shown in the Album list.
		/// </summary>
		/// <remarks>
		/// The number of assets is visible by default.
		/// </remarks>
		/// <value><c>true</c> if display albums number of assets; otherwise, <c>false</c>.</value>
		public bool DisplayAlbumsNumberOfAssets { get; set; }

		/// <summary>
		/// Automatically disables the "Done" button if nothing is selected.
		/// </summary>
		/// <remarks>
		/// Defaults to <c>true</c>
		/// </remarks>
		/// <value><c>true</c> if auto disable done button; otherwise, <c>false</c>.</value>
		public bool AutoDisableDoneButton { get; set; }

		/// <summary>
		/// Use the picker either for miltiple image selections, or just a single selection. 
		/// In the case of a single selection the VC is closed on selection so the Done button 
		/// is neither displayed or used. 
		/// </summary>
		/// <remarks>Default is <c>true</c></remarks>
		/// <value><c>true</c> if allows multiple selection; otherwise, <c>false</c>.</value>
		public bool AllowsMultipleSelection { get; set; }

		/// <summary>
		/// In the case where allowsMultipleSelection = <c>false</c>, set this to <c>true</c> to have the user confirm their selection.
		/// </summary>
		/// <remarks>Default is <c>false</c></remarks>
		/// <value><c>true</c> if confirm single selection; otherwise, <c>false</c>.</value>
		public bool ConfirmSingleSelection { get; set; }

		/// <summary>
		/// If set, it displays this string (if ConfirmSingleSelection = <c>true</c>) instead of the localised default.
		/// </summary>
		/// <value>The confirm single selection prompt.</value>
		public string ConfirmSingleSelectionPrompt { get; set; }

		/// <summary>
		/// True to always show the toolbar, with a camera button allowing new photos to be taken. False to auto show/hide the
		/// toolbar, and have no camera button. 
		/// </summary>
		/// <remarks>Default is <c>false</c>. If <c>true</c>, this renders DisplaySelectionInfoToolbar a no-op.</remarks>
		/// <value><c>true</c> if show camera button; otherwise, <c>false</c>.</value>
		public bool ShowCameraButton { get; set; }

		/// <summary>
		/// True to auto select the image(s) taken with the camera if ShowCameraButton = <c>true</c>. 
		/// In the case of AllowsMultipleSelection = <c>true</c>, this will trigger the selection handler too.
		/// </summary>
		public bool AutoSelectCameraImages { get; set; }

		/// <summary>
		/// If set, allows the user to edit camera images before selecting them.
		/// </summary>
		public bool AllowsEditingCameraImages { get; set; }

		/// <summary>
		/// The color for all backgrounds; behind the table and cells. Defaults to UIColor.White
		/// </summary>
		public UIColor PickerBackgroundColor { get; set; }

		/// <summary>
		/// The color for text in the views. This needs to work with PickerBackgroundColor! Default of UIColor.DarkTextColor
		/// </summary>
		public UIColor PickerTextColor { get; set; }

		/// <summary>
		/// The color for the background tint of the toolbar. Defaults to null.
		/// </summary>
		public UIColor ToolbarBarTintColor { get; set; }

		/// <summary>
		/// The color of the text on the toolbar
		/// </summary>
		public UIColor ToolbarTextColor { get; set; }

		/// <summary>
		/// The tint colour used for any buttons on the toolbar
		/// </summary>
		public UIColor ToolbarTintColor { get; set; }

		/// <summary>
		/// The background color of the toolbar. Defaults to null.
		/// </summary>
		public UIColor ToolbarBackgroundColor { get; set; }

		/// <summary>
		/// The background of the navigation bar. Defaults to null.
		/// </summary>
		public UIColor NavigationBarBackgroundColor { get; set; }

		/// <summary>
		/// The color for the text in the navigation bar. Defaults to UIColor.DarkTextColor
		/// </summary>
		public UIColor NavigationBarTextColor { get; set; }

		/// <summary>
		/// The tint color used for any buttons on the navigation Bar
		/// </summary>
		public UIColor NavigationBarTintColor { get; set; }

		/// <summary>
		/// The color for the background tint of the navigation bar. Defaults to null.
		/// </summary>
		public UIColor NavigationBarBarTintColor { get; set; }

		/// <summary>
		/// The font to use everywhere. Defaults to system font. It is advised if you set this to check, and possibly 
		/// set, appropriately the custom font sizes. For font information, check http://www.iosfonts.com/
		/// </summary>
		/// <value>The name of the picker font.</value>
		public string PickerFontName { get; set; }

		/// <summary>
		/// The font to use everywhere. Defaults to bold system font. It is advised if you set this to check, and 
		/// possibly set, appropriately the custom font sizes.
		/// </summary>
		public string PickerBoldFontName { get; set; }

		/// <summary>
		/// Font size of regular text in the picker.
		/// </summary>
		public float PickerFontNormalSize { get; set ; }

		/// <summary>
		/// Font size of the header text in the picker.
		/// </summary>
		public float PickerFontHeaderSize { get; set; }

		/// <summary>
		/// On iPhones this will matter if custom navigation bar colours are being used. 
		/// Defaults to UIStatusBarStyle.Default
		/// </summary>
		/// <value>The picker status bar style.</value>
		public UIStatusBarStyle PickerStatusBarStyle { get; set; }

		/// <summary>
		/// <c>true</c> to use the custom font (or its default) in the navigation bar, <c>false</c> to leave to iOS Defaults.
		/// </summary>
		public bool UseCustomFontForNavigationBar { get; set; }

        /// <summary>
        /// Gets or sets the sort order for the image grid.
        /// Default is Ascending, i.e. the oldest images first.
        /// </summary>
		public SortOrder GridSortOrder { get; set; }

		#endregion

        /// <summary>
        /// Adds one asset to the selection and updates the UI.
        /// </summary>
		public void SelectAsset (PHAsset asset)
		{
			if (!_selectedAssets.Exists(a => a.LocalIdentifier == asset.LocalIdentifier)) {
				_selectedAssets.Add (asset);
				UpdateDoneButton ();

				if (!AllowsMultipleSelection) {
					if (ConfirmSingleSelection) {
						var message = ConfirmSingleSelectionPrompt ?? "picker.confirm.message".Translate (defaultValue: "Do you want to select the image you tapped on?");

						var alert = UIAlertController.Create ("picker.confirm.title".Translate (defaultValue: "Are you sure?"), message, UIAlertControllerStyle.Alert);
						alert.AddAction (UIAlertAction.Create ("picker.action.no".Translate (defaultValue: "No"), UIAlertActionStyle.Cancel, null));
						alert.AddAction (UIAlertAction.Create ("picker.action.yes".Translate (defaultValue: "Yes"), UIAlertActionStyle.Default, action => {
							FinishPickingAssets (this, EventArgs.Empty);
						}));

						PresentViewController (alert, true, null);
					} else {
						FinishPickingAssets (this, EventArgs.Empty);
					}
				}
				else if (DisplaySelectionInfoToolbar || ShowCameraButton) {
					UpdateToolbar ();
				}
			}
		}
			
		public void DeselectAsset (PHAsset asset)
		{
			_selectedAssets.Remove (asset);
			if (!_selectedAssets.Any ()) {
				UpdateDoneButton ();
			}

			if (DisplaySelectionInfoToolbar || ShowCameraButton) {
				UpdateToolbar ();
			}
		}

		public void Dismiss (object sender, EventArgs args)
		{
			// Explicitly unregister observers because we cannot predict when the GC cleans up
			Unregister ();

			Canceled?.Invoke(this, EventArgs.Empty);

			PresentingViewController.DismissViewController (true, null);
		}

		public void FinishPickingAssets (object sender, EventArgs args)
		{
			// Explicitly unregister observers because we cannot predict when the GC cleans up
			Unregister ();

			FinishedPickingAssets?.Invoke(this, new MultiAssetEventArgs(_selectedAssets.ToArray()));

			PresentingViewController.DismissViewController (true, null);
		}

		private void UpdateDoneButton()
		{
			if (!AllowsMultipleSelection) {
				return;
			}

			var nav = ChildViewControllers [0] as UINavigationController;
			if (nav != null) {
				foreach (var vc in nav.ViewControllers) {
					vc.NavigationItem.RightBarButtonItem.Enabled = !AutoDisableDoneButton || _selectedAssets.Any ();
				}
			}
		}

		private void UpdateToolbar()
		{
			if (!AllowsMultipleSelection && !ShowCameraButton && AdditionalToolbarItems.Length == 0) {
				return;
			}

			var nav = ChildViewControllers [0] as UINavigationController;
			if (nav != null) {
				foreach (var vc in nav.ViewControllers) {
					var index = 1;
					if (ShowCameraButton) {
						index++;
					}
					if (vc.ToolbarItems != null) {
						vc.ToolbarItems [index].SetTitleTextAttributes (ToolbarTitleTextAttributes, UIControlState.Normal);
						vc.ToolbarItems [index].SetTitleTextAttributes (ToolbarTitleTextAttributes, UIControlState.Disabled);
						vc.ToolbarItems [index].Title = ToolbarTitle;
					}
					var toolbarHidden = !ShowCameraButton && !_selectedAssets.Any () && AdditionalToolbarItems.Length == 0;
					vc.NavigationController.SetToolbarHidden (toolbarHidden, true);
				}
			}
		}

		private async void CameraButtonPressed(object sender, EventArgs e)
		{
			if (! await EnsureHasCameraAccess ()) 
			{
				return;
			}

			if (!UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera)) {
				var alert = UIAlertController.Create ("No Camera!",
					            "Sorry, this device does not have a camera.",
					            UIAlertControllerStyle.Alert);
				alert.AddAction (UIAlertAction.Create ("OK", UIAlertActionStyle.Default, null));

				PresentViewController(alert, true, null);
				return;
			}

			// This allows the selection of the image taken to be better seen if the user is not already in that VC
			if (AutoSelectCameraImages && _navigationController.TopViewController is GMAlbumsViewController) {
				((GMAlbumsViewController)_navigationController.TopViewController).SelectAllAlbumsCell ();
			}

			var picker = new UIImagePickerController () {
				SourceType = UIImagePickerControllerSourceType.Camera,
				MediaTypes = new string [] { UTType.Image },
                AllowsEditing = AllowsEditingCameraImages,
				Delegate = new GMImagePickerDelegate (this),
				ModalPresentationStyle = UIModalPresentationStyle.Popover
			};

			var popover = picker.PopoverPresentationController;
			popover.PermittedArrowDirections = UIPopoverArrowDirection.Any;
			popover.BarButtonItem = (UIBarButtonItem) sender;

			PresentViewController (picker, false, null);
		}

		private class GMImagePickerDelegate : UIImagePickerControllerDelegate
		{
			private readonly GMImagePickerController _parent;

			public GMImagePickerDelegate(GMImagePickerController parent)
			{
				_parent = parent;
			}

			public override async void FinishedPickingMedia (UIImagePickerController picker, NSDictionary info)
			{
				await picker.PresentingViewController.DismissViewControllerAsync (true);

				var mediaType = (NSString) info[UIImagePickerController.MediaType];
				if (mediaType == UTType.Image) {
					var image = (UIImage) (info[UIImagePickerController.EditedImage] ?? info[UIImagePickerController.OriginalImage]);
					image.SaveToPhotosAlbum((img, error) => {
						if (error != null) {
							var alert = UIAlertController.Create("Image Not Saved",
								"Sorry, unable to save the new image!",
								UIAlertControllerStyle.Alert);

							alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
							_parent.PresentViewController(alert, true, null);
						}

						// Note: The image view will auto refresh as the photo's are being observed in the other VCs
					});
				}

			}

			public override void Canceled (UIImagePickerController picker)
			{
				picker.PresentingViewController.DismissViewController (true, null);
			}
		}

		#region Events

		public delegate void MultiAssetEventHandler(object sender, MultiAssetEventArgs args);
		public delegate void SingleAssetEventHandler(object sender, SingleAssetEventArgs args);

		/// <summary>
		/// Tells the delegate that the user finish picking photos or videos.
		/// </summary>
		public event MultiAssetEventHandler FinishedPickingAssets;

		/// <summary>
		/// Tells the delegate that the user cancelled the pick operation.
		/// </summary>
		public event EventHandler Canceled;

		public delegate void CancellableAssetEventHandler(object sender, CancellableAssetEventArgs args);

		/// <summary>
		/// Ask the delegate if the specified asset should be shown.
		/// </summary>
		public event CancellableAssetEventHandler ShouldShowAsset;

		/// <summary>
		/// Ask the delegate if the specified asset should be enabled for selection.
		/// </summary>
		public event CancellableAssetEventHandler ShouldEnableAsset;

		/// <summary>
		/// Asks the delegate if the specified asset should be selected.
		/// </summary>
		public event CancellableAssetEventHandler ShouldSelectAsset;

		/// <summary>
		/// Tells the delegate that the asset was selected.
		/// </summary>
		public event SingleAssetEventHandler AssetSelected;

		/// <summary>
		/// Asks the delegate if the specified asset should be deselected.
		/// </summary>
		public event CancellableAssetEventHandler ShouldDeselectAsset;

		/// <summary>
		/// Tells the delegate that the item at the specified path was deselected.
		/// </summary>
		public event SingleAssetEventHandler AssetDeselected;

		/// <summary>
		/// Asks the delegate if the specified asset should be highlighted.
		/// </summary>
		public event CancellableAssetEventHandler ShouldHighlightAsset;

		/// <summary>
		/// Tells the delegate that asset was highlighted.
		/// </summary>
		public event SingleAssetEventHandler AssetHighlighted;

		/// <summary>
		/// Tells the delegate that the highlight was removed from the asset.
		/// </summary>
		public event SingleAssetEventHandler AssetUnhighlighted;

		/// <summary>
		/// Sets the tint color for the camera button image. defaults to UIColor.DarkTextColor.
		/// </summary>
		public UIColor CameraButtonTintColor { get; set; }

		/// <summary>
		/// Sets additional toolbar items that are shown to the right of the toolbar's title.
		/// </summary>
		public UIBarButtonItem[] AdditionalToolbarItems { get; set; }

		#endregion

		public GMImagePickerController(IntPtr handle): base (handle)
		{
		}

		public GMImagePickerController()
		{
			_selectedAssets = new List<PHAsset> ();	

			// Default values:
			DisplaySelectionInfoToolbar = true;
			DisplayAlbumsNumberOfAssets = true;
			AutoDisableDoneButton = true;
			AllowsMultipleSelection = true;
			ConfirmSingleSelection = false;
			ShowCameraButton = false;
			AdditionalToolbarItems = new UIBarButtonItem[0];

			// Grid configuration:
			ColsInPortrait = 3;
			ColsInLandscape = 5;
			MinimumInteritemSpacing = 2.0f;

			// Sample of how to select the collections you want to display:
			CustomSmartCollections = new [] {
				PHAssetCollectionSubtype.SmartAlbumRecentlyAdded,
				PHAssetCollectionSubtype.SmartAlbumVideos,
				PHAssetCollectionSubtype.SmartAlbumSlomoVideos,
				PHAssetCollectionSubtype.SmartAlbumTimelapses,
				PHAssetCollectionSubtype.SmartAlbumBursts,
				PHAssetCollectionSubtype.SmartAlbumPanoramas
			};
			// If you don't want to show smart collections, just set CustomSmartCollections to null

			// Which media types will display
			MediaTypes = new[] {
				PHAssetMediaType.Audio,
				PHAssetMediaType.Video,
				PHAssetMediaType.Image
			};

			PreferredContentSize = PopoverContentSize;

			// UI Customization
			PickerBackgroundColor = UIColor.White;
			PickerTextColor = UIColor.DarkTextColor;
			PickerFontName = UIFont.SystemFontOfSize (14.0f).Name;
			PickerBoldFontName = UIFont.BoldSystemFontOfSize(17.0f).Name;
			PickerFontNormalSize = 14.0f;
			PickerFontHeaderSize = 17.0f;
			CameraButtonTintColor = UIColor.DarkTextColor;

			NavigationBarTextColor = UIColor.DarkTextColor;
			NavigationBarTintColor = UIColor.DarkTextColor;

			ToolbarTextColor = UIColor.DarkTextColor;
			ToolbarTintColor = UIColor.DarkTextColor;

			PickerStatusBarStyle = UIStatusBarStyle.Default;

			SetupNavigationController ();
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);

			// Ensure nav and toolbar customisations are set. Defaults are in place, but the user may have changed them
			View.BackgroundColor = PickerBackgroundColor;

			_navigationController.Toolbar.Translucent = true;

			_navigationController.Toolbar.BarTintColor = ToolbarBarTintColor;
			_navigationController.Toolbar.TintColor = ToolbarTintColor;
			_navigationController.Toolbar.BackgroundColor = ToolbarBackgroundColor;

			_navigationController.NavigationBar.TintColor = NavigationBarTintColor;
			_navigationController.NavigationBar.BarTintColor = NavigationBarBarTintColor;
			_navigationController.NavigationBar.BackgroundColor = NavigationBarBackgroundColor;

			UIStringAttributes attributes;
			if (UseCustomFontForNavigationBar) {
				attributes = new UIStringAttributes { ForegroundColor = NavigationBarTextColor, 
					Font = UIFont.FromName (PickerBoldFontName, PickerFontHeaderSize)
				};
			} else {
				attributes = new UIStringAttributes { ForegroundColor = NavigationBarTextColor };
			}
			_navigationController.NavigationBar.TitleTextAttributes = attributes;

			UpdateToolbar ();
		}

		/// <summary>
		/// Checks if access to the Camera is granted, and if not (Denied), shows an error message.
		/// </summary>
		public async Task<bool> EnsureHasCameraAccess()
		{
			var status = AVCaptureDevice.GetAuthorizationStatus (AVMediaType.Video);

			if (status == AVAuthorizationStatus.Denied) {
				var alert = UIAlertController.Create (CustomCameraAccessDeniedErrorTitle ?? "picker.camera-access-denied.title".Translate (),
					            CustomCameraAccessDeniedErrorMessage ?? "picker.camera-access-denied.message".Translate (),
					            UIAlertControllerStyle.Alert);

				alert.AddAction (UIAlertAction.Create ("picker.navigation.cancel-button".Translate("Cancel"), UIAlertActionStyle.Cancel, null));
				alert.AddAction (UIAlertAction.Create ("picker.navigation.settings-button".Translate("Settings"), UIAlertActionStyle.Default, (action) => UIApplication.SharedApplication.OpenUrl(NSUrl.FromString(UIApplication.OpenSettingsUrlString))));

				await PresentViewControllerAsync (alert, true);
				return false;
			} else if (status == AVAuthorizationStatus.NotDetermined) {
				return await AVCaptureDevice.RequestAccessForMediaTypeAsync (AVMediaType.Video) && 
					await EnsureHasPhotosPermission ();
			}

			return await EnsureHasPhotosPermission ();
		}

		/// <summary>
		/// Checks if access to Photos is granted, and if not (Denied), shows an error message.
		/// </summary>
		public async Task<bool> EnsureHasPhotosPermission()
		{
			var status = ALAssetsLibrary.AuthorizationStatus;

			if (status == ALAuthorizationStatus.Denied) {
				var alert = UIAlertController.Create (CustomPhotosAccessDeniedErrorTitle ?? "picker.photo-access-denied.title".Translate (),
					            CustomPhotosAccessDeniedErrorMessage ?? "picker.photo-access-denied.message".Translate (),
					            UIAlertControllerStyle.Alert);

				alert.AddAction (UIAlertAction.Create ("picker.navigation.cancel-button".Translate("Cancel"), UIAlertActionStyle.Cancel, null));
				alert.AddAction (UIAlertAction.Create ("picker.navigation.settings-button".Translate("Settings"), UIAlertActionStyle.Default, (action) => UIApplication.SharedApplication.OpenUrl(NSUrl.FromString(UIApplication.OpenSettingsUrlString))));

				await PresentViewControllerAsync (alert, true);
				return false;
			}

			return true;
		}

		private void SetupNavigationController()
		{
			if (_albumsViewController == null) {
				_albumsViewController = new GMAlbumsViewController ();
			}
			_navigationController = new UINavigationController (_albumsViewController);
			_navigationController.Delegate = new GMNavigationControllerDelegate ();

			_navigationController.NavigationBar.Translucent = true;

			_navigationController.View.Frame = View.Frame;
			_navigationController.WillMoveToParentViewController (this);
			View.AddSubview (_navigationController.View);
			AddChildViewController (_navigationController);
			_navigationController.DidMoveToParentViewController (this);
		}

		internal void NotifyAssetSelected(PHAsset asset)
		{
			var e = AssetSelected;
			if (e != null) {
				e (this, new SingleAssetEventArgs (asset));
			}
		}

        internal void NotifyAssetDeselected(PHAsset asset)
		{
			var e = AssetDeselected;
			if (e != null) {
				e (this, new SingleAssetEventArgs (asset));
			}
		}

        internal void NotifyAssetHighlighted(PHAsset asset)
		{
			var e = AssetHighlighted;
			if (e != null) {
				e (this, new SingleAssetEventArgs (asset));
			}
		}

        internal void NotifyAssetUnhighlighted(PHAsset asset)
		{
			var e = AssetUnhighlighted;
			if (e != null) {
				e (this, new SingleAssetEventArgs (asset));
			}
		}

        internal bool VerifyShouldEnableAsset(PHAsset asset) 
		{
			return VerifyCancellableAssetEventHandler (asset, ShouldEnableAsset);
		}

        internal bool VerifyShouldShowAsset(PHAsset asset)
		{
			return VerifyCancellableAssetEventHandler (asset, ShouldShowAsset);
		}

        internal bool VerifyShouldHighlightAsset(PHAsset asset)
		{
			return VerifyCancellableAssetEventHandler (asset, ShouldHighlightAsset);
		}

        internal bool VerifyShouldDeselectAsset(PHAsset asset)
		{
			return VerifyCancellableAssetEventHandler (asset, ShouldDeselectAsset);
		}

        internal bool VerifyShouldSelectAsset(PHAsset asset) 
		{
			return VerifyCancellableAssetEventHandler (asset, ShouldSelectAsset);
		}

		private bool VerifyCancellableAssetEventHandler(PHAsset asset, CancellableAssetEventHandler del, bool defaultValue = true) 
		{
			var result = defaultValue;

			var e = del;
			if (e != null) {
				var args = new CancellableAssetEventArgs (asset);
				e (this, args);

				result = !args.Cancel;
			}
			return result;
		}

		private class GMNavigationControllerDelegate : UINavigationControllerDelegate
		{
			// Placeholder class to act as NavigationControllerDelegate
		}
			
		public override UIStatusBarStyle PreferredStatusBarStyle ()
		{
			return PickerStatusBarStyle;
		}

		private string ToolbarTitle
		{
			get {
				if (!_selectedAssets.Any ()) {
					return null;
				}
					
				var nImages = _selectedAssets.Count (a => a.MediaType == PHAssetMediaType.Image);
				var nVideos = _selectedAssets.Count (a => a.MediaType == PHAssetMediaType.Video);

				if (nImages > 0 && nVideos > 0) {
					return string.Format ("picker.selection.multiple-items".Translate (defaultValue: "{0} items selected"), nImages + nVideos);
				} else if (nImages > 1) {
					return string.Format ("picker.selection.multiple-photos".Translate (defaultValue: "{0} photos selected"), nImages);
				} else if (nImages == 1) {
					return "picker.selection.single-photo".Translate (defaultValue: "1 photo selected");
				} else if (nVideos > 1) {
					return string.Format ("picker.selection.multiple-videos".Translate (defaultValue: "{0} videos selected"), nVideos);
				} else if (nVideos == 1) {
					return "picker.selection.single-video".Translate (defaultValue: "1 video selected");
				} else {
					return null;
				}
			}
		}

		private UITextAttributes ToolbarTitleTextAttributes 
		{
			get {
				return new UITextAttributes {
					TextColor = ToolbarTextColor,
					Font = UIFont.FromName (PickerFontName, PickerFontHeaderSize)
				};
			}
		}

		private UIBarButtonItem CreateTitleButtonItem ()
		{
			var title = new UIBarButtonItem (ToolbarTitle,
				            UIBarButtonItemStyle.Plain,
				            null, null);

			var attributes = ToolbarTitleTextAttributes;
			title.SetTitleTextAttributes (attributes, UIControlState.Normal);
			title.SetTitleTextAttributes (attributes, UIControlState.Disabled);
			title.Enabled = false;

			return title;
		}

		private UIBarButtonItem CreateSpaceButtonItem ()
		{
			return new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace);
		}

		private UIBarButtonItem CreateCameraButtonItem ()
		{
			return new UIBarButtonItem(UIBarButtonSystemItem.Camera, CameraButtonPressed)
			{
				TintColor = CameraButtonTintColor
			};
		}

        internal UIBarButtonItem[] GetToolbarItems ()
		{
			var title = CreateTitleButtonItem ();
			var space = CreateSpaceButtonItem ();

			var items = new List<UIBarButtonItem> ();
			if (ShowCameraButton) {
				items.Add (CreateCameraButtonItem());
			}
			items.Add (space);
			items.Add (title);
			items.Add (space);

			items.AddRange(AdditionalToolbarItems);

			return items.ToArray ();
		}

		private void Unregister ()
		{
			if (_albumsViewController != null) {
				_albumsViewController.Dispose ();
				_albumsViewController = null;
			}
		}

		protected override void Dispose (bool disposing)
		{
			Unregister ();
			base.Dispose (disposing);
		}
	}		
}

