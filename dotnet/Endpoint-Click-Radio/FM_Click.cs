using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using GHIElectronics.Endpoint.Core;

namespace EndpointClickRadio
{
        public class FM_Click
        {
        //public class RadioFM1
        //{

        /// <summary>The channel returned by <see cref="Seek" /> when no Channel is found.</summary>
        public const double INVALID_CHANNEL = -1.0;

        /// <summary>The minimum volume the device can output.</summary>
        public const int MIN_VOLUME = 0;

        /// <summary>The maximum volume the device can output.</summary>
        public const int MAX_VOLUME = 255;
        private const byte I2C_ADDRESS = 0x10;
        private const byte REGISTER_DEVICEID = 0x00;
        private const byte REGISTER_CHIPID = 0x01;
        private const byte REGISTER_POWERCFG = 0x02;
        private const byte REGISTER_CHANNEL = 0x03;
        private const byte REGISTER_SYSCONFIG1 = 0x04;
        private const byte REGISTER_SYSCONFIG2 = 0x05;
        private const byte REGISTER_STATUSRSSI = 0x0A;
        private const byte REGISTER_READCHAN = 0x0B;
        private const byte REGISTER_RDSA = 0x0C;
        private const byte REGISTER_RDSB = 0x0D;
        private const byte REGISTER_RDSC = 0x0E;
        private const byte REGISTER_RDSD = 0x0F;

        //Register 0x02 - POWERCFG
        private const byte BIT_SMUTE = 15;
        private const byte BIT_DMUTE = 14;
        private const byte BIT_SKMODE = 10;
        private const byte BIT_SEEKUP = 9;
        private const byte BIT_SEEK = 8;

        //Register 0x03 - CHANNEL
        private const byte BIT_TUNE = 15;

        //Register 0x04 - SYSCONFIG1
        private const byte BIT_RDS = 12;
        private const byte BIT_DE = 11;

        //Register 0x05 - SYSCONFIG2
        private const byte BIT_SPACE1 = 5;
        private const byte BIT_SPACE0 = 4;

        //Register 0x0A - STATUSRSSI
        private const byte BIT_RDSR = 15;
        private const byte BIT_STC = 14;
        private const byte BIT_SFBL = 13;
        private const byte BIT_AFCRL = 12;
        private const byte BIT_RDSS = 11;
        private const byte BIT_STEREO = 8;
        private const int RADIO_TEXT_GROUP_CODE = 2;
        private const int TOGGLE_FLAG_POSITION = 5;
        private const int CHARS_PER_SEGMENT = 2;
        private const int MAX_MESSAGE_LENGTH = 64;
        private const int MAX_SEGMENTS = 16;
        private const int MAX_CHARS_PER_GROUP = 4;
        private const int VERSION_A_TEXT_SEGMENT_PER_GROUP = 2;
        private const int VERSION_B_TEXT_SEGMENT_PER_GROUP = 1;
        private int currentVolume;
        private bool radioTextWorkerRunning;
        private Thread radioTextWorkerThread;
        private string currentRadioText;
        //private I2cBus i2cBus;
        //GpioPin resetPin;
        //GpioPin selPin;
        private int spacingDivisor;
        private int baseChannel;
        private ushort[] registers;
        private RadioTextChangedHandler onRadioTextChanged;
        private I2cDevice i2cController;

        /// <summary>Represents the delegate that is used to handle the <see cref="RadioTextChanged" /> event.</summary>
        /// <param name="sender">The <see cref="RadioFM1" /> that raised the event.</param>
        /// <param name="newRadioText">The new Radio Text.</param>
        public delegate void RadioTextChangedHandler(FM_Click sender, string newRadioText);

        /// <summary>Raised when new Radio Text is available.</summary>
        public event RadioTextChangedHandler RadioTextChanged;

        /// <summary>Gets or sets the Volume of the radio.</summary>

        public int Volume
        {
            get
            {
                return this.currentVolume;
            }

            set
            {
                // if (value > RadioFM1.MAX_VOLUME || value < RadioFM1.MIN_VOLUME) throw new ArgumentOutOfRangeException("value", "The volume provided was outside the allowed range.");

                this.currentVolume = value;
                this.SetDeviceVolume((ushort)value,i2cController);
            }
        }

