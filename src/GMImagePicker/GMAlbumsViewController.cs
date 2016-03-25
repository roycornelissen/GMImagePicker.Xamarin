//
//  GMAlbumsViewController.cs
//  GMPhotoPicker.Xamarin
//
//  Created by Roy Cornelissen on 23/03/16.
//  Based on original GMImagePicker implementation by Guillermo Muntaner Perelló.
//  https://github.com/guillermomuntaner/GMImagePicker
//

using System;
using UIKit;
using CoreGraphics;
using Photos;
using Foundation;
using System.Linq;
using System.Collections.Generic;
using CoreFoundation;

namespace GMImagePicker
{
	internal class GMAlbumsViewController: UITableViewController, IPHPhotoLibraryChangeObserver
	{
		// Measuring iOS8 Photos APP at @2x (iPhone5s):
		//   The rows are 180px/90pts
		//   Left image border is 21px/10.5pts
		//   Separation between image and text is 42px/21pts (double the previouse one)
		//   The bigger image measures 139px/69.5pts including 1px/0.5pts white border.
		//   The second image measures 131px/65.6pts including 1px/0.5pts white border. Only 3px/1.5pts visible
		//   The third image measures 123px/61.5pts  including 1px/0.5pts white border. Only 3px/1.5pts visible

		public static int AlbumRowHeight = 90;
		public static int AlbumLeftToImageSpace = 10;
		public static int AlbumImageToTextSpace = 21;
		public static readonly float AlbumGradientHeight = 20.0f;
		public static readonly CGSize AlbumThumbnailSize1 = new CGSize(70.0f, 70.0f);
		public static readonly CGSize AlbumThumbnailSize2 = new CGSize(66.0f, 66.0f);
		public static readonly CGSize AlbumThumbnailSize3 = new CGSize(62.0f, 62.0f);

		private List<PHFetchResult> _collectionsFetchResults;
		private List<string> _collectionsLocalizedTitles;
		private PHFetchResult[][] _collectionsFetchResultsAssets;
		private string[][] _collectionsFetchResultsTitles;
		private GMImagePickerController _picker;
		private PHCachingImageManager _imageManager;

		private const string AllPhotosReuseIdentifier = "AllPhotosCell";
		private const string CollectionCellReuseIdentifier = "CollectionCell";

		public GMAlbumsViewController (): base(UITableViewStyle.Plain)
		{
			PreferredContentSize = GMImagePickerController.PopoverContentSize;
		}
			
		public void PhotoLibraryDidChange (PHChange changeInstance)
		{
			// Call might come on any background queue. Re-dispatch to the main queue to handle it.
			DispatchQueue.MainQueue.DispatchAsync (() => {
				List<PHFetchResult> updatedCollectionsFetchResults = null;

				foreach (var collectionsFetchResult in _collectionsFetchResults){
					var changeDetails = changeInstance.GetFetchResultChangeDetails(collectionsFetchResult);
					if (changeDetails != null) {
						if (updatedCollectionsFetchResults == null) {
							updatedCollectionsFetchResults = _collectionsFetchResults.ToList();
						}
						updatedCollectionsFetchResults[updatedCollectionsFetchResults.IndexOf(collectionsFetchResult)] = changeDetails.FetchResultAfterChanges;
					}

					// This only affects to changes in albums level (add/remove/edit album)
					if (updatedCollectionsFetchResults != null) {
						_collectionsFetchResults = updatedCollectionsFetchResults;
					}

					// However, we want to update if photos are added, so the counts of items & thumbnails are updated too.
					// Maybe some checks could be done here , but for now is OKey.
					UpdateFetchResults();
					TableView.ReloadData();
				}
			});			
		}
			
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			_picker = (GMImagePickerController) NavigationController.ParentViewController;
			View.BackgroundColor = _picker.PickerBackgroundColor;

			// Navigation bar customization
			if (!string.IsNullOrWhiteSpace (_picker.CustomNavigationBarPrompt)) {
				NavigationItem.Prompt = _picker.CustomNavigationBarPrompt;
			}

			_imageManager = new PHCachingImageManager ();

			// Table view aspect
			TableView.RowHeight = AlbumRowHeight;
			TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			TableView.Source = new GMAlbumsViewTableViewSource (this);

			// Buttons
			var barButtonItemAttributes = new UITextAttributes {
				Font = UIFont.FromName(_picker.PickerFontName, _picker.PickerFontHeaderSize)
			};
					
