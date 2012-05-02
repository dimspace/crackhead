using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Funny
{
    public delegate void Changed();
    
    public interface PagingViewDataSource
    {
        event Changed OnChanged;
        
        int Count { get;}
        
        UIView GetView(int index);
    }
}

