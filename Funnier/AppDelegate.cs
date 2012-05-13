using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using FlickrNet;

namespace Funny
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        // class-level declarations
        
        public override UIWindow Window {
            get;
            set;
        }

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
        {
            Debug.WriteLine("Application started on thread {0}:{1}", 
                              System.Threading.Thread.CurrentThread.ManagedThreadId, System.Threading.Thread.CurrentThread.Name);

            FetchCartoonsIfConnected();
            return true;
        }
        
        // This method is invoked when the application is about to move from active to inactive state.
        // OpenGL applications should use this method to pause.
        public override void OnResignActivation (UIApplication application)
        {
        }
        
        // This method should be used to release shared resources and it should store the application state.
        // If your application supports background exection this method is called instead of WillTerminate
        // when the user quits.
        public override void DidEnterBackground (UIApplication application)
        {
            Debug.WriteLine("DidEnterBackground");
            FlickrDataSource.Get().SaveLastViewedImageIndex();
        }
        
        /// This method is called as part of the transiton from background to active state.
        public override void WillEnterForeground (UIApplication application)
        {
            Debug.WriteLine("FlickrDataSource.Stale = {0}", FlickrDataSource.Get().Stale);

            if (FlickrDataSource.Get().Stale) {
                FetchCartoonsIfConnected();
            }
        }
        
        public override void ReceivedLocalNotification(UIApplication application, 
          UILocalNotification notification)
        {
            //Do something to respond to the scheduled local notification
            UIAlertView alert = new UIAlertView("Funnier", 
                    notification.AlertBody, null, "Okay");
            alert.Show();
        }
        
        private void FetchCartoonsIfConnected() {
            NetworkStatus status = Reachability.RemoteHostStatus();
            if (NetworkStatus.ReachableViaWiFiNetwork != status) {

                Debug.WriteLine("Skipping download.  Network status: {0}", status);
            } else {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    delegate {
                        FetchCartoons();
                    });
            }
        }
        
        private void FetchCartoons() {
            // FIXME revisit this error handling logic
            try {
                FlickrDataSource.Get().Fetch();
            } catch (System.Net.WebException ex) {
                Debug.WriteLine(ex);
                InvokeOnMainThread (delegate {
                    using (var alert = new UIAlertView ("Error", "Unable to download cartoons - " + ex.Message, null, "Ok")) {
                        alert.Show ();
                    }
                });                    
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
                InvokeOnMainThread (delegate {
                    using (var alert = new UIAlertView ("Error", "Unable to download cartoons - " + ex.Message, null, "Ok")) {
                        alert.Show ();
                    }
                });
            }
        }
    }
}

