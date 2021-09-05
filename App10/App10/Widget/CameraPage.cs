using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace App10.Widget
{
    public class CameraPage : ContentPage
    {
        public delegate void PhotoResultEventHandler(PhotoResultEventArgs result);

        public event PhotoResultEventHandler OnPhotoResult;

        public void SetPhotoResult(byte[] image, byte[] imageCompress, int width = -1, int height = -1)
        {
            OnPhotoResult?.Invoke(new PhotoResultEventArgs(image, imageCompress, width, height));
        }

        public void Cancel()
        {
            OnPhotoResult?.Invoke(new PhotoResultEventArgs());
        }

    }

    public class PhotoResultEventArgs : EventArgs
    {

        public PhotoResultEventArgs()
        {
            Success = false;
        }

        public PhotoResultEventArgs(byte[] image, byte[] imageCompress, int width, int height)
        {
            Success = true;
            Image = image;
            ImageCompress = imageCompress;
            Width = width;
            Height = height;
        }

        public byte[] Image { get; private set; }
        public byte[] ImageCompress { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool Success { get; private set; }
    }
}
