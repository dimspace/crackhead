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
using FlickrCache;
using MonoTouchUtils;

/// <summary>
/// Author: Saxon D'Aubin
/// </summary>
namespace Funnier
{
    public partial class ImageViewController : UIViewController
    {
        private const string NoConnectionMessage = 
            "Sorry, but there's no connection available to download cartoons.  Please try again with a wifi connection.";
        private PagingScrollView scrollView;
        private readonly PhotosetCache dataSource;
        
        public ImageViewController(IntPtr handle) : base (handle)
        {
            dataSource = FlickrDataSource.Get().PhotosetCache;
        }
        
        private void PhotoAdded(PhotosetPhoto photo) {
            InvokeOnMainThread (delegate {
                lblLoadingMessage.Hidden = true;
                (scrollView.DataSource as DataSource).AddPhoto(photo);
            });
        }
        
        public override void DidReceiveMemoryWarning ()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning ();
            
            // Release any cached data, images, etc that aren't in use.
            scrollView.FreeUnusedViews();
        }

        private void CheckConnectionAndDisplayMessage ()
        {
            // when a device on a cell network first starts our app, it's often told there's no connection
            // only to get a reachability notification a moment later.  because of that, we delay our 
            // "No connection" message for a moment to make sure that's actually the case
            if (NetworkStatus.NotReachable == Reachability.RemoteHostStatus()) {
                var ev = new AutoResetEvent(false);
                ThreadPool.RegisterWaitForSingleObject(ev, delegate(Object obj, bool timedOut) {
                        if (NetworkStatus.NotReachable == Reachability.RemoteHostStatus()) {
                            // ok, we really don't have a connection
                            InvokeOnMainThread(delegate {
                                lblLoadingMessage.Text = NoConnectionMessage;
                            });
                        }
                    }, 
                    null, TimeSpan.FromSeconds(3), true);
            }

        }

        private void NoticeImagesChanged(int totalCount, int recentlyArrivedCount, int recentlyDownloadedCount) {
            if (recentlyArrivedCount == 0) return;

            // we only want to display a message with the initial download
            dataSource.ImagesChanged -= NoticeImagesChanged;

            string message;
            if (recentlyDownloadedCount == recentlyArrivedCount) {
                message = String.Format("{0} new cartoon{1} arrived.", 
                                            recentlyArrivedCount, recentlyArrivedCount > 1 ? "s" : "");
            } else {
                message = String.Format(
                    "{0} new cartoon{1} arrived.  {2} were downloaded.  The rest will be downloaded when a wifi connection is available", 
                    recentlyArrivedCount, (recentlyDownloadedCount > 1 ? "s" : ""), recentlyDownloadedCount);
            }

            InvokeOnMainThread(delegate {
                SendNotification(message, recentlyArrivedCount); 
                lblLoadingMessage.Text = message;
            });
        }

        private void NoticeMessages(string message) {
            InvokeOnMainThread(delegate {
                Debug.WriteLine("ImageViewController received message: {0}", message);
                lblLoadingMessage.Text = message;
                View.SetNeedsDisplay();
            });
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
                    dataSource.LastViewedImageIndex = scrollView.GetCurrentViewIndex();
                });
                
            };
            
            dataSource.Added += PhotoAdded;
            dataSource.ImagesChanged += NoticeImagesChanged;
            dataSource.Messages += NoticeMessages;

            scrollView.DataSource = new DataSource(dataSource.Photos);
            if (dataSource.Photos.Length == 0) {
                CheckConnectionAndDisplayMessage();
            } else {
                lblLoadingMessage.Hidden = true;
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

            FlickrDataSource.Get().FetchCartoonsIfConnected();
        }
        
        public override void ViewWillAppear (bool animated)
        {
            Debug.WriteLine("ImageViewController.ViewWillAppear");
            base.ViewWillAppear (animated);
            var lastViewedIndex = dataSource.LastViewedImageIndex;
            if (lastViewedIndex > 0) {
                scrollView.ScrollToView(lastViewedIndex);
            }
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
                    scrollView.ScrollToView(dataSource.Photos.Length - 1);
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
            dataSource.Added -= PhotoAdded;
            dataSource.ImagesChanged -= NoticeImagesChanged;
            dataSource.Messages -= NoticeMessages;

            ReleaseDesignerOutlets ();
            Debug.WriteLine("View unload");
        }
        
        public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
        {
            return true;
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

        private void SendNotification(string message, int count) {
            Debug.WriteLine("Sending notification: {0}", message);
            UILocalNotification notification = new UILocalNotification{
                    FireDate = DateTime.Now,
                    TimeZone = NSTimeZone.LocalTimeZone,
                    AlertBody = message,
                    RepeatInterval = 0,
                    ApplicationIconBadgeNumber = count
                };
            UIApplication.SharedApplication.InvokeOnMainThread(delegate {
                UIApplication.SharedApplication.ScheduleLocalNotification(notification);
            });
        }
        
        private class DataSource : PagingViewDataSource {
            private readonly List<PhotosetPhoto> photos;
            public event Changed OnChanged;
            
            public DataSource(PhotosetPhoto[] photos) {
                this.photos = new List<PhotosetPhoto>(photos);
                if (null != OnChanged) { OnChanged(); }
            }
            
            public int Count { 
                get {
                    return photos.Count;
                }
            }
        
            public void AddPhoto(PhotosetPhoto newPhoto) {
                this.photos.Add(newPhoto);
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

