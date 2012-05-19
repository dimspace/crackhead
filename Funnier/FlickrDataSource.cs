//    Licensed to the Apache Software Foundation (ASF) under one
//    or more contributor license agreements.  See the NOTICE file
//    distributed with this work for additional information
//    regarding copyright ownership.  The ASF licenses this file
//    to you under the Apache License, Version 2.0 (the
//    "License"); you may not use this file except in compliance
//    with the License.  You may obtain a copy of the License at
//    
//     http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing,
//    software distributed under the License is distributed on an
//    "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//    KIND, either express or implied.  See the License for the
//    specific language governing permissions and limitations
//    under the License.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using FlickrNet;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;

/// <summary>
/// Author: sdaubin
/// </summary>
namespace Funny
{
    public delegate void PhotosAdded(PhotoInfo[] photos);
    public delegate void NoticeMessage(string message);
    
    public class FlickrDataSource
    {
        private const uint CarrierDownloadLimitInBytes = 1048576
#if DEBUG
             / 4;  // limit to 1/4 mb
#else
             / 2;  // limit to 1/2 mb
#endif
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
        public event NoticeMessage Messages;
        
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

            // if the connection isn't wifi, hook up a listener so that we'll notice when wifi is available
            if (NetworkStatus.ReachableViaWiFiNetwork != Reachability.RemoteHostStatus()) {
                Debug.WriteLine("Attach reachability listener");
                Reachability.ReachabilityChanged += ReachabilityChanged;
            }
        }

        private bool PhotosAvailable {
            get {
                return photos.Count == 0 || photos.Count != Photos.Count;
            }
        }

        private void ReachabilityChanged(object sender, EventArgs args) {
            NetworkStatus status = Reachability.RemoteHostStatus();
            Debug.WriteLine("Reachability changed: {0}", status);

            if (NetworkStatus.ReachableViaWiFiNetwork == status || 
                    (NetworkStatus.ReachableViaCarrierDataNetwork == status && 
                        PhotosAvailable)) {
//                Reachability.ReachabilityChanged += ReachabilityChanged;
                System.Threading.ThreadPool.QueueUserWorkItem(delegate {
                    if (photos.Count == 0) {
                        Fetch (status);
                    } else {
                        FetchImages(status);
                    }
                });
            }
        }
        
        public bool Stale {
            get {
#if DEBUG
                TimeSpan staleSpan = TimeSpan.FromMinutes(5);
#else
                TimeSpan staleSpan = TimeSpan.FromHours(6);
#endif
                // photos are stale if we've never fetched or it's been more than 6 hours
                return lastPhotoFetchTimestamp == null ? true : 
                        (DateTime.UtcNow - lastPhotoFetchTimestamp) > staleSpan;
            }
        }
        
        public ICollection<PhotoInfo> Photos {
            get {
                if (NetworkStatus.ReachableViaWiFiNetwork == Reachability.RemoteHostStatus()) {
                    return photos.Values;
                } else {
                    List<PhotoInfo> photoList = new List<PhotoInfo>(photos.Count);
                    foreach (KeyValuePair<string, PhotoInfo> entry in photos) {
                        if (null != FileCacher.LoadUrl(entry.Value.Url, false)) {
                            photoList.Add(entry.Value);
                        }
                    }
                    return photoList;
                }
            }
        }
        
        /// <summary>
        /// Fetch photo information from Flickr and persist it to local storage if there are new photos.
        /// </summary>
        public void Fetch(NetworkStatus status) {
            lastPhotoFetchTimestamp = DateTime.UtcNow;
            PhotosetPhotoCollection photos;

            Debug.WriteLine("Http request on thread {0}:{1}", 
                              System.Threading.Thread.CurrentThread.ManagedThreadId, System.Threading.Thread.CurrentThread.Name);

            if (Messages != null) {
                Messages("Updating the cartoon list");
            }

            try {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
                photos = flickr.PhotosetsGetPhotos(FlickrAuth.photosetId);
            } finally {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
            }
            
            bool changed = false;
            List<PhotoInfo> newPhotos = new List<PhotoInfo>();
            lock (this.photos.Values) {
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
            }
            
            // REVIEW - consider always overwriting the local data with the remote info,
            // that way we don't have to worry about local caches getting into a bad state
            
            if (changed) {
                Save();
            }

            bool isWifi = NetworkStatus.ReachableViaWiFiNetwork == status;
            if (newPhotos.Count > 0) {
                // Fire the Added event if we're on wifi - otherwise we're probably not going to download all images
                if (null != Added && isWifi) {
                    Added(newPhotos.ToArray());
                }

                var message = String.Format("{0} new cartoon{1} arrived.  Downloading images.", 
                                            newPhotos.Count, newPhotos.Count > 1 ? "s" : "");
                if (null != Messages) {
                    Messages(message);
                }

                if (isWifi) {
                    SendNotification(message, newPhotos.Count); 
                }
            }
            int dlCount = FetchImages(status);
            if (!isWifi && dlCount > 0) {
                SendNotification(String.Format(
                    "{0} new cartoon{1} arrived.  {2} were downloaded.  The rest will be downloaded when a wifi connection is available", 
                                           newPhotos.Count, (dlCount > 1 ? "s" : ""), dlCount), newPhotos.Count); 
            }
        }

        private int FetchImages(NetworkStatus status) {
            bool isWifi = NetworkStatus.ReachableViaWiFiNetwork == status;
            List<PhotoInfo> downloadedPhotos = new List<PhotoInfo>();
            // limit cell downloads
            uint byteLimit = NetworkStatus.ReachableViaCarrierDataNetwork == status ? CarrierDownloadLimitInBytes : UInt32.MaxValue;
            uint byteCount = 0;
            // warm the image file cache
            lock (this.photos.Values) {
                foreach (PhotoInfo p in this.photos.Values) {
                    if (FileCacher.LoadUrl(p.Url, false) == null) {
                        var data = FileCacher.LoadUrl(p.Url, true);
                        byteCount += data.Length;
                        downloadedPhotos.Add(p);
                        if (null != Added) {
                            Added(new PhotoInfo[] {p});
                        }
                        if (byteCount > byteLimit) {
                            break;
                        }
                    }
                }
            }
            return downloadedPhotos.Count;
        }

        private void SendNotification(string message, int count) {
            Debug.WriteLine("Sending notification: {0}", message);
            UILocalNotification notification = new UILocalNotification{
                    FireDate = DateTime.Now,
                    TimeZone = NSTimeZone.LocalTimeZone,
                    AlertBody = message,
                    RepeatInterval = 0,
                    ApplicationIconBadgeNumber = count
                };
            UIApplication.SharedApplication.ScheduleLocalNotification(notification);
        }
        
        private string GetUrl(Photo photo) {
            return DeviceUtils.IsIPad() ? photo.LargeUrl : photo.MediumUrl;
        }
        
        /// <summary>
        /// Save all of the photo information to user defaults.
        /// </summary>
        public void Save() {
            
            NSMutableArray arr = new NSMutableArray();

            lock (this.photos) {
                foreach (PhotoInfo info in this.photos.Values) {
                    arr.Add(info.Serialize());
                }
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

