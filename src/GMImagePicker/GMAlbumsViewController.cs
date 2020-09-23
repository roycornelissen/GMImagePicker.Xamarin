//
//  GMAlbumsViewController.cs
//  GMPhotoPicker.Xamarin
//
//  Created by Roy Cornelissen on 23/03/16.
//  Based on original GMImagePicker implementation by Guillermo Muntaner Perelló.
//  https://github.com/guillermomuntaner/GMImagePicker
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using Photos;
using UIKit;

namespace GMImagePicker
{
    internal class GMAlbumsViewController: UITableViewController, IPHPhotoLibraryChangeObserver
	{
		private const string CellReuseIdentifier = "AlbumCell";

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

		public GMAlbumsViewController() : base(UITableViewStyle.Plain)
		{
			PreferredContentSize = GMImagePickerController.PopoverContentSize;
		}

		public void PhotoLibraryDidChange (PHChange changeInstance)
		{
			Debug.WriteLine($"{this.GetType().Name}: PhotoLibraryDidChange");
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
				}

				// Search for new photos and select them if camera is turned on
				if (_picker.AutoSelectCameraImages && _picker.ShowCameraButton) {
					foreach (var collection in _collectionsFetchResultsAssets) {
						foreach (var fetchResult in collection) {
							var changeDetails = changeInstance.GetFetchResultChangeDetails (fetchResult);

							if (changeDetails != null && changeDetails.InsertedObjects != null) {
								foreach (var asset in changeDetails.InsertedObjects.OfType<PHAsset>()) {
									_picker.SelectAsset (asset);
								}
							}
						}
					}
				}

				// However, we want to update if photos are added, so the counts of items & thumbnails are updated too.
				// Maybe some checks could be done here , but for now is OKey.
				UpdateFetchResults();
			
