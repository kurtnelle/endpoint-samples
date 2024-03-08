#! /usr/bin/python 
import time

# import GHI
import GHIElectronics.Endpoint.Core.EPM815.Gpio as Gpio
import GHIElectronics.Endpoint.Core.EPM815.Pwm as Pwm

def main():
    pwm = Pwm.PwmController(Pwm.Pin.PA3)

    pwm.Frequency = 1000
    pwm.DutyCycle = 0.5

    pwm.Start()

    pwm.Stop()

if __name__ == "__main__":
    main()