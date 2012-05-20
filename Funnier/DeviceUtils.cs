using System;

using MonoTouch.UIKit;

/// <summary>
/// Author: Saxon D'Aubin
/// </summary>
namespace Funnier
{
    public static class DeviceUtils
    {
        public static bool IsIPad() {
            // REVIEW there's probably a better way to do this
            return UIScreen.MainScreen.ApplicationFrame.Width > 700;
        }
    }
}

