using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Drivers.FocalTech.FT5xx6;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Device.Gpio.Drivers;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherApp.Properties;

namespace WeatherApp
{
    public class Weather
    {
        SKBitmap bitmap;
        SKBitmap bitmapBackground;
        SKBitmap bitmapKeyboard;
        SKCanvas canvas;

        FT5xx6Controller touch;

        int city_x = 10;
        int city_y = 10;
        int city_w = 200;
        int city_h = 25;

        int keyboard_y = 80;
        int key_w = 48;
        int key_h = 48;

        string[][] key_chars;
        int[][] key_pos_x;
        int[][] key_pos_y;

        SKPaint paintWhite = new SKPaint() { Style = SKPaintStyle.Stroke, Color = SKColors.White };
        SKPaint paintWhiteFill = new SKPaint() { Style = SKPaintStyle.Fill, Color = SKColors.White };
        SKPaint paintBlack = new SKPaint() { Style = SKPaintStyle.Fill, Color = SKColors.Black };

        WeatherInfo weatherInfo;

      

        public string CurrentLocation { get; set; }  = string.Empty;

        public Weather()
        {
            bitmap = new SKBitmap(480, 272, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

            var img = Resources.backgnd;
            var info = new SKImageInfo(480, 272); // width and height of rect
            bitmapBackground = SKBitmap.Decode(img, info);

            img = Resources.keyboard;
            info = new SKImageInfo(480, 192); // width and height of rect
            bitmapKeyboard = SKBitmap.Decode(img, info);

            canvas = new SKCanvas(bitmap);
        
            weatherInfo = new WeatherInfo("your API key");

            Display.Initialize();
            InitTouch();
            InitKeys();
        }

        private void InitTouch()
        {
            var resetPin = EPM815.Gpio.Pin.PF2 % 16;
            var resetPort = EPM815.Gpio.Pin.PF2 / 16;

            var gpioController = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(resetPort));
            gpioController.OpenPin(resetPin);
            gpioController.Write(resetPin, PinValue.Low);

            Thread.Sleep(100);

            gpioController.Write(resetPin, PinValue.High);


            EPM815.I2c.Initialize(EPM815.I2c.I2c5);

            touch = new FT5xx6Controller(EPM815.I2c.I2c5, EPM815.Gpio.Pin.PB11);

            touch.TouchUp += Touch_TouchUp;
        }

        private void InitKeys()
        {
            key_pos_x = new int[4][];
            key_pos_y = new int[4][];
            key_chars = new string[4][];


            key_pos_x[0] = new int[10];
            key_pos_y[0] = new int[10] { keyboard_y, keyboard_y, keyboard_y, keyboard_y, keyboard_y, keyboard_y, keyboard_y, keyboard_y, keyboard_y, keyboard_y };
            key_chars[0] = new string[10] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" };

            for (int i = 0; i < key_pos_x[0].Length; i++)
            {
                key_pos_x[0][i] = i * key_w;
            }


            key_pos_x[1] = new int[9];
            key_pos_y[1] = new int[9] { keyboard_y + 1 * key_h, keyboard_y + 1 * key_h, keyboard_y + 1 * key_h, keyboard_y + 1 * key_h, keyboard_y + 1 * key_h, keyboard_y + 1 * key_h, keyboard_y + 1 * key_h, keyboard_y + 1 * key_h, keyboard_y + 1 * key_h };
            key_chars[1] = new string[9] { "a", "s", "d", "f", "g", "h", "j", "k", "l" };
            for (int i = 0; i < key_pos_x[1].Length; i++)
            {
                key_pos_x[1][i] = i * key_w + key_w / 2;
            }

            key_pos_x[2] = new int[9];
            key_chars[2] = new string[9] { "none", "z", "x", "c", "v", "b", "n", "m", "del" };
            key_pos_y[2] = new int[9] { keyboard_y + 2 * key_h, keyboard_y + 2 * key_h, keyboard_y + 2 * key_h, keyboard_y + 2 * key_h, keyboard_y + 2 * key_h, keyboard_y + 2 * key_h, keyboard_y + 2 * key_h, keyboard_y + 2 * key_h, keyboard_y + 2 * key_h };
            for (int i = 0; i < key_pos_x[2].Length; i++)
            {
                key_pos_x[2][i] = i * key_w + key_w / 2;
            }

            key_pos_x[3] = new int[9];
            key_chars[3] = new string[9] { "none", "none", "space", "space", "space", "space", "none", "back", "done" };
            key_pos_y[3] = new int[9] { keyboard_y + 3 * key_h, keyboard_y + 3 * key_h, keyboard_y + 3 * key_h, keyboard_y + 3 * key_h, keyboard_y + 3 * key_h, keyboard_y + 3 * key_h, keyboard_y + 3 * key_h, keyboard_y + 3 * key_h, keyboard_y + 3 * key_h };
            for (int i = 0; i < key_pos_x[3].Length; i++)
            {
                key_pos_x[3][i] = i * key_w + key_w / 2;
            }
        }

