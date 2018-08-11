//
//  GMGridViewController.cs
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
using System.Linq;
using System.Collections.Generic;
using Foundation;
using CoreFoundation;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GMImagePicker
{
	internal class GMGridViewController: UICollectionViewController, IPHPhotoLibraryChangeObserver
	{
		private const string GMGridViewCellIdentifier = "GMGridViewCellIdentifier";

		private readonly GMImagePickerController _picker;
		private static CGSize AssetGridThumbnailSize;
		private static UICollectionViewFlowLayout _portraitLayout;
		private static UICollectionViewFlowLayout _landscapeLayout;
		private PHCachingImageManager _imageManager;

		public PHFetchResult AssetsFetchResults { get; set; }

		public GMGridViewController (IntPtr handle): base(handle)
		{
		}

		public GMGridViewController (GMImagePickerController picker) : base(CollectionViewFlowLayoutForOrientation(UIApplication.SharedApplication.StatusBarOrientation, picker))
		{
			//Custom init. The picker contains custom information to create the FlowLayout
			_picker = picker;
			_picker.FinishedPickingAssets += OnCleanup;
			_picker.Canceled += OnCleanup;

			//Compute the thumbnail pixel size:
			var scale = UIScreen.MainScreen.Scale;

			var layout = (UICollectionViewFlowLayout)Layout;
			AssetGridThumbnailSize = new CGSize (layout.ItemSize.Width * scale, layout.ItemSize.Height * scale);

			CollectionView.AllowsMultipleSelection = _picker.AllowsMultipleSelection;
			CollectionView.RegisterClassForCell (typeof(GMGridViewCell), GMGridViewCellIdentifier);

			PreferredContentSize = GMImagePickerController.PopoverContentSize;
		}

		void OnCleanup(object sender, EventArgs e)
		{
			Unregister();
		}

		public override void ViewDidDisappear(bool animated)
		{
			if (animated)
			{
				Unregister();
			}
			base.ViewDidDisappear(animated);
		}

		private static UICollectionViewFlowLayout CollectionViewFlowLayoutForOrientation (UIInterfaceOrientation orientation, GMImagePickerController picker)
		{
			nfloat screenWidth;
			nfloat screenHeight;

			//Ipad popover is not affected by rotation!
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad) {
				screenWidth = picker.View.Bounds.Width;
				screenHeight = picker.View.Bounds.Height;
			} else {
				var insets = UIEdgeInsets.Zero;
				if (picker.View.RespondsToSelector(new ObjCRuntime.Selector("safeAreaInsets"))) {
					insets = picker.View.SafeAreaInsets;
				}
				var horizontalInsets = insets.Right + insets.Left;
				var verticalInsets = insets.Bottom + insets.Top;

				if (UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeLeft ||
					UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeRight) {
					screenHeight = picker.View.Bounds.Width - horizontalInsets;
					screenWidth = picker.View.Bounds.Height - verticalInsets;
				} else {
					screenWidth = picker.View.Bounds.Width - horizontalInsets;
					screenHeight = picker.View.Bounds.Height - verticalInsets;
				}
			}

			if ((UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad) ||
				(UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.Portrait ||
					UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.PortraitUpsideDown))
			{
				if (_portraitLayout == null) {
					var cellTotalUsableWidth = screenWidth - (picker.ColsInPortrait - 1) * picker.MinimumInteritemSpacing;
					var itemSize = new CGSize (cellTotalUsableWidth / picker.ColsInPortrait, cellTotalUsableWidth / picker.ColsInPortrait);
					var cellTotalUsedWidth = (double)itemSize.Width * picker.ColsInPortrait;
					var spaceTotalWidth = screenWidth - cellTotalUsedWidth;
					var spaceWidth = spaceTotalWidth / (picker.ColsInPortrait - 1);

					_portraitLayout = new UICollectionViewFlowLayout {
						MinimumInteritemSpacing = picker.MinimumInteritemSpacing,
						ItemSize = itemSize,
						MinimumLineSpacing = (nfloat) spaceWidth
					};

					if (_portraitLayout.RespondsToSelector(new ObjCRuntime.Selector("sectionInsetReference"))) {
						_portraitLayout.SectionInsetReference = UICollectionViewFlowLayoutSectionInsetReference.SafeArea;
					}
				}
				return _portraitLayout;
			} else {
				if (_landscapeLayout == null) {
					var cellTotalUsableWidth = screenHeight - (picker.ColsInLandscape - 1) * picker.MinimumInteritemSpacing;
					var itemSize = new CGSize (cellTotalUsableWidth / picker.ColsInLandscape, cellTotalUsableWidth / picker.ColsInLandscape);
					var cellTotalUsedWidth = (double)itemSize.Width * picker.ColsInLandscape;
					var spaceTotalWidth = screenHeight - cellTotalUsedWidth;
					var spaceWidth = spaceTotalWidth / (picker.ColsInLandscape - 1);
					_landscapeLayout = new UICollectionViewFlowLayout {
						MinimumInteritemSpacing = picker.MinimumInteritemSpacing,
						ItemSize = itemSize,
						MinimumLineSpacing = (nfloat) spaceWidth
					};

					if (_landscapeLayout.RespondsToSelector(new ObjCRuntime.Selector("sectionInsetReference"))) {
						_landscapeLayout.SectionInsetReference = UICollectionViewFlowLayoutSectionInsetReference.SafeArea;
					}
				}
				return _landscapeLayout;
			}
		}

		private void SetupViews ()
		{
			CollectionView.BackgroundColor = UIColor.Clear;
			View.BackgroundColor = _picker.PickerBackgroundColor;
		}

		private void SetupButtons ()
		{
			if (_picker.AllowsMultipleSelection) {
				var doneTitle = _picker.CustomDoneButtonTitle ?? "picker.navigation.done-button".Translate (defaultValue: "Done");
				NavigationItem.RightBarButtonItem = new UIBarButtonItem (doneTitle, 
					UIBarButtonItemStyle.Done,
					FinishPickingAssets);

				NavigationItem.RightBarButtonItem.Enabled = !_picker.AutoDisableDoneButton || _picker.SelectedAssets.Any ();
			} else {
				var cancelTitle = _picker.CustomCancelButtonTitle ?? "picker.navigation.cancel-button".Translate (defaultValue: "Cancel");
				NavigationItem.RightBarButtonItem = new UIBarButtonItem (cancelTitle, 
					UIBarButtonItemStyle.Done,
					Dismiss);
			}
			if (_picker.UseCustomFontForNavigationBar) {
				var barButtonItemAttributes = new UITextAttributes {
					Font = UIFont.FromName(_picker.PickerFontName, _picker.PickerFontHeaderSize)
				};
				NavigationItem.RightBarButtonItem.SetTitleTextAttributes (barButtonItemAttributes, UIControlState.Normal);
				NavigationItem.RightBarButtonItem.SetTitleTextAttributes (barButtonItemAttributes, UIControlState.Highlighted);
			}
		}

		private void SetupToolbar ()
		{
			ToolbarItems = _picker.GetToolbarItems();
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

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			SetupViews ();

			if (!string.IsNullOrEmpty(_picker.CustomNavigationBarPrompt)) {
				NavigationItem.Prompt = _picker.CustomNavigationBarPrompt;
			}

			_imageManager = new PHCachingImageManager ();
			ResetCachedAssets ();

			// Register for changes
			PHPhotoLibrary.SharedPhotoLibrary.RegisterChangeObserver (this);
		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);
			SetupButtons();
			SetupToolbar();

			if (_picker.GridSortOrder == SortOrder.Ascending)
			{
				// Scroll to bottom (newest images are at the bottom)
				CollectionView.SetNeedsLayout();
				CollectionView.LayoutIfNeeded();

				CollectionView.SetContentOffset(new CGPoint(0, CollectionView.CollectionViewLayout.CollectionViewContentSize.Height), false);

				var item = CollectionView.NumberOfItemsInSection(0) - 1;
				_newestItemPath = NSIndexPath.FromItemSection(item, 0);
			}
		}

		NSIndexPath _newestItemPath;

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);

			if (_newestItemPath != null && _newestItemPath.Section >= 0 && _newestItemPath.Row >= 0 && _newestItemPath.Item >= 0 && _picker.GridSortOrder == SortOrder.Ascending)
			{
				// Scroll to bottom (newest images are at the bottom)
				CollectionView.ScrollToItem(_newestItemPath, UICollectionViewScrollPosition.Bottom, false);
			}

			UpdateCachedAssets();
		}

		#region Asset Caching
		private CGRect _previousPreheatRect;

		private void ResetCachedAssets ()
		{
			_imageManager.StopCaching ();
			_previousPreheatRect = CGRect.Empty;
		}

		private void ComputeDifferenceBetweenRect (CGRect oldRect, CGRect newRect, Action<CGRect> removedHandler, Action<CGRect> addedHandler)
		{
			if (CGRect.Intersect (newRect, oldRect) != CGRect.Empty) {
				var oldMaxY = oldRect.GetMaxY ();
				var oldMinY = oldRect.GetMinY ();
				var newMaxY = newRect.GetMaxY ();
				var newMinY = newRect.GetMinY ();

				if (newMaxY > oldMaxY) {
					var rectToAdd = new CGRect (newRect.X, oldMaxY, newRect.Size.Width, (newMaxY - oldMaxY));
					addedHandler (rectToAdd);
				}
				if (oldMinY > newMinY) {
					var rectToAdd = new CGRect (newRect.X, newMinY, newRect.Size.Width, (oldMinY - newMinY));
					addedHandler (rectToAdd);
				}
				if (newMaxY < oldMaxY) {
					var rectToRemove = new CGRect (newRect.X, newMaxY, newRect.Size.Width, (oldMaxY - newMaxY));
					removedHandler (rectToRemove);
				}
				if (oldMinY < newMinY) {
					var rectToRemove = new CGRect (newRect.X, oldMinY, newRect.Size.Width, (newMinY - oldMinY));
					removedHandler (rectToRemove);
				}
			} else {
				addedHandler (newRect);
				removedHandler (oldRect);
			}
		}

		private void UpdateCachedAssets ()
		{
			var isViewVisible = IsViewLoaded && View.Window != null;
			if (!isViewVisible) {
				return;
			}

			// The preheat window is twice the height of the visible rect
			var preheatRect = CollectionView.Bounds;
			preheatRect = preheatRect.Inset (0.0f, -0.5f * preheatRect.Height);

			// If scrolled by a "reasonable" amount...
			var delta = Math.Abs(preheatRect.GetMidY() - _previousPreheatRect.GetMidY());
			if (delta > CollectionView.Bounds.Height / 3.0f) {
				// Compute the assets to start caching and to stop caching.
				var addedIndexPaths = new List<NSIndexPath> ();
				var removedIndexPaths = new List<NSIndexPath> ();

				ComputeDifferenceBetweenRect (_previousPreheatRect, 
					preheatRect, 
					(removedRect) => {
						removedIndexPaths.AddRange(GetIndexPathsForElementsInRect(removedRect));
					},
					(addedRect) => {
						addedIndexPaths.AddRange(GetIndexPathsForElementsInRect(addedRect));
					});

				var assetsToStartCaching = GetAssetsAtIndexPaths (addedIndexPaths);
				var assetsToStopCaching = GetAssetsAtIndexPaths (removedIndexPaths);

				var options = new PHImageRequestOptions
				{
					Synchronous = false,
					NetworkAccessAllowed = true,
					DeliveryMode = PHImageRequestOptionsDeliveryMode.Opportunistic,
					ResizeMode = PHImageRequestOptionsResizeMode.Fast
				};

				if (assetsToStartCaching != null) {
					_imageManager.StartCaching (assetsToStartCaching, 
						AssetGridThumbnailSize,
						PHImageContentMode.AspectFill,
						options);
				}
				if (assetsToStopCaching != null) {
					_imageManager.StopCaching (assetsToStopCaching,
						AssetGridThumbnailSize,
						PHImageContentMode.AspectFill,
						options);
				}

				_previousPreheatRect = preheatRect;
			}
		}

		private PHAsset[] GetAssetsAtIndexPaths(ICollection<NSIndexPath> indexPaths)
		{
			if (!indexPaths.Any()) {
				return null;
			}

			var assets = new List<PHAsset> ();

			foreach (var indexPath in indexPaths) {
				var asset = (PHAsset) AssetsFetchResults [indexPath.Item];
				assets.Add (asset);
			}

			return assets.ToArray ();
		}
			
		private NSIndexPath[] GetIndexPathsForElementsInRect (CGRect rect)
		{
			var indexPaths = new List<NSIndexPath> ();

			var allLayoutAttributes = Layout.LayoutAttributesForElementsInRect (rect);
			foreach (var layoutAttributes in allLayoutAttributes) {
				var indexPath = layoutAttributes.IndexPath;
				indexPaths.Add (indexPath);
			}

			return indexPaths.ToArray ();
		}

		#endregion

		#region Photo Library Changes

		public void PhotoLibraryDidChange (PHChange changeInstance)
		{
			Debug.WriteLine($"{this.GetType().Name}: PhotoLibraryDidChange");

			// Call might come on any background queue. Re-dispatch to the main queue to handle it.
			DispatchQueue.MainQueue.DispatchAsync (() => {
				// check if there are changes to the assets (insertions, deletions, updates)
				var collectionChanges = changeInstance.GetFetchResultChangeDetails(AssetsFetchResults);
				if (collectionChanges != null) {
					var collectionView = CollectionView;
					if (CollectionView == null)
					{
						return;
					}

					// get the new fetch result
					AssetsFetchResults = collectionChanges.FetchResultAfterChanges;

					if (!collectionChanges.HasIncrementalChanges || collectionChanges.HasMoves) {
						// we need to reload all if the incremental diffs are not available
						collectionView.ReloadData();
					} else {
						// if we have incremental diffs, tell the collection view to animate insertions and deletions
						collectionView.PerformBatchUpdates(() =>
						{
							var removedIndexes = collectionChanges.RemovedIndexes;
							if (removedIndexes != null && removedIndexes.Count > 0)
							{
								collectionView.DeleteItems(GetIndexesWithSection(removedIndexes, 0));
							}

							if (collectionChanges.InsertedIndexes != null && collectionChanges.InsertedIndexes.Count > 0)
							{
								collectionView.InsertItems(GetIndexesWithSection(collectionChanges.InsertedIndexes, 0));

								var changedIndexes = collectionChanges.ChangedIndexes;
								if (changedIndexes != null && changedIndexes.Count > 0)
								{
									collectionView.ReloadItems(GetIndexesWithSection(changedIndexes, 0));
								}
							}
						}, (x) => {
							if (_picker.GridSortOrder == SortOrder.Ascending)
							{
								var item = collectionView.NumberOfItemsInSection(0) - 1;
							    if (item >= 0)
							    {
							        var path = NSIndexPath.FromItemSection(item, 0);
							        collectionView.ScrollToItem(path, UICollectionViewScrollPosition.Bottom, true);
							    }
							}
							else
							{
								var path = NSIndexPath.FromItemSection(0, 0);
								collectionView.ScrollToItem(path, UICollectionViewScrollPosition.Top, true);
							}
						});

						if (collectionChanges.InsertedIndexes != null && collectionChanges.InsertedIndexes.Count > 0)
						{
							if (_picker.ShowCameraButton && _picker.AutoSelectCameraImages)
							{
								foreach (var path in GetIndexesWithSection(collectionChanges.InsertedIndexes, 0))
								{
									ItemSelected(collectionView, path);
								}
							}
						}
					}
					ResetCachedAssets();
				}
			});
		}

		private NSIndexPath[] GetIndexesWithSection(NSIndexSet indexes, nint section) 
		{
			var indexPaths = new List<NSIndexPath>();
			indexes.EnumerateIndexes((nuint idx, ref bool stop) => {
				indexPaths.Add(NSIndexPath.FromItemSection((nint) idx, section));
			});
			return indexPaths.ToArray();
		}

		protected override void Dispose (bool disposing)
		{
			Unregister ();
			base.Dispose (disposing);
		}

		private void Unregister([CallerMemberName] string calledBy = "")
		{
			_picker.FinishedPickingAssets -= OnCleanup;
			_picker.Canceled -= OnCleanup;
			Debug.WriteLine($"{GetType().Name}: Unregister called by {calledBy}");
			ResetCachedAssets ();
			PHPhotoLibrary.SharedPhotoLibrary.UnregisterChangeObserver (this);
		}

		#endregion

		public override UIStatusBarStyle PreferredStatusBarStyle ()
		{
			return _picker.PickerStatusBarStyle;
		}

		public override void Scrolled (UIScrollView scrollView)
		{
			UpdateCachedAssets ();
		}

		#region Rotation

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration)
		{
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad) {
				return;
			}

			var layout = CollectionViewFlowLayoutForOrientation (toInterfaceOrientation, _picker);

			//Update the AssetGridThumbnailSize:
			var scale = UIScreen.MainScreen.Scale;
			AssetGridThumbnailSize = new CGSize (layout.ItemSize.Width * scale, layout.ItemSize.Height * scale);

			ResetCachedAssets ();

			var options = new PHImageRequestOptions
			{
				Synchronous = false,
				NetworkAccessAllowed = true,
				DeliveryMode = PHImageRequestOptionsDeliveryMode.Opportunistic,
				ResizeMode = PHImageRequestOptionsResizeMode.Fast
			};

			//This is optional. Reload visible thumbnails:
			foreach (var cell in CollectionView.VisibleCells) {
				var typedCell = (GMGridViewCell)cell;
				var currentTag = cell.Tag;
				_imageManager.RequestImageForAsset (typedCell.Asset,
					AssetGridThumbnailSize,
				    PHImageContentMode.AspectFill,
                    options,
                    (image, info) => {
						// Only update the thumbnail if the cell tag hasn't changed. Otherwise, the cell has been re-used.
						if (cell.Tag == currentTag && typedCell.ImageView != null && image != null) {
							typedCell.ImageView.Image = image;
						}
					});
			}
			CollectionView.SetCollectionViewLayout (layout, true);
		}

		public override nint NumberOfSections(UICollectionView collectionView)
		{
			return 1;
		}

		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var cell = (GMGridViewCell)CollectionView.DequeueReusableCell(GMGridViewCellIdentifier, indexPath);

			var asset = (PHAsset)AssetsFetchResults[indexPath.Item];

			if (cell != null)
			{
				// Increment the cell's tag
				var currentTag = cell.Tag + 1;
				cell.Tag = currentTag;

				_imageManager.RequestImageForAsset(asset,
					AssetGridThumbnailSize,
					PHImageContentMode.AspectFill,
					new PHImageRequestOptions { DeliveryMode = PHImageRequestOptionsDeliveryMode.Opportunistic, ResizeMode = PHImageRequestOptionsResizeMode.Fast },
					(image, info) => {
							// Only update the thumbnail if the cell tag hasn't changed. Otherwise, the cell has been re-used.
							if (cell.Tag == currentTag && cell.ImageView != null && image != null)
						{
							cell.ImageView.Image = image;
						}
					});
			}

			cell.Bind(asset);
			cell.ShouldShowSelection = _picker.AllowsMultipleSelection;

			// Optional protocol to determine if some kind of assets can't be selected (long videos, etc...)
			cell.IsEnabled = _picker.VerifyShouldEnableAsset(asset);

			if (_picker.SelectedAssets.Contains(asset))
			{
				cell.Selected = true;
				CollectionView.SelectItem(indexPath, false, UICollectionViewScrollPosition.None);
			}
			else
			{
				cell.Selected = false;
			}
			
			ConfigureSelectCellAccessibilityAttributes(cell, cell.Selected);
			return cell;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			var count = AssetsFetchResults.Count;
			return count;
		}

		public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var asset = (PHAsset)AssetsFetchResults[indexPath.Item];
			_picker.SelectAsset(asset);
			_picker.NotifyAssetSelected(asset);
		}

		public override bool ShouldDeselectItem(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var asset = (PHAsset)AssetsFetchResults[indexPath.Item];
			return _picker.VerifyShouldDeselectAsset(asset);
		}

		public override bool ShouldHighlightItem(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var asset = (PHAsset)AssetsFetchResults[indexPath.Item];
			return _picker.VerifyShouldHighlightAsset(asset);
		}

		public override void ItemHighlighted(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var asset = (PHAsset)AssetsFetchResults[indexPath.Item];
			_picker.NotifyAssetHighlighted(asset);
		}

		public override void ItemUnhighlighted(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var asset = (PHAsset)AssetsFetchResults[indexPath.Item];
			_picker.NotifyAssetUnhighlighted(asset);
		}

		public override bool ShouldSelectItem(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var asset = (PHAsset)AssetsFetchResults[indexPath.Item];

			var cell = (GMGridViewCell)collectionView.CellForItem(indexPath);
			ConfigureSelectCellAccessibilityAttributes(cell, true);
			
			if (!cell.IsEnabled)
			{
				return false;
			}
			else
			{
				return _picker.VerifyShouldSelectAsset(asset);
			}
		}

		public override void ItemDeselected(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var asset = (PHAsset)AssetsFetchResults[indexPath.Item];
			var cell = (GMGridViewCell)collectionView.CellForItem(indexPath);

			_picker.DeselectAsset(asset);
			_picker.NotifyAssetDeselected(asset);
			ConfigureSelectCellAccessibilityAttributes(cell, false);
		}
		
		#region Voiceover Accessibility Configuration
		private static void ConfigureSelectCellAccessibilityAttributes(GMGridViewCell selectedCell, bool isSelected)
		{
			selectedCell.AccessibilityTraits = UIAccessibilityTrait.Button;
			selectedCell.IsAccessibilityElement = true;
			if (!isSelected)
			{
				selectedCell.AccessibilityHint = "Check to select the image";
			}
			else
			{
				selectedCell.AccessibilityHint = "Uncheck to deselect the image";
			}
		}
		#endregion
	}

	#endregion
}

