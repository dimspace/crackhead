using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;


namespace Funny
{
    public interface IResizable
    {
        void Resize(SizeF size, double duration);
    }
}

