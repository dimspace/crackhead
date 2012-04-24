using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using FlickrNet;

namespace Funny
{
    public partial class FunnyViewController : UIViewController
    {
        private PointF rotateContentOffset;
        private UIScrollView scrollView;
        private Flickr flickr;
        private readonly List<UIImageView> imageViews = new List<UIImageView>();
        
        static bool UserInterfaceIdiomIsPhone {
            get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
        }

        public FunnyViewController (IntPtr handle) : base (handle)
        {
        }
        
        #region View lifecycle
        
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            flickr = new Flickr (FlickrAuth.apiKey, FlickrAuth.sharedSecret);

            scrollView = new UIScrollView();
            
            scrollView.PagingEnabled = true;
            scrollView.ScrollEnabled = true;
            
            scrollView.DirectionalLockEnabled = true;
            scrollView.ShowsVerticalScrollIndicator = false;
            scrollView.ShowsHorizontalScrollIndicator = false;
            scrollView.MaximumZoomScale = 2.0f;
            scrollView.MinimumZoomScale = 1.0f;
            scrollView.ContentMode = UIViewContentMode.TopLeft;

            scrollView.BackgroundColor = UIColor.White;
            scrollView.AutosizesSubviews = true;
            
            View = scrollView;
            
            StartLoading();
        }
        
        private void StartLoading() {

            ThreadPool.QueueUserWorkItem(
            delegate {
                try {
                    Network = true;
                    var photos = flickr.PhotosetsGetPhotos("72157629877473203");
                    Network = false;
                    ImagesLoaded(photos);
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    InvokeOnMainThread (delegate {
                        using (var alert = new UIAlertView ("Error", "While accessing Flickr - " + ex.Message, null, "Ok")) {
                            alert.Show ();
                        }
                    });
                }
            });
        }
        
        static bool Network {
            get {
                return UIApplication.SharedApplication.NetworkActivityIndicatorVisible;
            }
            set {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = value;
            }
        }
        
        public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            base.WillRotate (toInterfaceOrientation, duration);
            
            rotateContentOffset = scrollView.ContentOffset;
            scrollView.Hidden = true;
        }
        
        public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate (fromInterfaceOrientation);
            
//            scrollView.LayoutSubviews();
            float x = 0;
            float newOffset = 0;
            RectangleF bounds =  UIScreen.MainScreen.Bounds;
            
            if (InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || 
                    InterfaceOrientation == UIInterfaceOrientation.LandscapeRight) {
                bounds = new RectangleF(0, 0, bounds.Height, bounds.Width);
            }
            foreach (UIImageView iv in imageViews) {
                if (x == rotateContentOffset.X) {
                    newOffset = x;
                }
                iv.Frame = new RectangleF(x, 0, bounds.Width, bounds.Height);
                x += bounds.Width;
            }
            scrollView.ZoomScale = 1.0f;          
//            scrollView.ContentOffset = new PointF(newOffset, 0);
            scrollView.ContentSize = new SizeF(x, bounds.Height);
            scrollView.LayoutSubviews();
            scrollView.Hidden = false;
        }
        
        private void ImagesLoaded(PhotosetPhotoCollection photoset) {
            imageViews.Clear();            
            RectangleF bounds =  UIScreen.MainScreen.Bounds;
            float x = 0;
            
            foreach (Photo p in photoset) {
                Network = true;
                var data = NSData.FromUrl (new NSUrl (p.MediumUrl));
                var image = UIImage.LoadFromData (data);
                Network = false;
                
                var imageView = new UIImageView(image);
//                imageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight;
                imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
                
                imageView.Frame = new RectangleF(x, 0, bounds.Width, bounds.Height);
                x += bounds.Width;
                imageViews.Add(imageView);
            }
            
            InvokeOnMainThread (delegate {
                scrollView.AddSubviews(imageViews.ToArray());
            });
            
            scrollView.ContentSize = new SizeF(x, bounds.Height);
        }
        
        #endregion
        
        public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
        {
            // Return true for supported orientations
            if (UserInterfaceIdiomIsPhone) {
                return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
            } else {
                return true;
            }
        }
    }
}

