!ReloadPanelBuffer

global int ReloadPanelError
ReloadPanelError = 0

global int ReloadPanelStateError,ReloadPanelFreeError,ReloadPanelSearchError,ReloadPanelSlowSensorError

ReloadPanelStateError = 1
ReloadPanelFreeError = 2
ReloadPanelSearchError = 3
ReloadPanelSlowSensorError = 4

int WaitTimeToSearch
int ReloadDelayTime

if CURRENT_STATUS = LOADED_STATUS								!IF CURRENT STATE = LOADED STATE
	CURRENT_STATUS = RELOADING_STATUS								!SET STATE = RELOADING STATUS
	START FreePanelBufferIndex,1									!START FREE PANEL BUFFER
	TILL ^ PST(FreePanelBufferIndex).#RUN							!UNTIL FREE PANEL COMPLETE
	if PanelFreed = 1												!IF PANEL IS FREED
		CALL ContinueReloading											!CALL CONTINUERELOADING
	else															!ELSE IF PANEL IS NOT FREED
		ReloadPanelError = ReloadPanelFreeError							!SET RELOAD PANEL FREE ERROR
		CALL ErrorExit														!CALL ERROR EXIT
	end
else															!ELSE IF CURRENT STATE IS NOT = LOADED STATE
	if CURRENT_STATUS = PRERELEASED_STATUS							!IF CURRENT STATUS = PRERELEASED STATUS
		CURRENT_STATUS = RELOADING_STATUS								!SET CURRENT STATUS = RELOADING STATUS
		CALL ContinueReloading											!CALL CONTINUERELOADING
	else															!ELSE IF CURRENT STATE IS NOT PRELEASED STATUS
		ReloadPanelError = ReloadPanelStateError						!SET RELOAD PANEL FREE ERROR
		CALL ErrorExit														!CALL ERROR EXIT
	end

end


STOP

ContinueReloading:
	CALL StartConveyorBeltsUpstreamInternalSpeed				!START CONVEYOR BELT UPSTREAM
	TILL EntryOpto_Bit = 1,WaitTimeToSearch						!UNTIL ENTRY OPTO BLOCKED OR TIMEOUT
	if EntryOpto_Bit = 1										!IF ENTRY OPTO BLOCKED
		CALL StopConveyorBelts										!STOP CONVEYOR BELT
		WAIT ReloadDelayTime										!WAIT UNTIL RELOAD DELAY TIME EXPIRES
		CALL RaiseBoardStop											!RAISE STOPPER THEN LOCK STOPPER
		CALL StartConveyorBeltsDownstreamInternalSpeed				!START CONVEYOR BELT DOWNSTREAM
		START InternalMachineLoadBufferIndex,1						!START INTERNAL MACHINE LOAD BUFFER
		TILL ^ PST(InternalMachineLoadBufferIndex).#RUN				!UNTIL INTERNAL MACHINE LOAD BUFFER END
	else														!ELSE IF ENTRY OPTO NOT BLOCKED
		ReloadPanelError = ReloadPanelSearchError					!SET RELOAD PANEL FREE ERROR
		CALL ErrorExit												!CAL ERROR EXIT
	end
RET


StartConveyorBeltsDownstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed
RET

RaiseBoardStop:
	RaiseBoardStopStopper_Bit = 1
	IF StopperArmUp_Bit = 1
		LockStopper_Bit = 1
	END
RET

StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET

StartConveyorBeltsUpstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,-ConveyorBeltLoadingSpeed
RET


ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET
