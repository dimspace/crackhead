using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

/// <summary>
/// Author: sdaubin
/// </summary>
namespace Funny
{
    /// <summary>
    /// An interface for something that has an animated resize.
    /// </summary>
    public interface IResizable
    {
        void Resize(SizeF size, double duration);
    }
}

