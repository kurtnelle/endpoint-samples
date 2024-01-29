using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WeatherApp
{
    public class WeatherInfo
    {
        string apiKey;
        public WeatherInfo(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public string ImageLocation { get; private set; } = "N/A";
        public string LabCondtion { get; private set; } = "N/A";
        public string LabDetail { get; private set; } = "N/A";
        public string LabSunset { get; private set; } = "N/A";
        public string LabSunrise { get; private set; } = "N/A";
        public string LabWindspeed { get; private set; } = "N/A";
        public string LabPressure { get; private set; } = "N/A";
        public string TemperatureMax { get; private set; } = "N/A";
        public string TemperatureMin { get; private set; } = "N/A";
        public string Temperature { get; private set; } = "N/A";
        public string Humidity { get; private set; } = "N/A";

        public void GetInfo(string location)
        {
            using (WebClient web = new WebClient())
            {
                try
                {
                    string url = string.Format("http://api.openweathermap.org/data/2.5/weather?q={0}&appid={1}&units=imperial", location, this.apiKey); //units=metric for C
                    var json = web.DownloadString(url);
                    WeatherInfo.root Info = JsonConvert.DeserializeObject<WeatherInfo.root>(json);
                    this.ImageLocation = "https://openweathermap.org/img/w/" + Info.weather[0].icon + ".png";
                    LabCondtion = Info.weather[0].main;
                    LabDetail = Info.weather[0].description;

                    var dtsset = ConvertDateTime(Info.sys.sunset);
                    var dtsrise = ConvertDateTime(Info.sys.sunrise);

                    LabSunset = $"{dtsset.Hour}:{dtsset.Minute}";
                    LabSunrise = $"{dtsrise.Hour}:{dtsrise.Minute}";
                    LabWindspeed = Info.wind.speed.ToString();
                    LabPressure = Info.main.pressure.ToString();
                    TemperatureMax = Info.main.temp_max.ToString() + "°F";
                    TemperatureMin = Info.main.temp_min.ToString() + "°F";
                    Temperature = Info.main.temp.ToString() + "°F";
                    Humidity = Info.main.humidity.ToString() + "%";
                }
                catch
                {
                    LabCondtion = "N/A";
                    LabDetail = "N/A";
                    LabSunset = "N/A";
                    LabSunrise = "N/A";
                    LabWindspeed = "N/A";
                    LabPressure = "N/A";
                    TemperatureMax = "N/A";
                    TemperatureMin = "N/A";
                    Temperature = "N/A";
                    Humidity = "N/A";
                }


            }
        }

        DateTime ConvertDateTime(long millisec)

        {
            DateTime day = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            day = day.AddSeconds(millisec).ToLocalTime();
            return day;
        }

        public class coord
        {
            public double lon { get; set; }
            public double lat { get; set; }

        }
        public class weather
        {

            public string main { get; set; }
            public string description { get; set; }
            public string icon { get; set; }
        }
        public class main
        {
            public double temp { get; set; }
            public double temp_min { get; set; }
            public double temp_max { get; set; }
            public double pressure { get; set; }
            public double humidity { get; set; }

        }
        public class wind
        {
            public double speed { get; set; }
        }
        public class sys
        {
            public long sunrise { get; set; }
            public long sunset { get; set; }
        }
        public class root
        {
            public coord coord { get; set; }
            public List<weather> weather { get; set; }
            public main main { get; set; }
            public wind wind { get; set; }
            public sys sys { get; set; }

        }
    }
}
