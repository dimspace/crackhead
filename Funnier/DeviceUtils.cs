using System;

using MonoTouch.UIKit;

namespace Funny
{
    public static class DeviceUtils
    {
        public static bool IsIPad() {
            return UIScreen.MainScreen.ApplicationFrame.Width > 700;
        }
    }
}

