//    Licensed to the Apache Software Foundation (ASF) under one
//    or more contributor license agreements.  See the NOTICE file
//    distributed with this work for additional information
//    regarding copyright ownership.  The ASF licenses this file
//    to you under the Apache License, Version 2.0 (the
//    "License"); you may not use this file except in compliance
//    with the License.  You may obtain a copy of the License at
//    
//     http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing,
//    software distributed under the License is distributed on an
//    "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//    KIND, either express or implied.  See the License for the
//    specific language governing permissions and limitations
//    under the License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using FlickrNet;

/// <summary>
/// Author: sdaubin
/// </summary>
namespace Funny
{
    public partial class ImageViewController : UIViewController
    {
        private PagingScrollView scrollView;
        private readonly FlickrDataSource dataSource;
        
        public ImageViewController(IntPtr handle) : base (handle)
        {
            dataSource = FlickrDataSource.Get();
        }
        
        private void PhotosAdded(PhotoInfo[] photos) {
            InvokeOnMainThread (delegate {
                lblLoadingMessage.Hidden = true;
                if (null == scrollView.DataSource) {
                    scrollView.DataSource = new DataSource(photos);
                } else {
                    (scrollView.DataSource as DataSource).AddPhotos(photos);
                }
//                            scrollView.DataSource = new DataSource(photos);
                
            });
        }
        
        public override void DidReceiveMemoryWarning ()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning ();
            
            // Release any cached data, images, etc that aren't in use.
            scrollView.FreeUnusedViews();
        }
        
        
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();            
            
            Debug.WriteLine("Image controller view did load");
            
            scrollView = new PagingScrollView(View.Bounds);
            // set our scroll view to automatically resize on rotation
            scrollView.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;

            View.BackgroundColor = UIColor.White;
            scrollView.BackgroundColor = UIColor.Clear;
            View.AddSubview(scrollView);
            
            scrollView.OnScroll += delegate {
                // clear the icon badge number.  In the future we might want to update it as cartoons are viewed
                UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
                
                SetToolbarHidden(true);
                // we have to do this on another thread because the scroll hasn't finished yet
                ThreadPool.QueueUserWorkItem(delegate {
                    FlickrDataSource.Get().LastViewedImageIndex = scrollView.GetCurrentViewIndex();
                });
                
            };
            
            dataSource.Added += PhotosAdded;
            dataSource.Messages += delegate(string message) {
                InvokeOnMainThread(delegate {
                    lblLoadingMessage.Text = message;
                    View.SetNeedsDisplay();
                });
            };
            if (dataSource.Photos.Count > 0) {
                lblLoadingMessage.Hidden = true;
                scrollView.DataSource = new DataSource(dataSource.Photos);
            } else if (NetworkStatus.NotReachable == Reachability.RemoteHostStatus()) {
                lblLoadingMessage.Text = "Sorry, but there's no connection available to download cartoons.  Please try again with a wifi connection.";
            }
            
            scrollView.AddGestureRecognizer(new UITapGestureRecognizer(this, new MonoTouch.ObjCRuntime.Selector("tapToggleToolbar")));
            var spacerButton = new UIBarButtonItem(UIBarButtonSystemItem.FixedSpace);
            spacerButton.Width = 5;
            toolbar.SetItems(new UIBarButtonItem[] {
//                spacerButton,
                GetFirstImageButton(), 
                new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
                GetLastImageButton(),
                spacerButton}, false);
            View.BringSubviewToFront(toolbar);
        }
        
        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);
            var lastViewedIndex = FlickrDataSource.Get().LastViewedImageIndex;
            scrollView.ScrollToView(lastViewedIndex);
            scrollView.FinishRotation(lastViewedIndex);
        }
        
        private UIBarButtonItem GetFirstImageButton() {
            return new UIBarButtonItem(UIBarButtonSystemItem.Rewind, 
                delegate {
                    scrollView.ScrollToView(0);
                });
        }
        
        private UIBarButtonItem GetLastImageButton() {
            return new UIBarButtonItem(UIBarButtonSystemItem.FastForward, 
                delegate {
                    Debug.WriteLine("Width {0}", View.Frame.Width);
                    scrollView.ScrollToView(dataSource.Photos.Count - 1);
                });
        }
        
        [Export("tapToggleToolbar")]
        private void Tap(UITapGestureRecognizer sender)
        {
            // animate showing and hiding the toolbar
            if (sender.State == UIGestureRecognizerState.Ended)
            {
                SetToolbarHidden(!toolbar.Hidden);
            }
        }
        
        private void SetToolbarHidden(bool hide) {
            if (hide == toolbar.Hidden) {
                return;
            }
            float height =  (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeLeft 
                             || UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeRight) ?
                            UIScreen.MainScreen.ApplicationFrame.Width : UIScreen.MainScreen.ApplicationFrame.Height;
            
            bool hidden = toolbar.Hidden;
            if (hidden) {
                toolbar.Frame = new RectangleF(toolbar.Frame.X, height, toolbar.Frame.Width, 
                                        toolbar.Frame.Height);
                toolbar.Hidden = false;
            }
            Debug.WriteLine("{0} toolbar, y = {1}", hidden ? "show" : "hide", height);
            
            UIView.Animate(0.2, 0, UIViewAnimationOptions.TransitionFlipFromBottom,
                delegate() {
                    float newY = height - (hidden ? toolbar.Frame.Height : 0);
                    toolbar.Frame = new RectangleF(toolbar.Frame.X, newY, 
                                        toolbar.Frame.Width, toolbar.Frame.Height);
                }, delegate() {
                    toolbar.Hidden = !hidden;
                }); 
        }

        public override void ViewDidUnload ()
        {
            base.ViewDidUnload ();
            
            // Clear any references to subviews of the main view in order to
            // allow the Garbage Collector to collect them sooner.
            //
            // e.g. myOutlet.Dispose (); myOutlet = null;
            
            // remove our event listener.  very important
            dataSource.Added -= PhotosAdded;
            ReleaseDesignerOutlets ();
            Debug.WriteLine("View unload");
        }
        
        public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
        {
            return true;
        }
        
        private static bool IsLandscape(UIInterfaceOrientation orientation) {
            return orientation == UIInterfaceOrientation.LandscapeRight || orientation == UIInterfaceOrientation.LandscapeLeft;
        }
        
        int? currentIndex;
        public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            currentIndex = scrollView.PrepareForRotation();
            base.WillRotate (toInterfaceOrientation, duration);
        }
        
        public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate (fromInterfaceOrientation);
            scrollView.FinishRotation(currentIndex);
        }
        
        private class DataSource : PagingViewDataSource {
            private readonly List<PhotoInfo> photos;
            public event Changed OnChanged;
            
            public DataSource(ICollection<PhotoInfo> photos) {
                this.photos = new List<PhotoInfo>();
                
                AddPhotos(photos);
            }
            
            public int Count { 
                get {
                    return photos.Count;
                }
            }
        
            public void AddPhotos(ICollection<PhotoInfo> newPhotos) {
                this.photos.AddRange(newPhotos);
                if (null != OnChanged) {
                    OnChanged();
                }
            }
        
            public void AddPhotos(PhotoInfo[] newPhotos) {
                this.photos.AddRange(newPhotos);
                if (null != OnChanged) {
                    OnChanged();
                }
            }

            public UIView GetView(int index) {
                var view = new CaptionedImage(photos[index]);
                view.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
                return view;
            }
        }
    }
}

