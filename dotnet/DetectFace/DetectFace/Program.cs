using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Devices.Camera;
using GHIElectronics.Endpoint.Devices.Display;
using GHIElectronics.Endpoint.Devices.UsbHost;
using SkiaSharp;
using DetectFace.Properties;
using OpenCvSharp;

namespace EndpointOpenCV
{
    internal class Program
    {

        static void Main(string[] args)
        {
            // initialize LCD
            var backlightPort = EPM815.Gpio.Pin.PD14 / 16;
            var backlightPin = EPM815.Gpio.Pin.PD14 % 16;

            var gpioDriver = new LibGpiodDriver(backlightPort);
            var gpioController = new GpioController(PinNumberingScheme.Logical, gpioDriver);

            gpioController.OpenPin(backlightPin, PinMode.Output);
            gpioController.Write(backlightPin, PinValue.High); // low is on

            var lockobj = new object();

            var configuration = new FBDisplay.Configuration()
            {
                Clock = 10000,
                Width = 480,
                Hsync_start = 480 + 2,
                Hsync_end = 480 + 2 + 41,
                Htotal = 480 + 2 + 41 + 2,
                Height = 272,
                Vsync_start = 272 + 2,
                Vsync_end = 272 + 2 + 10,
                Vtotal = 272 + 2 + 10 + 2,

            };

            var fbDisplay = new FBDisplay(configuration);

            var displayController = new DisplayController(fbDisplay);


            SKBitmap bitmap = new SKBitmap(480, 272, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            bitmap.Erase(SKColors.Transparent);

            // Initialize USB host for webcam
            var usbhostController = new UsbHostController();
            var webcamDetected = false;

            usbhostController.OnConnectionChangedEvent += (a, b) =>
            {
                if (b.DeviceStatus == DeviceConnectionStatus.Connected)
                {
                    webcamDetected = true;
                }
            };

            usbhostController.Enable();

            while (webcamDetected == false)
            {

                Thread.Sleep(1);
            }

            // OpenCV stub
            //https://github.com/opencv/opencv/tree/master/data/haarcascades            
            var cascade = new CascadeClassifier( @"/root/.epdata/DetectFace/haarcascade_frontalface_alt.xml");




            var webcam = new Webcam();
            // Make sure the web-cam supports 320x240 resolution or change to resolution your web-cam supports
            // Large resolution will slow down the speed.
            var setting = new CameraConfiguration()
            {
                Width = 320,
                Height = 240,
                ImageFormat = Format.Jpeg,
            };

            byte[] camData = null;
            webcam.Setting = setting;

            webcam.VideoStreamStart();

            webcam.FrameReceivedEvent += (a, b) =>
            {
               
                lock (lockobj)
                {
                    camData = b;
                }
            };

            var info = new SKImageInfo(webcam.Setting.Width, webcam.Setting.Height);
            var canvas = new SKCanvas(bitmap);
            while (true)
            {
                Mat srcImage = null;

                if (camData == null)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                lock (lockobj)
                {
                    if (camData != null)
                    {
                        srcImage = Cv2.ImDecode(camData, ImreadModes.Unchanged);
                    }
                }
               
                if (srcImage != null)
                {
                    var skiaImg = SKBitmap.Decode(camData, info);
                    var background = Resources.background;
                    var backgroundInfo = new SKImageInfo(480, 272);
                    var back_img = SKBitmap.Decode(background, backgroundInfo);
                    canvas.DrawBitmap(back_img, 0, 0);

                   
                    var detect = Resources.detect;
                    var detectInfo = new SKImageInfo(98, 98);
                    var detect_img = SKBitmap.Decode(detect, detectInfo);
                    

                    if (skiaImg != null)
                    {

                        canvas.DrawBitmap(skiaImg, 10, 17);
  
                        var grayImage = new Mat();
                        

                        Cv2.CvtColor(srcImage, grayImage, ColorConversionCodes.BGRA2GRAY);
                        Cv2.EqualizeHist(grayImage, grayImage);
                       ;
                        //detect face
                        var faces = cascade.DetectMultiScale(
                            image: grayImage,
                            scaleFactor: 1.1,
                            minNeighbors: 2,
                            flags: HaarDetectionTypes.DoRoughSearch | HaarDetectionTypes.ScaleImage,
                            minSize: new OpenCvSharp.Size(30, 30)
                            );

                        if (faces != null && faces.Length > 0)
                        {
                            foreach (var faceRect in faces)
                            {
                                using (SKPaint paint = new SKPaint())
                                {
                                    paint.Color = SKColor.Parse("#FF0977aa");
                                    paint.IsAntialias = true;
                                    paint.StrokeWidth = 3;
                                    paint.Style = SKPaintStyle.Stroke;
                                    canvas.DrawRect(faceRect.X+5, faceRect.Y, faceRect.Width+5, faceRect.Height+25, paint); //arguments are x position, y position, radius, and paint
                                }
                                canvas.DrawBitmap(detect_img, 360, 71);

                            }
                        }
                    }
                }
               
                var data = bitmap.Copy(SKColorType.Rgb565).Bytes;

                
                displayController.Flush(data);

                Thread.Sleep(10);
            }



        }

    }
}

