GMImagePicker.Xamarin
=====================
[![Build Status](https://www.bitrise.io/app/06bf5373d7948e25.svg?token=hvo0-OutqWYMxWNN9al50w&branch=master)](https://www.bitrise.io/app/06bf5373d7948e25)

An image & video picker supporting multiple selection and several customizations. Powered by the iOS 8 **Photo framework**.

This is a 1-to-1 port of the origina GMImagePicker component made by Guillermo Muntaner from Objective-C to C# for Xamarin.iOS.
The original source can be found here: [github.com/guillermomuntaner/GMImagePicker](https://github.com/guillermomuntaner/GMImagePicker).
This port is published with kind permission from Guillermo Muntaner.

### Screenshots

![Screenshot](GMImagePickerDemo.gif "Screenshot")  

### Features
1. Allows selection of multiple photos and videos, even from different albums.
2. Optional single selection mode.
3. Optional camera access.
4. Optional bottom toolbar with information about users selection.
5. Full and customizable acces to smart collections(**Favorites**, **Slo-mo** or **Recently deleted**). 
6. Filter by collections & albums.
7. Filter by media type.
8. Customizable colors, fonts and labels to ease branding of the App.
9. By default mimics UIImagePickerController in terms of features, appearance and behaviour.
10. Dynamically sized grid view, easy to customize and fully compatible with iPhone 6/6+ and iPad.
11. Works in landscape orientation and allow screen rotation!
12. It can be used as Popover on iPad, with customizable size.
13. Fast & small memory footprint powered by PHCachingImageManager.
14. Full adoption of new iOS8 **PhotoKit**. Returns and array of PHAssets.


## Usage

#### Installation

###### Manually 
Clone or download solution and use GMImagePicker.Xamarin csproj in your solution.

###### Nuget 
Get GMImagePicker.Xamarin package from Nuget and add it to your iOS application project.

#### Initialize the picker, hook up events and present it
```` csharp
var picker = new GMImagePickerController ();
picker.FinishedPickingAssets += (sender, args) => { 
    Console.WriteLine ("User finished picking assets. {0} items selected.", args.Assets.Length); 
};
await PresentViewControllerAsync (picker, true);
````

You can also implement optional an optional event handler for the `Canceled` event
```` csharp
picker.Canceled += (sender, args) { Console.WriteLine ("user canceled picking assets"); };
````

#### Customization
Before presenting the picker, you can customize some of its properties
```` csharp
...
//Display or not the selection info Toolbar:
picker.DisplaySelectionInfoToolbar = true;

//Display or not the number of assets in each album:
picker.DisplayAlbumsNumberOfAssets = true;

//Customize the picker title and prompt (helper message over the title)
picker.Title = "Custom title";
picker.CustomNavigationBarPrompt = "Custom helper message!";

//Customize the number of cols depending on orientation and the inter-item spacing
picker.ColsInPortrait = 3;
picker.ColsInLandscape = 5;
picker.MinimumInteritemSpacing = 2.0f;

//You can pick the smart collections you want to show:
picker.CustomSmartCollections = new [] { PHAssetCollectionSubtype.AlbumRegular, PHAssetCollectionSubtype.AlbumImported };

//Disable multiple selecion
picker.AllowsMultipleSelection = false;

//Show a promt to confirm single selection
picker.ConfirmSingleSelection = true;
picker.ConfirmSingleSelectionPrompt = "Do you want to select the image you have chosen?";

//Camera integration
picker.ShowCameraButton = true;
picker.AutoSelectCameraImages = true;

//Select the media types you want to show and filter out the rest
picker.MediaTypes = new [] { PHAssetMediaType.Image };

//UI color & text customizations
picker.PickerBackgroundColor = UIColor.Black;
picker.PickerTextColor = UIColor.White;
picker.ToolbarBarTintColor = UIColor.DarkGray;
picker.ToolbarTextColor = UIColor.White;
picker.ToolbarTintColor = UIColor.Red;
picker.NavigationBarBackgroundColor = UIColor.Black;
picker.NavigationBarTextColor = UIColor.White;
picker.NavigationBarTintColor = UIColor.Red;
picker.PickerFontName = "Verdana";
picker.PickerBoldFontName = "Verdana-Bold";
picker.PickerFontNormalSize = 14.0f;
picker.PickerFontHeaderSize = 17.0f;
picker.PickerStatusBarStyle = UIStatusBarStyle.LightContent;
picker.UseCustomFontForNavigationBar = true;
...
````


## Use it as popover on iPad
Also works as Popover on the iPad! (with customizable size)

![Screenshot](ipad.jpg "Screenshot")

This code works in both iPhone & iPad
```` csharp
...
var picker = new GMImagePickerController ();

picker.Title = "Custom title";
picker.CustomNavigationBarPrompt = "Custom helper message!";
picker.ColsInPortrait = 3;
picker.ColsInLandscape = 5;
picker.MinimumInteritemSpacing = 2.0f;
picker.ModalPresentationStyle = UIModalPresentation.Popover;

var popPC = picker.PopoverPresentationController;
popPC.PermittedArrowDirections = UIPopoverArrowDirection.Any;
popPC.SourceView = _gmImagePickerButton;
popPC.SourceRect = _gmImagePickerButton.Bounds;

ShowViewController(picker, null);
````


#### Minimum Requirement
Xamarin Studio / Visual Studio, Xamarin.iOS and iOS 8+


### License

The MIT License (MIT)

Copyright (c) 2016 Roy Cornelissen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


