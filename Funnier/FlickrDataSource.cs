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

using MonoTouchUtils;

/// <summary>
/// Author: Saxon D'Aubin
/// </summary>
namespace Funnier
{
    /// <summary>
    /// This singleton holds a reference to the photoset cache with all of the cartoon metadata.
    /// </summary>
    public class FlickrDataSource
    {
        
        private readonly static FlickrDataSource singleton = new FlickrDataSource();
        public static FlickrDataSource Get() {
            return singleton;
        }

        private readonly PhotosetCache photosetCache;
        public PhotosetCache PhotosetCache {
            get {
                return photosetCache;
            }
        }
        
        private FlickrDataSource ()
        {            
            photosetCache = new PhotosetCache(FlickrAuth.apiKey, FlickrAuth.sharedSecret, FlickrAuth.photosetId);
            var appDelegate = UIApplication.SharedApplication.Delegate as ApplicationEventEmitter;
            if (null != appDelegate) {
                appDelegate.EnteredBackground += EnteredBackground;
                appDelegate.EnteringForeground += EnteringForeground;
            }

        }

        private void EnteredBackground() {
            FlickrDataSource.Get().PhotosetCache.SaveLastViewedImageIndex();
        }

        private void EnteringForeground() {
            
            Debug.WriteLine("FlickrDataSource.Stale = {0}", PhotosetCache.Stale);

            if (PhotosetCache.Stale) {
                FetchCartoonsIfConnected();
            }
        }

        public void FetchCartoonsIfConnected() {
            NetworkStatus status = Reachability.RemoteHostStatus();
            Debug.WriteLine("Network status: {0}", status);
            var photoCount = FlickrDataSource.Get().PhotosetCache.Photos.Length;
            if (photoCount > 0 && NetworkStatus.ReachableViaCarrierDataNetwork == status) {
                Debug.WriteLine("Skipping download via carrier.  Photo count: {0}", photoCount);
                return;
            }
            if (NetworkStatus.NotReachable == status) {

                Debug.WriteLine("Skipping download.  Network status: {0}", status);
            } else {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    delegate {
                        FetchCartoons(status);
                    });
            }
        }
        
        private void FetchCartoons(NetworkStatus status) {
            // FIXME revisit this error handling logic
            // only display a modal error message if there are no photos (initial startup)
            var photoCount = FlickrDataSource.Get().PhotosetCache.Photos.Length;
            try {
                FlickrDataSource.Get().PhotosetCache.Fetch(status);
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
                if (photoCount == 0) {

                    UIApplication.SharedApplication.InvokeOnMainThread(delegate {
                        using (var alert = new UIAlertView ("Error", "Unable to download cartoons - " + ex.Message, null, "Ok")) {
                            alert.Show ();
                        }
                    });
                }
            }
        }
    }
}