using System;
using System.Globalization;
using EndpointClickRadio;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using GHIElectronics.Endpoint.Devices.Display;
using SkiaSharp;
using GHIElectronics.Endpoint.Core;
using static EndpointClickRadio.Resources;
using GHIElectronics.Endpoint.Drivers.FocalTech.FT5xx6;

var localDate = DateTime.Now;
Console.WriteLine(localDate.ToString());

var backlightPort = EPM815.Gpio.Pin.PD14 / 16;
var backlightPin = EPM815.Gpio.Pin.PD14 % 16;

var backlightDriver = new LibGpiodDriver((int)backlightPort);
var backlightController = new GpioController(PinNumberingScheme.Logical, backlightDriver);
backlightController.OpenPin(backlightPin);
backlightController.SetPinMode(backlightPin, PinMode.Output);
backlightController.Write(backlightPin, PinValue.High);

//Preset Stations
var preset1 = 89.3;
var preset2 = 94.7;
var preset3 = 96.3;
var preset4 = 97.1;
var preset5 = 97.9;
var preset6 = 101.1;
var preset7 = 101.9;
var preset8 = 106.7;

//Initialize FM Click module
var reset = EPM815.Gpio.Pin.PF4;
var cs = EPM815.Gpio.Pin.PA14;
FM_Click radio = new FM_Click(reset, cs);
double currentStation = 100;
int volume = 125;
radio.Channel = currentStation;
radio.Volume = volume;

//Initialize TouchScreen
var TouchResetPin = EPM815.Gpio.Pin.PF2 % 16;
var TouchResetPort = EPM815.Gpio.Pin.PF2 / 16;
var TouchController = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(TouchResetPort));
TouchController.OpenPin(TouchResetPin);
TouchController.Write(TouchResetPin, PinValue.Low);
Thread.Sleep(100);
TouchController.Write(TouchResetPin, PinValue.High);
EPM815.I2c.Initialize(EPM815.I2c.I2c5);
var touch = new FT5xx6Controller(EPM815.I2c.I2c5, EPM815.Gpio.Pin.PB11);
touch.TouchUp += Touch_TouchUp;

