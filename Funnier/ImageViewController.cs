using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using FlickrNet;

namespace Funny
{
    public partial class ImageViewController : UIViewController
    {
        private PagingScrollView scrollView;
        private readonly Flickr flickr;
        
        public ImageViewController(IntPtr handle) : base (handle)
        {
            flickr = new Flickr (FlickrAuth.apiKey, FlickrAuth.sharedSecret);
        }
        
        public override void DidReceiveMemoryWarning ()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning ();
            
            // Release any cached data, images, etc that aren't in use.
        }
        
        public override void SetValueForKey (NSObject value, NSString key)
        {
            try {
                base.SetValueForKey (value, key);
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }
        
        static bool Network {
            set {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = value;
            }
        }
        
        
        private void StartLoading() {

            ThreadPool.QueueUserWorkItem(
                delegate {
                    try {
                        Network = true;
                        var photos = flickr.PhotosetsGetPhotos("72157629877473203");
                        Network = false;
                        InvokeOnMainThread (delegate {
                            scrollView.DataSource = new DataSource(photos);
                        });
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
//                        Network = false;
                    }
                });
        }
        
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            
//            View.AutosizesSubviews = true;
            
            scrollView = new PagingScrollView();
            
            
//            UIScreen.MainScreen.Bounds
            
            scrollView.Frame = UIScreen.MainScreen.Bounds; //new RectangleF(0, 0, View.Frame.Width, View.Frame.Height);
            
            View.AddSubview(scrollView);
            StartLoading();
        }
        
        public override void ViewDidUnload ()
        {
            base.ViewDidUnload ();
            
            // Clear any references to subviews of the main view in order to
            // allow the Garbage Collector to collect them sooner.
            //
            // e.g. myOutlet.Dispose (); myOutlet = null;
            
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
            private readonly List<PhotoImage> photos;
            public DataSource(PhotosetPhotoCollection photos) {
                this.photos = new List<PhotoImage>();
                
                for (int i = 0; i < photos.Count; i++) {
                    Photo p = photos[i];
                    NSData data = FileCacher.LoadUrl(p.MediumUrl);
                    var image = UIImage.LoadFromData (data);
                    this.photos.Add(new PhotoImage(p, image));
                }
            }
            
            public int Count { 
                get {
                    return photos.Count;
                }
            }
        
            
            public UIView GetView(int index) {
                return new CaptionedImage(photos[index].image, photos[index].photo.Title);
            }
            
            private class PhotoImage {
                internal readonly Photo photo;
                internal readonly UIImage image;
                
                public PhotoImage(Photo photo, UIImage image) {
                    this.photo = photo;
                    this.image = image;
                }
            }
        }
    }
}

