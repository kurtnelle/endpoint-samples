#! /usr/bin/python

# reading Accel G248 
import time
import sys
from smbus import SMBus

# import GHI
import GHIElectronics.Endpoint.Core.EPM815.I2c as I2c 

i2caddress = 0x1C
i2cbus = SMBus(I2c.I2c6)

def WriteToRegister(reg: int, value: int):
    i2cbus.write_byte_data(i2caddress, reg, value)  

def ReadFromRegister(reg: int, count: int):
    ret = i2cbus.read_i2c_block_data(i2caddress, reg, count)

    return ret

def GetValues():
    data = [0] * 3
    read = ReadFromRegister(1, 6)

    data[0] = (read[0] << 2) | ((read[1] >> 6) & 0x3F)
    if (data[0] > 511):
        data[0] = data[0] - 1024

    data[1] = (read[2] << 2) | ((read[3] >> 6) & 0x3F)
    if (data[1] > 511):
        data[1] = data[1] - 1024

    data[2] = (read[4] << 2) | ((read[5] >> 6) & 0x3F)
    if (data[2] > 511):
        data[2] = data[2] - 1024

    return data



def main():
    I2c.Initialize(I2c.I2c6, 400)
    WriteToRegister(0x2A, 1)

    while (True):
        read = GetValues()
        x = read[0]
        y = read[1]
        z = read[2]

        print("x " + str(x) + ", y = " +str( y) + ", z = " + str(z))
        time.sleep(1)

        I2c.UnInitialize(I2c.I2c6)




if __name__ == "__main__":
    main()