        /// <summary>The maximum channel the radio and be tuned to.</summary>
        public double MaxChannel
        {
            private set;
            get;
        }

        /// <summary>The minimum channel the radio and be tuned to.</summary>
        public double MinChannel
        {
            private set;
            get;
        }

        /// <summary>Gets or sets the Channel of the radio.</summary>
        public double Channel
        {
            get
            {
                return this.GetDeviceChannel(i2cController) / 10.0;
            }

            set
            {
                if (value > this.MaxChannel || value < this.MinChannel) throw new ArgumentOutOfRangeException("value", "The Channel provided was outside the allowed range.");

                this.SetDeviceChannel((int)(value * 10), i2cController);
                this.currentRadioText = "N/A";
            }
        }

        /// <summary>Gets the current Radio Text.</summary>
        public string RadioText
        {
            get
            {
                return this.currentRadioText;
            }
        }

        /// <summary>The enumeration that determines which direction to Seek when calling Seek(direction);</summary>
        public enum SeekDirection
        {

            /// <summary>Seeks for a higher station number.</summary>
            Forward,

            /// <summary>Seeks for a lower station number.</summary>
            Backward
        };

        /// <summary>The radio frequency band.</summary>
        public enum Band
        {

            /// <summary>The band used in the United States and Europe (87.5-108MHz).</summary>
            USAEurope,

            /// <summary>The wide band used in the Japan (76-108MHz).</summary>
            JapanWide,

            /// <summary>The band used in Japan (76-90MHz).</summary>
            Japan
        }

        /// <summary>The radio channel spacing.</summary>
        public enum Spacing
        {

            /// <summary>Spacing in USA and Australia (200KHz).</summary>
            USAAustrailia,

            /// <summary>Spacing in Europe and Japan (100KHz).</summary>
            EuropeJapan
        }

        /// <summary>Constructs a new instance.</summary>      
        public FM_Click(int reset,int chipSelect)
        {
            this.radioTextWorkerRunning = true;
            this.currentRadioText = "N/A";
            this.spacingDivisor = 2;
            this.baseChannel = 875;
            this.registers = new ushort[16];

            var pinIdReset = reset;
            var pinPortReset = pinIdReset / 16;
            var pinNumberReset = pinIdReset % 16;

            var gpioDriverReset = new LibGpiodDriver(pinPortReset);
            var resetPin = new GpioController(PinNumberingScheme.Logical, gpioDriverReset);
            resetPin.OpenPin(pinNumberReset);
            resetPin.SetPinMode(pinNumberReset,PinMode.Output);

            var pinIdSelPin = chipSelect;
            var pinPortSelPin = pinIdSelPin / 16;
            var pinNumberSelPin = pinIdSelPin % 16;

            var gpioDriverSelPin = new LibGpiodDriver(pinPortSelPin);
            var selPin = new GpioController(PinNumberingScheme.Logical, gpioDriverSelPin);
            selPin.OpenPin(pinNumberSelPin);
            selPin.SetPinMode(pinNumberSelPin, PinMode.Output);

            selPin.Write(pinNumberSelPin, PinValue.High);

            //Wrapper:
            EPM815.I2c.Initialize(EPM815.I2c.I2c4);
            var i2cConnectionSetting = new I2cConnectionSettings(EPM815.I2c.I2c4, FM_Click.I2C_ADDRESS);
            this.i2cController = I2cDevice.Create(i2cConnectionSetting);


            //var settings = new I2cConnectionSettings(FM_Click.I2C_ADDRESS, 400);
            //var controller = I2cController.FromName(deviceBus);
            //var i2cBus = i2cController.GetDevice(i2cConnectionSetting);


            this.InitializeDevice(resetPin, pinNumberReset,i2cController);
            this.SetChannelConfiguration(Spacing.USAAustrailia, Band.USAEurope, i2cController);
            this.Channel = this.MinChannel;
            //this.Volume = RadioFM1.MAX_VOLUME;
            //this.radioTextWorkerThread = new Thread(this.RadioTextWorker(i2cController));
        }

