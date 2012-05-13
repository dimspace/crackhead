//    Licensed to the Apache Software Foundation (ASF) under one
//    or more contributor license agreements.  See the NOTICE file
//    distributed with this work for additional information
//    regarding copyright ownership.  The ASF licenses this file
//    to you under the Apache License, Version 2.0 (the
//    "License"); you may not use this file except in compliance
//    with the License.  You may obtain a copy of the License at
//    
//     http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing,
//    software distributed under the License is distributed on an
//    "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//    KIND, either express or implied.  See the License for the
//    specific language governing permissions and limitations
//    under the License.

using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

/// <summary>
/// Author: sdaubin
/// </summary>
namespace Funny
{
    /// <summary>
    /// A callback for a PagingViewDataSource change event.
    /// </summary>
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

