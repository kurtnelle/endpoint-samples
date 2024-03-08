#! /usr/bin/python 

import time

import gpiod
import GHIElectronics.Endpoint.Core.EPM815.Display as Display
import GHIElectronics.Endpoint.Core.EPM815.Gpio as Gpio

from datetime import datetime
import subprocess

def main():

    
    pin = Gpio.Pin.PD14 # led

    chip= gpiod.Chip(str(int(pin / 16)))
    line = chip.get_line(pin % 16)
    line.request(consumer='test', type=gpiod.LINE_REQ_DIR_OUT)

    line.set_value(1) # turn back ligh on

    configuration = Display.DisplayConfiguration()
    configuration.Clock = 10000
    configuration.Width = 480
    configuration.Hsync_start = 480 + 2
    configuration.Hsync_end = 480 + 2 + 41
    configuration.Htotal = 480 + 2 + 41 + 2
    configuration.Height = 272
    configuration.Vsync_start = 272 + 2
    configuration.Vsync_end = 272 + 2 + 10
    configuration.Vtotal = 272 + 2 + 10 + 2

    displayController = Display.DisplayController(configuration)

    frame = bytearray(configuration.Width * configuration.Height *  2)

    dt = subprocess.check_output(["date"]) 


    count = len(frame)
    for x in range(0, count,2):
        frame[x + 0] = 0xF8
        frame[x + 1] = 0x00
        
    fps = 0
    last_dt = datetime.now()
    while(True):
        now = datetime.now()
        displayController.Flush(frame, 0, count, configuration.Width, configuration.Height)
        if (now.second == last_dt.second):
            fps +=1
        else:
            print (fps)
            fps = 0
            last_dt = now






if __name__ == "__main__":
    main()