        /// <summary>Tells the radio to Seek in the given direction until it finds a station.</summary>
        /// <param name="direction">The direction to Seek the radio.</param>
        /// <remarks>It does wrap around when seeking.</remarks>
        /// <returns>The Channel that was tuned to or <see cref="INVALID_CHANNEL" /> if no Channel was found.</returns>
        public double Seek(SeekDirection direction)
        {
            this.currentRadioText = "N/A";

            if (this.SeekDevice(direction,i2cController))
                return this.Channel;
            else
                return FM_Click.INVALID_CHANNEL;
        }

        /// <summary>Increases the Volume by one.</summary>
        public void IncreaseVolume()
        {
            if (this.Volume == FM_Click.MAX_VOLUME) return;

            ++this.Volume;
        }

        /// <summary>Decreases the Volume by one.</summary>
        public void DecreaseVolume()
        {
            if (this.Volume == FM_Click.MIN_VOLUME) return;

            --this.Volume;
        }

        /// <summary>Sets the channel spacing and band of the device.</summary>
        /// <param name="spacing">The channel spacing.</param>
        /// <param name="band">The channel base band.</param>
        public void SetChannelConfiguration(Spacing spacing, Band band, I2cDevice i2cController)
        {
            this.ReadRegisters(i2cController);

            if (spacing == Spacing.USAAustrailia)
            {
                this.registers[FM_Click.REGISTER_SYSCONFIG2] &= 0xFFCF;
                this.spacingDivisor = 2;
            }
            else if (spacing == Spacing.EuropeJapan)
            {
                this.registers[FM_Click.REGISTER_SYSCONFIG2] &= 0xFFDF;
                this.spacingDivisor = 1;
            }
            else
            {
                throw new ArgumentException("You must provide a valid spacing.", "spacing");
            }

            if (band == Band.USAEurope)
            {
                this.registers[FM_Click.REGISTER_SYSCONFIG2] &= 0xFF3F;
                this.baseChannel = 875;
                this.MinChannel = 87.5;
                this.MaxChannel = 107.5;
            }
            else if (band == Band.JapanWide)
            {
                this.registers[FM_Click.REGISTER_SYSCONFIG2] &= 0xFF7F;
                this.baseChannel = 760;
                this.MinChannel = 76;
                this.MaxChannel = 108;
            }
            else if (band == Band.Japan)
            {
                this.registers[FM_Click.REGISTER_SYSCONFIG2] &= 0xFFBF;
                this.baseChannel = 760;
                this.MinChannel = 76;
                this.MaxChannel = 90;
            }
            else
            {
                throw new ArgumentException("You must provide a valid band.", "band");
            }

            this.UpdateRegisters(i2cController);
        }

        private void OnRadioTextChanged(FM_Click sender, string newRadioText)
        {
            var e = this.RadioTextChanged;

            if (e != null)
                e(sender, newRadioText);
        }

