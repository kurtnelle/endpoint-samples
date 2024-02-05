using GHIElectronics.Endpoint.Devices.Network;
using System.Runtime.Intrinsics.X86;

namespace StreamJpeg
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Network.WiFiInitialize();

            IPCamera httpCam = new IPCamera(["http://*:80/"]);

            httpCam.DoRun();

            while (true)
            {                
                Thread.Sleep(10000);
            }
        }
    }
}
