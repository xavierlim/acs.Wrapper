!ReloadPanelBuffer

ERROR_CODE = ERROR_SAFE

global int ReloadPanelStateError,ReloadPanelFreeError,ReloadPanelSearchError,ReloadPanelSlowSensorError,FreePanelToUnliftError,FreePanelToUnclampError

FreePanelToUnliftError = 303
FreePanelToUnclampError = 304

ReloadPanelStateError = 701
ReloadPanelFreeError = 702
ReloadPanelSearchError = 703
ReloadPanelSlowSensorError = 704


real SlowPosition
real absPosTemp
SlowPosition = 0
absPosTemp = 0



if CURRENT_STATUS = LOADED_STATUS								!IF CURRENT STATE = LOADED STATE
	CURRENT_STATUS = RELOADING_STATUS								!SET STATE = RELOADING STATUS
	START FreePanelBufferIndex,1									!START FREE PANEL BUFFER
	TILL ^ PST(FreePanelBufferIndex).#RUN							!UNTIL FREE PANEL COMPLETE

	if PanelFreed = 1												!IF PANEL IS FREED

	absPosTemp = RPOS(CONVEYOR_AXIS)
	SlowPosition = absPosTemp - (DistanceBetweenEntryAndStopSensor-DistanceBetweenSlowPositionAndEntrySensor-PanelLength)

		CALL ContinueReloading											!CALL CONTINUERELOADING

	else															!ELSE IF PANEL IS NOT FREED
		ERROR_CODE = ReloadPanelFreeError							!SET RELOAD PANEL FREE ERROR
		CALL ErrorExit														!CALL ERROR EXIT

	end

else																	!ELSE IF CURRENT STATE IS NOT = LOADED STATE

	if CURRENT_STATUS = PRERELEASED_STATUS	& ExitOpto_Bit = 1			!IF CURRENT STATUS = PRERELEASED STATUS AND EXIT OPTO BLOCKED
		CURRENT_STATUS = RELOADING_STATUS									!SET CURRENT STATUS = RELOADING STATUS

		absPosTemp = RPOS(CONVEYOR_AXIS)
		SlowPosition = absPosTemp - (DistanceBetweenEntrySensorAndExitSensor-DistanceBetweenSlowPositionAndEntrySensor-PanelLength )

		CALL ContinueReloading												!CALL CONTINUERELOADING 

	elseif 	CURRENT_STATUS = PRERELEASED_STATUS	& ExitOpto_Bit = 0		!ELSE IF CURRENT STATE IS PRELEASED STATUS AND EXIT OPTO NOT BLOCKED	
		CURRENT_STATUS = RELEASED_STATUS									!SET CURRENT_STATUS = RELEASED_STATUS TO PREPARE FOR LOADPANELBUFFER
		START LoadPanelBufferIndex,1										!CALL LOADPANELBUFFER
		TILL ^ PST(LoadPanelBufferIndex).#RUN								!UNTIL FREE PANEL COMPLETE

	elseif CURRENT_STATUS = RELEASED_STATUS	& EntryOpto_Bit = 1
		START LoadPanelBufferIndex,1
		TILL ^ PST(LoadPanelBufferIndex).#RUN

	else																!!!!!!!!!!!!!!!ELSE IF NONE OF THE CURRENT STATE CONDITIONS MET!!!!!!!!!!!!!!!!!		
		CALL LowerBoardStop												!CALL LOWERBOARDSTOPPER
		CALL ReloadPanel_FreePanelSeq									!START FREE PANEL BUFFER
		TILL PanelFreed = 1												!UNTIL FREE PANEL COMPLETE

		if PanelFreed = 1												!IF PANEL IS FREED
			
			absPosTemp = RPOS(CONVEYOR_AXIS)
			SlowPosition = absPosTemp - (DistanceBetweenEntryAndStopSensor-DistanceBetweenSlowPositionAndEntrySensor-PanelLength)
			if ExitOpto_Bit = 1
			SlowPosition = absPosTemp - (DistanceBetweenEntrySensorAndExitSensor-DistanceBetweenSlowPositionAndEntrySensor-PanelLength)
			end
			CALL ContinueReloading											!CALL CONTINUERELOADING

		end
	end

end


STOP

ContinueReloading:
	CALL StartConveyorBeltsUpstreamInternalSpeed				!START CONVEYOR BELT UPSTREAM

	TILL RPOS(CONVEYOR_AXIS) < SlowPosition , ReloadPanelBuffer_WaitTimeToSearch	!UNTIL sLOW POSITION REACHED OR TIMEOUT
	CALL AdjustConveyorBeltsUpstreamInternalSpeedToSlow								!ADJUST CONVEYOR BELT SPEED TO SLOW

	TILL EntryOpto_Bit = 1,ReloadPanelBuffer_WaitTimeToSearch						!UNTIL ENTRY OPTO BLOCKED OR TIMEOUT

	if EntryOpto_Bit = 1										!IF ENTRY OPTO BLOCKED
		CALL StopConveyorBelts										!STOP CONVEYOR BELT
		WAIT ReloadPanelBuffer_ReloadDelayTime										!WAIT UNTIL RELOAD DELAY TIME EXPIRES
		CALL RaiseBoardStop											!RAISE STOPPER THEN LOCK STOPPER
		CALL StartConveyorBeltsDownstreamInternalSpeed				!START CONVEYOR BELT DOWNSTREAM
		START InternalMachineLoadBufferIndex,1						!START INTERNAL MACHINE LOAD BUFFER
		TILL ^ PST(InternalMachineLoadBufferIndex).#RUN				!UNTIL INTERNAL MACHINE LOAD BUFFER END
	else														!ELSE IF ENTRY OPTO NOT BLOCKED
		CURRENT_STATUS = RELEASED_STATUS							!SET CURRENT_STATUS = RELEASED_STATUS TO PREPARE FOR LOADPANELBUFFER
		START LoadPanelBufferIndex,1								!CALL LOADPANELBUFFER
	end
RET


StartConveyorBeltsDownstreamInternalSpeed:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed*ConveyorDirection
RET

RaiseBoardStop:
	RaiseBoardStopStopper_Bit = 1
	TILL StopperArmUp_Bit = 1
	wait 1000
	LockStopper_Bit = 1
	TILL StopperLocked_Bit = 1
	
RET

LowerBoardStop:	
	LockStopper_Bit = 0
	TILL StopperUnlocked_Bit = 1
	RaiseBoardStopStopper_Bit = 0
	TILL StopperArmDown_Bit = 1
RET

StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET

StartConveyorBeltsUpstreamInternalSpeed:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,-ConveyorBeltLoadingSpeed*ConveyorDirection
RET

AdjustConveyorBeltsUpstreamInternalSpeedToSlow:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,-ConveyorBeltSlowSpeed*ConveyorDirection
RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

ReloadPanel_FreePanelSeq:
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
RET


UnclampPanel:
	ClampPanel_Bit = 0
RET