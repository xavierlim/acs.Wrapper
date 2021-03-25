#/ Controller version = 3.01
#/ Date = 03-Mar-21 2:40 PM
#/ User remarks = 
#21
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

if CURRENT_STATUS = LOADED_STATUS
	CURRENT_STATUS = RELOADING_STATUS
	START FreePanelBufferIndex,1
	TILL ^ PST(FreePanelBufferIndex).#RUN
	if PanelFreed = 1
		CALL ContinueReloading
	else
		ReloadPanelError = ReloadPanelFreeError
		CALL ErrorExit
	end
else
	if CURRENT_STATUS = PRERELEASED_STATUS
		CURRENT_STATUS = RELOADING_STATUS
		CALL ContinueReloading
	else
		ReloadPanelError = ReloadPanelStateError
		CALL ErrorExit
	end

end


STOP

ContinueReloading:
	CALL StartConveyorBeltsUpstreamInternalSpeed
	TILL IN(EntryOpto_Port).EntryOpto_Bit = 1,WaitTimeToSearch
	if IN(EntryOpto_Port).EntryOpto_Bit = 1
		CALL StopConveyorBelts
		WAIT ReloadDelayTime
		CALL RaiseBoardStop
		CALL StartConveyorBeltsDownstreamInternalSpeed
		START InternalMachineLoadBufferIndex,1
		TILL ^ PST(InternalMachineLoadBufferIndex).#RUN
	else
		ReloadPanelError = ReloadPanelSearchError
		CALL ErrorExit
	end
RET


StartConveyorBeltsDownstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed
RET

RaiseBoardStop:
	OUT(LockStopper_Port).LockStopper_Port = 1
	OUT(RaiseBoardStopStopper_Port).RaiseBoardStopStopper_Bit = 1
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


