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
using Photos;
using CoreAnimation;
using CoreGraphics;
using Foundation;

namespace GMImagePicker
{
	internal class GMGridViewCell: UICollectionViewCell
	{
		public PHAsset Asset { get; private set; }

		//The imageView
		public UIImageView ImageView { get; private set; }
		//Video additional information
		private UIImageView _videoIcon;
		private UILabel _videoDuration;
		private UIView _gradientView;
		private CAGradientLayer _gradient;
		//Selection overlay
		public bool ShouldShowSelection { get; set; }
		private UIView _coverView;
		private UIButton _selectedButton;

		public bool IsEnabled { get; set; }

		private static UIFont TitleFont = UIFont.SystemFontOfSize(12f);
		private static float TitleHeight = 20.0f;
		private static UIColor TitleColor = UIColor.White;

		public GMGridViewCell (IntPtr handle): base (handle)
		{
			Initialize ();
		}

		[Export("initWithFrame:frame")]
		public GMGridViewCell (CGRect frame) : base(frame)
		{
			Console.WriteLine ("initWithFrame:frame");
			Initialize ();
		}
			
		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			ContentView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			ContentView.TranslatesAutoresizingMaskIntoConstraints = true;
		}
			
		public void Initialize () 
		{
			if (ImageView != null) {
				return;
			}

			Opaque = false;
			IsEnabled = true;

			var cellSize = ContentView.Bounds.Size.Width;

			// The image view
			ImageView = new UIImageView
			{
				Frame = new CGRect(0, 0, cellSize, cellSize),
				ContentMode = UIViewContentMode.ScaleAspectFill,
				ClipsToBounds = true,
				TranslatesAutoresizingMaskIntoConstraints = false,
				AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth
			};
			AddSubview (ImageView);

			// The video gradient, label & icon
			var x_offset = 4.0f;
			_gradientView = new UIView (new CGRect (0, Bounds.Size.Height - TitleHeight, Bounds.Size.Width, TitleHeight)) {
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin,
				TranslatesAutoresizingMaskIntoConstraints = false,
				Hidden = true
			};

			var topGradient = UIColor.FromRGBA (0, 0, 0, 0);
			var botGradient = UIColor.FromRGBA (0, 0, 0, 0.8f);
			_gradient = new CAGradientLayer {
				Frame = _gradientView.Bounds,
				Colors = new [] { topGradient.CGColor, botGradient.CGColor }
			};
			_gradientView.Layer.AddSublayer (_gradient);

			_videoIcon = new UIImageView (new CGRect(x_offset, Bounds.Size.Height - TitleHeight, Bounds.Size.Width - (2 * x_offset), TitleHeight)) {
				ContentMode = UIViewContentMode.Left,
				Image = UIImage.FromBundle("GMVideoIcon"),
				TranslatesAutoresizingMaskIntoConstraints = false,
				AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleWidth,
				Hidden = true
			};
			AddSubview (_videoIcon);

			_videoDuration = new UILabel {
				Font = TitleFont,
				TextColor = TitleColor,
				TextAlignment = UITextAlignment.Right,
				Frame = new CGRect(x_offset, Bounds.Size.Height - TitleHeight, Bounds.Size.Width - (2 * x_offset), TitleHeight),
				ContentMode = UIViewContentMode.Right,
				TranslatesAutoresizingMaskIntoConstraints = false,
				AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleWidth,
				Hidden = true
			};
			AddSubview (_videoDuration);

			// Selection overlay & icon
			_coverView = new UIView (Bounds) {
				TranslatesAutoresizingMaskIntoConstraints = false,
				AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth,
				BackgroundColor = UIColor.FromRGBA(0.24f, 0.47f, 0.85f, 0.6f),
				Hidden = true
			};
			AddSubview (_coverView);

			_selectedButton = new UIButton (UIButtonType.Custom) {
				Frame = new CGRect(2 * Bounds.Size.Width / 3, 0 * Bounds.Size.Width / 3, Bounds.Size.Width / 3, Bounds.Size.Width / 3),
				ContentMode = UIViewContentMode.TopRight,
				AdjustsImageWhenHighlighted = false,
				TranslatesAutoresizingMaskIntoConstraints = false,
				AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth,
				Hidden = false,
				UserInteractionEnabled = false
			};
			_selectedButton.SetImage (null, UIControlState.Normal);
			_selectedButton.SetImage (UIImage.FromBundle ("GMSelected"), UIControlState.Selected);
			AddSubview (_selectedButton);

			// Note: the views above are created in case this is toggled per cell, on the fly, etc.!
			ShouldShowSelection = true;
		}

		// Required to resize the CAGradientLayer because it does not support auto resizing.
		public override void LayoutSubviews ()
		{
			base.LayoutSubviews ();
			_gradient.Frame = _gradientView.Bounds;
		}

		public void Bind (PHAsset asset)
		{
			this.Asset = asset;

			if (Asset.MediaType == PHAssetMediaType.Video) {
				_videoIcon.Hidden = false;
				_videoDuration.Hidden = false;
				_gradientView.Hidden = false;
				_videoDuration.Text = GetDurationWithFormat (Asset.Duration);
			} else {
				_videoIcon.Hidden = true;
				_videoDuration.Hidden = true;
				_gradientView.Hidden = true;
			}
		}

		public override bool Selected {
			get {
				return base.Selected;
			}
			set {
				base.Selected = value;
				if (!this.ShouldShowSelection) {
					return;
				}

				_coverView.Hidden = !value;
				_selectedButton.Selected = value;
			}
		}

		private string GetDurationWithFormat (double duration)
		{
			var seconds = duration % 60;
			var minutes = (duration / 60) % 60;
			return string.Format ("{0:00}:{1:00}", minutes, seconds);
		}
	}
}