        private void RadioTextWorker(I2cDevice i2cController)
        {
            char[] currentRadioText = new char[FM_Click.MAX_MESSAGE_LENGTH];
            bool[] isSegmentPresent = new bool[FM_Click.MAX_SEGMENTS];
            int endOfMessage = -1;
            int endSegmentAddress = -1;
            string lastMessage = "";
            int lastTextToggleFlag = -1;
            bool waitForNextMessage = false;

            while (this.radioTextWorkerRunning)
            {
                this.ReadRegisters(i2cController);
                ushort a = this.registers[FM_Click.REGISTER_RDSA];
                ushort b = this.registers[FM_Click.REGISTER_RDSB];
                ushort c = this.registers[FM_Click.REGISTER_RDSC];
                ushort d = this.registers[FM_Click.REGISTER_RDSD];
                bool ready = (this.registers[FM_Click.REGISTER_STATUSRSSI] & (1 << FM_Click.BIT_RDSR)) != 0;

                if (ready)
                {
                    int programIDCode = a;
                    int groupTypeCode = b >> 12;
                    int versionCode = (b >> 11) & 0x1;
                    int trafficIDCode = (b >> 10) & 0x1;
                    int programTypeCode = (b >> 5) & 0x1F;

                    if (groupTypeCode == FM_Click.RADIO_TEXT_GROUP_CODE)
                    {
                        int textToggleFlag = b & (1 << (FM_Click.TOGGLE_FLAG_POSITION - 1));
                        if (textToggleFlag != lastTextToggleFlag)
                        {
                            currentRadioText = new char[FM_Click.MAX_MESSAGE_LENGTH];
                            lastTextToggleFlag = textToggleFlag;
                            waitForNextMessage = false;
                        }
                        else if (waitForNextMessage)
                        {
                            continue;
                        }

                        int segmentAddress = (b & 0xF);
                        int textAddress = -1;
                        isSegmentPresent[segmentAddress] = true;

                        if (versionCode == 0)
                        {
                            textAddress = segmentAddress * FM_Click.CHARS_PER_SEGMENT * FM_Click.VERSION_A_TEXT_SEGMENT_PER_GROUP;
                            currentRadioText[textAddress] = (char)(c >> 8);
                            currentRadioText[textAddress + 1] = (char)(c & 0xFF);
                            currentRadioText[textAddress + 2] = (char)(d >> 8);
                            currentRadioText[textAddress + 3] = (char)(d & 0xFF);
                        }
                        else
                        {
                            textAddress = segmentAddress * FM_Click.CHARS_PER_SEGMENT * FM_Click.VERSION_B_TEXT_SEGMENT_PER_GROUP;
                            currentRadioText[textAddress] = (char)(d >> 8);
                            currentRadioText[textAddress + 1] = (char)(d & 0xFF);
                        }

                        if (endOfMessage == -1)
                        {
                            for (int i = 0; i < FM_Click.MAX_CHARS_PER_GROUP; ++i)
                            {
                                if (currentRadioText[textAddress + i] == 0xD)
                                {
                                    endOfMessage = textAddress + i;
                                    endSegmentAddress = segmentAddress;
                                }
                            }
                        }

                        if (endOfMessage == -1)
                            continue;

                        bool complete = true;
                        for (int i = 0; i < endSegmentAddress; ++i)
                            if (!isSegmentPresent[i])
                                complete = false;

                        if (!complete)
                            continue;

                        string message = new string(currentRadioText, 0, endOfMessage);
                        if (message == lastMessage)
                        {
                            this.currentRadioText = message;
                            this.OnRadioTextChanged(this, message);
                            waitForNextMessage = true;

                            for (int i = 0; i < endSegmentAddress; ++i)
                                isSegmentPresent[i] = false;

                            endOfMessage = -1;
                            endSegmentAddress = -1;
                        }

                        lastMessage = message;
                    }

                    Thread.Sleep(35);
                }
                else
                {
                    Thread.Sleep(40);
                }
            }
        }

        private void InitializeDevice(GpioController resetPin, int pinNumberReset, I2cDevice i2cController )
        {
            resetPin.Write(pinNumberReset, PinValue.Low);
            Thread.Sleep(100);
            resetPin.Write(pinNumberReset, PinValue.High);
            Thread.Sleep(10);

            this.ReadRegisters(i2cController);
            this.registers[0x07] = 0x8100;
            this.UpdateRegisters(i2cController);

            Thread.Sleep(500);

            this.ReadRegisters(i2cController);
            this.registers[FM_Click.REGISTER_POWERCFG] = 0x4001;
            this.registers[FM_Click.REGISTER_SYSCONFIG1] |= (1 << FM_Click.BIT_RDS);
            this.registers[FM_Click.REGISTER_SYSCONFIG2] &= 0xFFCF;
            this.registers[FM_Click.REGISTER_SYSCONFIG2] &= 0xFFF0;
            this.registers[FM_Click.REGISTER_SYSCONFIG2] |= 0x000F;
            this.UpdateRegisters(i2cController);

            Thread.Sleep(110);
        }

