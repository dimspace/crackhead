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
        private float statusBarHeight;
        
        public ImageViewController(IntPtr handle) : base (handle)
        {
            dataSource = FlickrDataSource.Get();
        }
        
        private void PhotosAdded(List<PhotoInfo> photos) {
            InvokeOnMainThread (delegate {
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
            
            statusBarHeight = UIApplication.SharedApplication.StatusBarFrame.Height;
            
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
            if (dataSource.Photos.Count > 0) {
                scrollView.DataSource = new DataSource(dataSource.Photos);
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
            
            int lastViewedIndex = FlickrDataSource.Get().LastViewedImageIndex;
            if (lastViewedIndex > 0) {
                scrollView.ScrollToView(lastViewedIndex);
            }
        }
        
        public override void ViewWillAppear (bool animated)
        {
            base.ViewDidAppear (animated);
            Debug.WriteLine("ViewWillAppear {0}  {1}", scrollView.Bounds, View.Bounds);
            // hack to fix a bug in which the initial layout freaks if the device
            // is rotated to landscape before the controller appears
            if (View.Bounds.Width > View.Bounds.Height) {
                scrollView.Resize(View.Bounds.Size, 0);
            }
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
        
        public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            UIInterfaceOrientation fromOrientation = UIApplication.SharedApplication.StatusBarOrientation;
            
            // we only need to do fancy stuff when rotating portrait / landscape.
            // 180 degree rotations require nothing special
            if (IsLandscape(fromOrientation) != IsLandscape(toInterfaceOrientation)) {
                // We want to tell the scrollView what its new size will be after rotating and
                // we have to take the toolbar size into account.  Right now our height is smaller because it is reduced by the 
                // size of the toolbar, so add the toolbar height to the current height.  The current width will need to shrink
                // by the size of the toolbar.  Since this is a rotation, we're swapping width and height.                
                var newSize = new SizeF(View.Bounds.Height + statusBarHeight, View.Bounds.Width - statusBarHeight);
                
                Debug.WriteLine("Before rotate. {0} to {1}", scrollView.Frame.Size, newSize);
                
                scrollView.Resize(newSize, duration);
            }
                        
            base.WillRotate (toInterfaceOrientation, duration);
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

            public UIView GetView(int index) {
                return new CaptionedImage(photos[index]);
            }
        }
    }
}