			var cancelTitle = _picker.CustomCancelButtonTitle ?? "picker.navigation.cancel-button".Translate(defaultValue: "Cancel");
			NavigationItem.LeftBarButtonItem = new UIBarButtonItem (cancelTitle,
				UIBarButtonItemStyle.Plain,
				Dismiss);

			if (_picker.UseCustomFontForNavigationBar) {
				NavigationItem.LeftBarButtonItem.SetTitleTextAttributes(barButtonItemAttributes, UIControlState.Normal);
				NavigationItem.LeftBarButtonItem.SetTitleTextAttributes(barButtonItemAttributes, UIControlState.Highlighted);
			}

			if (_picker.AllowsMultipleSelection) {
				var doneTitle = _picker.CustomDoneButtonTitle ?? "picker.navigation.done-button".Translate(defaultValue: "Done");
				NavigationItem.RightBarButtonItem = new UIBarButtonItem (doneTitle,
					UIBarButtonItemStyle.Done,
					FinishPickingAssets);
				if (_picker.UseCustomFontForNavigationBar) {
					NavigationItem.RightBarButtonItem.SetTitleTextAttributes(barButtonItemAttributes, UIControlState.Normal);
					NavigationItem.RightBarButtonItem.SetTitleTextAttributes(barButtonItemAttributes, UIControlState.Highlighted);
				}
				NavigationItem.RightBarButtonItem.Enabled = _picker.AutoDisableDoneButton ? _picker.SelectedAssets.Any () : true;
			}

			// Bottom toolbar
			ToolbarItems = _picker.GetToolbarItems();

			// Title
			Title = _picker.Title ?? "picker.navigation.title".Translate(defaultValue: "Navigation bar default title");

			// Fetch PHAssetCollections
			var topLevelUserCollections = PHCollectionList.FetchTopLevelUserCollections(null);
			var smartAlbums = PHAssetCollection.FetchAssetCollections (PHAssetCollectionType.SmartAlbum, PHAssetCollectionSubtype.AlbumRegular, null);
			_collectionsFetchResults = new List<PHFetchResult> { topLevelUserCollections, smartAlbums };
			_collectionsLocalizedTitles = new List<string> { "picker.table.user-albums-header".Translate (defaultValue: "Albums"), "picker.table.smart-albums-header".Translate("Smart Albums") };

			UpdateFetchResults ();

			// Register for changes
			PHPhotoLibrary.SharedPhotoLibrary.RegisterChangeObserver(this);

			if (UIDevice.CurrentDevice.CheckSystemVersion (7, 0)) {
				EdgesForExtendedLayout = UIRectEdge.None;
			}
		}

		private void FinishPickingAssets (object sender, EventArgs args)
		{
			// Explicitly unregister observer because we cannot predict when the GC cleans up
			Unregister ();
			_picker.FinishPickingAssets (sender, args);
		}

		private void Dismiss (object sender, EventArgs args)
		{
			// Explicitly unregister observer because we cannot predict when the GC cleans up
			Unregister ();
			_picker.Dismiss (sender, args);
		}

		protected override void Dispose (bool disposing)
		{
			Unregister ();			
			base.Dispose (disposing);
		}

		private void Unregister() 
		{
			PHPhotoLibrary.SharedPhotoLibrary.UnregisterChangeObserver (this);
		}

		public override UIStatusBarStyle PreferredStatusBarStyle ()
		{
			return _picker.PickerStatusBarStyle;
		}

		public void SelectAllAlbumsCell()
		{
			var path = NSIndexPath.Create(0, 0);
			TableView.SelectRow (path, true, UITableViewScrollPosition.None);
		}

