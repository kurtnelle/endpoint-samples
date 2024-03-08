#! /usr/bin/python 

import gpiod
import time

# import GHI
import GHIElectronics.Endpoint.Core.EPM815.Gpio as Gpio


pin = Gpio.Pin.PC0 # led

chip=gpiod.Chip(str(int(pin / 16)))
line = chip.get_line(pin % 16)
line.request(consumer='test', type=gpiod.LINE_REQ_DIR_OUT)

while True:
    line.set_value(1)
    time.sleep(0.5)
    line.set_value(0)
    time.sleep(0.5)