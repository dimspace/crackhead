using System;
using System.Drawing; 
using System.Diagnostics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

namespace Funny
{
    public delegate void Scrolled(int index);
    
    public class PagingScrollView : UIView, IResizable
    {
        private PagingViewDataSource dataSource;
        private UIView[] views;
        private readonly UIScrollView scrollView;
        public event Scrolled OnScroll;
        
        public UIScrollView ScrollView {
            get {
                return scrollView;
            }
        }
        
        public PagingScrollView(RectangleF frame) : base(frame) {
            scrollView = new UIScrollView();
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
                    view.LayoutSubviews();
                    scrollView.AddSubview(view);
                }
            }
        }
        
        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            
            scrollView.Frame = Frame;

            Debug.WriteLine("layout {0}", Frame);
        }
        
        public void FreeUnusedViews() {
            // FIXME remove all off screen views in the scroll viewer
        }
        
        public int GetCurrentViewIndex() {
            if (dataSource == null) return 0;            
            float index = scrollView.ContentOffset.X / (scrollView.ContentSize.Width / dataSource.Count);
            return (int)index;
        }
        
        public void ScrollToView(int index) 
        {
            Debug.WriteLine("Scroll to view index {0}", index);
            scrollView.ContentOffset = new PointF(index * scrollView.Frame.Width, 0f);
            FireOnScroll();
        }

        /// <summary>
        /// Animate a resize (usually for a screen rotation).
        /// </summary>
        /// <param name='size'>
        /// Size.
        /// </param>
        /// <param name='duration'>
        /// Duration.
        /// </param>
        public void Resize(SizeF size, double duration) {
            if (null == dataSource) return;
            
            int currentViewIndex = GetCurrentViewIndex();
            
            UIView currentImage = views[currentViewIndex];
            float currentY = currentImage.Frame.Y;
            currentImage.RemoveFromSuperview();
            AddSubview(currentImage);
            BringSubviewToFront(currentImage);
            currentImage.Frame = new RectangleF(0, currentY, currentImage.Frame.Width, currentImage.Frame.Height);
            
            Debug.WriteLine("Current index = {0}", currentViewIndex);
            Debug.WriteLine("Current view = {0} {1}", currentImage, currentImage.Frame);
            
            scrollView.RemoveFromSuperview();
            
//            SetNeedsDisplay();
            
            UIView.Animate(duration, 0, UIViewAnimationOptions.TransitionNone,
                delegate() {
                    currentImage.Frame = new RectangleF(0, 0, size.Width, size.Height);
                    currentImage.LayoutSubviews();
                }, 
                delegate() {
                    Debug.WriteLine("animation done");

                    currentImage.RemoveFromSuperview();
                    scrollView.AddSubview(currentImage);
                    
                    // we have to adjust all the photo x origins, otherwise they'll overlap other images
                    for (int i = 0; i < dataSource.Count; i++) {
                        if (null != views[i]) { // && currentViewIndex != i) {
                            var currentFrame = views[i].Frame;
                            views[i].Frame = new RectangleF(i * size.Width, currentFrame.Y, currentFrame.Size.Width, currentFrame.Size.Height);
                        }
                    }
                    // but I don't want to incur the cost of trying to exactly resize and reposition every image - we 
                    // already do that lazily on scroll.  For now, just position the images around the currently selected one.
                    for (int i = Math.Max(0, currentViewIndex - 1); i < Math.Min(dataSource.Count, currentViewIndex + 2); i++) {
                        if (null != views[i] && currentViewIndex != i) {
                            views[i].Frame = new RectangleF(i * size.Width, 0, size.Width, size.Height);
                            views[i].LayoutSubviews();
                        }
                    }
            
                    scrollView.ContentSize = new SizeF(size.Width * dataSource.Count, size.Height);

                    scrollView.ContentOffset = new PointF(currentViewIndex * Frame.Width, 0);
                    AddSubview(scrollView);
                    //scrollView.Hidden = false;
                });
        }
        
        private void FireOnScroll() {
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
                views[i].LayoutSubviews();
            }
            
            if (null != OnScroll) {
                OnScroll(index);
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