				TableView.ReloadData();
			});			
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			_picker = (GMImagePickerController)NavigationController.ParentViewController;
			View.BackgroundColor = _picker.PickerBackgroundColor;

			// Navigation bar customization
			if (!string.IsNullOrWhiteSpace(_picker.CustomNavigationBarPrompt))
			{
				NavigationItem.Prompt = _picker.CustomNavigationBarPrompt;
			}

			_imageManager = new PHCachingImageManager();

			// Table view aspect
			TableView.RowHeight = AlbumRowHeight;
			TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;

			// Buttons
			var barButtonItemAttributes = new UITextAttributes
			{
				Font = FontParser.GetFont(_picker.PickerFontName, _picker.PickerFontHeaderSize)
			};

			var cancelTitle = _picker.CustomCancelButtonTitle ?? "picker.navigation.cancel-button".Translate(defaultValue: "Cancel");
			NavigationItem.LeftBarButtonItem = new UIBarButtonItem(cancelTitle,
				UIBarButtonItemStyle.Plain,
				Dismiss);

			if (!string.IsNullOrWhiteSpace(_picker.CustomBackButtonTitle))
            {
				NavigationItem.BackButtonTitle = _picker.CustomBackButtonTitle;
            }
			if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
            {
				NavigationItem.BackButtonDisplayMode = _picker.BackButtonDisplayMode;
            }
			else
			{
				if (_picker.BackButtonDisplayMode == UINavigationItemBackButtonDisplayMode.Minimal)
				{
					NavigationItem.BackButtonTitle = "";
				}
			}

			if (_picker.UseCustomFontForNavigationBar)
			{
				NavigationItem.LeftBarButtonItem.SetTitleTextAttributes(barButtonItemAttributes, UIControlState.Normal);
				NavigationItem.LeftBarButtonItem.SetTitleTextAttributes(barButtonItemAttributes, UIControlState.Highlighted);
			}

			if (_picker.AllowsMultipleSelection)
			{
				var doneTitle = _picker.CustomDoneButtonTitle ?? "picker.navigation.done-button".Translate(defaultValue: "Done");
				NavigationItem.RightBarButtonItem = new UIBarButtonItem(doneTitle,
					UIBarButtonItemStyle.Done,
					FinishPickingAssets);
				if (_picker.UseCustomFontForNavigationBar)
				{
					NavigationItem.RightBarButtonItem.SetTitleTextAttributes(barButtonItemAttributes, UIControlState.Normal);
					NavigationItem.RightBarButtonItem.SetTitleTextAttributes(barButtonItemAttributes, UIControlState.Highlighted);
				}
				NavigationItem.RightBarButtonItem.Enabled = !_picker.AutoDisableDoneButton || _picker.SelectedAssets.Any();
			}

			// Bottom toolbar
			ToolbarItems = _picker.GetToolbarItems();

			// Title
			Title = _picker.Title ?? "picker.navigation.title".Translate(defaultValue: "Navigation bar default title");

			// Fetch PHAssetCollections
			var topLevelUserCollections = PHCollection.FetchTopLevelUserCollections(null);
			var smartAlbums = PHAssetCollection.FetchAssetCollections(PHAssetCollectionType.SmartAlbum, PHAssetCollectionSubtype.AlbumRegular, null);
			_collectionsFetchResults = new List<PHFetchResult> { topLevelUserCollections, smartAlbums };
			_collectionsLocalizedTitles = new List<string> { "picker.table.user-albums-header".Translate(defaultValue: "Albums"), "picker.table.smart-albums-header".Translate("Smart Albums") };

			UpdateFetchResults();

			// Register for changes
			PHPhotoLibrary.SharedPhotoLibrary.RegisterChangeObserver(this);
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
			Debug.WriteLine($"{GetType().Name}: Unregister");
			PHPhotoLibrary.SharedPhotoLibrary.UnregisterChangeObserver (this);
		}

		public override UIStatusBarStyle PreferredStatusBarStyle ()
		{
			return _picker.PickerStatusBarStyle;
		}

		public void SelectAllAlbumsCell()
		{
			var path = NSIndexPath.Create(0, 0);
			RowSelected(TableView, path);
		}

		private void UpdateFetchResults()
		{
			//What I do here is fetch both the albums list and the assets of each album.
			//This way I have acces to the number of items in each album, I can load the 3
			//thumbnails directly and I can pass the fetched result to the gridViewController.
			_collectionsFetchResultsAssets = null;
			_collectionsFetchResultsTitles = null;

			//Fetch PHAssetCollections:
			var topLevelUserCollections = _collectionsFetchResults[0];
			var smartAlbums = _collectionsFetchResults[1];

			// All album: Sorted by descending creation date.
			var allFetchResults = new List<PHFetchResult>();
			var allFetchResultLabels = new List<string> ();

			var options = new PHFetchOptions {
				Predicate = NSPredicate.FromFormat("mediaType in %@", ToNSArray(_picker.MediaTypes)),
				SortDescriptors = new [] { new NSSortDescriptor("creationDate", _picker.GridSortOrder == SortOrder.Ascending) },
			};
			var assetsFetchResult = PHAsset.FetchAssets (options);
			allFetchResults.Add (assetsFetchResult);
			allFetchResultLabels.Add ("picker.table.all-photos-label".Translate (defaultValue: "All photos"));

			//User albums:
			var userFetchResults = new List<PHFetchResult>();
			var userFetchResultLabels = new List<string> ();

			foreach (var assetCollection in topLevelUserCollections.OfType<PHAssetCollection>()) {
				var collectionOptions = new PHFetchOptions {
					Predicate = NSPredicate.FromFormat("mediaType in %@", ToNSArray(_picker.MediaTypes)),
                    SortDescriptors = new[] { new NSSortDescriptor("creationDate", _picker.GridSortOrder == SortOrder.Ascending) },
                };

				//Albums collections are always PHAssetCollectionType=1 & PHAssetCollectionSubtype=2
				var collectionAssetsFetchResult = PHAsset.FetchAssets(assetCollection, collectionOptions);
				userFetchResults.Add (collectionAssetsFetchResult);
				userFetchResultLabels.Add (assetCollection.LocalizedTitle);
			}

			//Smart albums: Sorted by descending creation date.
			var smartFetchResults = new List<PHFetchResult>();
			var smartFetchResultLabels = new List<string> ();

			foreach (var assetCollection in smartAlbums.OfType<PHAssetCollection>()) {
				//Smart collections are PHAssetCollectionType=2;
				if (_picker.CustomSmartCollections != null && _picker.CustomSmartCollections.Contains (assetCollection.AssetCollectionSubtype)) {
					var smartFetchOptions = new PHFetchOptions {
						Predicate = NSPredicate.FromFormat("mediaType in %@", ToNSArray(_picker.MediaTypes)),
						SortDescriptors = new [] { new NSSortDescriptor ("creationDate", _picker.GridSortOrder == SortOrder.Ascending) },
					};

					var smartAssetsFetchResult = PHAsset.FetchAssets (assetCollection, smartFetchOptions);
					if (smartAssetsFetchResult.Any ()) {
						smartFetchResults.Add (smartAssetsFetchResult);
						smartFetchResultLabels.Add (assetCollection.LocalizedTitle);
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

		private NSArray ToNSArray(IReadOnlyCollection<PHAssetMediaType> managed)
		{
			var mediaTypes = new NSMutableArray ((nuint)managed.Count);
			foreach (var mediaType in _picker.MediaTypes)
			{
			    mediaTypes.Add (FromObject(mediaType));
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

		public override nint NumberOfSections(UITableView tableView)
		{
			return _collectionsFetchResultsAssets.Length;
		}

		public override nint RowsInSection(UITableView tableView, nint section)
		{
			var fetchResult = _collectionsFetchResultsAssets[(int)section];
			return fetchResult.Length;
		}

		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			var cell = (GMAlbumsViewCell)tableView.DequeueReusableCell(CellReuseIdentifier);
			if (cell == null)
			{
				cell = new GMAlbumsViewCell(UITableViewCellStyle.Subtitle, CellReuseIdentifier);
				cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
			}

			// Increment the cell's tag
			var currentTag = cell.Tag + 1;
			cell.Tag = currentTag;

			// Set the label
			cell.TextLabel.Font = FontParser.GetFont(_picker.PickerFontName, _picker.PickerFontHeaderSize);
			cell.TextLabel.Text = _collectionsFetchResultsTitles[indexPath.Section][indexPath.Row];
			cell.TextLabel.TextColor = _picker.PickerTextColor;

			// Retrieve the pre-fetched assets for this album:
			var assetsFetchResult = _collectionsFetchResultsAssets[indexPath.Section][indexPath.Row];

			// Display the number of assets
			if (_picker.DisplayAlbumsNumberOfAssets)
			{
				cell.DetailTextLabel.Font = FontParser.GetFont(_picker.PickerFontName, _picker.PickerFontNormalSize);
				// Just use the number of assets. Album app does this:
				cell.DetailTextLabel.Text = string.Format("{0:0}", assetsFetchResult.Count);
				cell.DetailTextLabel.TextColor = _picker.PickerTextColor;
			}

			var numberOfAssets = assetsFetchResult.Count;

			// Set the 3 images (if exists):
			if (numberOfAssets > 0)
			{
				var scale = UIScreen.MainScreen.Scale;

				var options = new PHImageRequestOptions
				{
					Synchronous = false,
					NetworkAccessAllowed = true,
					DeliveryMode = PHImageRequestOptionsDeliveryMode.Opportunistic,
					ResizeMode = PHImageRequestOptionsResizeMode.Fast
				};

				// Compute the thumbnail pixel size:
				var tableCellThumbnailSize1 = new CGSize(AlbumThumbnailSize1.Width * scale, AlbumThumbnailSize1.Height * scale);
				var asset = (PHAsset)assetsFetchResult[_picker.GridSortOrder == SortOrder.Ascending ? numberOfAssets - 1 : 0];
				cell.SetVideoLayout(asset.MediaType == PHAssetMediaType.Video);
				_imageManager.RequestImageForAsset(asset,
					tableCellThumbnailSize1,
					PHImageContentMode.AspectFill,
					options,
					(image, info) => {
						if (cell.Tag == currentTag && cell.ImageView1 != null && image != null)
						{
							cell.ImageView1.Image = image;
						}
					});

				// Second & third images:
				// TODO: Only preload the 3pixels height visible frame!
				if (numberOfAssets > 1)
				{
					// Compute the thumbnail pixel size:
					var tableCellThumbnailSize2 = new CGSize(AlbumThumbnailSize2.Width * scale, AlbumThumbnailSize2.Height * 2);
					asset = (PHAsset)assetsFetchResult[_picker.GridSortOrder == SortOrder.Ascending ? numberOfAssets - 2 : 1];
					_imageManager.RequestImageForAsset(asset,
						tableCellThumbnailSize2,
						PHImageContentMode.AspectFill,
						options,
						(image, info) => {
							if (cell.Tag == currentTag && cell.ImageView2 != null && image != null)
							{
								cell.ImageView2.Image = image;
							}
						});
				}
				else
				{
					cell.ImageView2.Image = null;
				}

				if (numberOfAssets > 2)
				{
					// Compute the thumbnail pixel size:
					var tableCellThumbnailSize3 = new CGSize(AlbumThumbnailSize3.Width * scale, AlbumThumbnailSize3.Height * 2);
					asset = (PHAsset)assetsFetchResult[_picker.GridSortOrder == SortOrder.Ascending ? numberOfAssets - 3 : 2];
					_imageManager.RequestImageForAsset(asset,
						tableCellThumbnailSize3,
						PHImageContentMode.AspectFill,
						options,
						(image, info) => {
							if (cell.Tag == currentTag && cell.ImageView3 != null && image != null)
							{
								cell.ImageView3.Image = image;
							}
						});
				}
				else
				{
					cell.ImageView3.Image = null;
				}
			}
			else
			{
				cell.SetVideoLayout(false);
				var emptyFolder = UIImage.FromFile("GMEmptyFolder");
				cell.ImageView3.Image = emptyFolder;
				cell.ImageView2.Image = emptyFolder;
				cell.ImageView1.Image = emptyFolder;
			}
			return cell;
		}

		public override async void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			// Remove selection so it looks better on slide in
			tableView.DeselectRow(indexPath, true);

			if (!await _picker.EnsureHasPhotosPermission())
			{
				return;
			}

			var cell = tableView.CellAt(indexPath);

			// Init the GMGridViewController
			var gridViewController = new GMGridViewController(_picker);

			// Set the title
			gridViewController.Title = _collectionsFetchResultsTitles[indexPath.Section][indexPath.Row];
			// Use the prefetched assets!
			gridViewController.AssetsFetchResults = _collectionsFetchResultsAssets[indexPath.Section][indexPath.Row];

			// Push GMGridViewController
			NavigationController.PushViewController(gridViewController, true);
		}

		public override void WillDisplayHeaderView(UITableView tableView, UIView headerView, nint section)
		{
			var header = (UITableViewHeaderFooterView)headerView;
			header.ContentView.BackgroundColor = UIColor.Clear;

			if (header.BackgroundView != null)
			{
				header.BackgroundView.BackgroundColor = _picker.PickerBackgroundColor;
			}
			else if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
			{
				if (header.BackgroundConfiguration != null)
				{
					header.BackgroundConfiguration.BackgroundColor = _picker.PickerBackgroundColor;
				}
			}

			// Default is a bold font, but keep this styled as a normal font
			header.TextLabel.Font = FontParser.GetFont(_picker.PickerFontName, _picker.PickerFontNormalSize);
			header.TextLabel.TextColor = _picker.PickerTextColor;
		}

		public override string TitleForHeader(UITableView tableView, nint section)
		{
			//Tip: returning null hides the section header!

			string title = null;
			if (section > 0)
			{
				// Only show title for non-empty sections:
				var fetchResult = _collectionsFetchResultsAssets[section];
				if (fetchResult.Any())
				{
					title = _collectionsLocalizedTitles[(int)section - 1];
				}
			}
			return title;
		}
		#endregion
	}
}
