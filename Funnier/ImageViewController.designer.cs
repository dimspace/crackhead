// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Funny
{
	[Register ("ImageViewController")]
	partial class ImageViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIToolbar toolbar { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lblLoadingMessage { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (toolbar != null) {
				toolbar.Dispose ();
				toolbar = null;
			}

			if (lblLoadingMessage != null) {
				lblLoadingMessage.Dispose ();
				lblLoadingMessage = null;
			}
		}
	}
}
