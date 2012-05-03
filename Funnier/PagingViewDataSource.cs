using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Funny
{
    public delegate void Changed();
    
    /// <summary>
    /// The data source for a PagingScrollView.
    /// </summary>
    public interface PagingViewDataSource
    {
        /// <summary>
        /// Fires when this data source changes (either the count, or one of the views).
        /// </summary>
        event Changed OnChanged;
        
        /// <summary>
        /// Gets the count of views.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        int Count { get;}
        
        /// <summary>
        /// Gets the view at a given index.
        /// </summary>
        /// <returns>
        /// The view.
        /// </returns>
        /// <param name='index'>
        /// Index.
        /// </param>
        UIView GetView(int index);
    }
}

