using System;
using System.Drawing; 

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

namespace Funny
{
    public class PagingScrollView : UIView, IResizable
    {
        private PagingViewDataSource dataSource;
        private UIView[] views;
        private readonly UIScrollView scrollView;
        
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
                    view.Frame = new RectangleF(i * Frame.Width, 0, Frame.Width, Frame.Height);
                    scrollView.AddSubview(view);
                }
            }
        }
        
        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            
            scrollView.Frame = Frame;
        }
        
        public PagingScrollView()
        {
            scrollView = new UIScrollView();
            scrollView.PagingEnabled = true;
            scrollView.ScrollEnabled = true;
            
            scrollView.DirectionalLockEnabled = true;
            scrollView.ShowsVerticalScrollIndicator = false;
            scrollView.ShowsHorizontalScrollIndicator = false;
            
            scrollView.Delegate = new ScrollViewDelegate(this);
            AddSubview(scrollView);
        }
        
        public int GetImageIndex() {
            if (dataSource == null) return 0;            
            float index = scrollView.ContentOffset.X / (scrollView.ContentSize.Width / dataSource.Count);
            return (int)index;
        }
        
        /// <summary>
        /// Holds the index of the current view while a resize is occurring.
        /// </summary>
        private int currentViewIndex;
        
        public void Resize(SizeF size, double duration) {
            if (null == dataSource) return;
            
            int currentViewIndex = GetImageIndex();
            
            UIView currentImage = views[currentViewIndex];
            currentImage.RemoveFromSuperview();
            AddSubview(currentImage);
            BringSubviewToFront(currentImage);
            currentImage.Frame = new RectangleF(0, 0, currentImage.Frame.Width, currentImage.Frame.Height);
            
            scrollView.Hidden = true;
            SetNeedsDisplay();
            
            UIView.Animate(duration, 0, UIViewAnimationOptions.LayoutSubviews,
                delegate() {
                    currentImage.Frame = new RectangleF(0, 0, size.Width, size.Height);
                }, 
                delegate() {
                    currentImage.RemoveFromSuperview();
                    scrollView.AddSubview(currentImage);
                    currentImage.Frame = new RectangleF(currentViewIndex * Frame.Width, 0, currentImage.Frame.Width, currentImage.Frame.Height);
                    
                    scrollView.ContentOffset = new PointF(currentViewIndex * Frame.Width, 0);
                    scrollView.Hidden = false;
                });
            
            this.Frame = new RectangleF(Frame.X, Frame.Y, size.Width, size.Height);
            
            // hide all views but the current one - they'll mess up the animation
            for (int i = 0; i < dataSource.Count; i++) {
                if (null != views[i] && currentViewIndex != i) {
                    views[i].Frame = new RectangleF(i * size.Width, 0, size.Width, size.Height);
                }
            }
            
            scrollView.ContentSize = new SizeF(size.Width * dataSource.Count, size.Height);
        }
        
        
        private class ScrollViewDelegate : UIScrollViewDelegate {
            private readonly PagingScrollView view;
            internal ScrollViewDelegate(PagingScrollView view) {
                this.view = view;
            }
            
            public override void Scrolled (UIScrollView scrollView)
            {                
                SizeF bounds =  view.Frame.Size;
                int index = view.GetImageIndex();
                for (int i = Math.Max(0, index - 1); i < Math.Min(view.dataSource.Count, index + 2); i++) {
                    float x = i * bounds.Width;
                    if (null == view.views[i]) {
                        view.views[i] = view.dataSource.GetView(i);
                        view.scrollView.AddSubview(view.views[i]);
                    }
                    
                    view.views[i].Frame = new RectangleF(x, 0, view.Frame.Width, view.Frame.Height);
                }
            }
            
        }
        
    }
}

