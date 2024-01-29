using GHIElectronics.Endpoint.Devices.Rtc;

namespace WeatherApp
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var weather = new Weather();

            weather.DrawBackgroud();

            weather.DrawBanner("Please wait for connecting to WiFi network....", 30, 120);

            weather.Flush();

            Network.IntializeNetwork();

            weather.DrawBanner("Please wait for connecting to weather system....", 30, 160);

            weather.Flush();

            weather.CurrentLocation = "New York";
            weather.GetWeatherInfo(weather.CurrentLocation);

            while (true)
            {


                weather.DrawBackgroud();

                weather.DrawInfomation();
                if (weather.IsShowKeyboard)
                {                        
                    weather.DrawKeyBoard();
                }
                else
                {
                    if (weather.IsLoadInfo)
                    {
                        weather.GetWeatherInfo(weather.CurrentLocation);

                        weather.IsLoadInfo = false;
                    }
                }
                    
                weather.Flush();

                Thread.Sleep(100);
            }
            
            Thread.Sleep(-1);
        }

        
    }
}
