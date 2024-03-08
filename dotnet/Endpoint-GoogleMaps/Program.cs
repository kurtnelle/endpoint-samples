using SkiaSharp;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using GHIElectronics.Endpoint.Devices.Display;
using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Devices.Network;
using GHIElectronics.Endpoint.Devices.Rtc;
using GHIElectronics.Endpoint.Drivers.FocalTech.FT5xx6;
using EndpointGoogleMap;

//Initialize RTC
var rtc = new RtcController();
rtc.DateTime = new DateTime(2024, 2, 29, 13, 24, 10);

//GoogleMaps
var imageWidth = 458;
var imageHeight = 228;
var zoomLevel = 14;
var latitude = 42.527714;
var longitude = -83.1036585;
var googleAPISignature = "YOUR GOOGLE API KEY";
var statusChanged = true;
var mapType = "roadmap";

//Initialize Display
var backlightPort = EPM815.Gpio.Pin.PD14 / 16;
var backlightPin = EPM815.Gpio.Pin.PD14 % 16;
var backlightDriver = new LibGpiodDriver((int)backlightPort);
var backlightController = new GpioController(PinNumberingScheme.Logical, backlightDriver);
backlightController.OpenPin(backlightPin);
backlightController.SetPinMode(backlightPin, PinMode.Output);
backlightController.Write(backlightPin, PinValue.High);

//Screen Size
var screenWidth = 480;
var screenHeight = 272;

var configuration = new FBDisplay.Configuration(){
    Clock = 10000,
    Width = 480,
    Hsync_start = 482,
    Hsync_end = 523,
    Htotal = 525,
    Height = 272,
    Vsync_start = 274,
    Vsync_end = 284,
    Vtotal = 285,
};
var fbDisplay = new FBDisplay(configuration);
var displayController = new DisplayController(fbDisplay);

//Initialize Touch
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

//Initialize Network
bool NetworkReady = false;
var networkType = NetworkInterfaceType.WiFi;

var networkSetting = new WiFiNetworkInterfaceSettings{
    Ssid = "YOUR SSID",
    Password = "YOUR PASSWORD",
    DhcpEnable = true,
};

var network = new NetworkController(networkType, networkSetting);

network.NetworkLinkConnectedChanged += (a, b) =>{
    if (b.Connected){
        Console.WriteLine("Connected");
        NetworkReady = true;
    }
    else{
        Console.WriteLine("Disconnected");
    }
};

network.NetworkAddressChanged += (a, b) =>{
    Console.WriteLine(string.Format("Address: {0}\n gateway: {1}\n DNS: {2}\n MAC: {3} ", b.Address, b.Gateway, b.Dns[0], b.MACAddress));
    NetworkReady = true;
};

new Thread(()=>network.Enable()).Start();
var cnt = 0;
while (NetworkReady == false){
    Console.WriteLine($"Waiting for connect {cnt++}");
    
    Thread.Sleep(250);
}

while (true){
    if (statusChanged) {
        await GetMap();
        statusChanged = false;
    }
    Thread.Sleep(10);
}
//Touch Events
void Touch_TouchUp(FT5xx6Controller sender, FT5xx6Controller.TouchEventArgs e){

    //Touch Zoom In
    if (e.X >= 428 && e.X <= 469 && e.Y >= 33 && e.Y <= 74){
        zoomLevel = zoomLevel + 1;
        Console.WriteLine("Zoom Level " +zoomLevel.ToString());
        statusChanged = true;
        return;
    }
    //Touch Zoom Out
    if (e.X >= 428 && e.X <= 469 && e.Y >= 220 && e.Y <= 260){
        zoomLevel = zoomLevel - 1;
        Console.WriteLine("Zoom Level " + zoomLevel.ToString());
        statusChanged = true;
        return;
    }
    //Touch Layer Style Road Map
    if (e.X >= 16 && e.X <= 59 && e.Y >= 218 && e.Y <= 258)
    {
        mapType = "roadmap";
        statusChanged = true;
        return;
    }
    //Touch Layer Style Satellite Map
    if (e.X >= 66 && e.X <= 109 && e.Y >= 218 && e.Y <= 258)
    {
        mapType = "satellite";
        statusChanged = true;
        return;
    }
    //Touch Layer Style 3
    if (e.X >= 116 && e.X <= 159 && e.Y >= 218 && e.Y <= 258)
    {
        mapType = "hybrid";
        statusChanged = true;
        return;
    }
}
async Task GetMap(){
    //SkiaSharp Initialization
    SKBitmap bitmap = new SKBitmap(screenWidth, screenHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
    bitmap.Erase(SKColors.Transparent);
    SKBitmap webBitmap;
    //Initialize Screen Canvas
    using (var screen = new SKCanvas(bitmap)){

        //Intialize Background
        var backgroundImage = Resources.background;
        var backgroundImageInfo = new SKImageInfo(480, 272);
        var background = SKBitmap.Decode(backgroundImage, backgroundImageInfo);
        screen.DrawBitmap(background, 0, 0);

        HttpClient httpClient = new HttpClient();
        ////Google Static Map URL
        string url = "https://maps.googleapis.com/maps/api/staticmap?center=" + latitude.ToString() + "," + longitude.ToString() + "&zoom="+zoomLevel.ToString()+"&size="+imageWidth.ToString()+"x"+imageHeight.ToString()+"&maptype="+ mapType +"&key=" + googleAPISignature;

        try{
            using (Stream stream = await httpClient.GetStreamAsync(url))
            using (MemoryStream memStream = new MemoryStream()){
                await stream.CopyToAsync(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
                var info = new SKImageInfo(imageWidth, imageHeight);
                webBitmap = SKBitmap.Decode(memStream, info);
                screen.DrawBitmap(webBitmap, 11, 33);         
            };
        }
        catch{
        }
        //Intialize Buttons
        var zoomInImage = Resources.buttonZoomIn;
        var zoomInImageInfo = new SKImageInfo(37, 37);
        var zoomInButton = SKBitmap.Decode(zoomInImage, zoomInImageInfo);
        var zoomOutImage = Resources.buttonZoomOut;
        var zoomOutImageInfo = new SKImageInfo(37, 37);
        var zoomOutButton = SKBitmap.Decode(zoomOutImage, zoomOutImageInfo);
        var roadmapImage = Resources.roadmap;
        var roadmapImageInfo = new SKImageInfo(43, 40);
        var roadmapButton = SKBitmap.Decode(roadmapImage, roadmapImageInfo);
        var satelliteImage = Resources.satellite;
        var satelliteImageInfo = new SKImageInfo(43, 40);
        var satelliteButton = SKBitmap.Decode(satelliteImage, satelliteImageInfo);
        var hybridImage = Resources.hybrid;
        var hybridImageInfo = new SKImageInfo(43, 40);
        var hybridButton = SKBitmap.Decode(hybridImage, hybridImageInfo);
        screen.DrawBitmap(zoomInButton, 430, 35);
        screen.DrawBitmap(zoomOutButton, 430, 222);
        screen.DrawBitmap(roadmapButton, 16, 218);
        screen.DrawBitmap(satelliteButton, 66, 218);
        screen.DrawBitmap(hybridButton, 116, 218);
        var data = bitmap.Copy(SKColorType.Rgb565).Bytes;
        displayController.Flush(data);
        Thread.Sleep(1);
    }
}

