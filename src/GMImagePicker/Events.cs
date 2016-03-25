using System;
using UIKit;
using Photos;
using CoreGraphics;

namespace GMImagePicker
{
	/// <summary>
	/// Contains the context assets for the event.
	/// </summary>
	public class MultiAssetEventArgs: EventArgs
	{
		public MultiAssetEventArgs(PHAsset[] assets)
		{
			Assets = assets;
		}

		/// <summary>
		/// The context assets for this event.
		/// </summary>
		public PHAsset[] Assets { get; private set; }
	}

	/// <summary>
	/// Contains the context asset for the event.
	/// </summary>
	public class SingleAssetEventArgs: EventArgs
	{
		public SingleAssetEventArgs (PHAsset asset)
		{
			Asset = asset;
		}

		/// <summary>
		/// The context asset for this event.
		/// </summary>
		public PHAsset Asset { get; private set; }
	}

	/// <summary>
	/// EventArgs for events (actions) that can be canceled by the caller.
	/// </summary>
	public class CancellableAssetEventArgs: SingleAssetEventArgs
	{
		public CancellableAssetEventArgs(PHAsset asset) : base (asset) { }

		/// <summary>
		/// Set to <c>true</c> if the action must be canceled.
		/// </summary>
		public bool Cancel { get; set; }
	}
}