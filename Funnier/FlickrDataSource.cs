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
        private readonly Dictionary<string, PhotoInfo> photos = new Dictionary<string, PhotoInfo>();
        private readonly Flickr flickr;
        
        public event PhotosAdded Added;
        
        public FlickrDataSource ()
        {            
            flickr = new Flickr (FlickrAuth.apiKey, FlickrAuth.sharedSecret);
            var photos = NSUserDefaults.StandardUserDefaults["Photos"] as NSArray;
            if (null != photos) {
                for (uint i = 0; i < photos.Count; i++) {
                    IntPtr ptr = photos.ValueAt(i);
                    NSDictionary dict = Runtime.GetNSObject(ptr) as NSDictionary;
                    PhotoInfo p = new PhotoInfo(dict);
                    this.photos.Add(p.Url, p);
                }
            }
        }
        
        public ICollection<PhotoInfo> Photos {
            get {
                return photos.Values;
            }
        }
        
        public void Fetch() {
            PhotosetPhotoCollection photos;
            try {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
                photos = flickr.PhotosetsGetPhotos("72157629877473203");
            } finally {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
            }
            
            List<PhotoInfo> newPhotos = new List<PhotoInfo>();
            foreach (Photo p in photos) {
                if (!this.photos.ContainsKey(p.MediumUrl)) {
                    PhotoInfo info = new PhotoInfo(p.MediumUrl, p.Title);
                    this.photos[info.Url] = info;
                    newPhotos.Add(info);
                }
            }
            
            if (newPhotos.Count > 0) {
                Save();
                Added(newPhotos);
            }
        }
        
        public void Save() {
            
            NSMutableArray arr = new NSMutableArray();
            
            foreach (PhotoInfo info in this.photos.Values) {
                arr.Add(info.Serialize());
            }
            
            NSUserDefaults.StandardUserDefaults["Photos"] = arr;
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }
    }
    

    
    public class PhotoInfo {
        public string Url { get; private set;}
        public string Caption {get; private set;}
        
        public PhotoInfo(string url, string caption) {
            Url = url;
            Caption = caption;
        }
        
        public PhotoInfo(NSDictionary dictionary) {
            NSObject url;
            if (dictionary.TryGetValue(new NSString("url"), out url)) {
                Url = url.ToString();
            } else {
                throw new Exception("url missing");
            }
            
            NSObject caption;
            if (dictionary.TryGetValue(new NSString("caption"), out caption)) {
                Caption = caption.ToString();
            } else {
                throw new Exception("caption missing");
            }
        }
        
        public NSDictionary Serialize() {
            NSMutableDictionary dict = new NSMutableDictionary();
            dict[new NSString("url")] = new NSString(Url);
            dict[new NSString("caption")] = new NSString(Caption);
            return dict;
        }
    }
}

