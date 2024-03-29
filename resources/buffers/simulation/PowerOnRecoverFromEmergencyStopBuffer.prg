!PowerOnRecoverFromEmergencyStopBuffer

global int FreePanelToUnliftError, FreePanelToUnclampError, PowerOnRecoveryWidthNotHomed

FreePanelToUnliftError = 303
FreePanelToUnclampError = 304
PowerOnRecoveryWidthNotHomed = 900


int WidthToW_0_Position
START ConveyorResetBufferIndex, 1
TILL ^ PST(ConveyorResetBufferIndex).#RUN
WAIT 5000

CALL InitializeACS
TILL Reset_Button_Bit = 1 ,PowerOnRecoveryBuffer_WaitTimeToReset
CALL InitializeMotors

CALL EnableOptos
!CALL ErrorExit

if EstopAndDoorOpenFeedback_Bit = 0																				!IF SAFETY NOT ENGAGED
	CURRENT_STATUS = ERROR_STATUS																					!SET CURRENT STATUS = ERROR STATUS
	CALL ErrorExit																									!CALL ERROR EXIT

else
	EMO_RECOVERY:

	！Simulation
	！if ConveyorLifterHomed = 0
	！START LifterHomingBufferIndex, 1																											!START WIDTH HOMING BUFFER
	！TILL ^ PST(LifterHomingBufferIndex).#RUN
	！end

	CALL EMO_Recovery_FreePanelSeq
	TILL PanelFreed = 1

		CURRENT_STATUS = SAFE_STATUS																					!SET CURRENT STATUS = SAFE STATUS		
		
	if ByPassR2L = 1 | ByPassL2R = 1																						!IF BYPASS MODE = 1
		START BypassModeBufferIndex,1																				!START BYPASS MODE BUFFER
	else																										!ELSE 
		CURRENT_STATUS = SEARGING_STATUS																			!SET CURRENT STATUS = SEARCHING STATUS
		if (EntryOpto_Bit = 1 | ExitOpto_Bit = 1 | BoardStopPanelAlignSensor_Bit = 1)								!IF ANY SENSORS BLOCKED
			CALL StartConveyorBeltsDownstream																			!START CONVEYOR BELT DOWNSTREAM
			CALL ContinueFindPanel																						!START FIND PANEL BUFFER
		else																										!ELSE
			CALL StartConveyorBeltsDownstream																			!START CONVEYOR BELT DOWNSTREAM
			TILL (EntryOpto_Bit = 1 | ExitOpto_Bit = 1 | BoardStopPanelAlignSensor_Bit = 1) ,PowerOnRecoveryBuffer_WaitTimeToSearch			!TIL ANY SENSORS BLOCKED OR TIMEOUT
			if (EntryOpto_Bit = 1 | ExitOpto_Bit = 1 | BoardStopPanelAlignSensor_Bit = 1)									!IF ANY SENSORS BLOCKED
				CALL ContinueFindPanel																							!START FIND PANEL BUFFER
			else																											!ELSE IF TIMEOUT
				CALL StopConveyorBelts																						!STOP CONVEYOR BELT
				!CALL HomeWidth																								!START WIDTH HOMING BUFFER
			end
		end
	end
end

STOP


ContinueFindPanel:																									
	!if ConveyorWidthHomed = 1																						!IF WIDTH HOMED
	    TILL ExitOpto_Bit = 1,PowerOnRecoveryBuffer_WaitTimeToExit																			!WAIT UNTIL EXIT OPTO BLOCKED OR TIMEOUT
		if ExitOpto_Bit = 1																								!IF EXIT OPTO BLOCKED	
			CALL StopConveyorBelts																							!STOP CONVEYOR BELT
			CURRENT_STATUS = PRERELEASED_STATUS																				!SET STATUS = PRERELEASED STATE
		else																											!ELSE IF TIMEOUT
			CALL ErrorExit																								!CALL ERROR EXIT
			CURRENT_STATUS = ERROR_STATUS																				!SET STATUS = ERROR STATUS
		end
	!else																											!IF WIDTH NOT HOMED
		!ERROR_CODE = PowerOnRecoveryWidthNotHomed																		!PowerOnRecoveryWidthNotHomed error code 900
		!CALL ErrorExit																								!CALL ERROR EXIT
		!CURRENT_STATUS = ERROR_STATUS																					!SET CURRENT STATUS = ERROR STATUS
	!end