        private string DecodeKeyTouch(int x, int y)
        {
            for (int r = 0; r < 4; r++)
            {

                for (var c = 0; c < key_chars[r].Length; c++)
                {

                    if (y >= key_pos_y[r][c] && y < key_pos_y[r][c] + key_h)
                    {
                        if (x >= key_pos_x[r][c] && x < key_pos_x[r][c] + key_w)
                        {
                            return key_chars[r][c];
                        }
                    }
                }
            }

            return null;
        }

        public bool IsShowKeyboard { get; set; }      
        public bool IsLoadInfo { get; set; }

        private void Touch_TouchUp(FT5xx6Controller sender, FT5xx6Controller.TouchEventArgs e)
        {
            if (e.X < 240 && e.Y < 50)
            {
                this.IsShowKeyboard = true;

                return;
            }

            if (this.IsShowKeyboard)
            {
                var keyDetected = DecodeKeyTouch(e.X, e.Y);

                if (keyDetected != null)
                {


                    switch (keyDetected)
                    {
                        case "del":
                            if (this.CurrentLocation != string.Empty && this.CurrentLocation.Length > 1)
                            {
                                this.CurrentLocation = this.CurrentLocation.Substring(0, this.CurrentLocation.Length - 1);
                            }
                            else if (this.CurrentLocation != string.Empty && this.CurrentLocation.Length > 0)
                            {
                                this.CurrentLocation = string.Empty;
                            }
                            break;
                        case "none":
                            break;

                        case "back":
                        case "done":
                            this.IsShowKeyboard = false;

                            IsLoadInfo = true;
                            break;

                        case "space":
                            if (this.CurrentLocation.Length > 0)
                            {
                                this.CurrentLocation += " ";
                            }

                            break;
                        default:
                            this.CurrentLocation += keyDetected;
                            break;

                    }
                }
            }
        }

        

        public void DrawBackgroud()
        {
            canvas.DrawBitmap(bitmapBackground, 0, 0);
        }

