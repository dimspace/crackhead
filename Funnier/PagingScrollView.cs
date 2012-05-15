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
using System.Drawing; 
using System.Diagnostics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

/// <summary>
/// Author: sdaubin
/// </summary>
namespace Funny
{
    /// <summary>
    /// A delegate that is fired when a paging scroll view is scrolled.
    /// </summary>
    public delegate void Scrolled();
    
    public class PagingScrollView : UIView
    {
        private PagingViewDataSource dataSource;
        private UIView[] views;
        private readonly UIScrollView scrollView;
        public event Scrolled OnScroll;
        
        public PagingScrollView(RectangleF frame) : base(frame) {
            scrollView = new UIScrollView(frame);
            scrollView.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
            scrollView.PagingEnabled = true;
            scrollView.ScrollEnabled = true;
            
            scrollView.DirectionalLockEnabled = true;
            scrollView.ShowsVerticalScrollIndicator = false;
            scrollView.ShowsHorizontalScrollIndicator = false;
            
            scrollView.Delegate = new ScrollViewDelegate(this);
            AddSubview(scrollView);
        }
        
        public PagingViewDataSource DataSource {
            get {
                return dataSource;
            }
            
            set {
                dataSource = value;
                views = new UIView[dataSource.Count];
//                scrollView.ContentOffset = new PointF(currentImageIndex * bounds.Width, 0);
                scrollView.ContentSize = new SizeF(Frame.Width * dataSource.Count, Frame.Height);
                
                // render up to the first 2 views
                RenderViews(0, Math.Min(2, dataSource.Count));
                
                DataSource.OnChanged += delegate() {
                    UIView[] existingViews = views;
                    views = new UIView[dataSource.Count];
                    Array.Copy(existingViews, views, existingViews.Length);
                    scrollView.ContentSize = new SizeF(Frame.Width * dataSource.Count, Frame.Height);
                    RenderViews(0, Math.Min(2, dataSource.Count));
                };
            }
        }

        public void RenderViews(int start, int end) {
            for (int i = start; i < end; i++) {
                if (null == views[i]) {
                    UIView view = dataSource.GetView(i);
                    views[i] = view;
                    
                    view.Frame = new RectangleF(i * Frame.Width, 0, Frame.Size.Width, Frame.Size.Height);
                    scrollView.AddSubview(view);
                }
            }
        }

        public void FreeUnusedViews() {
            // FIXME remove all off screen views in the scroll viewer
            var index = GetCurrentViewIndex();
            for (int i = 0; i < views.Length; i++) {
                if (i < index - 1 || i > index + 1) {
                    if (null != views[i]) {
                        Debug.WriteLine("Releasing view {0}", views[i]);
                        views[i].RemoveFromSuperview();
                        views[i] = null;
                    }
                }
            }
        }
        
        public int GetCurrentViewIndex() {
            return (int) (scrollView.ContentOffset.X / scrollView.Frame.Size.Width);
        }
        
        public void ScrollToView(int index) 
        {
            Debug.WriteLine("Scroll to view index {0}", index);
            scrollView.ContentOffset = new PointF(index * scrollView.Frame.Width, 0f);
            FireOnScroll();
        }
  
        public int? PrepareForRotation() {
            if (null == dataSource) {
                return null;
            }
            var index = GetCurrentViewIndex();
            
            // destroy all views but the current one
            for (int i = 0; i < dataSource.Count; i++) {
                if (i != index && null != views[i]) {
                    views[i].RemoveFromSuperview();
                    views[i] = null;
                }
            }
            float maxBound = Math.Max(Bounds.Width, Bounds.Height);
            scrollView.ContentSize = new SizeF(maxBound * dataSource.Count, maxBound);
            
            return index;
        }
        
        public void FinishRotation(int? currentViewIndex) {
            if (null != dataSource) {
                scrollView.ContentSize = new SizeF(Bounds.Width * dataSource.Count, Bounds.Height);
            }
            if (currentViewIndex.HasValue) {
                scrollView.ContentOffset = new PointF(currentViewIndex.Value * Bounds.Width, Bounds.Y);
            }
        }
        
        private void FireOnScroll() {
            if (null == dataSource) {
                return;
            }
            SizeF bounds =  Frame.Size;
            int index = GetCurrentViewIndex();
            for (int i = Math.Max(0, index - 1); i < Math.Min(dataSource.Count, index + 2); i++) {
                float x = i * bounds.Width;
                if (null == views[i]) {
                    views[i] = dataSource.GetView(i);
                    scrollView.AddSubview(views[i]);
                }
                SizeF size = Frame.Size;
                views[i].Frame = new RectangleF(x, 
                        0, size.Width, size.Height);
            }
            
            if (null != OnScroll) {
                OnScroll();
            }
        }
        
        private class ScrollViewDelegate : UIScrollViewDelegate {
            private readonly PagingScrollView view;
            internal ScrollViewDelegate(PagingScrollView view) {
                this.view = view;
            }
            
            public override void Scrolled (UIScrollView scrollView)
            {
                view.FireOnScroll();
            }
        }
        
    }
}

