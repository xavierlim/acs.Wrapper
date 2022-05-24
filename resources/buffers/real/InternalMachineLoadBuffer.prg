!InternalMachineLoadBuffer

ERROR_CODE = ERROR_SAFE

global int LoadPanelAlignBeforeSlowSensorError,LoadPanelSecureError,LoadPanelAlignError,LoadPanelSlowSensorError

LoadPanelSlowSensorError = 404
LoadPanelAlignBeforeSlowSensorError = 405
LoadPanelAlignError = 406
LoadPanelSecureError = 407


real SlowPosition
real absPosTemp
SlowPosition = 0
absPosTemp = 0

TILL EntryOpto_Bit = 0
if EntryOpto_Bit = 0
	absPosTemp = RPOS(CONVEYOR_AXIS)
END

if Panel_Count = 1
SlowPosition = DistanceBetweenEntryAndStopSensor-DistanceBetweenSlowPositionAndStopSensor-PanelLength + absPosTemp
end

if Panel_Count = 0
SlowPosition = DistanceBetweenEntryAndStopSensor-DistanceBetweenSlowPositionAndStopSensor-PanelLength + absPosTemp - 20
end

TILL RPOS(CONVEYOR_AXIS) > SlowPosition

IF BoardStopPanelAlignSensor_Bit = 1								
	ERROR_CODE = LoadPanelAlignBeforeSlowSensorError			
END

CALL AdjustConveyorBeltSpeedToSlow
!edit by issac for troubleshooting									
!TILL BoardStopPanelAlignSensor_Bit
TILL BoardStopPanelAlignSensor_Bit,InternalMachineLoadBuffer_WaitTimeToAlign					
if BoardStopPanelAlignSensor_Bit = 1								
	WAIT InternalMachineLoadBuffer_WaitTimeToAlign
	CALL TurnOffConveyorBeltMotor										
	START SecurePanelBufferIndex,1										
	TILL ^ PST(SecurePanelBufferIndex).#RUN								
	if PanelSecured = 1													
		CURRENT_STATUS = LOADED_STATUS										
	else																
		ERROR_CODE = LoadPanelSecureError							
		CALL ErrorExit														
	end
else																
	ERROR_CODE = LoadPanelAlignError							
	CALL ErrorExit														
end

STOP



ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

AdjustConveyorBeltSpeedToSlow:
	ACC (CONVEYOR_AXIS) = 10000
	DEC (CONVEYOR_AXIS) = 16000
	JOG/v CONVEYOR_AXIS,ConveyorBeltSlowSpeed
RET

TurnOffConveyorBeltMotor:
	HALT CONVEYOR_AXIS
RET