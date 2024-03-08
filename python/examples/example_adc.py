#! /usr/bin/python 

import time

# import GHI
import GHIElectronics.Endpoint.Core.EPM815.Adc as Adc
import GHIElectronics.Endpoint.Core.EPM815.Gpio as Gpio

def main():
    
    adc = Adc.AdcController(Adc.Pin.PA3)
   
    while (True):
        print(adc.Read())

        time.sleep(1)
        




if __name__ == "__main__":
    main()