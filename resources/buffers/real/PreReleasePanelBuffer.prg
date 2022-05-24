#/ Controller version = 3.10
#/ Date = 4/22/2022 5:49 PM
#/ User remarks = 
#19
!PreReleasePanelBuffer

ERROR_CODE = ERROR_SAFE

global int PreReleaseNotLoadedError,PreReleasePanelNotFreedError,PreReleaseWaitTOError

PreReleaseNotLoadedError = 501
PreReleasePanelNotFreedError = 502
PreReleaseWaitTOError = 503


real SlowPosition
real absPosTemp
SlowPosition = 0
absPosTemp = 0

absPosTemp = RPOS(CONVEYOR_AXIS)
SlowPosition = absPosTemp + (DistanceBetweenStopSensorAndExitSensor - DistanceBetweenSlowPositionAndExitSensor)


if CURRENT_STATUS = LOADED_STATUS
	CURRENT_STATUS = PRERELEASING_STATUS

	PanelFreed = 0
	START FreePanelBufferIndex,1
	TILL ^ PST(FreePanelBufferIndex).#RUN

	if PanelFreed = 0
		ERROR_CODE = PreReleasePanelNotFreedError
		CALL ErrorExit
		CURRENT_STATUS = ERROR_STATUS
	else
	    ReleaseCommandReceived = 0
		CALL StartConveyorBeltsDownstream

		TILL RPOS(CONVEYOR_AXIS) > SlowPosition
		CALL AdjustConveyorBeltSpeedToSlow
		TILL ExitOpto_Bit = 1 | ReleaseCommandReceived <> 0	 ,PreReleasePanelBuffer_WaitTimeToExit					!wait until exit opto blocked or timeout

		if (ExitOpto_Bit = 1 & ReleaseCommandReceived = 0)										!if exit opto blocked and no receive command and NOT timeout
			CALL TurnOffConveyor																!STOP CONVEYOR BELT
			CURRENT_STATUS = PRERELEASED_STATUS													!SET CURRENT_STATUS = PRERELEASED

		elseif (ReleaseCommandReceived <> 0 & ExitOpto_Bit = 0)									!if exit opto not blocked and receive command and NOT timeout
			!PreRelease Panel End

		elseif (ReleaseCommandReceived = 0 & ExitOpto_Bit = 0)									!exit opto not blocked and no receive command AND timeout

			ERROR_CODE = PreReleaseWaitTOError	!call error
			CALL ErrorExit	
			CURRENT_STATUS = ERROR_STATUS
		end		
	end

else
	ERROR_CODE = PreReleaseNotLoadedError
	CALL ErrorExit	
	CURRENT_STATUS = ERROR_STATUS
end

STOP

StartConveyorBeltsDownstream:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,ConveyorBeltReleaseSpeed
RET

AdjustConveyorBeltSpeedToSlow:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,ConveyorBeltSlowSpeed
RET

TurnOffConveyor:
	HALT CONVEYOR_AXIS
RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET
