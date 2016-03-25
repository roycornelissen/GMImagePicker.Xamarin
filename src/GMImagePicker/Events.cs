//
//  Events.cs
//  GMPhotoPicker.Xamarin
//
//  Created by Roy Cornelissen on 23/03/16.
//  Based on original GMImagePicker implementation by Guillermo Muntaner Perelló.
//  https://github.com/guillermomuntaner/GMImagePicker
//

using System;
using Photos;

namespace GMImagePicker
{
    /// <summary>
    /// Contains the context assets for the event.
    /// </summary>
    public class MultiAssetEventArgs: EventArgs
	{
		internal MultiAssetEventArgs(PHAsset[] assets)
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
		internal SingleAssetEventArgs (PHAsset asset)
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
		internal CancellableAssetEventArgs(PHAsset asset) : base (asset) { }

		/// <summary>
		/// Set to <c>true</c> if the action must be canceled.
		/// </summary>
		public bool Cancel { get; set; }
	}
}