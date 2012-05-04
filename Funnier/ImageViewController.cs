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
            
#if DEBUG
            Console.WriteLine("Image controller view did load");
#endif
            
            scrollView = new PagingScrollView();
            
            
//            UIScreen.MainScreen.Bounds
            
            scrollView.Frame = UIScreen.MainScreen.Bounds; //new RectangleF(0, 0, View.Frame.Width, View.Frame.Height);
            
            View.AddSubview(scrollView);
            
            dataSource.Added += PhotosAdded;
            if (dataSource.Photos.Count > 0) {
                scrollView.DataSource = new DataSource(dataSource.Photos);
            }
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
            private readonly List<PhotoImage> photos;
            public event Changed OnChanged;
            
            public DataSource(ICollection<PhotoInfo> photos) {
                this.photos = new List<PhotoImage>();
                
                AddPhotos(photos);
            }
            
            public int Count { 
                get {
                    return photos.Count;
                }
            }
        
            public void AddPhotos(ICollection<PhotoInfo> newPhotos) {
                NetworkStatus status = Reachability.RemoteHostStatus();
                foreach (PhotoInfo p in newPhotos) {
                    // on a cache miss, only download if a wifi network is available
                    NSData data = FileCacher.LoadUrl(p.Url, NetworkStatus.ReachableViaWiFiNetwork == status);
                    if (null == data) {
                        Console.WriteLine("Image was null.  Network status: {0}", status);
                    } else {
                        var image = UIImage.LoadFromData (data);
                        this.photos.Add(new PhotoImage(p, image));
                    }
                }
                if (null != OnChanged) {
                    OnChanged();
                }
            }
            
            public UIView GetView(int index) {
                return new CaptionedImage(photos[index].image, photos[index].photo.Caption);
            }
            
            private class PhotoImage {
                internal readonly PhotoInfo photo;
                internal readonly UIImage image;
                
                public PhotoImage(PhotoInfo photo, UIImage image) {
                    this.photo = photo;
                    this.image = image;
                }
            }
        }
    }
}

