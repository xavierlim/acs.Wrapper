!BypassModeBuffer

ERROR_CODE = ERROR_SAFE

int BypassSensorBlockedError,BypassAcqError,BypassExitError,BypassReleaseError,BypassSmemaError

BypassSensorBlockedError = 101
BypassAcqError = 102
BypassExitError = 103
BypassReleaseError = 104
BypassSmemaError = 105


if 	(EntryOpto_Bit = 1 & ExitOpto_Bit = 1 & BoardStopPanelAlignSensor_Bit = 1)								!IF ALL SENSORS BLOCKED
	ERROR_CODE = BypassSensorBlockedError																	!ERROR
	CALL TurnOnAllLightAndSoundTheHom																			!ALARM AND LIGHT
	CALL ErrorExit																								!ERROR EXIT
else																										!ELSE IF NOT ALL SENSORS BLOCKED
	CURRENT_STATUS = BYPASS_STATUS																				!SET CURRENT STATUS = BYPASS STATUS
	if ExitOpto_Bit = 1																							!IF EXIT OPTO BLOCKED
		CALL SetDownstreamSmemaBoardAvailable																		!SET DOWNSTREAM SMEMA AVAILABLE
		!TILL DownstreamMachineReadySignal_Bit = 1																	!UNTIL DOWNSTREAM MACHINE SMEMA READY
		CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease													!CONTINUE CONVEYOR BELT DOWNSTREAM SPEED TO RELEASE
	else																										!ELSE IF EXIT OPTO NOT BLOCKED
		CALL StartConveyorBeltsDownstreamInternalSpeed																!START CONVEYOR BELT DOWNSTEAM INTERNAL SPEED
		TILL EntryOpto_Bit = 0 | ExitOpto_Bit = 0 | BoardStopPanelAlignSensor_Bit = 0,BypassModeBuffer_WaitTimeToSearch				!UNTIL ANY OPTO BLOCKED OR TIMEOUT
		if (EntryOpto_Bit = 1 | ExitOpto_Bit = 1 | BoardStopPanelAlignSensor_Bit = 1) 								!IF ANY SENSOR BLOCKED
			CALL SendPanel																								!CALL SEND PANEL
		else																										!ELSE IF NOT ANY SENSOR BLOCKED
			CALL GetPanel																								!CALL GET PANEL
		end
	end
end

STOP


GetPanel:
	CALL SetUpstreamSmemaMachineReady																		!SET UPSTREAM MACHINE SMEMA READY
	TILL UpstreamBoardAvailableSignal_Bit = 1																!UNTIL UPSTREAM BOARD AVAILABLE SIGNAL
	CALL StartConveyorBeltsDownstreamAndS_Acq																!CALL START CONVEYOR BELT DOWNSTREAM
	TILL EntryOpto_Bit = 1,BypassModeBuffer_WaitTimeToAcq																	!UNTIL ENTRY OPTO BLOCKED OR TIMEOUT
	if EntryOpto_Bit = 1																					!IF ENTRY OPTO BLOCKED
RET1:	TILL EntryOpto_Bit = 0																					!WAIT UNTIL ENTRY OPTO UNBLOCKED
		TILL EntryOpto_Bit = 1,BypassModeBuffer_WaitTimeToCutout																	!WAIT UNTIL ENTRY SENSOR BLOCKED OR TIMEOUT
		if EntryOpto_Bit = 1																					!IF ENTRY OPTO BLOCKED
			GOTO RET1																								!GO BACK AND WAIT UNTIL ENTRY OPTO UNBLOCKED
		else																									!ELSE IF ENTRY OPTO IS UNBLOCKED
			CALL ClearUpstreamSmemaMachineReady																		!CLEAR UPSTREAM SMEMA MACHINE READY
			CALL AdjustConveyorBeltSpeedToInternalSpeed																!ADJUST CONVEYOR SPEED TO INTERNAL SPEED
			CALL SendPanel																							!CALL SEND PANEL
		end
	else																									!ELSE IF ENTRY OPTO UNBLOCKED
		ERROR_CODE = BypassAcqError																		!SET ERROR
		CALL ErrorExit																							!CALL ERROR EXIT
		CALL TurnOnAllLightAndSoundTheHom																		!ALARM
	end
RET


