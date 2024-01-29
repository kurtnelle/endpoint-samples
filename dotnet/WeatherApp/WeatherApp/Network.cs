using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GHIElectronics.Endpoint.Devices.Network;
using GHIElectronics.Endpoint.Core;
using System.Device.Gpio.Drivers;
using System.Device.Gpio;

namespace WeatherApp
{
    internal static class Network
    {
        public static bool NetworkReady = false;
        public static bool IntializeNetwork()
        {
            
            var networkType = GHIElectronics.Endpoint.Devices.Network.NetworkInterfaceType.WiFi;
            var networkSetting = new WiFiNetworkInterfaceSettings
            {
                Ssid = "your ssid",
                Password = "your pwd",
                DhcpEnable = true,
            };

            var network = new NetworkController(networkType, networkSetting);



            network.NetworkLinkConnectedChanged += (a, b) =>
            {
                if (b.Connected)
                {
                    Console.WriteLine("Connected");
                    NetworkReady = true;

                }
                else
                {
                    Console.WriteLine("Disconnected");
                }
            };

            network.NetworkAddressChanged += (a, b) =>
            {
                Console.WriteLine(string.Format("Address: {0}\n gateway: {1}\n DNS: {2}\n MAC: {3} ", b.Address, b.Gateway, b.Dns[0], b.MACAddress));
                NetworkReady = true;
            };


            network.Enable();

            while (NetworkReady == false)
            {
                Thread.Sleep(250);
            }

            new Thread(ThreadGpio).Start();

            return true;
        }

        static void ThreadGpio()
        {
            var pinid = EPM815.Gpio.Pin.PC0;

            var gpioDriver = new LibGpiodDriver((int)pinid / 16);
            var gpioController = new GpioController(PinNumberingScheme.Logical, gpioDriver);


            gpioController.OpenPin(pinid % 16);
            gpioController.SetPinMode(pinid % 16, PinMode.Output);

            while (true)
            {
                gpioController.Write(pinid % 16, PinValue.High);
                Thread.Sleep(100);

                gpioController.Write(pinid % 16, PinValue.Low);
                Thread.Sleep(100);

            }
        }
    }
}
