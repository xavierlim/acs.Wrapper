!FreePanelBuffer

ERROR_CODE = ERROR_SAFE

global int FreePanelStopUpError,FreePanelOptoBlockedError,FreePanelToUnliftError,FreePanelToUnclampError

FreePanelStopUpError = 301
FreePanelOptoBlockedError = 302
FreePanelToUnliftError = 303
FreePanelToUnclampError = 304


PanelFreed = 0

if StopperArmDown_Bit = 1
	CALL TurnOnPanelSensingOptos	
	if (EntryOpto_Bit = 0 & ExitOpto_Bit = 0)
		CALL UnclampPanel
		WAIT FreePanelBuffer_UnclampLiftDelayTime
		CALL LowerLifter 
		TILL Lifter_Lowered = 1,FreePanelBuffer_WaitTimeToUnlift
		if Lifter_Lowered <> 1
			ERROR_CODE = FreePanelToUnliftError
		else
			TILL RearClampDown_Bit & FrontClampDown_Bit,FreePanelBuffer_WaitTimeToUnclamp
			if(RearClampDown_Bit & FrontClampDown_Bit)
				PanelFreed = 1
			else
				ERROR_CODE = FreePanelToUnclampError
			end
		end
	else
		ERROR_CODE = FreePanelOptoBlockedError
	end
else
	ERROR_CODE = FreePanelStopUpError
end

STOP


TurnOnPanelSensingOptos:
	StopSensor_Bit = 1
RET

LowerLifter:
	Lifter_Lowered = 0
	ptp/v LIFTER_AXIS,0,50
	till ^MST(LIFTER_AXIS).#MOVE
	wait 200
	Lifter_Lowered = 1
RET


UnclampPanel:
	ClampPanel_Bit = 0
RET