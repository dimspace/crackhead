using System;
using System.Drawing;
using System.Diagnostics;
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
            //captionLabel.Layer.BorderColor = new CGColor (1f, 1f, 1f);
            //captionLabel.Layer.BorderWidth = 2;
   
            if (null != imageView) {
                PositionCaption(imageView.Frame);
            }
            AddSubview(captionLabel);
            this.BackgroundColor = UIColor.White;
        }
        
        public override SizeF SizeThatFits(SizeF size)
        {
            if (size.Width > size.Height) {
                return size;
            }
            if (null == image) {
                return size;
            }
            size = GetImageFrame(size).Size;
            Debug.WriteLine("SizeThatFits " + size);
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
            float newHeight = image.Size.Height;
            float newWidth = image.Size.Width;
            float newXOrigin = 0;
                     
            if (image.Size.Width > size.Width) { 
                newWidth = size.Width;
                newHeight = (image.Size.Height * size.Width) / image.Size.Width;
            }
            if (newHeight > (size.Height-20)) {
                newHeight = size.Height-20;
                newWidth = image.Size.Width * size.Height / image.Size.Height;
                newXOrigin = (size.Width - newWidth) / 2; //
            }
            
            return new RectangleF(newXOrigin,0, newWidth, newHeight);
        } //
        
        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

//            Debug.WriteLine("CaptionedImage.LayoutSubviews");
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