        public void DrawInfomation()
        {
            SKFont sKFont = new SKFont();
            SKFont sKFontBig = new SKFont();

            sKFont.Size = 20;
            sKFontBig.Size = 50;

            SKTextBlob textBlob;


            textBlob = SKTextBlob.Create("City: ", sKFont);

            canvas.DrawText(textBlob, city_x, city_y + sKFont.Size, paintWhiteFill);

            if (this.CurrentLocation != string.Empty)
            {
                textBlob = SKTextBlob.Create(this.CurrentLocation.ToUpper(), sKFont);
            }
            else
            {
                textBlob = SKTextBlob.Create("N/A", sKFont);
            }

            canvas.DrawText(textBlob, city_x + 50, city_y + sKFont.Size, paintWhiteFill);


            textBlob = SKTextBlob.Create(weatherInfo.Temperature, sKFontBig);
            canvas.DrawText(textBlob, 30, 100, paintWhiteFill);

            

            //TemperatureMax *
            textBlob = SKTextBlob.Create("Temp. max: ", sKFont);
            canvas.DrawText(textBlob, 10, 150, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.TemperatureMax, sKFont);
            canvas.DrawText(textBlob, 10 + 130, 150, paintWhiteFill);

            //TemperatureMin *
            textBlob = SKTextBlob.Create("Temp. min: ", sKFont);
            canvas.DrawText(textBlob, 10, 180, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.TemperatureMin, sKFont);
            canvas.DrawText(textBlob, 10 + 130, 180, paintWhiteFill);

            //Humidity *
            textBlob = SKTextBlob.Create("Humidity: ", sKFont);
            canvas.DrawText(textBlob, 10, 210, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.Humidity, sKFont);
            canvas.DrawText(textBlob, 10 + 130, 210, paintWhiteFill);

            //LabWindspeed *
            textBlob = SKTextBlob.Create("Wind speed: ", sKFont);
            canvas.DrawText(textBlob, 10, 240, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.LabWindspeed, sKFont);
            canvas.DrawText(textBlob, 10 + 130, 240, paintWhiteFill);



            //Condition
            textBlob = SKTextBlob.Create("Condition: ", sKFont);
            canvas.DrawText(textBlob, 240, 120, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.LabCondtion, sKFont);
            canvas.DrawText(textBlob, 240 + 100, 120, paintWhiteFill);

            //LabDetail
            textBlob = SKTextBlob.Create("Detail: ", sKFont);
            canvas.DrawText(textBlob, 240, 150, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.LabDetail, sKFont);
            canvas.DrawText(textBlob, 240 + 100, 150, paintWhiteFill);

            //LabSunset
            textBlob = SKTextBlob.Create("Sunset: ", sKFont);
            canvas.DrawText(textBlob, 240, 180, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.LabSunset, sKFont);
            canvas.DrawText(textBlob, 240 + 100, 180, paintWhiteFill);

            //Sunrise
            textBlob = SKTextBlob.Create("Sunrise: ", sKFont);
            canvas.DrawText(textBlob, 240, 210, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.LabSunrise, sKFont);
            canvas.DrawText(textBlob, 240 + 100, 210, paintWhiteFill);

            //Sunrise
            textBlob = SKTextBlob.Create("Pressure: ", sKFont);
            canvas.DrawText(textBlob, 240, 240, paintWhiteFill);

            textBlob = SKTextBlob.Create(weatherInfo.LabPressure, sKFont);
            canvas.DrawText(textBlob, 240 + 100, 240, paintWhiteFill);
        }

        public void DrawBanner(string text, int x, int y)
        {
            SKFont sKFont = new SKFont();

            sKFont.Size = 20;

            SKTextBlob textBlob;

            textBlob = SKTextBlob.Create(text, sKFont);

            canvas.DrawText(textBlob, x, y, paintWhiteFill);
        }
    
        public void DrawTextBox(string text)
        {


            SKFont sKFont = new SKFont();

            sKFont.Size = 20;

            SKTextBlob textBlob;
            // the rectangle
            var rect = SKRect.Create(0, 55, 480, 25);
            // the brush (fill with white)
            canvas.DrawRect(rect, paintWhiteFill);

            // draw fill

            if (text != string.Empty)
            {
                textBlob = SKTextBlob.Create(text.ToUpper(), sKFont);

                canvas.DrawText(textBlob, 2, 55 + 2 + sKFont.Size, paintBlack);
            }




        }

        public void DrawKeyBoard()
        {
            canvas.DrawBitmap(bitmapKeyboard, 0, keyboard_y);
            DrawTextBox(this.CurrentLocation);
        }

        public void GetWeatherInfo(string location)
        {
            this.weatherInfo.GetInfo(location);

            if (this.weatherInfo.Temperature == "N/A")
            {
                this.CurrentLocation = string.Empty;
            }
        }

        public void Flush()
        {
            var data = bitmap.Copy(SKColorType.Rgb565).Bytes;
            Display.Flush(data, 0, data.Length);
        }
    }
}
