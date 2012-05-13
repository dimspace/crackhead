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
using System.IO;
using System.Diagnostics;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

/// <summary>
/// Author: sdaubin
/// </summary>
namespace Funny
{
    public static class FileCacher
    {
        /// <summary>
        /// Loads the NSData for the given URL, looking first on the file system.  If the url is 
        /// remotely fetched it is cached on the FS.
        /// If the url isn't cached and the wifi network isn't available, null is returned.
        /// </summary>
        /// <returns>
        /// The URL.
        /// </returns>
        /// <param name='url'>
        /// URL.
        /// </param>
        /// <param name='prefix'>
        /// Prefix.
        /// </param>
        public static NSData LoadUrl(string url, bool downloadCacheMisses = true, string prefix = null) {
            int index = url.LastIndexOf("/");
            var fileName = url.Substring(index);
            
            var folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var builder = new System.Text.StringBuilder(folder);
            if (prefix != null) {
                builder.Append(prefix);
            }
            builder.Append(fileName);
            var path = builder.ToString();
            if (File.Exists(path)) 
            {
                try {
                    byte[] bytes = File.ReadAllBytes(path);
                    return NSData.FromArray(bytes);
                } catch (Exception ex) {
                    Debug.WriteLine(ex);
                    // okay, let's fall back to downloading the file
                    if (downloadCacheMisses)
                    {
                        return FetchUrl(path, url);
                    }
                }
            } 
            else if (downloadCacheMisses)
            {
                return FetchUrl(path, url);
            }
            return null;
        }
    
        /// <summary>
        /// Fetchs the URL data, stores it in a file, and returns the data.
        /// </summary>
        /// <returns>
        /// The URL.
        /// </returns>
        /// <param name='path'>
        /// Path.
        /// </param>
        /// <param name='url'>
        /// URL.
        /// </param>
        private static NSData FetchUrl(string path, string url) 
        {

            Debug.WriteLine("FromUrl: {0} on thread {1}:{2}", 
                          url,
                          System.Threading.Thread.CurrentThread.ManagedThreadId, System.Threading.Thread.CurrentThread.Name);
            try {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
                var data = NSData.FromUrl (new NSUrl (url));
                
                byte[] dataBytes = new byte[data.Length];
    
                System.Runtime.InteropServices.Marshal.Copy(data.Bytes, dataBytes, 0, Convert.ToInt32(data.Length));
                File.WriteAllBytes(path, dataBytes);
                return data;
            } finally {
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
            }
        }
    }
}

