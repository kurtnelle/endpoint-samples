using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Devices.UsbHost;
using System;
using System.Collections.Generic;
using System.Device.Gpio.Drivers;
using System.Device.Gpio;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GHIElectronics.Endpoint.Devices.Camera;
using GHIElectronics.Endpoint.Devices.Display;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;

namespace StreamJpeg
{
    internal class IPCamera
    {
        HttpListener listener;
        int frameReceivedCount = 0;
        byte[] jpegData;
        bool isHttpWriting = false;
        public IPCamera(string[] prefixes)
        {
            this.listener = new HttpListener();

            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            WebcamInitialize();
        }

        
        public Task DoRun()
        {
            listener.Start();

            return Task.Run(() =>
            {
                Stream output;
                var lastFrameReceivedCount = frameReceivedCount;


                HttpListenerContext context = listener.GetContext();

                HttpListenerResponse response = context.Response;

                response.ContentType = "multipart/x-mixed-replace; boundary=--imgboundary";

                output = response.OutputStream;

                for (; ; )
                {
                    if (lastFrameReceivedCount != frameReceivedCount)
                    {
                        isHttpWriting = true;

                        ASCIIEncoding encode = new ASCIIEncoding();
                        var boundary = encode.GetBytes("\r\n--imgboundary\r\nContent-Type: image/jpeg\r\nContent-Length:" + jpegData.Length + "\r\n\r\n");

                        output.Write(boundary);

                        output.Write(jpegData);

                        output.Flush();


                        isHttpWriting = false;

                        context.Response.Headers.Clear();
                    }

                    Thread.Sleep(10);
                }

            }); ;
        }

        public void WebcamInitialize()
        {
          

            var usbhostController = new UsbHostController();

            usbhostController.OnConnectionChangedEvent += (a, b) =>
            {
                var arg = b;

                if (arg.DeviceStatus == DeviceConnectionStatus.Connected)
                {
                    Console.WriteLine("id: " + arg.DeviceId);
                    Console.WriteLine("name: " + arg.DeviceName);
                    Console.WriteLine("type: " + arg.Type);

                    if (arg.Type == GHIElectronics.Endpoint.Devices.Usb.DeviceType.Webcam && arg.DeviceName.IndexOf("video0") > 0)
                    {
                        var webcam = new Webcam(arg.DeviceName);

                        var setting = new CameraConfiguration()
                        {
                            Width = 320,
                            Height = 240,
                            ImageFormat = Format.Jpeg,
                        };

                        webcam.Setting = setting;

                        webcam.VideoStreamStart();


                        webcam.FrameReceivedEvent += Webcam_FrameReceivedEvent;
                    }
                }

            };

            usbhostController.Enable();

        }


        private void Webcam_FrameReceivedEvent(Webcam sender, byte[] data)
        {
            if (!isHttpWriting)
            {
                frameReceivedCount++;

                jpegData = data;
            }
        }
    }
}