        private void ReadRegisters(I2cDevice i2cController)
        {
            byte[] data = new byte[32];


            i2cController.Read(data);

            for (int i = 0, x = 0xA; i < 12; i += 2, ++x)
                this.registers[x] = (ushort)((data[i] << 8) | (data[i + 1]));

            for (int i = 12, x = 0x0; i < 32; i += 2, ++x)
                this.registers[x] = (ushort)((data[i] << 8) | (data[i + 1]));
        }

        private void UpdateRegisters(I2cDevice i2cController)
        {
            byte[] data = new byte[12];

            for (int x = 0x02, i = 0; x < 0x08; ++x, i += 2)
            {
                data[i] = (byte)(this.registers[x] >> 8);
                data[i + 1] = (byte)(this.registers[x] & 0x00FF);
            }

            i2cController.Write(data);
        }

        private void SetDeviceVolume(ushort volume, I2cDevice i2cController)
        {
            this.ReadRegisters(i2cController);
            this.registers[FM_Click.REGISTER_SYSCONFIG2] &= 0xFFF0;
            this.registers[FM_Click.REGISTER_SYSCONFIG2] |= volume;
            this.UpdateRegisters(i2cController);
        }

        private int GetDeviceChannel(I2cDevice i2cController)
        {
            this.ReadRegisters(i2cController);

            int Channel = this.registers[FM_Click.REGISTER_READCHAN] & 0x03FF;

            return Channel * this.spacingDivisor + this.baseChannel;
        }

        private void SetDeviceChannel(int newChannel, I2cDevice i2cController)
        {
            newChannel -= this.baseChannel;
            newChannel /= this.spacingDivisor;

            this.ReadRegisters(i2cController);
            this.registers[FM_Click.REGISTER_CHANNEL] &= 0xFE00;
            this.registers[FM_Click.REGISTER_CHANNEL] |= (ushort)newChannel;
            this.registers[FM_Click.REGISTER_CHANNEL] |= (1 << FM_Click.BIT_TUNE);
            this.UpdateRegisters(i2cController);

            while (true)
            {
                this.ReadRegisters(i2cController);
                if ((this.registers[FM_Click.REGISTER_STATUSRSSI] & (1 << BIT_STC)) != 0)
                    break;
            }

            this.ReadRegisters(i2cController);
            this.registers[FM_Click.REGISTER_CHANNEL] &= 0x7FFF;
            this.UpdateRegisters(i2cController);

            while (true)
            {
                this.ReadRegisters(i2cController);
                if ((this.registers[FM_Click.REGISTER_STATUSRSSI] & (1 << BIT_STC)) == 0)
                    break;
            }
        }

        private bool SeekDevice(SeekDirection direction, I2cDevice i2cController)
        {
            this.ReadRegisters(i2cController);

            this.registers[FM_Click.REGISTER_POWERCFG] &= 0xFBFF;

            if (direction == SeekDirection.Backward)
                this.registers[FM_Click.REGISTER_POWERCFG] &= 0xFDFF;
            else
                this.registers[FM_Click.REGISTER_POWERCFG] |= 1 << FM_Click.BIT_SEEKUP;

            this.registers[FM_Click.REGISTER_POWERCFG] |= (1 << FM_Click.BIT_SEEK);
            this.UpdateRegisters(i2cController);

            while (true)
            {
                this.ReadRegisters(i2cController);
                if ((this.registers[FM_Click.REGISTER_STATUSRSSI] & (1 << FM_Click.BIT_STC)) != 0)
                    break;
            }

            this.ReadRegisters(i2cController);
            int valueSFBL = this.registers[FM_Click.REGISTER_STATUSRSSI] & (1 << FM_Click.BIT_SFBL);
            this.registers[FM_Click.REGISTER_POWERCFG] &= 0xFEFF;
            this.UpdateRegisters(i2cController);

            while (true)
            {
                this.ReadRegisters(i2cController);
                if ((this.registers[FM_Click.REGISTER_STATUSRSSI] & (1 << FM_Click.BIT_STC)) == 0)
                    break;
            }

            if (valueSFBL > 0)
                return false;

            return true;
        }
    }
}
