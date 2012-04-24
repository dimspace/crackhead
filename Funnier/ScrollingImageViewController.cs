using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using FlickrNet;

namespace Funny
{
    public partial class ScrollingImageViewController : UIViewController
    {
        private float currentImageIndex;
//        private UIScrollView scrollView;
        private Flickr flickr;
        private PhotosetPhotoCollection photos;
        private readonly List<UIImageView> imageViews = new List<UIImageView>();
        
        static bool UserInterfaceIdiomIsPhone {
            get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
        }

        public ScrollingImageViewController (IntPtr handle) : base (handle)
        {
        }
        
        #region View lifecycle
        
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            flickr = new Flickr (FlickrAuth.apiKey, FlickrAuth.sharedSecret);

            scrollView.PagingEnabled = true;
            scrollView.ScrollEnabled = true;
            
            scrollView.DirectionalLockEnabled = true;
            scrollView.ShowsVerticalScrollIndicator = false;
            scrollView.ShowsHorizontalScrollIndicator = false;
            scrollView.MaximumZoomScale = 2.0f;
            scrollView.MinimumZoomScale = 1.0f;

            scrollView.BackgroundColor = UIColor.White;
            scrollView.AutosizesSubviews = true;
            
            scrollView.Delegate = new ScrollViewDelegate(this);
            
            RectangleF bounds =  UIScreen.MainScreen.Bounds;
            lblCaption.Frame = new RectangleF(0, bounds.Height - 100, bounds.Width, 100);
            
            
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
            
            // compute the index of the current image
            currentImageIndex = GetImageIndex();
        }
        
        private int GetImageIndex() {
            float index = scrollView.ContentOffset.X / (scrollView.ContentSize.Width / imageViews.Count);
            return (int)index;
        }
        
        public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate (fromInterfaceOrientation);
            
            float x = 0;
            RectangleF bounds =  UIScreen.MainScreen.Bounds;
            
            // in landscape flip height and width
            if (InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || 
                    InterfaceOrientation == UIInterfaceOrientation.LandscapeRight) {
                bounds = new RectangleF(0, 0, bounds.Height, bounds.Width);
            }
                        
            foreach (UIImageView iv in imageViews) {
                iv.Frame = new RectangleF(x, 0, bounds.Width, bounds.Height);
                x += bounds.Width;
            }

            scrollView.ContentOffset = new PointF(currentImageIndex * bounds.Width, 0);
            scrollView.ContentSize = new SizeF(x, bounds.Height);
            
            PositionCaption((int)currentImageIndex);
//            scrollView.LayoutSubviews();
        }
        
        private void PositionCaption(int imageIndex) {
            UIImageView imageView = imageViews[imageIndex];
            var imageFrame = imageView.Frame;
            lblCaption.Frame = new RectangleF(5, imageFrame.Height, 
                                                      lblCaption.Frame.Width, lblCaption.Frame.Height);
            scrollView.BringSubviewToFront(lblCaption);
        }
        
        private void ImagesLoaded(PhotosetPhotoCollection photoset) {
            imageViews.Clear();
            
            if (photoset.Count > 0) {
                photos = photoset;
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
                    
                    var frame = imageView.Frame;                    
                    imageView.Frame = new RectangleF(x, 0, 
                                                     Math.Min(frame.Width, bounds.Width),
                                                     Math.Min(frame.Height, bounds.Height));
                    
                    x += bounds.Width;
                    imageViews.Add(imageView);
                }
                
                InvokeOnMainThread (delegate {
                    PositionCaption(0);
                    lblCaption.Text = photoset[0].Title;
                    scrollView.AddSubviews(imageViews.ToArray());
                    scrollView.ContentSize = new SizeF(x, bounds.Height);
                });
                
            }
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
    
        private class ScrollViewDelegate : UIScrollViewDelegate {
            private readonly ScrollingImageViewController controller;
            internal ScrollViewDelegate(ScrollingImageViewController controller) {
                this.controller = controller;
            }
            
            public override void Scrolled (UIScrollView scrollView)
            {
                int index = controller.GetImageIndex();
                if (index > 0 && index < controller.imageViews.Count) {
                    var photo = controller.photos[index];
                    controller.lblCaption.Text = photo.Title;
                }
            }
        }
    }
}


