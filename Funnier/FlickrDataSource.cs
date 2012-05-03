using System;
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
        /// <summary>
        /// Dictionary of photo ids to photos.
        /// </summary>
        /// 
        private readonly Dictionary<string, PhotoInfo> photos = new Dictionary<string, PhotoInfo>();
        private readonly Flickr flickr;
        
        public event PhotosAdded Added;
        
        public FlickrDataSource ()
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
            PhotosetPhotoCollection photos;
            try {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
                photos = flickr.PhotosetsGetPhotos("72157629877473203");
            } finally {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
            }
            
            bool changed = false;
            List<PhotoInfo> newPhotos = new List<PhotoInfo>();
            foreach (Photo p in photos) {
                if (!this.photos.ContainsKey(p.PhotoId)) {
                    PhotoInfo info = new PhotoInfo(p.PhotoId, p.MediumUrl, p.Title);
                    this.photos[p.PhotoId] = info;
                    newPhotos.Add(info);
                } else {
                    PhotoInfo info = this.photos[p.PhotoId];
                    // this is a bit of a hack.  if the title changes (maybe correcting a typo)
                    // we want to be able to invalid client caches.  this change won't
                    // necessarily have an immediate effect though
                    if (!info.Url.Equals(p.Title)) {
                        changed = true;
                        info = new PhotoInfo(p.PhotoId, p.MediumUrl, p.Title);
                        this.photos[p.PhotoId] = info;
                    }
                }
            }
            
            // REVIEW - consider always overwriting the local data with the remote info,
            // that way we don't have to worry about local caches getting into a bad state
            
            if (newPhotos.Count > 0 || changed) {
                Save ();
            }
            if (newPhotos.Count > 0) {
                Added(newPhotos);
            }
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

