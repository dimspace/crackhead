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
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using FlickrCache;
using FlickrNet;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;

/// <summary>
/// Author: Saxon D'Aubin
/// </summary>
namespace Funnier
{
    public delegate void PhotoAdded(PhotosetPhoto photo);
    public delegate void NoticeMessage(string message);
    
    public class FlickrDataSource
    {
        private const uint CarrierDownloadLimitInBytes = 1048576
#if DEBUG
             / 4;  // limit to 1/4 mb
#else
             / 2;  // limit to 1/2 mb
#endif
        private const string OldPhotosDefaultsKey = "Photos";
        private const string PhotosDefaultsKey = "Photoset";
        private const string LastViewedImageIndexKey = "LastViewedImageIndex";
        /// <summary>
        /// Set of known photo ids, used to notice when new images have arrived.
        /// </summary>
        /// 
        private readonly HashSet<string> photoIds = new HashSet<string>();
        private readonly FlickrNet.Flickr flickr;

        private readonly FlickrCache.Photoset photoset;
        private DateTime? lastPhotoFetchTimestamp;
        public int LastViewedImageIndex { get; set; }
        
        public event PhotoAdded Added;
        public event NoticeMessage Messages;
        
        private readonly static FlickrDataSource singleton = new FlickrDataSource();
        public static FlickrDataSource Get() {
            return singleton;
        }
        
