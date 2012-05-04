using System;
using System.Drawing;
using System.Threading;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Funny
{
    /// <summary>
    /// A splash screen controller that waits for a couple of seconds then performs the "startApp" segue.
    /// </summary>
    public partial class SplashScreenController : UIViewController
    {
        public SplashScreenController(IntPtr handle) : base (handle)
        { }        
        
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            
            ThreadPool.QueueUserWorkItem(
                delegate {
                    // wait 2 seconds
                    Thread.Sleep(1000 * 2);
                    // segue on the main thread or nothing will happen!
                    InvokeOnMainThread (delegate {
                        PerformSegue("startApp", this);
                    });
                });
            // Perform any additional setup after loading the view, typically from a nib.
        }
        
        public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
        {
            // Return true for supported orientations
            return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
        }
    }
}