		private void UpdateFetchResults()
		{
			//What I do here is fetch both the albums list and the assets of each album.
			//This way I have acces to the number of items in each album, I can load the 3
			//thumbnails directly and I can pass the fetched result to the gridViewController.
			_collectionsFetchResultsAssets = null;
			_collectionsFetchResultsTitles = null;

			//Fetch PHAssetCollections:
			var topLevelUserCollections = (PHFetchResult) _collectionsFetchResults [0];
			var smartAlbums = (PHFetchResult) _collectionsFetchResults [1];

			// All album: Sorted by descending creation date.
			var allFetchResults = new List<PHFetchResult>();
			var allFetchResultLabels = new List<string> ();

			var options = new PHFetchOptions {
				Predicate = NSPredicate.FromFormat("mediaType in %@", ToNSArray(_picker.MediaTypes)),
				SortDescriptors = new [] { new NSSortDescriptor("creationDate", false) },
			};
			var assetsFetchResult = PHAsset.FetchAssets (options);
			allFetchResults.Add (assetsFetchResult);
			allFetchResultLabels.Add ("picker.table.all-photos-label".Translate (defaultValue: "All photos"));

			//User albums:
			var userFetchResults = new List<PHFetchResult>();
			var userFetchResultLabels = new List<string> ();

			foreach (PHCollection collection in topLevelUserCollections) {
				if (collection is PHAssetCollection) {
					var collectionOptions = new PHFetchOptions {
						Predicate = NSPredicate.FromFormat("mediaType in %@", ToNSArray(_picker.MediaTypes))
					};
					var assetCollection = (PHAssetCollection)collection;

					//Albums collections are always PHAssetCollectionType=1 & PHAssetCollectionSubtype=2
					var collectionAssetsFetchResult = PHAsset.FetchKeyAssets(assetCollection, collectionOptions);
					userFetchResults.Add (collectionAssetsFetchResult);
					userFetchResultLabels.Add (collection.LocalizedTitle);
				}
			}

			//Smart albums: Sorted by descending creation date.
			var smartFetchResults = new List<PHFetchResult>();
			var smartFetchResultLabels = new List<string> ();

			foreach (PHCollection collection in smartAlbums) {
				if (collection is PHAssetCollection) {
					var assetCollection = (PHAssetCollection)collection;

					//Smart collections are PHAssetCollectionType=2;
					if (_picker.CustomSmartCollections != null && _picker.CustomSmartCollections.Contains (assetCollection.AssetCollectionSubtype)) {
						var smartFetchOptions = new PHFetchOptions {
							Predicate = NSPredicate.FromFormat("mediaType in %@", ToNSArray(_picker.MediaTypes)),
							SortDescriptors = new [] { new NSSortDescriptor ("creationDate", false) },
						};

						var smartAssetsFetchResult = PHAsset.FetchKeyAssets (assetCollection, smartFetchOptions);
						if (smartAssetsFetchResult.Any ()) {
							smartFetchResults.Add (smartAssetsFetchResult);
							smartFetchResultLabels.Add (collection.LocalizedTitle);
						}
					}
				}
			}

			_collectionsFetchResultsAssets = new PHFetchResult[][] {
				allFetchResults.ToArray (),
				userFetchResults.ToArray (),
				smartFetchResults.ToArray ()
			};
			_collectionsFetchResultsTitles = new string[][] { 
				allFetchResultLabels.ToArray (),
				userFetchResultLabels.ToArray (),
				smartFetchResultLabels.ToArray ()
			};
		}

		private NSArray ToNSArray(PHAssetMediaType[] managed)
		{
			var mediaTypes = new NSMutableArray ((nuint) _picker.MediaTypes.Length);
			for (int i = 0; i < _picker.MediaTypes.Length; i++) {
				mediaTypes.Add (NSObject.FromObject(_picker.MediaTypes [i]));
			}

			return mediaTypes;
		}

