using System;
using System.IO;
using System.Diagnostics;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

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

