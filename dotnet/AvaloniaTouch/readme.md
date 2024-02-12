This project is tested on Visual Studio 2022

# Creating new project
- Create new project, select Avalonia C# project.
- Name the project "AvaloniaTouch".
- Select Desktop, click Next.
- Select Community Toolkit, click Next.
- Select Embedded Suport, click Create.
- Default template has two projects and wrong startup project. Select the project AvaloniaTouch.Desktop and set as startup project.

# Change csproj file
Open the csproj file, change <OutputType> from WinExe to Exe.

```
<OutputType>Exe</OutputType>
```

- Optional: Enable R2R for fast loaing project but can't debug some dll. R2R should be enabled when the project is completed. To enable R2R, just add this line into csproj file.

```
<PublishReadyToRun>true</PublishReadyToRun>
```

# Change Program.cs

Below is simple Program.cs to allow display, touchscreen work on Endpoint device:
```
public static int Main(string[] args)
{
    var backlightPort = EPM815.Gpio.Pin.PD14 / 16;
    var backlightPin = EPM815.Gpio.Pin.PD14 % 16;

    var gpioDriver = new LibGpiodDriver(backlightPort);
    var gpioController = new GpioController(PinNumberingScheme.Logical, gpioDriver);

    gpioController.OpenPin(backlightPin, PinMode.Output);
    gpioController.Write(backlightPin, PinValue.High); // low is on

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

    EPM815.I2c.Initialize(EPM815.I2c.I2c6);

    var touch = new FT5xx6Controller(EPM815.I2c.I2c6, EPM815.Gpio.Pin.PF12);

    var input = new InputDevice();
    input.EnableOnscreenKeyboard(displayController);

    var builder = BuildAvaloniaApp();

    touch.TouchDown += (a, b) =>
    {
        input.UpdateTouchPoint(b.X, b.Y, TouchEvent.Pressed);
    };

    touch.TouchUp += (a, b) =>
    {
        input.UpdateTouchPoint(b.X, b.Y, TouchEvent.Released);
    };

    return builder.StartLinuxFbDev(new string[] { "--fbdev" }, "/dev/fb0", 1, input);
}


// Avalonia configuration, don't remove; also used by visual designer.
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont();
		
```




