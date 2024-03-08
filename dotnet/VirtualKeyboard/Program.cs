using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Devices.Display;
using GHIElectronics.Endpoint.Drivers.FocalTech.FT5xx6;
using GHIElectronics.Endpoint.Drivers.VirtualKeyboard;
using Microsoft.VisualBasic;
using SkiaSharp;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;

namespace VirtualKeyboardExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var backlightPort = EPM815.Gpio.Pin.PD14 / 16;
            var backlightPin = EPM815.Gpio.Pin.PD14 % 16;

            var gpioDriver = new LibGpiodDriver(backlightPort);
            var gpioController = new GpioController(PinNumberingScheme.Logical, gpioDriver);

            gpioController.OpenPin(backlightPin, PinMode.Output);
            gpioController.Write(backlightPin, PinValue.High); // low is on

            const int SCREEN_WIDTH = 480;
            const int SCREEN_HEIGHT = 272;

            var configuration = new FBDisplay.Configuration()
            {
                Clock = 10000,
                Width = SCREEN_WIDTH,
                Hsync_start = SCREEN_WIDTH + 2,
                Hsync_end = SCREEN_WIDTH + 2 + 41,
                Htotal = SCREEN_WIDTH + 2 + 41 + 2,
                Height = SCREEN_HEIGHT,
                Vsync_start = SCREEN_HEIGHT + 2,
                Vsync_end = SCREEN_HEIGHT + 2 + 10,
                Vtotal = SCREEN_HEIGHT + 2 + 10 + 2,

               

            };

            var fbDisplay = new FBDisplay(configuration);

            var displayController = new DisplayController(fbDisplay);

            // Make sure I2c touch and interrupt pin are correct with the board.

            EPM815.I2c.Initialize(EPM815.I2c.I2c6);

            var touch = new FT5xx6Controller(EPM815.I2c.I2c6, EPM815.Gpio.Pin.PF12);

            var keyboard = new VirtualKeyboard(displayController);

            touch.TouchUp += (a, b) =>
            {

                keyboard.UpdateKey(b.X, b.Y);
            };

            keyboard.Show();

            // clear the keyboard on the screen

            SKBitmap bitmap = new SKBitmap(SCREEN_WIDTH, SCREEN_HEIGHT, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            bitmap.Erase(SKColors.Transparent);


            var data = bitmap.Copy(SKColorType.Rgb565).Bytes;

            displayController.Flush(data);

            Console.WriteLine($"keyboard text: {keyboard.Text}");

            Thread.Sleep(-1);
        }

        
    }
}
