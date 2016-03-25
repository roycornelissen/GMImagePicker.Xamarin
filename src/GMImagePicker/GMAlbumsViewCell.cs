//
//  GMAlbumsViewCell.cs
//  GMPhotoPicker.Xamarin
//
//  Created by Roy Cornelissen on 23/03/16.
//  Based on original GMImagePicker implementation by Guillermo Muntaner Perelló.
//  https://github.com/guillermomuntaner/GMImagePicker
//

using System;
using UIKit;
using CoreAnimation;
using Foundation;
using CoreGraphics;

namespace GMImagePicker
{
    internal class GMAlbumsViewCell : UITableViewCell
	{
		//The imageViews
		public UIImageView ImageView1 { get; private set; }
		public UIImageView ImageView2 { get; private set; }
		public UIImageView ImageView3 { get; private set; }

		//Video additional information
		private UIImageView _videoIcon;
		private UIView _gradientView;
		private CAGradientLayer _gradient;

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			ContentView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		}

		public GMAlbumsViewCell (IntPtr handle): base (handle)
		{
		}

		[Export("initWithStyle:reuseIdentifier:")]
		public GMAlbumsViewCell (UITableViewCellStyle style, string reuseIdentifier)
			: base(style, reuseIdentifier)
		{
			Opaque = false;
			BackgroundColor = UIColor.Clear;
			TextLabel.BackgroundColor = BackgroundColor;
			DetailTextLabel.BackgroundColor = BackgroundColor;
			Accessory = UITableViewCellAccessory.DisclosureIndicator;

			// Border width of 1 pixel:
			nfloat borderWidth = 1.0f / UIScreen.MainScreen.Scale;

			// ImageView
			ImageView3 = new UIImageView {
				ContentMode = UIViewContentMode.ScaleAspectFill,
				Frame = new CGRect(GMAlbumsViewController.AlbumLeftToImageSpace + 4, 8, GMAlbumsViewController.AlbumThumbnailSize3.Width, GMAlbumsViewController.AlbumThumbnailSize3.Height),
				ClipsToBounds = true,
				TranslatesAutoresizingMaskIntoConstraints = true,
				AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin,
			};
			ImageView3.Layer.BorderColor = UIColor.White.CGColor;
			ImageView3.Layer.BorderWidth = borderWidth;
			ContentView.AddSubview (ImageView3);

			// ImageView
			ImageView2 = new UIImageView {
				ContentMode = UIViewContentMode.ScaleAspectFill,
				Frame = new CGRect(GMAlbumsViewController.AlbumLeftToImageSpace + 2, 8 + 2, GMAlbumsViewController.AlbumThumbnailSize2.Width, GMAlbumsViewController.AlbumThumbnailSize2.Height),
				ClipsToBounds = true,
				TranslatesAutoresizingMaskIntoConstraints = true,
				AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin,
			};
			ImageView2.Layer.BorderColor = UIColor.White.CGColor;
			ImageView2.Layer.BorderWidth = borderWidth;
			ContentView.AddSubview (ImageView2);

			// ImageView
			ImageView1 = new UIImageView {
				ContentMode = UIViewContentMode.ScaleAspectFill,
				Frame = new CGRect(GMAlbumsViewController.AlbumLeftToImageSpace, 8 + 4, GMAlbumsViewController.AlbumThumbnailSize1.Width, GMAlbumsViewController.AlbumThumbnailSize1.Height),
				ClipsToBounds = true,
				TranslatesAutoresizingMaskIntoConstraints = true,
				AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin,
			};
			ImageView1.Layer.BorderColor = UIColor.White.CGColor;
			ImageView1.Layer.BorderWidth = borderWidth;
			ContentView.AddSubview (ImageView1);

			// The video gradient, label & icon
			var gradientFrame = new CGRect(0.0f, GMAlbumsViewController.AlbumThumbnailSize1.Height - GMAlbumsViewController.AlbumGradientHeight, GMAlbumsViewController.AlbumThumbnailSize1.Width, GMAlbumsViewController.AlbumGradientHeight);
			_gradientView = new UIView (gradientFrame) { 
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin,
				TranslatesAutoresizingMaskIntoConstraints = true,
				Hidden = true
			};
			var topGradient = UIColor.FromRGBA (0f, 0f, 0f, 0f);
			var midGradient = UIColor.FromRGBA (0f, 0f, 0f, 0.33f);
			var botGradient = UIColor.FromRGBA (0f, 0f, 0f, 0.75f);
			_gradient = new CAGradientLayer () {
				Frame = _gradientView.Bounds,
				Colors = new[] {topGradient.CGColor, midGradient.CGColor, botGradient.CGColor},
				Locations = new NSNumber[] {0.0f, 0.5f, 1.0f},
			};
			_gradientView.Layer.InsertSublayer (_gradient, 0);
			ImageView1.AddSubview (_gradientView);

			// VideoIcon
			_videoIcon = new UIImageView {
				ContentMode = UIViewContentMode.ScaleAspectFill,
				Frame = new CGRect(3, GMAlbumsViewController.AlbumThumbnailSize1.Height - 4 - 8, 15, 8),
				Image = UIImage.FromBundle("GMVideoIcon"), // todo: check current bundle
				ClipsToBounds = true,
				TranslatesAutoresizingMaskIntoConstraints = true,
				AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin,
				Hidden = false
			};
			ImageView1.AddSubview (_videoIcon);

			// TextLabels
			TextLabel.Font = UIFont.SystemFontOfSize(17.0f);
			TextLabel.Lines = 1;
			TextLabel.TranslatesAutoresizingMaskIntoConstraints = false;
			TextLabel.AdjustsFontSizeToFitWidth = true;

			DetailTextLabel.Font = UIFont.SystemFontOfSize(17.0f);
			DetailTextLabel.Lines = 1;
			DetailTextLabel.TranslatesAutoresizingMaskIntoConstraints = false;
			DetailTextLabel.AdjustsFontSizeToFitWidth = true;

			// Set next text labels constraints :
			ContentView.AddConstraints (
				NSLayoutConstraint.FromVisualFormat("H:[imageView1]-(offset)-[textLabel]-|", 
					NSLayoutFormatOptions.DirectionLeadingToTrailing,
					new NSDictionary("offset", GMAlbumsViewController.AlbumImageToTextSpace),
					new NSDictionary("textLabel", TextLabel, "imageView1", ImageView1))
			);

			ContentView.AddConstraints (
				NSLayoutConstraint.FromVisualFormat("H:[imageView1]-(offset)-[detailTextLabel]-|", 
					NSLayoutFormatOptions.DirectionLeadingToTrailing,
					new NSDictionary("offset", GMAlbumsViewController.AlbumImageToTextSpace),
					new NSDictionary("detailTextLabel", DetailTextLabel, "imageView1", ImageView1))
			);

			ContentView.AddConstraints (new [] {
				NSLayoutConstraint.Create(TextLabel, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, TextLabel.Superview, NSLayoutAttribute.CenterY, 1f, 0f)
			});

			ContentView.AddConstraints (new [] {
				NSLayoutConstraint.Create(DetailTextLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, TextLabel.Superview, NSLayoutAttribute.CenterY, 1f, 4f)
			});
		}

		public void SetVideoLayout(bool isVideo)
		{
			_videoIcon.Hidden = !isVideo;
			_gradientView.Hidden = !isVideo;
		}

		public override void SetSelected (bool selected, bool animated)
		{
			base.SetSelected (selected, animated);

			// Configure the view for the selected state
		}
	}
}

