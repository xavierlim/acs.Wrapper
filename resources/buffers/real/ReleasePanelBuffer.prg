!ReleasePanelBuffer

ERROR_CODE = ERROR_SAFE

global int ReleasePanelStateError,ReleasePanelFreeError,ReleasePanelExitError,ReleasePanelReleaseError,ReleasePanelSmemaError

ReleasePanelStateError = 601
ReleasePanelFreeError = 602
ReleasePanelExitError = 603
ReleasePanelReleaseError = 604
ReleasePanelSmemaError = 605


real SlowPosition
real absPosTemp
SlowPosition = 0
absPosTemp = 0

absPosTemp = RPOS(CONVEYOR_AXIS)
SlowPosition = absPosTemp + (DistanceBetweenStopSensorAndExitSensor - DistanceBetweenSlowPositionAndExitSensor)



if (PingPongMode = 0)	!if not pingpong mode
	if CURRENT_STATUS = PRERELEASED_STATUS
		CURRENT_STATUS = RELEASING_STATUS
		CALL SetDownstreamBoardAvailable
		TILL DownstreamMachineReadySignal_Bit = 1
		CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease

	else
		if CURRENT_STATUS = PRERELEASING_STATUS
			CURRENT_STATUS = RELEASING_STATUS
			CALL ContinueFrom_SetDownstreamSmemaBoardAvailable
		else
			if CURRENT_STATUS = LOADED_STATUS
				CURRENT_STATUS = RELEASING_STATUS
				START FreePanelBufferIndex,1
				TILL ^ PST(FreePanelBufferIndex).#RUN
				if PanelFreed = 1
					CALL StartConveyorBeltsDownstreamInternalSpeed
					CALL ContinueFrom_SetDownstreamSmemaBoardAvailable
				else
					ERROR_CODE = ReleasePanelFreeError
					CALL ErrorExit	
				end
			else
				ERROR_CODE = ReleasePanelStateError
				CALL ErrorExit	
			end
		end
	end
else																							!if pingpong mode
	if CURRENT_STATUS = PRERELEASED_STATUS
		CURRENT_STATUS = RELEASING_STATUS
		CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease

	else 
		if 	CURRENT_STATUS = PRERELEASING_STATUS
			CURRENT_STATUS = RELEASING_STATUS					
		else
			if 	CURRENT_STATUS = LOADED_STATUS
				CURRENT_STATUS = RELEASING_STATUS
				START FreePanelBufferIndex,1
				TILL ^ PST(FreePanelBufferIndex).#RUN
					if PanelFreed = 1
						CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease_PingPongMode	!call pingpong mode version of release label
					else
						ERROR_CODE = ReleasePanelFreeError
						CALL ErrorExit	
					end
			else
				ERROR_CODE = ReleasePanelStateError
				CALL ErrorExit
			end
		end
	end
end

STOP

SetDownstreamBoardAvailable:
	DownStreamBoardAvailable_Bit = 1
RET

ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease:
		CALL SetConveyorBeltsDownstreamSpeedToRelease
		TILL RPOS(CONVEYOR_AXIS) > SlowPosition	
		CALL AdjustConveyorBeltSpeedToSlow
RET3:	TILL ExitOpto_Bit = 0,ReleasePanelBuffer_WaitTimeToRelease
		if ExitOpto_Bit = 0
			TILL ExitOpto_Bit = 1,ReleasePanelBuffer_WaitTimeToCutout
			if ExitOpto_Bit = 1
				GOTO RET3
			else
				CALL ClearDownstreamSmemaBoardAvailable
				TILL DownstreamMachineReadySignal_Bit = 0,ReleasePanelBuffer_WaitTimeToSmema
				if DownstreamMachineReadySignal_Bit = 1
					ERROR_CODE = ReleasePanelSmemaError
					CALL ErrorExit	
				else
					CALL TurnOffConveyorBelts
					BeltShroudVaccumON_Bit = 1
					Wait ReleasePanelBuffer_WaitTimeToBeltVacuum
					BeltShroudVaccumON_Bit = 0
					CURRENT_STATUS = RELEASED_STATUS
				end
			end
		else
			ERROR_CODE = ReleasePanelReleaseError
			CALL ErrorExit	
		end
RET

TurnOffConveyorBelts:
	HALT CONVEYOR_AXIS
RET

ClearDownstreamSmemaBoardAvailable:
	DownStreamBoardAvailable_Bit = 0
RET

SetConveyorBeltsDownstreamSpeedToRelease:
	JOG/v CONVEYOR_AXIS,ConveyorBeltReleaseSpeed*ConveyorDirection
RET

ContinueFrom_SetDownstreamSmemaBoardAvailable:
	CALL SetDownstreamSmemaBoardAvailable
	TILL RPOS(CONVEYOR_AXIS) > SlowPosition	
	CALL AdjustConveyorBeltSpeedToSlow
	TILL ExitOpto_Bit = 1,ReleasePanelBuffer_WaitTimeToExit
	if ExitOpto_Bit = 1
		if DownstreamMachineReadySignal_Bit = 1
			CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease
		else
			CALL StopConveyorBelts
			TILL DownstreamMachineReadySignal_Bit = 1
			CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease
		end
	else
		ERROR_CODE = ReleasePanelExitError
		CALL ErrorExit	
	end
RET


StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET

SetDownstreamSmemaBoardAvailable:
	DownStreamBoardAvailable_Bit = 1
RET


StartConveyorBeltsDownstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltReleaseSpeed*ConveyorDirection
RET

AdjustConveyorBeltSpeedToSlow:
	JOG/v CONVEYOR_AXIS,ConveyorBeltSlowSpeed*ConveyorDirection

RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET


ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease_PingPongMode:
			absPosTemp = RPOS(CONVEYOR_AXIS)
			SlowPosition = absPosTemp + (DistanceBetweenStopSensorAndExitSensor - DistanceBetweenSlowPositionAndExitSensor)

		CALL SetConveyorBeltsDownstreamSpeedToRelease
		TILL RPOS(CONVEYOR_AXIS) > SlowPosition	
		CALL AdjustConveyorBeltSpeedToSlow
		TILL ExitOpto_Bit = 1,ReleasePanelBuffer_WaitTimeToRelease

			if		ExitOpto_Bit = 1
					CALL TurnOffConveyorBelts
					BeltShroudVaccumON_Bit = 1
					Wait ReleasePanelBuffer_WaitTimeToBeltVacuum
					BeltShroudVaccumON_Bit = 0
					CURRENT_STATUS = RELEASED_STATUS

			else

			ERROR_CODE = ReleasePanelReleaseError
			CALL ErrorExit	

			end
RET