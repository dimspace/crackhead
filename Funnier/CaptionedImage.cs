using System;
using System.Drawing;
using System.Threading;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

namespace Funny
{
    public class CaptionedImage : UIView
    {
        private const int CaptionHeight = 70;
        private UIImage image;
        private UIImageView imageView;
        private readonly UILabel captionLabel;
        
        public CaptionedImage (PhotoInfo photoInfo)
        {
            NSData data = FileCacher.LoadUrl(photoInfo.Url, false);
            
            if (null == data) {
                if (Reachability.RemoteHostStatus().Equals(NetworkStatus.ReachableViaWiFiNetwork)) {
                    ThreadPool.QueueUserWorkItem(delegate {
                        data = FileCacher.LoadUrl(photoInfo.Url, true);
                        if (null == data) {
                            // this is not expected
                        } else {
                            InvokeOnMainThread(delegate {
                                SetImage(data); 
                            });
                        }
                    });
                } else {
                    // FIXME otherwise - display a different image?  can't download right now
                    // this should be somewhat uncommon, because it means we downloaded the 
                    // photo info (which required a network connection), but not the photo
                }
            } else {
                SetImage(data);
            }
            
            captionLabel = new UILabel();
            captionLabel.Text = photoInfo.Caption;
            //Times New Roman  TimesNewRomanPS-ItalicMT
//Times New Roman  TimesNewRomanPS-BoldMT
//Times New Roman  TimesNewRomanPSMT
//Times New Roman  TimesNewRomanPS-BoldItalicMT
            captionLabel.Font = UIFont.FromName("TimesNewRomanPS-ItalicMT", 14);
            
            captionLabel.LineBreakMode = UILineBreakMode.WordWrap;
            captionLabel.Lines = 2;
            captionLabel.ContentMode = UIViewContentMode.Bottom;
            captionLabel.TextAlignment = UITextAlignment.Center;

            captionLabel.BackgroundColor = UIColor.Clear;
   
            if (null != imageView) {
                PositionCaption(imageView.Frame);
            }
            AddSubview(captionLabel);
            this.BackgroundColor = UIColor.White;
        }
        
        public override SizeF SizeThatFits(SizeF size)
        {
            Console.WriteLine("SizeThatFits");
            size = GetImageFrame(size).Size;
            return new SizeF(size.Width, size.Height + 25);
        }
        
        private void SetImage(NSData data) {
            this.image = UIImage.LoadFromData(data);
            imageView = new UIImageView(image);
            imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
        
            imageView.Frame = GetImageFrame(Frame.Size);
            AddSubview(imageView);
        }
        
        private void PositionCaption(RectangleF bounds) {
            
            // prevent the text from running off the bottom of the screen in landscape mode
            float y = Math.Min (bounds.Height-40, Frame.Height - CaptionHeight - 10);
            
            var frame = new RectangleF(bounds.X + 5, y,
                                                  bounds.Width - 10, CaptionHeight);
            captionLabel.Frame = frame;
        }
        
        private RectangleF GetImageFrame(SizeF size) {
            float newHeight = (image.Size.Height * size.Width) / image.Size.Width;
            return new RectangleF(0,0, size.Width, newHeight);
        }
        
        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
#if DEBUG
            Console.WriteLine("CaptionedImage.LayoutSubviews");
#endif
            if (null != image) {
                imageView.Frame = GetImageFrame(Frame.Size);
                PositionCaption(imageView.Frame);
            }
        }
        
#if DEBUG
        public override string ToString ()
        {
            return captionLabel.Text;
        }
#endif
    }
}