RET

HomeWidth:

	if ConveyorWidthHomed = 1																						!IF WIDTH HOMED
		CURRENT_STATUS = RELEASED_STATUS																				!SET CURRENT STATUS = RELEASED
	else																											!ELSE
		CURRENT_STATUS = WIDTH_HOMING_STATUS																			!SET CURRENT STATUS = WIDTH HOMING STATUS
		CALL HomeConveyorWidthMotor																						!CALL CONVEYOR WIDTH HOMING BUFFER
		if ConveyorWidthHomed = 1																						!IF CONVEYOR WIDTH HOMED
			!CALL AdjustConveyorWidthToW_0																					!CALL CHANGE WIDTH BUFFER
			CURRENT_STATUS = RELEASED_STATUS																				!SET CURRENT STATUS = RELEASED STATUS
		else																											!ELSE
			ERROR_CODE = PowerOnRecoveryWidthNotHomed																		!PowerOnRecoveryWidthNotHomed error code 900
			CALL ErrorExit																									!CALL ERROR EXIT
			CURRENT_STATUS = ERROR_STATUS																					!SET CURRENT STATUS = ERROR STATUS
		end
	end
RET

HomeConveyorWidthMotor:																								
	START WidthHomingBufferIndex, 1																											!START WIDTH HOMING BUFFER
	TILL ^ PST(WidthHomingBufferIndex).#RUN																									!UNTIL BUFFER ENDS
RET

AdjustConveyorWidthToW_0:																							!START CHANGE WIDTH BUFFER
	PTP/em CONVEYOR_WIDTH_AXIS, WidthToW_0_Position																	!UNTIL WIDTH AT POSITION
RET



ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

InitializeACS:
!User to reset EMO to clear STO
RET

InitializeMotors:
ENABLE X_AXIS
ENABLE Y_AXIS
ENABLE Z_AXIS

ENABLE CONVEYOR_AXIS
ENABLE CONVEYOR_WIDTH_AXIS
ENABLE LIFTER_AXIS

till MST(X_AXIS).#ENABLED
till MST(Y_AXIS).#ENABLED
till MST(Z_AXIS).#ENABLED

till MST(CONVEYOR_AXIS).#ENABLED
till MST(CONVEYOR_WIDTH_AXIS).#ENABLED
till MST(LIFTER_AXIS).#ENABLED

RET

EnableOptos:
	StopSensor_Bit = 1
RET

StartConveyorBeltsDownstream:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,ConveyorBeltUnloadingSpeed
RET

StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET

EMO_Recovery_FreePanelSeq:
		CALL UnclampPanel
		TILL RearClampDown_Bit & FrontClampDown_Bit,5000
		CALL LowerLifter 
		TILL Lifter_Lowered = 1, 10000

		if Lifter_Lowered <> 1
			ERROR_CODE = FreePanelToUnliftError
		else
			TILL RearClampDown_Bit & FrontClampDown_Bit, 5000
			if(RearClampDown_Bit & FrontClampDown_Bit)
				PanelFreed = 1
			else
				ERROR_CODE = FreePanelToUnclampError
			end
		end
RET

LowerLifter:
	Lifter_Lowered = 0
	ptp/v LIFTER_AXIS,0,10
	till ^MST(LIFTER_AXIS).#MOVE
	wait 200
	LockStopper_Bit = 0
	RaiseBoardStopStopper_Bit = 0
	Lifter_Lowered = 1

	!Simulation
	RearClampDown_Bit = 1
	FrontClampDown_Bit = 1
RET


UnclampPanel:
	ClampPanel_Bit = 0
RET