		#region Rotation
		public override bool ShouldAutorotate ()
		{
			return true;
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations ()
		{
			return UIInterfaceOrientationMask.AllButUpsideDown;
		}
		#endregion

		#region TableViewSource

		private class GMAlbumsViewTableViewSource : UITableViewSource
		{
			private const string CellReuseIdentifier = "Cell";
			private readonly GMAlbumsViewController _parent;

			public GMAlbumsViewTableViewSource(GMAlbumsViewController parent) {
				_parent = parent;
			}

			public override nint NumberOfSections (UITableView tableView)
			{
				return _parent._collectionsFetchResultsAssets.Length;
			}

			public override nint RowsInSection (UITableView tableview, nint section)
			{
				var fetchResult = _parent._collectionsFetchResultsAssets [(int) section];
				return fetchResult.Length;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = (GMAlbumsViewCell)tableView.DequeueReusableCell (CellReuseIdentifier);
				if (cell == null) {
					cell = new GMAlbumsViewCell (UITableViewCellStyle.Subtitle, CellReuseIdentifier);
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				}

				// Increment the cell's tag
				var currentTag = cell.Tag + 1;
				cell.Tag = currentTag;

				// Set the label
				cell.TextLabel.Font = UIFont.FromName (_parent._picker.PickerFontName, _parent._picker.PickerFontHeaderSize);
				cell.TextLabel.Text = _parent._collectionsFetchResultsTitles [indexPath.Section][indexPath.Row];
				cell.TextLabel.TextColor = _parent._picker.PickerTextColor;

				// Retrieve the pre-fetched assets for this album:
				var assetsFetchResult = (PHFetchResult) _parent._collectionsFetchResultsAssets[indexPath.Section][indexPath.Row];

				// Display the number of assets
				if (_parent._picker.DisplayAlbumsNumberOfAssets) {
					cell.DetailTextLabel.Font = UIFont.FromName (_parent._picker.PickerFontName, _parent._picker.PickerFontNormalSize);
					// Just use the number of assets. Album app does this:
					cell.DetailTextLabel.Text = string.Format("{0:0}", assetsFetchResult.Count);
					cell.DetailTextLabel.TextColor = _parent._picker.PickerTextColor;
				}

				// Set the 3 images (if exists):
				if (assetsFetchResult.Any ()) {
					var scale = UIScreen.MainScreen.Scale;

					// Compute the thumbnail pixel size:
					var tableCellThumbnailSize1 = new CGSize (GMAlbumsViewController.AlbumThumbnailSize1.Width * scale, GMAlbumsViewController.AlbumThumbnailSize1.Height * scale);
					var asset = (PHAsset)assetsFetchResult [0];
					cell.SetVideoLayout (asset.MediaType == PHAssetMediaType.Video);
					_parent._imageManager.RequestImageForAsset (asset, tableCellThumbnailSize1, PHImageContentMode.AspectFill, null, (image, info) => {
						if (cell.Tag == currentTag) {
							cell.ImageView1.Image = image;
						}
					});

					// Second & third images:
					// TODO: Only preload the 3pixels height visible frame!
					if (assetsFetchResult.Count > 1) {
						// Compute the thumbnail pixel size:
						var tableCellThumbnailSize2 = new CGSize (GMAlbumsViewController.AlbumThumbnailSize2.Width * scale, GMAlbumsViewController.AlbumThumbnailSize2.Height * 2);
						asset = (PHAsset)assetsFetchResult [1];
						_parent._imageManager.RequestImageForAsset (asset, tableCellThumbnailSize2, PHImageContentMode.AspectFill, null, (image, info) => {
							if (cell.Tag == currentTag) {
								cell.ImageView2.Image = image;
							}
						});
					} else {
						cell.ImageView2.Image = null;
					}

					if (assetsFetchResult.Count > 2) {
						// Compute the thumbnail pixel size:
						var tableCellThumbnailSize3 = new CGSize (GMAlbumsViewController.AlbumThumbnailSize3.Width * scale, GMAlbumsViewController.AlbumThumbnailSize3.Height * 2);
						asset = (PHAsset)assetsFetchResult [2];
						_parent._imageManager.RequestImageForAsset (asset, tableCellThumbnailSize3, PHImageContentMode.AspectFill, null, (image, info) => {
							if (cell.Tag == currentTag) {
								cell.ImageView3.Image = image;
							}
						});
					} else {
						cell.ImageView3.Image = null;
					}
				} else {
					cell.SetVideoLayout (false);
					var emptyFolder = UIImage.FromBundle ("GMEmptyFolder");
					cell.ImageView3.Image = emptyFolder;
					cell.ImageView2.Image = emptyFolder;
					cell.ImageView1.Image = emptyFolder;
				}
				return cell;
			}
				
			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.CellAt (indexPath);

				// Init the GMGridViewController
				var gridViewController = new GMGridViewController (_parent._picker);

				// Set the title
				gridViewController.Title = cell.TextLabel.Text;
				// Use the prefetched assets!
				gridViewController.AssetsFetchResults = _parent._collectionsFetchResultsAssets [indexPath.Section] [indexPath.Row];

				// Remove selection so it looks better on slide in
				tableView.DeselectRow (indexPath, true);

				// Push GMGridViewController
				_parent.NavigationController.PushViewController (gridViewController, true);
			}

			public override void WillDisplayHeaderView (UITableView tableView, UIView headerView, nint section)
			{
				var header = (UITableViewHeaderFooterView)headerView;
				header.ContentView.BackgroundColor = UIColor.Clear;
				header.BackgroundView.BackgroundColor = UIColor.Clear;

				// Default is a bold font, but keep this styled as a normal font
				header.TextLabel.Font = UIFont.FromName(_parent._picker.PickerFontName, _parent._picker.PickerFontNormalSize);
				header.TextLabel.TextColor = _parent._picker.PickerTextColor;
			}

			public override string TitleForHeader (UITableView tableView, nint section)
			{
				//Tip: returning null hides the section header!

				string title = null;
				if (section > 0) {
					// Only show title for non-empty sections:
					var fetchResult = _parent._collectionsFetchResultsAssets[section];
					if (fetchResult.Any ()) {
						title = _parent._collectionsLocalizedTitles [(int) section - 1];
					}
				}
				return title;
			}
		}
		#endregion
	}
}