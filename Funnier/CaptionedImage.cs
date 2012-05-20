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
using System.Threading;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using FlickrCache;
using MonoTouchUtils;

/// <summary>
/// Author: Saxon D'Aubin
/// </summary>
namespace Funnier
{
    public class CaptionedImage : UIView
    {
        private const int CaptionHeight = 70;
        private volatile UIImage image;
        private volatile UIImageView imageView;
        private readonly UILabel captionLabel;
        
        public CaptionedImage (PhotosetPhoto photoInfo)
        {
                        
            captionLabel = new UILabel();
            captionLabel.Text = photoInfo.Title;
            //Times New Roman  TimesNewRomanPS-ItalicMT
//Times New Roman  TimesNewRomanPS-BoldMT
//Times New Roman  TimesNewRomanPSMT
//Times New Roman  TimesNewRomanPS-BoldItalicMT
            captionLabel.Font = UIFont.FromName("TimesNewRomanPS-ItalicMT", 
                                                (DeviceUtils.IsIPad() ? 20 : 14));
            
            captionLabel.LineBreakMode = UILineBreakMode.WordWrap;
            // set more lines than we need.  we resize to fit later
            captionLabel.Lines = 4;
            captionLabel.ContentMode = UIViewContentMode.Bottom;
            captionLabel.TextAlignment = UITextAlignment.Center;

            captionLabel.BackgroundColor = UIColor.Clear;
            //captionLabel.Layer.BorderColor = new CGColor (1f, 1f, 1f);
            //captionLabel.Layer.BorderWidth = 2;   
            
            NSData data = FileCacher.LoadUrl(photoInfo.Url, false);
            
            if (null == data) {
                if (Reachability.RemoteHostStatus().Equals(NetworkStatus.ReachableViaWiFiNetwork)) {
                    ThreadPool.QueueUserWorkItem(delegate {
                        data = FileCacher.LoadUrl(photoInfo.Url, true);
                        if (null == data) {
                            // this is not expected
                            Debug.WriteLine("Null data after LoadUrl: {0} - {1}", photoInfo.Title, photoInfo.Url);
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
                    Debug.WriteLine("No image downloaded: {0} - {1}", photoInfo.Title, photoInfo.Url);
#if DEBUG
                    var label = new UILabel();
                    label.Text = String.Format("No image downloaded: {0} - {1}", photoInfo.Title, photoInfo.Url);
                    AddSubview(label);
#endif
                }
            } else {
                SetImage(data);
            }

            AddSubview(captionLabel);
            this.BackgroundColor = UIColor.White;
        }

        private void SetImage(NSData data) {
            image = UIImage.LoadFromData(data);
            imageView = new UIImageView(image);
            imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
        
            AddSubview(imageView);
            BringSubviewToFront(captionLabel);
        }
        
        private void PositionCaption(RectangleF imageBounds) {
            var padding = 15;
            // the caption width is the width of our view bounds minus padding
            var width = Frame.Width - padding*2;
            
            // if the caption had to fit in the bounds of our entire view minus the left
            // and right padding, how large would its rect be?  We really only care about the height.
            var size = captionLabel.TextRectForBounds(
                new RectangleF(Frame.X + 15, Frame.Y, width, Frame.Height), 3);
            
            // prevent the text from running off the bottom of the screen in landscape mode
            float y = Math.Min(imageBounds.Y + imageBounds.Height + 40,
                               // we should always have 5 px of padding before running off the screen
                               Frame.Height - size.Height - 5);
            
            var frame = new RectangleF(padding, y, width, size.Height);
            captionLabel.Frame = frame;
            Debug.WriteLine("Caption frame: {0}, bounds: {1}, caption: {2}", frame, imageBounds, captionLabel.Text);
            
        }
        
        private RectangleF GetImageFrame(SizeF size) {
            // FIXME: we want to move the image a little up from center to give the caption room
            //var captionRoom = 18;
            
            // (mostly) center the image
            var imageSize = image.Size;
            var imageScale = Math.Min(size.Width/imageSize.Width, size.Height/imageSize.Height);
            var scaledImageSize = new SizeF(imageSize.Width*imageScale, imageSize.Height*imageScale);
            return new RectangleF(
                (float)Math.Floor(0.5f*(size.Width-scaledImageSize.Width)), 
                (float)Math.Floor(0.5f*(size.Height-scaledImageSize.Height)), 
                scaledImageSize.Width, scaledImageSize.Height);
        }
        
        public override void LayoutSubviews()
        {
//            base.LayoutSubviews ();

//            Debug.WriteLine("CaptionedImage.LayoutSubviews");
            if (null != image) {
                imageView.Frame = GetImageFrame(Frame.Size);
                PositionCaption(imageView.Frame);
                Debug.WriteLine("Image frame: {0}", imageView.Frame);
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

