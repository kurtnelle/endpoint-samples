using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Linq;
using System.Threading;

using Avalonia;
using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Devices.Display;
using GHIElectronics.Endpoint.Drivers.Avalonia.Input;
using GHIElectronics.Endpoint.Drivers.FocalTech.FT5xx6;
using Microsoft.VisualBasic;

namespace AvaloniaTouch.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] argso)
    {
        var backlightPort = EPM815.Gpio.Pin.PD14 / 16;
        var backlightPin = EPM815.Gpio.Pin.PD14 % 16;

        var gpioDriver = new LibGpiodDriver(backlightPort);
        var gpioController = new GpioController(PinNumberingScheme.Logical, gpioDriver);

        gpioController.OpenPin(backlightPin, PinMode.Output);
        gpioController.Write(backlightPin, PinValue.High); // low is on

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

        EPM815.I2c.Initialize(EPM815.I2c.I2c6);

        var touch = new FT5xx6Controller(EPM815.I2c.I2c6, EPM815.Gpio.Pin.PF12);


        var input = new InputDevice();
        input.EnableOnscreenKeyboard(displayController);

        var builder = BuildAvaloniaApp();
        var args = new string[] { "--fbdev" };

        touch.TouchDown += (a, b) =>
        {
            input.UpdateTouchPoint(b.X, b.Y, TouchEvent.Pressed);
        };

        touch.TouchUp += (a, b) =>
        {
            input.UpdateTouchPoint(b.X, b.Y, TouchEvent.Released);
        };

       

        if (args.Contains("--fbdev"))
        {
            SilenceConsole();

                
            return builder.StartLinuxFbDev(args, "/dev/fb0", 1, input);
            
        }





        return builder.StartWithClassicDesktopLifetime(args);
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()

            .LogToTrace();



    private static void SilenceConsole()
    {
        new Thread(() =>
        {
            Console.CursorVisible = false;
            while (true)
            {
                //Console.ReadKey(true);

                Thread.Sleep(10000);
            }
        })
        { IsBackground = true }.Start();
    }
}
