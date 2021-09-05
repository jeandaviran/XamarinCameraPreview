using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace App10
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageViewerPage : ContentPage
    {
        public ImageViewerPage(ImageSource image)
        {
            InitializeComponent();
            img.Source = image;
        }

        private void ImageButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }
    }
}