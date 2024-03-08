#! /usr/bin/python 

import time
import spidev 

# import GHI
import GHIElectronics.Endpoint.Core.EPM815.Gpio as Gpio
import GHIElectronics.Endpoint.Core.EPM815.Spi as Spi

def main():

    Spi.Initialize(Spi.Spi4)

    spi = spidev.SpiDev(Spi.Spi4, 0)
    spi.mode=0b00
    spi.lsbfirst=False
    spi.max_speed_hz=1000000

    while (True):
        response=spi.xfer([0x1,0x2,0x3,0x4])
        time.sleep(1)






if __name__ == "__main__":
    main()