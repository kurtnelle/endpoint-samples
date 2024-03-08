#! /usr/bin/python 

import time
import serial

# import GHI
import GHIElectronics.Endpoint.Core.EPM815.Gpio as Gpio
import GHIElectronics.Endpoint.Core.EPM815.SerialPort as SerialPort

def main():
    SerialPort.Initialize(SerialPort.Uart8, False)
    ser = serial.Serial(SerialPort.Uart8, baudrate=9600)

    ser.close()
    ser.open()
    if ser.isOpen():	
        while (True):
            ser.write(bytes("Hello I am Endpoint", 'utf-8'))    
                
            time.sleep(1)    
            
    ser.close()


if __name__ == "__main__":
    main()