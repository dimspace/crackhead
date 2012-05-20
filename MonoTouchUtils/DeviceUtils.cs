using System;

using MonoTouch.UIKit;

/// <summary>
/// Author: Saxon D'Aubin
/// </summary>
namespace MonoTouchUtils
{
    public static class DeviceUtils
    {
        /// <summary>
        /// Determines whether this device instance is an iPad.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this device instance is an iPad, otherwise, <c>false</c>.
        /// </returns>
        public static bool IsIPad() {
            // REVIEW there's probably a better way to do this
            return UIScreen.MainScreen.ApplicationFrame.Width > 700;
        }
    }
}

