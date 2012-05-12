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
                PhotoInfo info = new PhotoInfo(p.PhotoId, GetUrl(p), p.Title, p.Tags);
                if (!this.photos.ContainsKey(p.PhotoId)) {
                    newPhotos.Add(info);
                    changed = true;
                }
                // we always overwrite our in memory copy of the photo info, and this will be
                // persisted on any change.
                this.photos[p.PhotoId] = info;
            }
            
            // REVIEW - consider always overwriting the local data with the remote info,
            // that way we don't have to worry about local caches getting into a bad state
            
            if (changed) {
                Save();
            }
            
            if (newPhotos.Count > 0) {
                // Fire the Added event
                if (null != Added) {
                    Added(newPhotos);
                }
                var message = String.Format("{0} new cartoon{1} arrived", newPhotos.Count, newPhotos.Count > 1 ? "s" : "");
                UILocalNotification notification = new UILocalNotification{
                    FireDate = DateTime.Now,
                    TimeZone = NSTimeZone.LocalTimeZone,
                    AlertBody = message,
                    RepeatInterval = 0,
                    ApplicationIconBadgeNumber = newPhotos.Count
                };
                UIApplication.SharedApplication.ScheduleLocalNotification(notification);
            }

            // warm the image file cache
            foreach (PhotoInfo p in newPhotos) {
                FileCacher.LoadUrl(p.Url, true);
            }
        }
        
        private string GetUrl(Photo photo) {
            return DeviceUtils.IsIPad() ? photo.LargeUrl : photo.MediumUrl;
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
        public string[] Tags {get; private set;}
        
        public PhotoInfo(string id, string url, string caption, System.Collections.ObjectModel.Collection<string> tags) {
            Id = id;
            Url = url;
            Caption = caption;
            this.Tags = new string[tags.Count];
            tags.CopyTo(this.Tags, 0);
        }
        
        public PhotoInfo(NSDictionary dictionary) {
            Id = dictionary[new NSString("id")].ToString();
            Caption = dictionary[new NSString("caption")].ToString();
            Url = dictionary[new NSString("url")].ToString();
            
            NSArray tags = dictionary[new NSString("tags")] as NSArray;
            Tags = new string[tags.Count];
            for (uint i = 0; i < tags.Count; i++) {
                Tags[i] = tags.ValueAt(i).ToString();
            }
        }
        
        public NSDictionary Serialize() {
            NSMutableDictionary dict = new NSMutableDictionary();
            dict[new NSString("url")] = new NSString(Url);
            dict[new NSString("caption")] = new NSString(Caption);
            dict[new NSString("id")] = new NSString(Id);
            
            NSMutableArray tags = new NSMutableArray(Tags.Length);
            foreach (string tag in Tags) {
                tags.Add(new NSString(tag));
            }
            dict[new NSString("tags")] = tags;
            return dict;
        }
    }
}