        private FlickrDataSource ()
        {            
            flickr = new FlickrNet.Flickr (FlickrAuth.apiKey, FlickrAuth.sharedSecret);

            // load the photo metadata that was saved in user defaults
            try {
                FlickrCache.Photoset savedPhotos = UserDefaultsUtils.LoadObject<FlickrCache.Photoset>(PhotosDefaultsKey);
                if (savedPhotos == null) {
                    photoset = new FlickrCache.Photoset();
                    photoset.Photo = new PhotosetPhoto[0];
                } else {
                    photoset = savedPhotos;
                    Debug.WriteLine("Successfully loaded photoset data");
                    foreach (PhotosetPhoto p in photoset.Photo) {
                        photoIds.Add(p.Id);
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine("An error occurred deserializing data: {0}", ex);
                Debug.WriteLine(ex);
                photoset = new FlickrCache.Photoset();
                photoset.Photo = new PhotosetPhoto[0];
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
                return photoset.Photo.Length == 0 || photoset.Photo.Length != Photos.Length;
            }
        }

        /// <summary>
        /// The network connection status has changed.  Update if we have a connection.
        /// </summary>
        private void ReachabilityChanged(object sender, EventArgs args) {
            NetworkStatus status = Reachability.RemoteHostStatus();
            Debug.WriteLine("Reachability changed: {0}", status);

            if (NetworkStatus.ReachableViaWiFiNetwork == status || 
                    (NetworkStatus.ReachableViaCarrierDataNetwork == status && 
                        PhotosAvailable)) {
                System.Threading.ThreadPool.QueueUserWorkItem(delegate {
                    if (photoset.Photo.Length == 0) {
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
        
        public PhotosetPhoto[] Photos {
            get {
                // if we have a wifi connection, return all the photos assuming they'll be downloaded
                if (NetworkStatus.ReachableViaWiFiNetwork == Reachability.RemoteHostStatus()) {
                    return photoset.Photo;
                } 
                else 
                {
                    // otherwise, filter the set of photos to the ones that have been cached to storage
                    List<PhotosetPhoto> photoList = new List<PhotosetPhoto>(photoset.Photo.Length);
                    foreach (PhotosetPhoto p in photoset.Photo) {
                        if (null != FileCacher.LoadUrl(p.Url, false)) {
                            photoList.Add(p);
                        }
                    }
                    return photoList.ToArray();
                }
            }
        }

        private FlickrNet.Photoset GetPhotosetInfo() {
            bool networkIndicator = UIApplication.SharedApplication.NetworkActivityIndicatorVisible;
            try {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;

                return flickr.PhotosetsGetInfo(FlickrAuth.photosetId);
            } finally {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = networkIndicator;
            }
        }

        private PhotosetPhotoCollection GetPhotosetPhotoCollection() {
            bool networkIndicator = UIApplication.SharedApplication.NetworkActivityIndicatorVisible;
            try {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;

                return flickr.PhotosetsGetPhotos(FlickrAuth.photosetId);
            } finally {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = networkIndicator;
            }
        }
        
        /// <summary>
        /// Fetch photo information from Flickr and persist it to local storage if there are new photos.
        /// </summary>
        public void Fetch(NetworkStatus status) {
            lastPhotoFetchTimestamp = DateTime.UtcNow;

            Debug.WriteLine("Http request on thread {0}:{1}", 
                              System.Threading.Thread.CurrentThread.ManagedThreadId, System.Threading.Thread.CurrentThread.Name);

            var info = GetPhotosetInfo();
            if (photoset.LastUpdatedSpecified) {
                if (info.DateUpdated == photoset.LastUpdated && 
                        photoset.Photo != null && info.NumberOfPhotos == photoset.Photo.Length) {
                    Debug.WriteLine("Up to date : {0}", info.DateUpdated);
                    return;
                }
            }

            photoset.LastUpdated = info.DateUpdated;
            photoset.LastUpdatedSpecified = true;
            photoset.Title = info.Title;

            if (Messages != null) {
                Messages("Updating the cartoon list");
            }

            var photos = GetPhotosetPhotoCollection();

            List<PhotosetPhoto> newPhotos = new List<PhotosetPhoto>();
            List<PhotosetPhoto> allPhotos = new List<PhotosetPhoto>();
            lock (this.photoset) {
                photoset.Photo = new PhotosetPhoto[photos.Count];
                foreach (Photo p in photos) {

                    var thePhoto = CreatePhotosetPhoto(p);

                    allPhotos.Add(thePhoto);

                    if (!photoIds.Contains(p.PhotoId)) {
                        newPhotos.Add(thePhoto);
                        photoIds.Add(p.PhotoId);
                    }
                }
                photoset.Photo = allPhotos.ToArray();
            }

            Save();

            bool isWifi = NetworkStatus.ReachableViaWiFiNetwork == status;
            if (newPhotos.Count > 0) {
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
            if (!isWifi && dlCount > 0 && newPhotos.Count > 0) {
                SendNotification(String.Format(
                    "{0} new cartoon{1} arrived.  {2} were downloaded.  The rest will be downloaded when a wifi connection is available", 
                                           newPhotos.Count, (dlCount > 1 ? "s" : ""), dlCount), newPhotos.Count);
            }
        }

        /// <summary>
        /// A convenience constructor to create our PhotosetPhoto object from a FlickrNet photo.
        /// </summary>
        public static PhotosetPhoto CreatePhotosetPhoto(Photo p) {
            var photo = new FlickrCache.PhotosetPhoto();
            photo.Title = p.Title;
            photo.Id = p.PhotoId;
            photo.Url = GetUrl(p);
            photo.Tag = new List<string>(p.Tags).ToArray();

            return photo;
        }

        /// <summary>
        /// Returns the URL based on the type of device (larger image urls for the iPad).
        /// </summary>
        private static string GetUrl(Photo photo) {
            return Funnier.DeviceUtils.IsIPad() ? photo.LargeUrl : photo.MediumUrl;
        }

        /// <summary>
        /// Fetch images that have not already been cached.
        /// </summary>
        /// <returns>
        /// The count of the number of downloaded images.
        /// </returns>
        /// <param name='status'>
        /// The current network status.
        /// </param>
        private int FetchImages(NetworkStatus status) {
            if (NetworkStatus.NotReachable == status) return 0;
            List<PhotosetPhoto> downloadedPhotos = new List<PhotosetPhoto>();
            // limit cell downloads
            uint byteLimit = NetworkStatus.ReachableViaCarrierDataNetwork == status ? CarrierDownloadLimitInBytes : UInt32.MaxValue;
            uint byteCount = 0;
            // warm the image file cache
            lock (this.photoset) {
                foreach (var p in this.photoset.Photo) {
                    // if the photo is not cached, load it
                    if (FileCacher.LoadUrl(p.Url, false) == null) {
                        var data = FileCacher.LoadUrl(p.Url, true);
                        byteCount += data.Length;
                        downloadedPhotos.Add(p);
                        if (null != Added) {
                            Added(p);
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
            UIApplication.SharedApplication.InvokeOnMainThread(delegate {
                UIApplication.SharedApplication.ScheduleLocalNotification(notification);
            });
        }
        
        /// <summary>
        /// Save all of the photo information to user defaults.
        /// </summary>
        public void Save() {
            // remove the old storage key if it exists
            NSUserDefaults.StandardUserDefaults.RemoveObject(OldPhotosDefaultsKey);
            UserDefaultsUtils.SaveObject(PhotosDefaultsKey, photoset);
            Debug.WriteLine("Successfully saved photoset data");
        }
        
        public void SaveLastViewedImageIndex() {
            NSUserDefaults.StandardUserDefaults[LastViewedImageIndexKey] = new NSNumber(LastViewedImageIndex);
        }
    }
}