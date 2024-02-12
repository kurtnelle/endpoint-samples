using GHIElectronics.Endpoint.Devices.Network;
using Iot.Device.Mcp25xxx.Register;
using System.Net;

namespace BlazorApp.Services
{
    public enum NetworkStatus { Up, Down }
    public class WiFiNetworkingService
    {
        NetworkController _networkController;

        public NetworkStatus Status { get; private set; } = NetworkStatus.Down;
        public string Ssid { get; private set; } = string.Empty;
        public IPAddress? Address { get; private set; }
        public IPAddress? Gateway { get; private set; }
        public IPAddress? DNS { get; private set; }
        public string? MACAddress { get; private set; }

        public WiFiNetworkingService(IConfiguration configuration)
        {
            var networkType = NetworkInterfaceType.WiFi;
            var networkSetting = new WiFiNetworkInterfaceSettings
            {
                Ssid = configuration.GetValue<string>("WifiCredentials:Ssid"),
                Password = configuration.GetValue<string>("WifiCredentials:Password"),
                DhcpEnable = true,
            };

            _networkController = new NetworkController(networkType, networkSetting);
            _networkController.NetworkLinkConnectedChanged +=_networkController_NetworkLinkConnectedChanged;
            _networkController.NetworkAddressChanged +=_networkController_NetworkAddressChanged;
        }

        private void _networkController_NetworkAddressChanged(NetworkController sender, NetworkAddressChangedEventArgs e)
        {
            Ssid = ((WiFiNetworkInterfaceSettings)_networkController.ActiveInterfaceSettings).Ssid;
            Address =  e.Address;
            Gateway = e.Gateway;
            DNS = e.Dns[0];
            MACAddress = e.MACAddress;
            Console.WriteLine(string.Format("Address: {0}\nGateway: {1}\nDNS: {2}\nMAC: {3} ", e.Address, e.Gateway, e.Dns[0], e.MACAddress));
        }

        private void _networkController_NetworkLinkConnectedChanged(NetworkController sender, NetworkLinkConnectedChangedEventArgs e)
        {
            Status = e.Connected ? NetworkStatus.Up : NetworkStatus.Down;
        }
        public void Enable()
        {
            if (_networkController != null)
            {
                _networkController.Enable();
            }
        }
    }
}
