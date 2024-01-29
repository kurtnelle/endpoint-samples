using GHIElectronics.Endpoint.Core;
using System;
using System.Collections.Generic;
using System.Device.Gpio.Drivers;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using GHIElectronics.Endpoint.Devices.Display;
using System.Diagnostics;

namespace WeatherApp
{
    public static class Display
    {
        static DisplayController Controller;
        public static void Initialize()
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

            Controller = new DisplayController(fbDisplay);
        }

        public static void Flush(byte[] data, int offet, int length)
        {
            Controller.Flush(data, offet, length);
        }


    }
}
