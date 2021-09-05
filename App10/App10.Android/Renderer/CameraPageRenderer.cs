using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using App10.Droid.Renderer;
using App10.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Forms.Platform.Android;

[assembly: Xamarin.Forms.ExportRenderer(typeof(CameraPage), typeof(CameraPageRenderer))]
namespace App10.Droid.Renderer
{
    public class CameraPageRenderer : PageRenderer, TextureView.ISurfaceTextureListener,
                                      TextureView.IOnClickListener,
                                      Android.Hardware.Camera.IPictureCallback,
                                      Android.Hardware.Camera.IAutoFocusCallback

    {
        Activity CurrentContext => MainActivity.Instance;
        bool isRunning = false;

        public CameraPageRenderer(Context context) : base(context)
        {

        }

        RelativeLayout mainLayout;
        TextureView liveView;
        PaintCodeButton capturePhotoButton;

        Android.Hardware.Camera camera;

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Page> e)
        {
            base.OnElementChanged(e);
            SetupUserInterface();
            SetupEventHandlers();
        }

        void SetupUserInterface()
        {
            mainLayout = new RelativeLayout(Context);

            liveView = new TextureView(Context);
            RelativeLayout.LayoutParams liveViewParams = new RelativeLayout.LayoutParams(
                RelativeLayout.LayoutParams.MatchParent,
                RelativeLayout.LayoutParams.MatchParent);
            liveView.LayoutParameters = liveViewParams;
            mainLayout.AddView(liveView);

            capturePhotoButton = new PaintCodeButton(Context);
            RelativeLayout.LayoutParams captureButtonParams = new RelativeLayout.LayoutParams(
                RelativeLayout.LayoutParams.WrapContent,
                RelativeLayout.LayoutParams.WrapContent);
            captureButtonParams.Height = 120;
            captureButtonParams.Width = 120;
            capturePhotoButton.LayoutParameters = captureButtonParams;
            mainLayout.AddView(capturePhotoButton);

            AddView(mainLayout);
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);
            if (!changed)
                return;
            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);
            mainLayout.Measure(msw, msh);
            mainLayout.Layout(0, 0, r - l, b - t);

            capturePhotoButton.SetX(mainLayout.Width / 2 - 60);
            capturePhotoButton.SetY(mainLayout.Height - 200);
        }

        public void SetupEventHandlers()
        {
            capturePhotoButton.Click += async (sender, e) =>
            {
                if (!isRunning)
                {
                    isRunning = true;
                    camera.TakePicture(null, null, this);                    
                }
            };
            liveView.SurfaceTextureListener = this;
            liveView.SetOnClickListener(this);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                (Element as CameraPage).Cancel();
                return false;
            }
            return base.OnKeyDown(keyCode, e);
        }

        private void StopCamera()
        {
            camera.StopPreview();
            camera.Release();
        }

        private void StartCamera()
        {
            camera.SetDisplayOrientation(90);            
            camera.StartPreview();
            camera.CancelAutoFocus();
            camera.AutoFocus(this);
        }

        #region TextureView.ISurfaceTextureListener implementations

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            //If authorization not granted for camera
            if (ContextCompat.CheckSelfPermission(CurrentContext, Manifest.Permission.Camera) != Permission.Granted)
                //ask for authorization
                ActivityCompat.RequestPermissions(CurrentContext, new String[] { Manifest.Permission.Camera }, 50);
            else
            {
                camera = Android.Hardware.Camera.Open();
                var parameters = camera.GetParameters();
                var aspect = ((decimal)height) / ((decimal)width);

                // Find the preview aspect ratio that is closest to the surface aspect
                var previewSize = parameters.SupportedPreviewSizes
                                            .OrderBy(s => Math.Abs(s.Width / (decimal)s.Height - aspect))
                                            .First();

                System.Diagnostics.Debug.WriteLine($"Preview sizes: {parameters.SupportedPreviewSizes.Count}");
                //------------
                Android.Hardware.Camera.Size size = SizeCamera(parameters);
                //----------
                parameters.SetPreviewSize(previewSize.Width, previewSize.Height);
                parameters.SetPictureSize(size.Width, size.Height);
                parameters.SetRotation(90);
                //                             
                camera.SetParameters(parameters);
                camera.SetPreviewTexture(surface);

                StartCamera();                
            }
        }
        private Android.Hardware.Camera.Size SizeCamera(Android.Hardware.Camera.Parameters parameters)
        {
            List<Android.Hardware.Camera.Size> sizes = parameters.SupportedPictureSizes.ToList();
            Android.Hardware.Camera.Size size = sizes[0];
            for (int i = 0; i < sizes.Count; i++)
            {
                if (sizes[i].Width > size.Width)
                    size = sizes[i];
            }
            return size;
        }
        public bool OnSurfaceTextureDestroyed(Android.Graphics.SurfaceTexture surface)
        {
            StopCamera();
            return true;
        }

        public void OnSurfaceTextureSizeChanged(Android.Graphics.SurfaceTexture surface, int width, int height)
        {
        }

        public void OnSurfaceTextureUpdated(Android.Graphics.SurfaceTexture surface)
        {
        }

        public void OnShutter()
        {

        }        
        public async void OnPictureTaken(byte[] data, Android.Hardware.Camera camera)
        {
            camera.StopPreview();            
            var size = SizeCamera(camera.GetParameters());

            BitmapFactory.Options options = new BitmapFactory.Options()
            {
                InPurgeable = true,
            };
            Bitmap image_original = BitmapFactory.DecodeByteArray(data, 0, data.Length, options);

            byte[] imageBytes = null;

            using (var imageStream = new System.IO.MemoryStream())
            {
                await image_original.CompressAsync(Bitmap.CompressFormat.Jpeg, 75, imageStream);
                image_original.Recycle();
                imageBytes = imageStream.ToArray();
            }

            var imageCompress = ResizeImage(imageBytes, 120, 120, 65);
            (Element as CameraPage).SetPhotoResult(imageBytes, imageCompress, size.Width, size.Height);
            //camera.StartPreview();
            isRunning = false;

        }

        public void OnAutoFocus(bool success, Android.Hardware.Camera camera)
        {
            camera.CancelAutoFocus();
            var parameters = camera.GetParameters();
            if (parameters.FocusMode != Android.Hardware.Camera.Parameters.FocusModeContinuousPicture)
            {
                parameters.FocusMode = Android.Hardware.Camera.Parameters.FocusModeContinuousPicture;

                if (parameters.MaxNumFocusAreas > 0)
                {
                    parameters.FocusAreas = null;
                }
                camera.SetParameters(parameters);
                camera.StartPreview();
            }
        }

        public void OnClick(View v)
        {
            camera.AutoFocus(this); //Screen device tap autofocus
        }
        public byte[] ResizeImage(byte[] data, float width, float height, int compressQuality)
        {
            try
            {
                BitmapFactory.Options options = new BitmapFactory.Options()
                {
                    InPurgeable = true,
                };
                Bitmap image_original = BitmapFactory.DecodeByteArray(data, 0, data.Length, options);
                return CreateImage(image_original, width, height, compressQuality);
            }
            catch (System.Exception ex)
            {
                return new byte[0];
            }
        }
        private byte[] CreateImage(Bitmap bitmap, float width, float height, int compressQuality = 100)
        {
            float newHeight = 0;
            float newWidth = 0;

            var originalHeight = bitmap.Height;
            var originalWidth = bitmap.Width;

            if (originalHeight > originalWidth)
            {
                newHeight = height;
                float ratio = originalHeight / height;
                newWidth = originalWidth / ratio;
            }
            else
            {
                newWidth = width;
                float ratio = originalWidth / width;
                newHeight = originalHeight / ratio;
            }

            Bitmap resizedImage = Bitmap.CreateScaledBitmap(bitmap, (int)newWidth, (int)newHeight, true);
            bitmap.Recycle();

            using (MemoryStream ms = new MemoryStream())
            {
                var qualityNum = (float)compressQuality / 100;
                resizedImage.Compress(Bitmap.CompressFormat.Jpeg, (int)compressQuality, ms);
                resizedImage.Recycle();
                return ms.ToArray();
            }
        }        
        #endregion
    }

    public class PaintCodeButton : Button
    {
        public PaintCodeButton(Context context) : base(context)
        {
            Background.Alpha = 0;
        }


        protected override void OnDraw(Canvas canvas)
        {
            var frame = new Rect(Left, Top, Right, Bottom);

            Paint paint;
            // Local Colors
            var color = Color.White;

            RectF bezierRect = new RectF(
                frame.Left + (float)Java.Lang.Math.Floor((frame.Width() - 120f) * 0.5f + 0.5f),
                frame.Top + (float)Java.Lang.Math.Floor((frame.Height() - 120f) * 0.5f + 0.5f),
                frame.Left + (float)Java.Lang.Math.Floor((frame.Width() - 120f) * 0.5f + 0.5f) + 120f,
                frame.Top + (float)Java.Lang.Math.Floor((frame.Height() - 120f) * 0.5f + 0.5f) + 120f);
            Android.Graphics.Path bezierPath = new Android.Graphics.Path();
            bezierPath.MoveTo(frame.Left + frame.Width() * 0.5f, frame.Top + frame.Height() * 0.08333f);
            bezierPath.CubicTo(frame.Left + frame.Width() * 0.41628f, frame.Top + frame.Height() * 0.08333f, frame.Left + frame.Width() * 0.33832f, frame.Top + frame.Height() * 0.10803f, frame.Left + frame.Width() * 0.27302f, frame.Top + frame.Height() * 0.15053f);
            bezierPath.CubicTo(frame.Left + frame.Width() * 0.15883f, frame.Top + frame.Height() * 0.22484f, frame.Left + frame.Width() * 0.08333f, frame.Top + frame.Height() * 0.3536f, frame.Left + frame.Width() * 0.08333f, frame.Top + frame.Height() * 0.5f);
            bezierPath.CubicTo(frame.Left + frame.Width() * 0.08333f, frame.Top + frame.Height() * 0.73012f, frame.Left + frame.Width() * 0.26988f, frame.Top + frame.Height() * 0.91667f, frame.Left + frame.Width() * 0.5f, frame.Top + frame.Height() * 0.91667f);
            bezierPath.CubicTo(frame.Left + frame.Width() * 0.73012f, frame.Top + frame.Height() * 0.91667f, frame.Left + frame.Width() * 0.91667f, frame.Top + frame.Height() * 0.73012f, frame.Left + frame.Width() * 0.91667f, frame.Top + frame.Height() * 0.5f);
            bezierPath.CubicTo(frame.Left + frame.Width() * 0.91667f, frame.Top + frame.Height() * 0.26988f, frame.Left + frame.Width() * 0.73012f, frame.Top + frame.Height() * 0.08333f, frame.Left + frame.Width() * 0.5f, frame.Top + frame.Height() * 0.08333f);
            bezierPath.Close();
            bezierPath.MoveTo(frame.Left + frame.Width(), frame.Top + frame.Height() * 0.5f);
            bezierPath.CubicTo(frame.Left + frame.Width(), frame.Top + frame.Height() * 0.77614f, frame.Left + frame.Width() * 0.77614f, frame.Top + frame.Height(), frame.Left + frame.Width() * 0.5f, frame.Top + frame.Height());
            bezierPath.CubicTo(frame.Left + frame.Width() * 0.22386f, frame.Top + frame.Height(), frame.Left, frame.Top + frame.Height() * 0.77614f, frame.Left, frame.Top + frame.Height() * 0.5f);
            bezierPath.CubicTo(frame.Left, frame.Top + frame.Height() * 0.33689f, frame.Left + frame.Width() * 0.0781f, frame.Top + frame.Height() * 0.19203f, frame.Left + frame.Width() * 0.19894f, frame.Top + frame.Height() * 0.10076f);
            bezierPath.CubicTo(frame.Left + frame.Width() * 0.28269f, frame.Top + frame.Height() * 0.03751f, frame.Left + frame.Width() * 0.38696f, frame.Top, frame.Left + frame.Width() * 0.5f, frame.Top);
            bezierPath.CubicTo(frame.Left + frame.Width() * 0.77614f, frame.Top, frame.Left + frame.Width(), frame.Top + frame.Height() * 0.22386f, frame.Left + frame.Width(), frame.Top + frame.Height() * 0.5f);
            bezierPath.Close();

            paint = new Paint();
            paint.SetStyle(Android.Graphics.Paint.Style.Fill);
            paint.Color = (color);
            canvas.DrawPath(bezierPath, paint);

            paint = new Paint();
            paint.StrokeWidth = (1f);
            paint.StrokeMiter = (10f);
            canvas.Save();
            paint.SetStyle(Android.Graphics.Paint.Style.Stroke);
            paint.Color = (Color.Black);
            canvas.DrawPath(bezierPath, paint);
            canvas.Restore();

            RectF ovalRect = new RectF(
                frame.Left + (float)Java.Lang.Math.Floor(frame.Width() * 0.12917f) + 0.5f,
                frame.Top + (float)Java.Lang.Math.Floor(frame.Height() * 0.12083f) + 0.5f,
                frame.Left + (float)Java.Lang.Math.Floor(frame.Width() * 0.87917f) + 0.5f,
                frame.Top + (float)Java.Lang.Math.Floor(frame.Height() * 0.87083f) + 0.5f);
            Android.Graphics.Path ovalPath = new Android.Graphics.Path();
            ovalPath.AddOval(ovalRect, Android.Graphics.Path.Direction.Cw);

            paint = new Paint();
            paint.SetStyle(Android.Graphics.Paint.Style.Fill);
            paint.Color = (color);
            canvas.DrawPath(ovalPath, paint);

            paint = new Paint();
            paint.StrokeWidth = (1f);
            paint.StrokeMiter = (10f);
            canvas.Save();
            paint.SetStyle(Android.Graphics.Paint.Style.Stroke);
            paint.Color = (Color.Black);
            canvas.DrawPath(ovalPath, paint);
            canvas.Restore();
        }
    }
}