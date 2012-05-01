using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

namespace Funny
{
    public class CaptionedImage : UIView, IResizable
    {
        private readonly UIImage image;
        private readonly UIImageView imageView;
        
        public CaptionedImage (UIImage image, string caption)
        {
            this.image = image;
            imageView = new UIImageView(image);
            imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
            imageView.Frame = GetImageFrame(Frame.Size);
            AddSubview(imageView);
            this.BackgroundColor = UIColor.Green;
        }
        
        public void Resize(SizeF size, double duration) {
            this.Frame = GetImageFrame(size);
        }
        
        private RectangleF GetImageFrame(SizeF size) {
            float newHeight = (image.Size.Height * size.Width) / image.Size.Width;
            return new RectangleF(0,0, size.Width, newHeight);
        }
        
        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            
            imageView.Frame = GetImageFrame(Frame.Size);
        }
    }
}

