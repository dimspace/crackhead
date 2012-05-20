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
        }
    }
}