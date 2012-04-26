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
//        private PhotosetPhotoCollection photos;
        private readonly List<PhotoWithImage> photos = new List<PhotoWithImage>();
        
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
                    } catch (System.Net.WebException ex) {
                        Console.WriteLine(ex);
                        InvokeOnMainThread (delegate {
                            using (var alert = new UIAlertView ("Error", "While accessing Flickr - " + ex.Message, null, "Ok")) {
                                alert.Show ();
                            }
                        });                    
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex);
                        InvokeOnMainThread (delegate {
                            using (var alert = new UIAlertView ("Error", "While accessing Flickr - " + ex.Message, null, "Ok")) {
                                alert.Show ();
                            }
                        });
                    } finally {
                        Network = false;
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
            float index = scrollView.ContentOffset.X / (scrollView.ContentSize.Width / photos.Count);
            return (int)index;
        }
        
        private RectangleF GetBounds() {
            RectangleF bounds = UIScreen.MainScreen.Bounds;
            
            // in landscape flip height and width
            if (InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || 
                    InterfaceOrientation == UIInterfaceOrientation.LandscapeRight) {
                bounds = new RectangleF(0, 0, bounds.Height, bounds.Width);
            }
            return bounds;
        }
        
        public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate (fromInterfaceOrientation);
            
            RectangleF bounds = GetBounds();
   
            
            for (int i = 0; i < photos.Count; i++) {
                PhotoWithImage p = photos[i];
                p.Resize(new RectangleF(bounds.Width * i, 0, bounds.Width, bounds.Height));
            }

            scrollView.ContentOffset = new PointF(currentImageIndex * bounds.Width, 0);
            scrollView.ContentSize = new SizeF(bounds.Width * photos.Count, bounds.Height);
            
            
        }
        
        private void ImagesLoaded(PhotosetPhotoCollection photoset) {
            photos.Clear();
            
            if (photoset.Count > 0) {
                RectangleF bounds =  UIScreen.MainScreen.Bounds;
                
                for (int i = 0; i < photoset.Count; i++) {
                    Photo p = photoset[i];
                    NSData data = FileCacher.LoadUrl(p.MediumUrl);
                    
                    var image = UIImage.LoadFromData (data);
                                        
                    PhotoWithImage photoWithImage = new PhotoWithImage(p, image);
                    photos.Add(photoWithImage);
                    
                    if (i == 0) {
                        InvokeOnMainThread (delegate {
                    
                            scrollView.ContentSize = new SizeF(bounds.Width * 5, bounds.Height);
                            photoWithImage.Create(new RectangleF(0, 0, bounds.Width, bounds.Height), scrollView);
                        });
                    }
                }
                 
                InvokeOnMainThread (delegate {
                    
                    scrollView.ContentSize = new SizeF(bounds.Width * photos.Count, bounds.Height);
                    
                    // render first 3 images
                    for (int i = 0; i < Math.Min(photos.Count, 3); i++) {
                        float x = i * bounds.Width;
                        photos[i].Create(new RectangleF(x, 0, bounds.Width, bounds.Height), scrollView);
                    }
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
                RectangleF bounds =  controller.GetBounds();
                int index = controller.GetImageIndex();
                for (int i = index; i < Math.Min(controller.photos.Count, index + 3); i++) {
                    float x = i * bounds.Width;
                    controller.photos[i].Create(new RectangleF(x, 0, bounds.Width, bounds.Height), scrollView);
                }
                /*
                if (index > 0 && index < controller.imageViews.Count) {
                    var photo = controller.photos[index];
                    controller.lblCaption.Text = photo.Title;
                }*/
            }
        }
        
        private class PhotoWithImage {
            public Photo Photo {get; private set;}
            public volatile UIImageView imageView;
            public UILabel Caption {get; private set;}
            private SizeF originalImageSize;
            private readonly UIImage image;
            
            public PhotoWithImage(Photo photo, UIImage image) {
                this.Photo = photo;
                this.image = image;
            }
            
            public void Create(RectangleF bounds, UIScrollView scrollView) {
                if (null == imageView) {
                    imageView = new UIImageView(image);
                    this.originalImageSize = imageView.Frame.Size;
                    Caption = new UILabel();
                    
//Times New Roman  TimesNewRomanPS-ItalicMT
//Times New Roman  TimesNewRomanPS-BoldMT
//Times New Roman  TimesNewRomanPSMT
//Times New Roman  TimesNewRomanPS-BoldItalicMT
                    Caption.Font = UIFont.FromName("TimesNewRomanPSMT", 12);
                    
    //                imageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight;
                    scrollView.AddSubviews(imageView);
                    
                    imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
                    
                    
                                    
                    Caption.Text = Photo.Title;
    //                        lblCaption.SizeToFit();
                    Caption.Opaque = false;
                    Caption.ContentMode = UIViewContentMode.Center;
                    Caption.TextAlignment = UITextAlignment.Center;
                    
    //                        lblCaption.Center = new PointF(bounds.Width / 2, bounds.Height + lblCaption.Frame.Height);
                                    
                    scrollView.AddSubview(Caption);
                    Resize(bounds);
                }
            }
            
            private void PositionCaption() {
                var frame = imageView.Frame;
                frame = new RectangleF(frame.X + 5, frame.Height-40, 
                                                  frame.Width - 10, 50);
                Caption.Frame = frame;
            }
            
            public void Resize(RectangleF bounds) {
                if (null == imageView) {
                    return;
                }
                SizeF newImageBounds = new SizeF(Math.Min(bounds.Width, originalImageSize.Width),
                                                 Math.Min(bounds.Height, originalImageSize.Height));
    
                float newHeight = (newImageBounds.Width * originalImageSize.Height) / originalImageSize.Width;
                
                imageView.Frame = new RectangleF(bounds.X, 0, newImageBounds.Width, newHeight);
                PositionCaption();
            }
        }
    }
}


