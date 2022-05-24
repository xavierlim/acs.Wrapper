#/ Controller version = 3.10
#/ Date = 4/22/2022 5:51 PM
#/ User remarks = 
#24
!! Buffer for simultaneous load and unload
StopFlag = 0
START 25, 1
IF UpstreamBoardAvailableSignal_Bit = 1
STOP 20

ELSE IF UpstreamBoardAvailableSignal_Bit = 0
	WHILE ^UpstreamBoardAvailableSignal_Bit
	TILL UpstreamBoardAvailableSignal_Bit = 1
	STOP 20
	END

END
END
STOP