//Initialize Display
var screenWidth = 480;
var screenHeight = 272;
SKBitmap bitmap = new SKBitmap(screenWidth, screenHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
bitmap.Erase(SKColors.Transparent);

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



while (true) { 
using (var screen = new SKCanvas(bitmap))
{
        //Create Black Screen 
        screen.DrawColor(SKColors.Black);
        screen.Clear(SKColors.Black);

        // Draw background from resource
        var img = background;
        var info = new SKImageInfo(480, 272); // width and height of rect
        var sk_img = SKBitmap.Decode(img, info);
        screen.DrawBitmap(sk_img, 0, 0);

        // Font from Resources
        byte[] fontfile = Resources.LCD;
        Stream stream = new MemoryStream(fontfile);

        using (SKPaint currentText = new SKPaint())
        using (SKPaint presetText = new SKPaint())
        using (SKTypeface tf = SKTypeface.FromStream(stream))
        {
            // Current Station Text Properties
            currentText.Color = SKColors.Red;
            currentText.IsAntialias = true;
            currentText.StrokeWidth = 2;
            currentText.Style = SKPaintStyle.Fill;

            // Station Preset Text Properties
            presetText.Color = SKColors.White;
            presetText.IsAntialias = true;
            presetText.StrokeWidth = 2;
            presetText.Style = SKPaintStyle.Fill;

            // Current Station Font
            SKFont currentFont = new SKFont();
            currentFont.Size = 90;
            currentFont.Typeface = tf;
            SKTextBlob textBlob = SKTextBlob.Create(currentStation.ToString("F1"), currentFont);

            // Draw Current Station
            if (currentStation >= 100)
                screen.DrawText(textBlob, 152, 155, currentText);
            else
                screen.DrawText(textBlob, 170, 155, currentText);

            // Preset Buttons Font
            SKFont presetButtonsFont = new SKFont();
            presetButtonsFont.Size = 18;
            presetButtonsFont.Typeface = tf;

            // Draw Date and Time 
            SKTextBlob dateTime = SKTextBlob.Create(DateTime.Now.ToString(), presetButtonsFont);
            screen.DrawText(dateTime, 325, 37, presetText);

            // Preset 1
            SKTextBlob presetButton1 = SKTextBlob.Create(preset1.ToString("F1"), presetButtonsFont);
            if (preset1 >= 100)
                screen.DrawText(presetButton1, 32, 235, presetText);
            else
                screen.DrawText(presetButton1, 35, 235, presetText);

            // Preset 2
            SKTextBlob presetButton2 = SKTextBlob.Create(preset2.ToString("F1"), presetButtonsFont);
            if (preset2 >= 100)
                screen.DrawText(presetButton2, 81, 235, presetText);
            else
                screen.DrawText(presetButton2, 84, 235, presetText);

            // Preset 3
            SKTextBlob presetButton3 = SKTextBlob.Create(preset3.ToString("F1"), presetButtonsFont);
            if (preset3 >= 100)
                screen.DrawText(presetButton3, 129, 235, presetText);
            else
                screen.DrawText(presetButton3, 131, 235, presetText);

            // Preset 4
            SKTextBlob presetButton4 = SKTextBlob.Create(preset4.ToString("F1"), presetButtonsFont);
            if (preset4 >= 100)
                screen.DrawText(presetButton4, 177, 235, presetText);
            else
                screen.DrawText(presetButton4, 180, 235, presetText);

            // Preset 5
            SKTextBlob presetButton5 = SKTextBlob.Create(preset5.ToString("F1"), presetButtonsFont);
            if (preset5 >= 100)
                screen.DrawText(presetButton5, 226, 235, presetText);
            else
                screen.DrawText(presetButton5, 229, 235, presetText);

            // Preset 6
            SKTextBlob presetButton6 = SKTextBlob.Create(preset6.ToString("F1"), presetButtonsFont);
            if (preset6 >= 100)
                screen.DrawText(presetButton6, 275, 235, presetText);
            else
                screen.DrawText(presetButton6, 278, 235, presetText);

            // Preset 7
            SKTextBlob presetButton7 = SKTextBlob.Create(preset7.ToString("F1"), presetButtonsFont);
            if (preset7 >= 100)
                screen.DrawText(presetButton7, 324, 235, presetText);
            else
                screen.DrawText(presetButton7, 327, 235, presetText);

            // Preset 8
            SKTextBlob presetButton8 = SKTextBlob.Create(preset8.ToString("F1"), presetButtonsFont);
            if (preset8 >= 100)
                screen.DrawText(presetButton8, 373, 235, presetText);
            else
                screen.DrawText(presetButton8, 376, 235, presetText);
        }

        SKPaint volumeLineGreen = new SKPaint();
        volumeLineGreen.Color = SKColors.Green;
        volumeLineGreen.IsAntialias = true;
        volumeLineGreen.StrokeWidth = 15;
        volumeLineGreen.Style = SKPaintStyle.Fill;

        SKPaint volumeLineRed = new SKPaint();
        volumeLineRed.Color = SKColors.Red;
        volumeLineRed.IsAntialias = true;
        volumeLineRed.StrokeWidth = 15;
        volumeLineRed.Style = SKPaintStyle.Fill;

        for (int i = 82; i <= 215; i += 19)
        {
            screen.DrawLine(419, i, 453, i, volumeLineGreen);
        }

        switch (volume)
        {
            case 25:
                screen.DrawLine(419, 215, 453, 215, volumeLineRed);
                screen.DrawLine(419, 196, 453, 196, volumeLineRed);
                break;
            case 75:
                screen.DrawLine(419, 215, 453, 215, volumeLineRed);
                screen.DrawLine(419, 196, 453, 196, volumeLineRed);
                screen.DrawLine(419, 177, 453, 177, volumeLineRed);
                screen.DrawLine(419, 158, 453, 158, volumeLineRed);
                break;
            case 125:
                screen.DrawLine(419, 215, 453, 215, volumeLineRed);
                screen.DrawLine(419, 196, 453, 196, volumeLineRed);
                screen.DrawLine(419, 177, 453, 177, volumeLineRed);
                screen.DrawLine(419, 158, 453, 158, volumeLineRed);
                screen.DrawLine(419, 139, 453, 139, volumeLineRed);
                screen.DrawLine(419, 120, 453, 120, volumeLineRed);
                break;
            case 175:
                screen.DrawLine(419, 215, 453, 215, volumeLineRed);
                screen.DrawLine(419, 196, 453, 196, volumeLineRed);
                screen.DrawLine(419, 177, 453, 177, volumeLineRed);
                screen.DrawLine(419, 158, 453, 158, volumeLineRed);
                screen.DrawLine(419, 139, 453, 139, volumeLineRed);
                screen.DrawLine(419, 120, 453, 120, volumeLineRed);
                screen.DrawLine(419, 101, 453, 101, volumeLineRed);
                screen.DrawLine(419, 82, 453, 82, volumeLineRed);
                break;

        }

        //Flush to screen
        var data = bitmap.Copy(SKColorType.Rgb565).Bytes;
        displayController.Flush(data);
        Thread.Sleep(1);

        if (currentStation < 88.0) currentStation = 88.0;
        if (currentStation > 108.0) currentStation = 108;

        Thread.Sleep(100);
    }
 }
void Touch_TouchUp(FT5xx6Controller sender, FT5xx6Controller.TouchEventArgs e)
{
    Console.WriteLine("Touch Up " + e.X + ", " + e.Y);

    if (e.X >= 100 && e.X<=120 && e.Y >=115 && e.Y<=135)
    { 
        currentStation = currentStation - 0.1;
        FM_Click radio = new FM_Click(reset, cs);
        volume = volume;
        radio.Channel = currentStation;
        radio.Volume = volume;

        Console.WriteLine("Radio Station "+ currentStation.ToString());
        return;
    }



    if (e.X >= 340 && e.X <= 360 && e.Y >= 115 && e.Y <= 135)
    {
        currentStation = currentStation + 0.1;
        FM_Click radio = new FM_Click(reset, cs);
        volume = volume;
        radio.Channel = currentStation;
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Touch Preset 1
    if (e.X >= 30 && e.X <= 40 && e.Y >= 220 && e.Y <= 240)
    {       
        FM_Click radio = new FM_Click(reset, cs);
        currentStation = preset1;
        radio.Channel = currentStation;
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Touch Preset 2
    if (e.X >= 90 && e.X <= 110 && e.Y >= 220 && e.Y <= 240)
    {
        FM_Click radio = new FM_Click(reset, cs);
        currentStation = preset2;
        radio.Channel = currentStation;  
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Touch Preset 3
    if (e.X >= 130 && e.X <= 150 && e.Y >= 220 && e.Y <= 240)
    {
        FM_Click radio = new FM_Click(reset, cs);
        currentStation = preset3;
        radio.Channel = currentStation;
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Touch Preset 4
    if (e.X >= 175 && e.X <= 195 && e.Y >= 220 && e.Y <= 240)
    {
        FM_Click radio = new FM_Click(reset, cs);
        currentStation = preset4;
        radio.Channel = currentStation;
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Touch Preset 5
    if (e.X >= 230 && e.X <= 250 && e.Y >= 220 && e.Y <= 240)
    {
        FM_Click radio = new FM_Click(reset, cs);
        currentStation = preset5;
        radio.Channel = currentStation;
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Touch Preset 6
    if (e.X >= 280 && e.X <= 300 && e.Y >= 220 && e.Y <= 240)
    {
        FM_Click radio = new FM_Click(reset, cs);
        currentStation = preset6;
        radio.Channel = currentStation;
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Touch Preset 7
    if (e.X >= 320 && e.X <= 340 && e.Y >= 220 && e.Y <= 240)
    {
        FM_Click radio = new FM_Click(reset, cs);
        currentStation = preset7;
        radio.Channel = currentStation;
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Touch Preset 8
    if (e.X >= 380 && e.X <= 400 && e.Y >= 220 && e.Y <= 240)
    {
        FM_Click radio = new FM_Click(reset, cs);
        currentStation = preset8;
        radio.Channel = currentStation;
        radio.Volume = volume;
        Console.WriteLine("Radio Station " + currentStation.ToString());
        return;
    }

    //Volume Up
    if (e.X >= 420 && e.X <= 450 && e.Y >= 45 && e.Y <= 65)
    {
        volume = volume + 50;
        if (volume > 175)
            volume = 175;
        radio.Volume = volume;
        Console.WriteLine("Volume " + volume.ToString());
        return;
    }

    //Volume Down
    if (e.X >= 420 && e.X <= 450 && e.Y >= 230 && e.Y <= 250)
    {
        volume = volume - 50;
        if (volume < 0)
            volume = 0;
        radio.Volume = volume;
        Console.WriteLine("Volume " + volume.ToString());

        //Added to fix volume bug in module
        if (volume <= 0)
            volume = -25;

        return;
    }

}


