#/ Controller version = 3.10
#/ Date = 4/22/2022 5:48 PM
#/ User remarks = 
#17
!LoadPanelBuffer
	!LoadPanelBuffer runs at every start button

ERROR_CODE = ERROR_SAFE

global int LoadPanelNotReleasedError,LoadPanelSensorBlockedError,LoadPanelAcqError,LoadPanelSlowSensorError
Panel_Count = 0 !!!!! IssacTest

LoadPanelNotReleasedError = 401
LoadPanelSensorBlockedError = 402
LoadPanelAcqError = 403
LoadPanelSlowSensorError = 404


!Sequence only commence when status is RELEASED_STATUS
REPANEL_LOADING:

if (CURRENT_STATUS = RELEASED_STATUS | CURRENT_STATUS = RELEASING_STATUS) & StopFlag = 0 
	if (BoardStopPanelAlignSensor_Bit = 0)
		CURRENT_STATUS = LOADING_STATUS
		!DownStreamBoardAvailable_Bit = 0
		StopFlag = 0

		if (OperationMode = 1) !if InlineMode
		if SqTriggerSmemaUpStreamMachineReady = 0
			CALL UpstreamSmemaMachineReady
		end
			TILL UpstreamBoardAvailableSignal_Bit = 1
		end

		if (OperationMode = 2) !if OfflineMode
			TILL EntryOpto_Bit = 1
		end

		CALL RaiseBoardStop
		CALL StartConveyorBeltsDownstream
		TILL EntryOpto_Bit = 1,LoadPanelBuffer_WaitTimeToAcq
		if EntryOpto_Bit = 1
			CALL AdjustConveyorBeltSpeedToInternalSpeed
		
			IfSlowDown:	TILL EntryOpto_Bit = 0,LoadPanelBuffer_WaitTimeToAcq
				if EntryOpto_Bit = 0 !Unblocked

					if (OperationMode = 1) !if not pingpong mode
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
	end


!if stop button is pressed after loading buffer starts, while waiting for UpstreamBoardAvailableSignal_Bit
elseif 	CURRENT_STATUS = LOADING_STATUS
			CURRENT_STATUS = RELEASED_STATUS
			StopFlag = 0
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

!after machine power up and after width homed and in Released state, use load panel at exit and pressed start button.			
elseif 	CURRENT_STATUS = RELEASED_STATUS & ExitOpto_Bit = 1
			START ReloadPanelBufferIndex,1 
			TILL ^ PST(ReloadPanelBufferIndex).#RUN
			
!after panel is at Releasing state and stop button is pressed. When panel was removed from exit opto and start button is pressed.
elseif CURRENT_STATUS = RELEASING_STATUS & ExitOpto_Bit = 0
		if PST(ReleasePanelBufferIndex).#RUN
			STOP ReleasePanelBufferIndex
		end	
			StopFlag = 0
			CURRENT_STATUS = RELEASED_STATUS
			GOTO REPANEL_LOADING

! 7 Panel at exit after PowerOnRecovery, required to reload when start inspection
elseif 	ExitOpto_Bit = 1 & EMO_Release = 1
			START ReloadPanelBufferIndex,1 
			TILL ^ PST(ReloadPanelBufferIndex).#RUN
			EMO_Release = 0
			
!after panel is at Releasing state and stop button is pressed. When panel is at exit opto and start button is pressed.		
!elseif CURRENT_STATUS = RELEASING_STATUS & ExitOpto_Bit = 1 & (OperationMode = 1 | OperationMode = 2) & StopFlag = 0
!		if PST(ReleasePanelBufferIndex).#RUN
!			STOP ReleasePanelBufferIndex
!		end	
!			START InternalMachineLoadBufferIndex,1 
!			TILL ^ PST(InternalMachineLoadBufferIndex).#RUN
!			StopFlag = 0
			
elseif CURRENT_STATUS = RELEASING_STATUS & ExitOpto_Bit = 1 & OperationMode = 0
		DownStreamBoardAvailable_Bit = 0
		if PST(ReleasePanelBufferIndex).#RUN
			STOP ReleasePanelBufferIndex
		end	
			START ReloadPanelBufferIndex,1 
			TILL ^ PST(ReloadPanelBufferIndex).#RUN
			StopFlag = 0
			
elseif CURRENT_STATUS = RELEASING_STATUS & ExitOpto_Bit = 1 & EMO_Release = 0
		DownStreamBoardAvailable_Bit = 0
		if PST(ReleasePanelBufferIndex).#RUN
			STOP ReleasePanelBufferIndex
		end	
		ERROR_CODE = LoadPanelNotReleasedError
		CALL ErrorExit
		StopFlag = 0

elseif CURRENT_STATUS = ERROR_STATUS
	CALL ErrorExit
			
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
	Start 11,1
!	wait 1000
!	LockStopper_Bit = 1
RET

StartConveyorBeltsDownstream:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,ConveyorBeltAcquireSpeed
RET

AdjustConveyorBeltSpeedToInternalSpeed:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed
RET