StartConveyorBeltsDownstreamAndS_Acq:
	JOG/v CONVEYOR_AXIS,ConveyorBeltAcquireSpeed*ConveyorDirection
RET
AdjustConveyorBeltSpeedToInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed*ConveyorDirection
RET
ClearUpstreamSmemaMachineReady:
	SmemaUpStreamMachineReady_Bit = 0
RET

SetUpstreamSmemaMachineReady:
	SmemaUpStreamMachineReady_Bit = 1
RET

SendPanel:
	CALL SetDownstreamSmemaBoardAvailable																	!SET DOWNSTREAM SMEMA BOARD AVAILABLE
	TILL ExitOpto_Bit = 1,BypassModeBuffer_WaitTimeToExit																	!UNTIL EXIT OPTO BLOCKED OR TIMEOUT
	if ExitOpto_Bit = 1																						!IF EXIT OPTO BLOCKED
		if DownstreamMachineReadySignal_Bit = 1																	!IF DOWNSTREAM MACHINE READY
			CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease												!CALL CONVEYOR BELT DOWNSTREAM SPEED TO RELEASE
		else																									!ELSE IF DOWNSTREAM MACHINE NOT READY
			CALL StopConveyorBelts																					!STOP CONVEYOR BELT
			TILL DownstreamMachineReadySignal_Bit = 1																!WAIT UNTIL DOWNSTREAM MACHINE READ SIGNAL
			CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease												!CALL CONVEYOR BELT DOWNSTREAM SPEED TO RELEASE 
		end
	else																									!IF EXIT OPTO NOT BLOCKED								
		ERROR_CODE = BypassExitError																		!SET BYPASS ERROR
		CALL ErrorExit																							!CALL ERROR EXIT
		CALL TurnOnAllLightAndSoundTheHom																		!SET ALARM
	end
RET

StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET
StartConveyorBeltsDownstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed*ConveyorDirection
RET

ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease:								
		CALL SetConveyorBeltsDownstreamSpeedToRelease														!SET CONVEYOR BELT SPEED TO RELEASE
RET2:	TILL ExitOpto_Bit = 0,BypassModeBuffer_WaitTimeToRelease																!WAIT UNTIL EXIT OPTO UNBLOCKED OR TIMEOUT
		if ExitOpto_Bit = 0																					!IF EXIT OPTO UNBLOCKED
			TILL ExitOpto_Bit = 1,BypassModeBuffer_WaitTimeToCutout																!WAIT UNTIL EXIT OPTO BLOCKED OR TIMEOUT
			if ExitOpto_Bit = 1																					!IF EXIT OPTO BLOCKED
				GOTO RET2																							!GO BACK TO RET2
			else																								!ELSE IF EXIT OPTO NOT BLOCKED
				CALL ClearDownstreamSmemaBoardAvailable																!CLEAR DOWNSTREAM SMEMA BOARD AVAILABLE
				TILL DownstreamMachineReadySignal_Bit = 0,BypassModeBuffer_WaitTimeToSmema											!WAIT UNTIL DOWNSTREAM MACHINE NOT READY SIGNAL OR TIMEOUT
				if (DownstreamMachineReadySignal_Bit = 1)															!IF DOWNSTREAM MACHINE SMEMA READY SIGNAL
					ERROR_CODE = BypassSmemaError																	!SET BYPASS ERROR
					CALL ErrorExit																						!CALL ERROR EXIT
					CALL TurnOnAllLightAndSoundTheHom																	!SET ALARM
				else																								!ELSE IF DOWNSTREAM NOT READY SIGNAL
					CALL TurnOffConveyorBelts																			!TURN OFF CONVEYOR BELT
					CALL GetPanel																						!CALL GET PANEL
				end
			end


		else
			ERROR_CODE = BypassReleaseError
			CALL ErrorExit	
			CALL TurnOnAllLightAndSoundTheHom
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

SetDownstreamSmemaBoardAvailable:
	DownStreamBoardAvailable_Bit = 1
RET
TurnOnAllLightAndSoundTheHom:

	TowerLightRed_Bit = 1
	TowerLightYellow_Bit = 1
	TowerLightGreen_Bit = 1
	TowerLightBlue_Bit = 1
	TowerLightBuzzer_Bit = 1
 
RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET