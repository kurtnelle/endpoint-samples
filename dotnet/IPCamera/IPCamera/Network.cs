using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GHIElectronics.Endpoint.Devices.Network;

namespace StreamJpeg
{
    internal static class Network
    {
        public static void EthernetInitialize()
        {
            var networkReady = false;
            var networkType = GHIElectronics.Endpoint.Devices.Network.NetworkInterfaceType.Ethernet;
            var networkSetting = new NetworkInterfaceSettings
            {
                Address = new IPAddress(new byte[] { 192, 168, 86, 106 }),
                SubnetMask = new IPAddress(new byte[] { 255, 255, 255, 0 }),
                GatewayAddress = new IPAddress(new byte[] { 192, 168, 86, 1 }),
                DnsAddresses = new IPAddress[] { new IPAddress(new byte[] { 75, 75, 75, 75 }) },
                DhcpEnable = false,
            };

            var network = new NetworkController(networkType, networkSetting);

            network.NetworkLinkConnectedChanged += (a, b) =>
            {
                if (b.Connected)
                {
                    Console.WriteLine("Connected");

                    networkReady = true;    

                }
                else
                {
                    Console.WriteLine("Disconnected");
                }
            };

            network.NetworkAddressChanged += (a, b) =>
            {
                Console.WriteLine(string.Format("Address: {0}\n gateway: {1}\n DNS: {2}\n MAC: {3} ", b.Address, b.Gateway, b.Dns[0], b.MACAddress));
            };

            network.Enable();

            Console.WriteLine("Waiting for network ready!!!");
            while (!networkReady)
            {
                Thread.Sleep(1000); 
            }
        }
        public static bool WiFiInitialize()
        {
            var networkReady = false;
            var networkType = GHIElectronics.Endpoint.Devices.Network.NetworkInterfaceType.WiFi;
            var networkSetting = new WiFiNetworkInterfaceSettings
            {
                Ssid = "user_ssid",
                Password = "user_pass",
                DhcpEnable = true,
            };

            var network = new NetworkController(networkType, networkSetting);



            network.NetworkLinkConnectedChanged += (a, b) =>
            {
                if (b.Connected)
                {
                    Console.WriteLine("Connected");
                    networkReady = true;

                }
                else
                {
                    Console.WriteLine("Disconnected");
                }
            };

            network.NetworkAddressChanged += (a, b) =>
            {
                Console.WriteLine(string.Format("This address is for typing on your browser: {0}\n gateway: {1}\n DNS: {2}\n MAC: {3} ", b.Address, b.Gateway, b.Dns[0], b.MACAddress));
               
            };


            network.Enable();

            Console.WriteLine("Waiting for network ready!!!");
            while (networkReady == false)
            {
                Thread.Sleep(1000);
            }
            return true;
        }
    }
}
