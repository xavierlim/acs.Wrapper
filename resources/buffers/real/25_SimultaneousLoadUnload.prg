!! Buffer for simultaneous load and unload

global int ReleasePanelStateError,ReleasePanelFreeError,ReleasePanelExitError,ReleasePanelReleaseError,ReleasePanelSmemaError

ReleasePanelStateError = 601
ReleasePanelFreeError = 602
ReleasePanelExitError = 603
ReleasePanelReleaseError = 604
ReleasePanelSmemaError = 605

TILL ExitOpto_Bit = 0,ReleasePanelBuffer_WaitTimeToRelease
		if ExitOpto_Bit = 0
				CALL ClearDownstreamSmemaBoardAvailable
				TILL DownstreamMachineReadySignal_Bit = 0,ReleasePanelBuffer_WaitTimeToSmema
				if DownstreamMachineReadySignal_Bit = 1
					ERROR_CODE = ReleasePanelSmemaError
					CALL ErrorExit
				end
		else if ExitOpto_Bit = 1
			ERROR_CODE = ReleasePanelReleaseError
			CALL ErrorExit
		end
		end
		
STOP

ClearDownstreamSmemaBoardAvailable:
DownStreamBoardAvailable_Bit = 0
SmemaDownStreamFailedBoardAvailable_Bit = 0
	
	if (SmemaFailedBoardMode = SmemaFailedBoardModeNormal)
    SmemaDownStreamFailedBoardAvailable_Bit = 0

	elseif (SmemaFailedBoardMode = SmemaFailedBoardModeCustom)
    SmemaDownStreamFailedBoardAvailable_Bit = 0
	CustomerDO1Signal_Bit = 0

	elseif (SmemaFailedBoardMode = SmemaFailedBoardModeNotifyUpstream)
    ! ignore

	elseif (SmemaFailedBoardMode = SmemaFailedBoardModeInverseLogic)
    ! trigger failed board output to downstream according to FailedBoard flag, inverted
	! SmemaDownStreamFailedBoardAvailable_Bit to be inverted
    SmemaDownStreamFailedBoardAvailable_Bit = 1
	CustomerDO1Signal_Bit = 0
	end

RET
RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET
