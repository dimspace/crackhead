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
        
        private float GetViewY(SizeF bounds) {
            bool portrait = Frame.Width < Frame.Height;
            if (portrait) {
                return (UIScreen.MainScreen.Bounds.Height - bounds.Height) / 2;
            } else {
                return 0;
            }
        }
        
        public void RenderViews(int start, int end) {
            for (int i = start; i < end; i++) {
                if (null == views[i]) {
                    UIView view = dataSource.GetView(i);
                    views[i] = view;
                    
                    SizeF size = view.SizeThatFits(Frame.Size);
                    float y = GetViewY(size);
                    view.Frame = new RectangleF(i * Frame.Width, y, size.Width, size.Height);
                    scrollView.AddSubview(view);
                }
            }
        }
        
        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            
            scrollView.Frame = Frame;
#if DEBUG
            Console.WriteLine("layout " + Frame);
#endif
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
        
        public void FreeUnusedViews() {
            // FIXME remove all off screen views in the scroll viewer
        }
        
        public int GetImageIndex() {
            if (dataSource == null) return 0;            
            float index = scrollView.ContentOffset.X / (scrollView.ContentSize.Width / dataSource.Count);
            return (int)index;
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
            
            int currentViewIndex = GetImageIndex();
            
            UIView currentImage = views[currentViewIndex];
            
            float currentY = currentImage.Frame.Y;
            currentImage.RemoveFromSuperview();
            AddSubview(currentImage);
            BringSubviewToFront(currentImage);
            currentImage.Frame = new RectangleF(0, currentY, currentImage.Frame.Width, currentImage.Frame.Height);
            
#if DEBUG
            Console.WriteLine("Current index = {0}", currentViewIndex);
            Console.WriteLine("Current view = {0} {1}", currentImage, currentImage.Frame);
#endif
            
            //scrollView.Hidden = true;
            scrollView.RemoveFromSuperview();
            
            SetNeedsDisplay();
            SizeF newSize = currentImage.SizeThatFits(size);
            
            Animate(duration, 0, UIViewAnimationOptions.TransitionNone,
                delegate() {
                    currentImage.Frame = new RectangleF(0, currentY, newSize.Width, newSize.Height);
                }, 
                delegate() {
#if DEBUG
                    Console.WriteLine("animation done");
#endif
                    currentImage.RemoveFromSuperview();
                    scrollView.AddSubview(currentImage);
                    currentImage.Frame = new RectangleF(currentViewIndex * Frame.Width, 0, currentImage.Frame.Width, currentImage.Frame.Height);
                    
                    for (int i = 0; i < dataSource.Count; i++) {
                        if (null != views[i] && currentViewIndex != i) {
                            newSize = views[i].SizeThatFits(size);
                            views[i].Frame = new RectangleF(i * size.Width, 0, newSize.Width, newSize.Height);
                        }
                    }
            
                    scrollView.ContentSize = new SizeF(size.Width * dataSource.Count, size.Height);

                    scrollView.ContentOffset = new PointF(currentViewIndex * Frame.Width, 0);
                    AddSubview(scrollView);
                    //scrollView.Hidden = false;
                });
            
            this.Frame = new RectangleF(Frame.X, Frame.Y, size.Width, size.Height);            
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
                        
                        SizeF size = view.views[i].SizeThatFits(view.Frame.Size);
                        view.views[i].Frame = new RectangleF(x, 
                                view.GetViewY(size), size.Width, size.Height);
                    }
                }
            }
            
        }
        
    }
}

