//
//  TranslationExtensions.cs
//  GMPhotoPicker.Xamarin
//
//  Created by Roy Cornelissen on 23/03/16.
//  Based on original GMImagePicker implementation by Guillermo Muntaner Perelló.
//  https://github.com/guillermomuntaner/GMImagePicker
//

using Foundation;

namespace GMImagePicker
{
    [Register ("DummyClass")]
	internal class DummyClass: NSObject
	{
	}

	internal static class TranslationExtensions
	{
		public static string Translate(this string translate, string defaultValue = "*NO TRANSLATION*")
		{
			var bundleClass = new DummyClass ().Class;
			var languageBundle = NSBundle.FromClass (bundleClass);
			var translatedString = languageBundle.GetLocalizedString(translate, defaultValue, "GMImagePicker");
			return translatedString;
		}
	}
}

