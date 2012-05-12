using System;
using System.Diagnostics;
using System.Collections.Generic;

using FlickrNet;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;

namespace Funny
{
    public delegate void PhotosAdded(List<PhotoInfo> photos);
    
    public class FlickrDataSource
    {
        private const string PhotosDefaultsKey = "Photos";
        private const string LastViewedImageIndexKey = "LastViewedImageIndex";
        /// <summary>
        /// Dictionary of photo ids to photos.
        /// </summary>
        /// 
        private readonly Dictionary<string, PhotoInfo> photos = new Dictionary<string, PhotoInfo>();
        private readonly Flickr flickr;
        private DateTime? lastPhotoFetchTimestamp;
        public int LastViewedImageIndex { get; set; }
        
        public event PhotosAdded Added;
        
        private readonly static FlickrDataSource singleton = new FlickrDataSource();
        public static FlickrDataSource Get() {
            return singleton;
        }
        
        private FlickrDataSource ()
        {            
            flickr = new Flickr (FlickrAuth.apiKey, FlickrAuth.sharedSecret);
            var photos = NSUserDefaults.StandardUserDefaults[PhotosDefaultsKey] as NSArray;
            if (null != photos) {
                for (uint i = 0; i < photos.Count; i++) {
                    IntPtr ptr = photos.ValueAt(i);
                    NSDictionary dict = Runtime.GetNSObject(ptr) as NSDictionary;
                    PhotoInfo p = new PhotoInfo(dict);
                    this.photos.Add(p.Id, p);
                }
            }
            var lastIndex = NSUserDefaults.StandardUserDefaults[LastViewedImageIndexKey] as NSNumber;
            if (null != lastIndex) {
                LastViewedImageIndex = lastIndex.Int32Value;
            }
        }
        
        public bool Stale {
            get {
                // photos are stale if we've never fetched or it's been more than 6 hours
                return lastPhotoFetchTimestamp == null ? true : 
                        (DateTime.UtcNow - lastPhotoFetchTimestamp) > TimeSpan.FromHours(6);
            }
        }
        
        public ICollection<PhotoInfo> Photos {
            get {
                return photos.Values;
            }
        }
        
        /// <summary>
        /// Fetch photo information from Flickr and persist it to local storage if there are new photos.
        /// </summary>
        public void Fetch() {
            lastPhotoFetchTimestamp = DateTime.UtcNow;
            PhotosetPhotoCollection photos;

            Debug.WriteLine("Http request on thread {0}:{1}", 
                              System.Threading.Thread.CurrentThread.ManagedThreadId, System.Threading.Thread.CurrentThread.Name);

            try {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
                photos = flickr.PhotosetsGetPhotos(FlickrAuth.photosetId);
            } finally {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
            }
            
            bool changed = false;
            List<PhotoInfo> newPhotos = new List<PhotoInfo>();
            foreach (Photo p in photos) {
                if (!this.photos.ContainsKey(p.PhotoId)) {
                    PhotoInfo info = new PhotoInfo(p.PhotoId, GetUrl(p), p.Title);
                    this.photos[p.PhotoId] = info;
                    newPhotos.Add(info);
                } else {
                    PhotoInfo info = this.photos[p.PhotoId];
                    // this is a bit of a hack.  if the title changes (maybe correcting a typo)
                    // we want to be able to invalid client caches.  this change won't
                    // necessarily have an immediate effect though
                    if (!info.Caption.Equals(p.Title)) {
                        changed = true;
                        info = new PhotoInfo(p.PhotoId, GetUrl(p), p.Title);
                        this.photos[p.PhotoId] = info;
                    }
                }
            }
            
            // REVIEW - consider always overwriting the local data with the remote info,
            // that way we don't have to worry about local caches getting into a bad state
            
            if (newPhotos.Count > 0 || changed) {
                Save();
            }
            if (newPhotos.Count > 0 && null != Added) {
                Added(newPhotos);
            }

            // warm the image file cache with the first two images
            foreach (PhotoInfo p in this.photos.Values) {
                FileCacher.LoadUrl(p.Url, true);
            }
        }
        
        private string GetUrl(Photo photo) {
            return UIScreen.MainScreen.ApplicationFrame.Width > 700 ? photo.LargeUrl : photo.MediumUrl;
        }
        
        /// <summary>
        /// Save all of the photo information to user defaults.
        /// </summary>
        public void Save() {
            
            NSMutableArray arr = new NSMutableArray();
            
            foreach (PhotoInfo info in this.photos.Values) {
                arr.Add(info.Serialize());
            }
            
            NSUserDefaults.StandardUserDefaults[PhotosDefaultsKey] = arr;
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }
        
        public void SaveLastViewedImageIndex() {
            NSUserDefaults.StandardUserDefaults[LastViewedImageIndexKey] = new NSNumber(LastViewedImageIndex);
        }
    }
    
    /// <summary>
    /// Photo information.  Supports serializing to (and from) an NSDictionary.
    /// </summary>
    public class PhotoInfo {
        public string Id { get; private set;}
        public string Url { get; private set;}
        public string Caption {get; private set;}
        
        public PhotoInfo(string id, string url, string caption) {
            Id = id;
            Url = url;
            Caption = caption;
        }
        
        public PhotoInfo(NSDictionary dictionary) {
            Id = dictionary[new NSString("id")].ToString();
            Caption = dictionary[new NSString("caption")].ToString();
            Url = dictionary[new NSString("url")].ToString();
        }
        
        public NSDictionary Serialize() {
            NSMutableDictionary dict = new NSMutableDictionary();
            dict[new NSString("url")] = new NSString(Url);
            dict[new NSString("caption")] = new NSString(Caption);
            dict[new NSString("id")] = new NSString(Id);
            return dict;
        }
    }
}

