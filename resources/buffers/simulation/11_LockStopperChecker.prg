#/ Controller version = 3.10
#/ Date = 4/22/2022 5:46 PM
#/ User remarks = 
#11

!LockStopperChecker

WAIT 500
LockStopper_Bit = 1
till StopperLocked_Bit = 1
stop
