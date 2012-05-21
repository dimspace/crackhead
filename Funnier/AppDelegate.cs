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
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouchUtils;

using FlickrNet;

/// <summary>
/// Author: Saxon D'Aubin
/// </summary>
namespace Funnier
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate, ApplicationEventEmitter
    {
    
        public event EnteringForeground EnteringForeground;
        public event EnteredBackground EnteredBackground;
    
        // class-level declarations
        
        public override UIWindow Window {
            get;
            set;
        }

        private const string OldPhotosDefaultsKey = "Photos";

        private void CleanUpUserDefaults ()
        {
            // remove the old storage key if it exists
            if (null != NSUserDefaults.StandardUserDefaults[OldPhotosDefaultsKey]) {
                NSUserDefaults.StandardUserDefaults.RemoveObject(OldPhotosDefaultsKey);
                NSUserDefaults.StandardUserDefaults.Synchronize();
            }
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

            CleanUpUserDefaults();
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
            if (null != EnteredBackground) {
                EnteredBackground();
            }
        }
        
        /// This method is called as part of the transiton from background to active state.
        public override void WillEnterForeground (UIApplication application)
        {
            if (null != EnteringForeground) {
                EnteringForeground();
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
    }
}

