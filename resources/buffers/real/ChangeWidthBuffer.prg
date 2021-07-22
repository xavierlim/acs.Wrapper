!ChangeWidthBuffer

ERROR_CODE = ERROR_SAFE

global  int ChangeWidthToError,ChangeWidthToHomedError,ChangeWidthToNotAtSpecifiedError

ChangeWidthToError = 201
ChangeWidthToHomedError = 202
ChangeWidthToNotAtSpecifiedError = 203
ChangeWidthPanelPresent = 204


int ConveyorSpecifiedWidth

START FreePanelBufferIndex,1									!Perform Free Panel Buffer to lower lifter and Clamp
TILL ^PST(FreePanelBufferIndex).#RUN
TILL PanelFreed = 1

CALL PanelSearch												!Perform Panel Search

if ERROR_CODE <> ChangeWidthPanelPresent						!Only Execute change width buffer if no panel is found. else error is already reported in PanelSearch label

	if CURRENT_STATUS = RELEASED_STATUS
		if ConveyorWidthHomed = 1
			CURRENT_STATUS = CHANGING_WIDTH_STATUS
			CALL MoveConveyorToSpecifiedWidth
			if (^AST(CONVEYOR_WIDTH_AXIS).#MOVE) 
				CURRENT_STATUS = RELEASED_STATUS
			else
				ERROR_CODE = ChangeWidthToNotAtSpecifiedError
				CALL ErrorExit
			end
		else
			ERROR_CODE = ChangeWidthToHomedError
			CALL ErrorExit
		end
	else
		ERROR_CODE = ChangeWidthToError
		CALL ErrorExit
	end

end

STOP

PanelSearch:
	CALL StartConveyorBeltsDownstreamInternalSpeed																!START CONVEYOR BELT
	TILL (EntryOpto_Bit = 1 | ExitOpto_Bit = 1 | BoardStopPanelAlignSensor_Bit = 1) ,ChangeWidthBuffer_WaitTimeToSearch			!UNTIL ANY SENSORS BLOCKED OR TIMEOUT

	IF ^(EntryOpto_Bit = 1 | ExitOpto_Bit = 1 | BoardStopPanelAlignSensor_Bit = 1)								!IF SENSORS NOT BLOCKED AND TIMEOUT
		CURRENT_STATUS = RELEASED_STATUS																			!SET CURRENT STATUS RELEASED STATUS
		HALT CONVEYOR_AXIS																							!STOP CONVEYOR BELT
		TILL ^MST(CONVEYOR_AXIS).#MOVE																				!TILL CONVEYOR BELT STOP MOVING

	ELSE																										!ELSE IF SENSORS BLOCKED
		HALT CONVEYOR_AXIS																							!STOP CONVEYOR BELT
		TILL ^MST(CONVEYOR_AXIS).#MOVE																				!TILL CONVEYOR BELT STOP MOVING																						
		ERROR_CODE = ChangeWidthPanelPresent																		!SET ERROR CODE CHANGEWIDTHPANELPRESENT

	END 
RET

StartConveyorBeltsDownstreamInternalSpeed:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,ConveyorBeltAcquireSpeed*ConveyorDirection
RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

MoveConveyorToSpecifiedWidth:
	VEL (CONVEYOR_WIDTH_AXIS) = 80
	PTP/em CONVEYOR_WIDTH_AXIS, ConveyorSpecifiedWidth
	till ^MST(CONVEYOR_WIDTH_AXIS).#MOVE
	wait 200
RET