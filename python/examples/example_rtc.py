#! /usr/bin/python 

import time
from datetime import datetime
import sys

# import GHI
import GHIElectronics.Endpoint.Core.EPM815.Rtc as Rtc


def main():
    rtc = Rtc.RtcController()
    rtc.EnableChargeMode(Rtc.BatteryChargeMode.FAST)
    rtc.Now = datetime(2024,3,5,1,5,00)
    print(rtc.Now)



if __name__ == "__main__":
    main()