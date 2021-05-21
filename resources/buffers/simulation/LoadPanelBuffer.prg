!LoadPanelBuffer
	!LoadPanelBuffer runs at every start button

ERROR_CODE = ERROR_SAFE

global int LoadPanelNotReleasedError,LoadPanelSensorBlockedError,LoadPanelAcqError,LoadPanelSlowSensorError


LoadPanelNotReleasedError = 401
LoadPanelSensorBlockedError = 402
LoadPanelAcqError = 403
LoadPanelSlowSensorError = 404


!Simulation
WAIT 3000
ERROR_CODE = ERROR_SAFE
CURRENT_STATUS = LOADED_STATUS
STOP


!Sequence only commence when status is RELEASED_STATUS
REPANEL_LOADING:

if CURRENT_STATUS = RELEASED_STATUS
	if (EntryOpto_Bit = 0 & ExitOpto_Bit = 0 & BoardStopPanelAlignSensor_Bit = 0)
		CURRENT_STATUS = LOADING_STATUS

		if (PingPongMode = 0) !if not pingpong mode
			CALL UpstreamSmemaMachineReady
			TILL UpstreamBoardAvailableSignal_Bit = 1
		end

		CALL RaiseBoardStop
		CALL StartConveyorBeltsDownstream
		TILL EntryOpto_Bit = 1,LoadPanelBuffer_WaitTimeToAcq
		if EntryOpto_Bit = 1
		
			IfSlowDown:	TILL EntryOpto_Bit = 0,LoadPanelBuffer_WaitTimeToAcq
				if EntryOpto_Bit = 0 !Unblocked

					if (PingPongMode = 0) !if not pingpong mode
						CALL ClearUpstreamSmemaMachineReady
					end

				CALL AdjustConveyorBeltSpeedToInternalSpeed
				START InternalMachineLoadBufferIndex,1
				TILL ^ PST(InternalMachineLoadBufferIndex).#RUN
				
				else
				
			   		GOTO IfSlowDown
				end
		else
			ERROR_CODE = LoadPanelAcqError
			CALL ErrorExit
		end
	else
		ERROR_CODE = LoadPanelSensorBlockedError
		CALL ErrorExit
	end


!if stop button is pressed after loading buffer starts, while waiting for UpstreamBoardAvailableSignal_Bit
elseif 	CURRENT_STATUS = LOADING_STATUS
			CURRENT_STATUS = RELEASED_STATUS
			goto REPANEL_LOADING

!if panel already loaded, and stop button is press regardless inspection started or not	
elseif 	CURRENT_STATUS = LOADED_STATUS
			!DO NOTHING, SQ WILL REINSPECT PANEL

!after panel is at PreReleased state and stop button is pressed. When panel was removed from exit opto and start button is pressed.
elseif 	CURRENT_STATUS = PRERELEASED_STATUS & ExitOpto_Bit = 0
			CURRENT_STATUS = RELEASED_STATUS
			GOTO REPANEL_LOADING

!after panel is at PreReleased state and stop button is pressed. When panel is at exit opto and start button is pressed.		
elseif 	CURRENT_STATUS = PRERELEASED_STATUS & ExitOpto_Bit = 1
			START ReloadPanelBufferIndex,1 
			TILL ^ PST(ReloadPanelBufferIndex).#RUN
			
!after panel is at Releasing state and stop button is pressed. When panel was removed from exit opto and start button is pressed.
elseif CURRENT_STATUS = RELEASING_STATUS & ExitOpto_Bit = 0
		if PST(ReleasePanelBufferIndex).#RUN
			STOP ReleasePanelBufferIndex
		end	
			CURRENT_STATUS = RELEASED_STATUS
			GOTO REPANEL_LOADING

!after panel is at Releasing state and stop button is pressed. When panel is at exit opto and start button is pressed.		
elseif CURRENT_STATUS = RELEASING_STATUS & ExitOpto_Bit = 1
		if PST(ReleasePanelBufferIndex).#RUN
			STOP ReleasePanelBufferIndex
		end	
			START ReloadPanelBufferIndex,1 
			TILL ^ PST(ReloadPanelBufferIndex).#RUN
			
else			
	ERROR_CODE = LoadPanelNotReleasedError
	CALL ErrorExit

end

STOP
RET
ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

UpstreamSmemaMachineReady:
	SmemaUpStreamMachineReady_Bit = 1
RET

ClearUpstreamSmemaMachineReady:
	SmemaUpStreamMachineReady_Bit = 0
RET


RaiseBoardStop:
	RaiseBoardStopStopper_Bit = 1
	Till StopperArmUp_Bit
	wait 1000
	LockStopper_Bit = 1
RET

StartConveyorBeltsDownstream:
	JOG/v CONVEYOR_AXIS,ConveyorBeltAcquireSpeed*ConveyorDirection
RET

AdjustConveyorBeltSpeedToInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed*ConveyorDirection
RET