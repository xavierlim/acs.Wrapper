!SecurePanelBuffer

real Stage_1_LifterOnlyDistance
real Stage_2_LifterAndClamperDistance

ERROR_CODE = ERROR_SAFE

global int SecurePanelToClampedError,SecurePanelToLiftedError,SecurePanelToUnstopError


SecurePanelToClampedError = 801
SecurePanelToLiftedError = 802
SecurePanelToUnstopError = 803

int StageLifterResult
real absPosTemp

PanelSecured = 0

CALL TurnOffPanelSensingOptos
StageLifterResult = 0
CALL Stage_1_LifterOnly
if StageLifterResult = 1
	StageLifterResult = 0
	CALL Stage_2_LifterAndClamper
	if StageLifterResult = 1
		CALL LowerPanelStopper
		TILL (StopperUnlocked_Bit & StopperArmDown_Bit),SecurePanelBuffer_WaitTimeToUnstop
		if (StopperUnlocked_Bit & StopperArmDown_Bit)
			PanelSecured = 1
		else
			ERROR_CODE = SecurePanelToUnstopError
		end
	end
end
STOP

TurnOffPanelSensingOptos:
	StopSensor_Bit = 0
RET


Stage_1_LifterOnly:
	absPosTemp = RPOS(LIFTER_AXIS)+Stage_1_LifterOnlyDistance
	ptp (LIFTER_AXIS), absPosTemp	
	till ^AST(LIFTER_AXIS).#MOVE,SecurePanelBuffer_WaitTimeToLifted
	
	if (AST(LIFTER_AXIS).#MOVE)
		HALT LIFTER_AXIS
		ERROR_CODE = SecurePanelToLiftedError
		StageLifterResult = 0

		
	else
		StageLifterResult = 1
	end
RET


Stage_2_LifterAndClamper:
	CALL ClampPanel
	absPosTemp = RPOS(LIFTER_AXIS)+Stage_2_LifterAndClamperDistance
	ptp (LIFTER_AXIS), absPosTemp
	TILL (RearClampUp_Bit & FrontClampUp_Bit),SecurePanelBuffer_ClampLiftDelayTime
	if (RearClampUp_Bit & FrontClampUp_Bit)
		till ^AST(LIFTER_AXIS).#MOVE,SecurePanelBuffer_WaitTimeToLifted

		if (AST(LIFTER_AXIS).#MOVE)
			HALT LIFTER_AXIS
			ERROR_CODE = SecurePanelToLiftedError
			StageLifterResult = 0

		else
			StageLifterResult = 1
		end
	else
		HALT LIFTER_AXIS
		ERROR_CODE = SecurePanelToClampedError
		StageLifterResult = 0
	end

RET

ClampPanel:
	ClampPanel_Bit = 1
RET

LowerPanelStopper:
	LockStopper_Bit = 0
	RaiseBoardStopStopper_Bit = 0
RET