using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading;

using Avalonia;
using AvaloniaTouch.Desktop.Properties;
using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Devices.Display;
using GHIElectronics.Endpoint.Drivers.Avalonia.Input;
using GHIElectronics.Endpoint.Drivers.FocalTech.FT5xx6;

using SkiaSharp;

namespace AvaloniaTouch.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        

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

        //Loading Screen
        new Thread(() =>
        {

            var backlightPort = EPM815.Gpio.Pin.PD14 / 16;
            var backlightPin = EPM815.Gpio.Pin.PD14 % 16;
            
            var gpioBacklightController = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(backlightPort));

            gpioBacklightController.OpenPin(backlightPin, PinMode.Output);
            gpioBacklightController.Write(backlightPin, PinValue.High); // low is on


            SKBitmap bitmap = new SKBitmap(fbDisplay.Width, fbDisplay.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            bitmap.Erase(SKColors.Transparent);

            var total_step = 6;
            var step_length = 380 / total_step + 1;
            
            // Give about 7 seconds to load Avalonia UI libraries.
            for (int i = 0; i <= total_step; i++)
            {


                //Initialize the SkiaSharp Canvas
                using (var screen = new SKCanvas(bitmap))
                {
                    //Draw Logo
                    var img = Resources.logo;
                    var info = new SKImageInfo(200, 133);
                    var sk_img = SKBitmap.Decode(img, info);
                    screen.DrawBitmap(sk_img, 140, 20);

                    //Draw Avalonia Logo
                    var avalonlogo = Resources.avaloniaLogo;
                    var info2 = new SKImageInfo(400, 68);
                    var sk_imgAvaloniaLogo = SKBitmap.Decode(avalonlogo, info2);
                    screen.DrawBitmap(sk_imgAvaloniaLogo, 30, 160);

                    //Using SkiaTypeface
                    byte[] fontfile = Resources.Myriad_Pro_Bold_Italic;
                    var stream = new MemoryStream(fontfile);

                    // Draw Line

                    using (SKPaint line = new SKPaint())
                    {
                        line.Color = SKColors.Blue;
                        line.IsAntialias = true;
                        line.StrokeWidth = 20;
                        line.Style = SKPaintStyle.Fill;

                        //Rounds the ends of the line
                        line.StrokeCap = SKStrokeCap.Round;

                        if (50 + i * step_length < 430)
                        {
                            screen.DrawLine(50, 250, 50 + i * step_length, 250, line);
                        }
                        else
                        {
                            screen.DrawLine(50, 250, 430, 250, line);
                        }
                    }
                    // Flush to screen
                    var data = bitmap.Copy(SKColorType.Rgb565).Bytes;
                    displayController.Flush(data);
                    Thread.Sleep(1000);

                    Console.WriteLine($"i = {i}");
                }
            }
        })
        .Start();

        // Touch 
        var resetTouchPin = EPM815.Gpio.Pin.PF2 % 16;
        var resetTouchPort = EPM815.Gpio.Pin.PF2 / 16;

        var gpioTouchController = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(resetTouchPort));
        gpioTouchController.OpenPin(resetTouchPin);
        gpioTouchController.Write(resetTouchPin, PinValue.Low);

        Thread.Sleep(100);

        gpioTouchController.Write(resetTouchPin, PinValue.High);

        EPM815.I2c.Initialize(EPM815.I2c.I2c5);

        var touch = new FT5xx6Controller(EPM815.I2c.I2c5, EPM815.Gpio.Pin.PB11);

        var input = new InputDevice();
        input.EnableOnscreenKeyboard(displayController);

        var builder = BuildAvaloniaApp();

        touch.TouchDown += (a, b) =>
        {
            input.UpdateTouchPoint(b.X, b.Y, TouchEvent.Pressed);
        };

        touch.TouchUp += (a, b) =>
        {
            input.UpdateTouchPoint(b.X, b.Y, TouchEvent.Released);
        };

        return builder.StartLinuxFbDev(new string[] { "--fbdev" }, "/dev/fb0", 1, input);
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();




    
}
