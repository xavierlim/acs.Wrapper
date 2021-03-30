!EmergencyStopBuffer

!AUTOEXEC:

CALL EnableOptos	
CALL Unclamp	
CALL LowerStopper	
CALL ClearBoardAvailable	
CALL ClearMachineReady	
CURRENT_STATUS = SAFE_STATUS
ERROR_CODE = ERROR_SAFE

STOP

EnableOptos:
	StopSensor_Bit = 1
RET

Unclamp:
	ClampPanel_Bit = 0
RET

LowerStopper:
	LockStopper_Bit = 0
	RaiseBoardStopStopper_Bit = 0
RET

ClearBoardAvailable:
	DownStreamBoardAvailable_Bit = 0
RET

ClearMachineReady:
	SmemaUpStreamMachineReady_Bit = 0
RET