using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

namespace Funny
{
    public class CaptionedImage : UIView
    {
        private readonly UIImage image;
        private readonly UIImageView imageView;
        private readonly UILabel captionLabel;
        
        public CaptionedImage (UIImage image, string caption)
        {
            this.image = image;
            imageView = new UIImageView(image);
            
            captionLabel = new UILabel();
            captionLabel.Text = caption;
            //Times New Roman  TimesNewRomanPS-ItalicMT
//Times New Roman  TimesNewRomanPS-BoldMT
//Times New Roman  TimesNewRomanPSMT
//Times New Roman  TimesNewRomanPS-BoldItalicMT
            captionLabel.Font = UIFont.FromName("TimesNewRomanPS-ItalicMT", 14);
            
            imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
            imageView.Frame = GetImageFrame(Frame.Size);
            AddSubview(imageView);
            
#if DEBUG
            // FIXME debugging
//            this.BackgroundColor = UIColor.Green;
#endif
            
            captionLabel.Opaque = false;
            captionLabel.ContentMode = UIViewContentMode.Center;
            captionLabel.TextAlignment = UITextAlignment.Center;
            captionLabel.BackgroundColor = UIColor.Clear;
                    
    //                        lblCaption.Center = new PointF(bounds.Width / 2, bounds.Height + lblCaption.Frame.Height);
            PositionCaption(imageView.Frame);
            AddSubview(captionLabel);
        }
        
        private void PositionCaption(RectangleF bounds) {
            
            // prevent the text from running off the bottom of the screen in landscape mode
            float y = Math.Min (bounds.Height-40, Frame.Height - 60);
            
            var frame = new RectangleF(bounds.X + 5, y,
                                                  bounds.Width - 10, 50);
            captionLabel.Frame = frame;
        }
        
        private RectangleF GetImageFrame(SizeF size) {
            float newHeight = (image.Size.Height * size.Width) / image.Size.Width;
            return new RectangleF(0,0, size.Width, newHeight);
        }
        
        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            
            imageView.Frame = GetImageFrame(Frame.Size);
            PositionCaption(imageView.Frame);
        }
    }
}

