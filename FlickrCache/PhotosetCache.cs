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
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using FlickrCache;
using FlickrNet;

using MonoTouchUtils;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;

/// <summary>
/// Author: Saxon D'Aubin
/// </summary>
namespace FlickrCache
{
    public delegate void PhotoAdded(PhotosetPhoto photo);
    public delegate void NoticeMessage(string message);
    public delegate void ImagesChanged(int totalCount, int recentlyArrivedCount, int recentlyDownloadedCount);
    
    public class PhotosetCache
    {
        private const uint CarrierDownloadLimitInBytes = 1048576
#if DEBUG
             / 4;  // limit to 1/4 mb
#else
             / 2;  // limit to 1/2 mb
#endif

        private readonly string photosetUserDefaultsKeyName;
        private readonly string photosetLastViewedImageIndexKeyName;

        /// <summary>
        /// Set of known photo ids, used to notice when new images have arrived.
        /// </summary>
        /// 
        private readonly HashSet<string> photoIds = new HashSet<string>();
        private readonly FlickrNet.Flickr flickr;
        private readonly string photosetId;

        private readonly FlickrCache.Photoset photoset;
        private DateTime? lastPhotoFetchTimestamp;
        public int LastViewedImageIndex { get; set; }
        
        public event PhotoAdded Added;
        public event NoticeMessage Messages;
        public event ImagesChanged ImagesChanged;
        
        public PhotosetCache(string apiKey, string sharedSecret, string photosetId)
        {
            this.photosetId = photosetId;
            photosetUserDefaultsKeyName = "Photoset_" + photosetId;
            photosetLastViewedImageIndexKeyName = "LastViewedImageIndex_" + photosetId;

            flickr = new FlickrNet.Flickr (apiKey, sharedSecret);

            // load the photo metadata that was saved in user defaults
            try {
                FlickrCache.Photoset savedPhotos = UserDefaultsUtils.LoadObject<FlickrCache.Photoset>(photosetUserDefaultsKeyName);
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

            var lastIndex = NSUserDefaults.StandardUserDefaults[photosetLastViewedImageIndexKeyName] as NSNumber;
            if (null != lastIndex) {
                LastViewedImageIndex = lastIndex.Int32Value;
            }

            // hook up a listener so that we'll notice when connectivity changes
            Debug.WriteLine("Attach reachability listener");
            Reachability.ReachabilityChanged += ReachabilityChanged;
        }

        public void Close() {
            Reachability.ReachabilityChanged -= ReachabilityChanged;
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
                Debug.WriteLine("Fetching photoset info");
                return flickr.PhotosetsGetInfo(photosetId);
            } finally {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = networkIndicator;
            }
        }

        private PhotosetPhotoCollection GetPhotosetPhotoCollection() {
            bool networkIndicator = UIApplication.SharedApplication.NetworkActivityIndicatorVisible;
            try {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
                Debug.WriteLine("Fetching photoset photo list");
                return flickr.PhotosetsGetPhotos(photosetId, PhotoSearchExtras.AllUrls | PhotoSearchExtras.Tags);
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

            int dlCount = FetchImages(status);
            if (null != ImagesChanged) {
                ImagesChanged(photoset.Photo.Length, newPhotos.Count, dlCount);
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

        private static bool LargerThanBounds(float width, float height, SizeF bounds) {
            return width > bounds.Width || height > bounds.Height;
        }

        /// <summary>
        /// Returns the URL by picking the smallest image that is larger than the device screen bounds.
        /// </summary>
        private static string GetUrl(Photo photo) {
            var bounds = UIScreen.MainScreen.Bounds.Size;
            if (photo.SmallWidth.HasValue && LargerThanBounds(photo.SmallWidth.Value, photo.SmallHeight.Value, bounds)) {
                return photo.SmallUrl;
            }
            if (photo.MediumWidth.HasValue && LargerThanBounds(photo.MediumWidth.Value, photo.MediumHeight.Value, bounds)) {
                return photo.MediumUrl;
            }
            if (photo.Medium640Width.HasValue && LargerThanBounds(photo.Medium640Width.Value, photo.Medium640Height.Value, bounds)) {
                return photo.Medium640Url;
            }
            return photo.LargeUrl;
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


        
        /// <summary>
        /// Save all of the photo information to user defaults.
        /// </summary>
        public void Save() {
            UserDefaultsUtils.SaveObject(photosetUserDefaultsKeyName, photoset);
            Debug.WriteLine("Successfully saved photoset data");
        }
        
        public void SaveLastViewedImageIndex() {
            NSUserDefaults.StandardUserDefaults[photosetLastViewedImageIndexKeyName] = new NSNumber(LastViewedImageIndex);
        }
    }
}