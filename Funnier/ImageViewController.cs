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
            
            scrollView = new PagingScrollView();
            
            
//            UIScreen.MainScreen.Bounds
            
            scrollView.Frame = UIScreen.MainScreen.Bounds; //new RectangleF(0, 0, View.Frame.Width, View.Frame.Height);
            
            View.BackgroundColor = UIColor.White;
            scrollView.BackgroundColor = UIColor.Clear;
            View.AddSubview(scrollView);
            
            scrollView.OnScroll += delegate() {
                SetToolbarHidden(true);
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
        }
        
        private UIBarButtonItem GetFirstImageButton() {
            return new UIBarButtonItem(UIBarButtonSystemItem.Rewind, 
                delegate {
                    scrollView.ScrollView.ContentOffset = new PointF(0f, 0f);
                });
        }
        
        private UIBarButtonItem GetLastImageButton() {
            return new UIBarButtonItem(UIBarButtonSystemItem.FastForward, 
                delegate {
                    scrollView.ScrollView.ContentOffset = new PointF((dataSource.Photos.Count - 1) * View.Frame.Width, 0f);
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
        }
        
        public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
        {
            // Return true for supported orientations
            return true; // (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
        }
        
        public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            SizeF newSize = UIScreen.MainScreen.Bounds.Size; //View.Frame.Size;
            
            if (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft ||
                        toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight) {
                newSize = new SizeF(newSize.Height, newSize.Width);
            }
            View.Frame = new RectangleF(0, 0, newSize.Width, newSize.Height);
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
                ThreadPool.QueueUserWorkItem(delegate {
                    FetchImages();
                });
                if (null != OnChanged) {
                    OnChanged();
                }                 
            }
            
            private void FetchImages() {
                NetworkStatus status = Reachability.RemoteHostStatus();
                foreach (PhotoInfo p in this.photos) {
                    // on a cache miss, only download if a wifi network is available
                    // this just warms the cache.  The CaptionImage will use it later
                    NSData data = FileCacher.LoadUrl(p.Url, NetworkStatus.ReachableViaWiFiNetwork == status);

                    if (null == data) {                        
                        Debug.WriteLine("Image was null.  Network status: {0}", status);
                    }
                }
            }
            
            public UIView GetView(int index) {
                return new CaptionedImage(photos[index]);
            }
        }
    }
}

