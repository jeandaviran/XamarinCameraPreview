using App10.Widget;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace App10
{
    public partial class MainPage : ContentPage
    {
        bool isRunning;
        Image imgTemp;
        List<ImgSource> imgs = new List<ImgSource>();
        public MainPage()
        {
            InitializeComponent();
            //TakePhotoButton.Clicked += TakePhotoButton_Clicked;

            var tapGestureRecognizer = new TapGestureRecognizer();
            Photo1.GestureRecognizers.Add(tapGestureRecognizer);
            Photo2.GestureRecognizers.Add(tapGestureRecognizer);
            Photo3.GestureRecognizers.Add(tapGestureRecognizer);
            Photo4.GestureRecognizers.Add(tapGestureRecognizer);
            Photo5.GestureRecognizers.Add(tapGestureRecognizer);
            Photo6.GestureRecognizers.Add(tapGestureRecognizer);
            Photo7.GestureRecognizers.Add(tapGestureRecognizer);
            Photo8.GestureRecognizers.Add(tapGestureRecognizer);
            Photo9.GestureRecognizers.Add(tapGestureRecognizer);
            tapGestureRecognizer.Tapped += new EventHandler((e, s) => TakePhoto(e));
        }
        async void TakePhotoButton_Clicked(object sender, System.EventArgs e)
        {
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted)
            {
                var cameraPage = new CameraPage();
                cameraPage.OnPhotoResult += CameraPage_OnPhotoResult;
                await Navigation.PushModalAsync(cameraPage);
            }
        }
        async Task TakePhoto(object sender)
        {
            if (!isRunning)
            {
                isRunning = true;
                try
                {
                    imgTemp = (Image)sender;
                    string source = imgTemp.Source.ToString();
                    if (source != "File: img_camera_preview.jpg")
                    {
                        var sourceVysor = imgs.FirstOrDefault(x => x.Key == imgTemp.Id.ToString());
                        await Navigation.PushAsync(new ImageViewerPage(sourceVysor.Source));
                        isRunning = false;
                        return;
                    }

                    var status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status == PermissionStatus.Granted)
                    {
                        var cameraPage = new CameraPage();
                        cameraPage.OnPhotoResult += CameraPage_OnPhotoResult;
                        await Navigation.PushModalAsync(cameraPage);
                    }
                }
                catch (Exception)
                {
                }
                isRunning = false;
            }
        }
        async void CameraPage_OnPhotoResult(PhotoResultEventArgs result)
        {
            await Navigation.PopModalAsync();
            if (!result.Success)
                return;

            //Photo.Source = ImageSource.FromStream(() => new MemoryStream(result.Image));
            imgs.Add(new ImgSource() { Key = imgTemp.Id.ToString(), Source = ImageSource.FromStream(() => new MemoryStream(result.Image)) }); ;
            imgTemp.Source = ImageSource.FromStream(() => new MemoryStream(result.ImageCompress));
            //string base64String = Convert.ToBase64String(result.Image, 0, result.Image.Length);
        }
    }

    public class ImgSource
    {
        public string Key { get; set; }
        public ImageSource Source { get; set; }
    }
}
