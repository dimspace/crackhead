using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using FlickrNet;

namespace Funny
{
    public partial class ImageViewController : UIViewController
    {
        private PagingScrollView scrollView;
        private readonly FlickrDataSource dataSource;
        private float toolbarHeight;
        
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
            
            scrollView = new PagingScrollView(View.Bounds);
            // set our scroll view to automatically resize on rotation
            scrollView.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
   
            // figure out the height of the toolbar
            toolbarHeight = UIScreen.MainScreen.Bounds.Width == View.Bounds.Width ?
                UIScreen.MainScreen.Bounds.Height - View.Bounds.Height :
                UIScreen.MainScreen.Bounds.Width - View.Bounds.Height;

            View.BackgroundColor = UIColor.White;
            scrollView.BackgroundColor = UIColor.Clear;
            View.AddSubview(scrollView);
            
            scrollView.OnScroll += delegate(int index) {
                SetToolbarHidden(true);
                FlickrDataSource.Get().LastViewedImageIndex = index;
            };
            
            dataSource.Added += PhotosAdded;
            if (dataSource.Photos.Count > 0) {
                scrollView.DataSource = new DataSource(dataSource.Photos);
            }
            
            scrollView.AddGestureRecognizer(new UITapGestureRecognizer(this, new MonoTouch.ObjCRuntime.Selector("tapToggleToolbar")));
            toolbar.SetItems(new UIBarButtonItem[] { 
                GetFirstImageButton(), 
                new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
                GetLastImageButton() }, false);
            View.BringSubviewToFront(toolbar);
            
            int lastViewedIndex = FlickrDataSource.Get().LastViewedImageIndex;
            if (lastViewedIndex > 0) {
                scrollView.ScrollToView(lastViewedIndex);
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
        
        public override void ViewWillUnload ()
        {
            base.ViewWillUnload ();
            Debug.WriteLine("ViewWillUnload");
        }
        
        public override void ViewWillDisappear (bool animated)
        {
            base.ViewWillDisappear (animated);
            Debug.WriteLine("ViewWillDisappear");
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
        
        public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            var newSize = new SizeF(View.Bounds.Height + toolbarHeight, View.Bounds.Width - toolbarHeight);
            
            Debug.WriteLine("Before rotate {0}", scrollView.Frame);
            
            scrollView.Resize(newSize, duration);
                        
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

