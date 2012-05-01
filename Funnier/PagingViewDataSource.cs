using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Funny
{
    public interface PagingViewDataSource
    {
        int Count { get;}
        
        UIView GetView(int index);
    }
}

