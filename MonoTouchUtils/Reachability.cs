/*
This sample is licensed under the terms of the MIT X11 license

Copyright 2009-2011 Novell Inc and the individuals listed
on the ChangeLog entries.

Copyright 2011 Xamarin Inc

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Net;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.SystemConfiguration;
using MonoTouch.CoreFoundation;

public enum NetworkStatus {
 NotReachable,
 ReachableViaCarrierDataNetwork,
 ReachableViaWiFiNetwork
}
     
public static class Reachability {
 public static string HostName = "www.google.com";
 
 public static bool IsReachableWithoutRequiringConnection (NetworkReachabilityFlags flags)
 {
     // Is it reachable with the current network configuration?
     bool isReachable = (flags & NetworkReachabilityFlags.Reachable) != 0;

     // Do we need a connection to reach it?
     bool noConnectionRequired = (flags & NetworkReachabilityFlags.ConnectionRequired) == 0;

     // Since the network stack will automatically try to get the WAN up,
     // probe that
     if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
         noConnectionRequired = true;

     return isReachable && noConnectionRequired;
 }

 // Is the host reachable with the current network configuration
 public static bool IsHostReachable (string host)
 {
     if (host == null || host.Length == 0)
         return false;

     using (var r = new NetworkReachability (host)){
         NetworkReachabilityFlags flags;
         
         if (r.TryGetFlags (out flags)){
             return IsReachableWithoutRequiringConnection (flags);
         }
     }
     return false;
 }

 // 
 // Raised every time there is an interesting reachable event, 
 // we do not even pass the info as to what changed, and 
 // we lump all three status we probe into one
 //
 public static event EventHandler ReachabilityChanged;
 
 static void OnChange (NetworkReachabilityFlags flags)
 {
     var h = ReachabilityChanged;
     if (h != null)
         h (null, EventArgs.Empty);
 }
 
 //
 // Returns true if it is possible to reach the AdHoc WiFi network
 // and optionally provides extra network reachability flags as the
 // out parameter
 //
 static NetworkReachability adHocWiFiNetworkReachability;
 public static bool IsAdHocWiFiNetworkAvailable (out NetworkReachabilityFlags flags)
 {
     if (adHocWiFiNetworkReachability == null){
         adHocWiFiNetworkReachability = new NetworkReachability (new IPAddress (new byte [] {169,254,0,0}));
         adHocWiFiNetworkReachability.SetCallback (OnChange);
         adHocWiFiNetworkReachability.Schedule (CFRunLoop.Current, CFRunLoop.ModeDefault);
     }
     
     if (!adHocWiFiNetworkReachability.TryGetFlags (out flags))
         return false;
     
     return IsReachableWithoutRequiringConnection (flags);
 }
 
 static NetworkReachability defaultRouteReachability;
 static bool IsNetworkAvaialable (out NetworkReachabilityFlags flags)
 {
     if (defaultRouteReachability == null){
         defaultRouteReachability = new NetworkReachability (new IPAddress (0));
         defaultRouteReachability.SetCallback (OnChange);
         defaultRouteReachability.Schedule (CFRunLoop.Current, CFRunLoop.ModeDefault);
     }
     if (defaultRouteReachability.TryGetFlags (out flags))
         return false;
     return IsReachableWithoutRequiringConnection (flags);
 }   
 
 static NetworkReachability remoteHostReachability;
 public static NetworkStatus RemoteHostStatus ()
 {
     NetworkReachabilityFlags flags;
     bool reachable;
     
     if (remoteHostReachability == null){
         remoteHostReachability = new NetworkReachability (HostName);

         // Need to probe before we queue, or we wont get any meaningful values
         // this only happens when you create NetworkReachability from a hostname
         reachable = remoteHostReachability.TryGetFlags (out flags);
         
         remoteHostReachability.SetCallback (OnChange);
         remoteHostReachability.Schedule (CFRunLoop.Current, CFRunLoop.ModeDefault);
     } else
         reachable = remoteHostReachability.TryGetFlags (out flags);         
     
     if (!reachable)
         return NetworkStatus.NotReachable;

     if (!IsReachableWithoutRequiringConnection (flags))
         return NetworkStatus.NotReachable;
     
     if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
         return NetworkStatus.ReachableViaCarrierDataNetwork;
             
     return NetworkStatus.ReachableViaWiFiNetwork;
 }
 
 public static NetworkStatus InternetConnectionStatus ()
 {
     NetworkReachabilityFlags flags;
     bool defaultNetworkAvailable = IsNetworkAvaialable (out flags);
     if (defaultNetworkAvailable){
         if ((flags & NetworkReachabilityFlags.IsDirect) != 0)
             return NetworkStatus.NotReachable;
     } else if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
         return NetworkStatus.ReachableViaCarrierDataNetwork;
     return NetworkStatus.ReachableViaWiFiNetwork;
 }
 
 public static NetworkStatus LocalWifiConnectionStatus ()
 {
     NetworkReachabilityFlags flags;
     if (IsAdHocWiFiNetworkAvailable (out flags)){
         if ((flags & NetworkReachabilityFlags.IsDirect) != 0)
             return NetworkStatus.ReachableViaWiFiNetwork;
     }
     return NetworkStatus.NotReachable;
 }
}
