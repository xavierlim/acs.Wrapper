!InternalErrorExitBuffer

CALL ErrorExit
STOP



ErrorExit:
	CALL TurnOffMotors
	ENABLE LIFTER_AXIS
	WAIT 1000
	CALL LowerLifter
	DISABLE LIFTER_AXIS
	CALL Unclamp	
	CALL LowerStopper	
	CALL ClearBoardAvailable	
	CALL ClearMachineReady	
	CURRENT_STATUS = ERROR_STATUS
RET

TurnOffMotors:
	DISABLEALL
RET

LowerLifter:
	Lifter_Lowered = 0
	ptp/v LIFTER_AXIS,0,10
	till ^MST(LIFTER_AXIS).#MOVE
	wait 200
	Lifter_Lowered = 1


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