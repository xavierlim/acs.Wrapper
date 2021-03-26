#/ Controller version = 3.01
#/ Date = 01-Feb-21 9:50 AM
#/ User remarks = 
#5
!HOMING BUFFER FOR X


int AXIS
int NEED_FIND_INDEX

FDEF(AXIS).#LL=0       ! Disable the axis left limit default response
FDEF(AXIS).#RL=0       ! Disable the axis left limit default response
FDEF(AXIS).#SLL=0
FDEF(AXIS).#SRL=0




HALT(AXIS)
MFLAGS(AXIS).#DEFCON = 1 !just in case
MFLAGS(AXIS).#HOME = 0



!the motion is 
!	1) in fast 
!	2) out fast
!	3) in slow
!	4) out slow

! 1) in fast
JOG/v(AXIS), -HOME_VEL_IN(AXIS) !Move to the left limit switch
TILL FAULT(0).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 2) out fast
JOG/v(AXIS), HOME_VEL_IN(AXIS) !Move to the left limit switch
TILL ^FAULT(0).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 3) in slow
JOG/v(AXIS), -HOME_VEL_OUT(AXIS) !Move to the left limit switch
TILL FAULT(0).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 4) out slow
JOG/v(AXIS),HOME_VEL_OUT(AXIS) !Move out of the left limit   
if(NEED_FIND_INDEX = 1)
	IST(AXIS).#IND=0       
	TILL IST(AXIS).#IND    
else
	TILL ^FAULT(0).#SLL  !Wait for the left limit release
end

KILL(AXIS)
TILL ^AST(AXIS).#MOVE

SET APOS(AXIS) = HOME_OFFSET(AXIS)

MFLAGS(AXIS).#HOME = 1

STOP

RESTORE_SETTINGS:
	FDEF(AXIS).#LL=1       ! enable the axis left limit default response
	FDEF(AXIS).#RL=1
STOP

on ^PST(5).#RUN
	call RESTORE_SETTINGS
ret

on MFLAGS(AXIS).#HOME
	block; if MFLAGS(AXIS).#HOME
		FDEF(AXIS).#SLL=1
		FDEF(AXIS).#SRL=1	
	end; end
ret

on ^MFLAGS(AXIS).#HOME
	block; if ^MFLAGS(AXIS).#HOME
		FDEF(AXIS).#SLL=0
		FDEF(AXIS).#SRL=0	
	end; end
ret
#6
!HOMING BUFFER FOR Y


int AXIS
int NEED_FIND_INDEX

FDEF(AXIS).#LL=0       ! Disable the axis left limit default response
FDEF(AXIS).#RL=0       ! Disable the axis left limit default response
FDEF(AXIS).#SLL=0
FDEF(AXIS).#SRL=0




HALT(AXIS)
MFLAGS(AXIS).#DEFCON = 1 !just in case
MFLAGS(AXIS).#HOME = 0



!the motion is 
!	1) in fast 
!	2) out fast
!	3) in slow
!	4) out slow

! 1) in fast
JOG/v(AXIS), -HOME_VEL_IN(AXIS) !Move to the left limit switch
TILL FAULT(1).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 2) out fast
JOG/v(AXIS), HOME_VEL_IN(AXIS) !Move to the left limit switch
TILL ^FAULT(1).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 3) in slow
JOG/v(AXIS), -HOME_VEL_OUT(AXIS) !Move to the left limit switch
TILL FAULT(1).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 4) out slow
JOG/v(AXIS),HOME_VEL_OUT(AXIS) !Move out of the left limit   
if(NEED_FIND_INDEX = 1)
	IST(AXIS).#IND=0       
	TILL IST(AXIS).#IND    
else
	TILL ^FAULT(1).#SLL  !Wait for the left limit release
end

KILL(AXIS)
TILL ^AST(AXIS).#MOVE

SET APOS(AXIS) = HOME_OFFSET(AXIS)

MFLAGS(AXIS).#HOME = 1

STOP

RESTORE_SETTINGS:
	FDEF(AXIS).#LL=1       ! enable the axis left limit default response
	FDEF(AXIS).#RL=1
STOP

on ^PST(6).#RUN
	call RESTORE_SETTINGS
ret

on MFLAGS(AXIS).#HOME
	block; if MFLAGS(AXIS).#HOME
		FDEF(AXIS).#SLL=1
		FDEF(AXIS).#SRL=1	
	end; end
ret

on ^MFLAGS(AXIS).#HOME
	block; if ^MFLAGS(AXIS).#HOME
		FDEF(AXIS).#SLL=0
		FDEF(AXIS).#SRL=0	
	end; end
ret
#7
!HOMING BUFFER FOR Z


int AXIS
int NEED_FIND_INDEX

FDEF(AXIS).#LL=0       ! Disable the axis left limit default response
FDEF(AXIS).#RL=0       ! Disable the axis left limit default response
FDEF(AXIS).#SLL=0
FDEF(AXIS).#SRL=0




HALT(AXIS)
MFLAGS(AXIS).#DEFCON = 1 !just in case
MFLAGS(AXIS).#HOME = 0



!the motion is 
!	1) in fast 
!	2) out fast
!	3) in slow
!	4) out slow

! 1) in fast
JOG/v(AXIS), -HOME_VEL_IN(AXIS) !Move to the left limit switch
TILL FAULT(4).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 2) out fast
JOG/v(AXIS), HOME_VEL_IN(AXIS) !Move to the left limit switch
TILL ^FAULT(4).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 3) in slow
JOG/v(AXIS), -HOME_VEL_OUT(AXIS) !Move to the left limit switch
TILL FAULT(4).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 4) out slow
JOG/v(AXIS),HOME_VEL_OUT(AXIS) !Move out of the left limit   
if(NEED_FIND_INDEX = 1)
	IST(AXIS).#IND=0       
	TILL IST(AXIS).#IND    
else
	TILL ^FAULT(4).#SLL  !Wait for the left limit release
end

KILL(AXIS)
TILL ^AST(AXIS).#MOVE

SET APOS(AXIS) = HOME_OFFSET(AXIS)

MFLAGS(AXIS).#HOME = 1

STOP

RESTORE_SETTINGS:
	FDEF(AXIS).#LL=1       ! enable the axis left limit default response
	FDEF(AXIS).#RL=1
STOP

on ^PST(7).#RUN
	call RESTORE_SETTINGS
ret

on MFLAGS(AXIS).#HOME
	block; if MFLAGS(AXIS).#HOME
		FDEF(AXIS).#SLL=1
		FDEF(AXIS).#SRL=1	
	end; end
ret

on ^MFLAGS(AXIS).#HOME
	block; if ^MFLAGS(AXIS).#HOME
		FDEF(AXIS).#SLL=0
		FDEF(AXIS).#SRL=0	
	end; end
ret
#12
!BypassModeBuffer

int BypassModeError
BypassModeError = 0

int BypassSensorBlockedError,BypassAcqError,BypassExitError,BypassReleaseError,BypassSmemaError

BypassSensorBlockedError = 1
BypassAcqError = 2
BypassExitError = 3
BypassReleaseError = 4
BypassSmemaError = 5

int WaitTimeToSearch
int WaitTimeToAcq
int WaitTimeToCutout
int WaitTimeToExit
int WaitTimeToRelease
int WaitTimeToSmema

if 	(IN(EntryOpto_Port).EntryOpto_Bit = 1 & IN(ExitOpto_Port).ExitOpto_Bit = 1 & IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit = 1)
	BypassModeError = BypassSensorBlockedError
	CALL TurnOnAllLightAndSoundTheHom
	CALL ErrorExit	
else
	CURRENT_STATUS = BYPASS_STATUS
	if IN(ExitOpto_Port).ExitOpto_Bit = 1
		CALL SetDownstreamSmemaBoardAvailable
		TILL IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 1
		CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease
	else
		CALL StartConveyorBeltsDownstreamInternalSpeed
		TILL (IN(EntryOpto_Port).EntryOpto_Bit = 0 & IN(ExitOpto_Port).ExitOpto_Bit = 0 & IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit = 0),WaitTimeToSearch
		if (IN(EntryOpto_Port).EntryOpto_Bit = 1 & IN(ExitOpto_Port).ExitOpto_Bit = 1 & IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit = 1) 
			CALL SendPanel
		else
			CALL GetPanel
		end
	end
end

STOP


GetPanel:
	CALL SetUpstreamSmemaMachineReady
	TILL IN(UpstreamBoardAvailableSignal_Port).UpstreamBoardAvailableSignal_Bit = 1
	CALL StartConveyorBeltsDownstreamAndS_Acq
	TILL IN(EntryOpto_Port).EntryOpto_Bit = 1,WaitTimeToAcq
	if IN(EntryOpto_Port).EntryOpto_Bit = 1
RET1:	TILL IN(EntryOpto_Port).EntryOpto_Bit = 0
		TILL IN(EntryOpto_Port).EntryOpto_Bit = 1,WaitTimeToCutout
		if IN(EntryOpto_Port).EntryOpto_Bit = 1
			GOTO RET1
		else
			CALL ClearUpstreamSmemaMachineReady
		end
	else
		BypassModeError = BypassAcqError
		CALL ErrorExit	
		CALL TurnOnAllLightAndSoundTheHom
		CALL AdjustConveyorBeltSpeedToInternalSpeed
		CALL SendPanel
	end
RET


StartConveyorBeltsDownstreamAndS_Acq:
	JOG/v CONVEYOR_AXIS,ConveyorBeltAcquireSpeed
RET
AdjustConveyorBeltSpeedToInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed
RET
ClearUpstreamSmemaMachineReady:
	OUT(SmemaUpStreamMachineReady_Port).SmemaUpStreamMachineReady_Bit = 0
RET

SetUpstreamSmemaMachineReady:
	OUT(SmemaUpStreamMachineReady_Port).SmemaUpStreamMachineReady_Bit = 1
RET

SendPanel:
	CALL SetDownstreamSmemaBoardAvailable
	TILL IN(ExitOpto_Port).ExitOpto_Bit = 1,WaitTimeToExit
	if IN(ExitOpto_Port).ExitOpto_Bit = 1
		if IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 1
			CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease
		else
			CALL StopConveyorBelts
			TILL IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 1
			CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease
		end
	else
		BypassModeError = BypassExitError
		CALL ErrorExit	
		CALL TurnOnAllLightAndSoundTheHom
	end
RET

StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET
StartConveyorBeltsDownstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed
RET

ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease:
		CALL SetConveyorBeltsDownstreamSpeedToRelease
RET2:	TILL IN(ExitOpto_Port).ExitOpto_Bit = 0,WaitTimeToRelease
		if IN(ExitOpto_Port).ExitOpto_Bit = 0
			TILL IN(ExitOpto_Port).ExitOpto_Bit = 1,WaitTimeToCutout
			if IN(ExitOpto_Port).ExitOpto_Bit = 1
				GOTO RET2
			else
				CALL ClearDownstreamSmemaBoardAvailable
				TILL IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 0,WaitTimeToSmema
				if (IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 1)
					BypassModeError = BypassSmemaError
					CALL ErrorExit	
					CALL TurnOnAllLightAndSoundTheHom
				else
					CALL TurnOffConveyorBelts
					CALL GetPanel
				end
			end


		else
			BypassModeError = BypassReleaseError
			CALL ErrorExit	
			CALL TurnOnAllLightAndSoundTheHom
		end
RET

TurnOffConveyorBelts:
	HALT CONVEYOR_AXIS
RET
ClearDownstreamSmemaBoardAvailable:
	OUT(DownStreamBoardAvailable_Port).DownStreamBoardAvailable_Bit = 0
RET
SetConveyorBeltsDownstreamSpeedToRelease:
	JOG/v CONVEYOR_AXIS,ConveyorBeltReleaseSpeed
RET

SetDownstreamSmemaBoardAvailable:
	OUT(DownStreamBoardAvailable_Port).DownStreamBoardAvailable_Bit = 1
RET
TurnOnAllLightAndSoundTheHom:

	OUT(TowerLightRed_Port).TowerLightRed_Bit = 1
	OUT(TowerLightYellow_Port).TowerLightYellow_Bit = 1
	OUT(TowerLightGreen_Port).TowerLightGreen_Bit = 1
	OUT(TowerLightBlue_Port).TowerLightBlue_Bit = 1
	OUT(TowerLightBuzzer_Port).TowerLightBuzzer_Bit = 1
 
RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET


#13
!ChangeWidthBuffer

global int ChangeWidthStateError
ChangeWidthStateError = 0

global  int ChangeWidthToError,ChangeWidthToHomedError,ChangeWidthToNotAtSpecifiedError



ChangeWidthToError=1
ChangeWidthToHomedError=2
ChangeWidthToNotAtSpecifiedError=3

int ConveyorSpecifiedWidth

if CURRENT_STATUS = RELEASED_STATUS
	if ConveyorWidthHomed = 1
		CURRENT_STATUS = CHANGING_WIDTH_STATUS
		CALL MoveConveyorToSpecifiedWidth
		if ConveyorSpecifiedWidth = RPOS(CONVEYOR_WIDTH_AXIS)
			CURRENT_STATUS = RELEASED_STATUS
		else
			ChangeWidthStateError = ChangeWidthToNotAtSpecifiedError
			CALL ErrorExit
		end
	else
		ChangeWidthStateError = ChangeWidthToHomedError
		CALL ErrorExit
	end
else
	ChangeWidthStateError = ChangeWidthToError
	CALL ErrorExit
end

STOP

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

MoveConveyorToSpecifiedWidth:
	PTP/em CONVEYOR_WIDTH_AXIS, ConveyorSpecifiedWidth
RET


#14
!EmergencyStopBuffer

AUTOEXEC:

CALL EnableOptos	
CALL LowerLifter	
CALL Unclamp	
CALL LowerStopper	
CALL ClearBoardAvailable	
CALL ClearMachineReady	
CURRENT_STATUS = SAFE_STATUS

STOP

EnableOptos:
	OUT(StopSensor_Port).StopSensor_Bit = 1
RET

LowerLifter:
	JOG LIFTER_AXIS,-
	TILL IN(LifterLowered_Port).LifterLowered_Bit = 1
	HALT LIFTER_AXIS
RET

Unclamp:
	OUT(ClampPanel_Port).ClampPanel_Bit = 0
RET

LowerStopper:
	OUT(LockStopper_Port).LockStopper_Bit = 0
	OUT(RaiseBoardStopStopper_Port).RaiseBoardStopStopper_Bit = 0
RET

ClearBoardAvailable:
	OUT(DownStreamBoardAvailable_Port).DownStreamBoardAvailable_Bit = 0
RET

ClearMachineReady:
	OUT(SmemaUpStreamMachineReady_Port).SmemaUpStreamMachineReady_Bit = 0
RET

#15
!FreePanelBuffer

global int FreePanelStateError
FreePanelStateError = 0

global int FreePanelStopUpError,FreePanelOptoBlockedError,FreePanelToUnliftError,FreePanelToUnclampError

FreePanelStopUpError=1
FreePanelOptoBlockedError=2
FreePanelToUnliftError=3
FreePanelToUnclampError=4

int UnclampLiftDelayTime
int WaitTimeToUnlift
int WaitTimeToUnclamp

PanelFreed = -1

if IN(StopperArmDown_Port).StopperArmDown_Bit = 1
	CALL TurnOnPanelSensingOptos	
	if (IN(EntryOpto_Port).EntryOpto_Bit = 0 & IN(ExitOpto_Port).ExitOpto_Bit = 0)
		CALL LowerLifter
		WAIT UnclampLiftDelayTime
		CALL UnclampPanel
		TILL IN(LifterLowered_Port).LifterLowered_Bit = 1,WaitTimeToUnlift
		if IN(LifterLowered_Port).LifterLowered_Bit <> 1
			FreePanelStateError = FreePanelToUnliftError
		else
			TILL (IN(RearClampDown_Port).RearClampDown_Bit & IN(FrontClampDown_Port).FrontClampDown_Bit),WaitTimeToUnclamp
			if(IN(RearClampDown_Port).RearClampDown_Bit & IN(FrontClampDown_Port).FrontClampDown_Bit)
				PanelFreed = 1
			else
				FreePanelStateError = FreePanelToUnclampError
			end
		end
	else
		FreePanelStateError = FreePanelOptoBlockedError
	end
else
	FreePanelStateError = FreePanelStopUpError
end

STOP


TurnOnPanelSensingOptos:
	OUT(StopSensor_Port).StopSensor_Bit = 1
RET

LowerLifter:
	JOG LIFTER_AXIS,-
	TILL IN(LifterLowered_Port).LifterLowered_Bit = 1
	HALT LIFTER_AXIS
RET


UnclampPanel:
	OUT(ClampPanel_Port).ClampPanel_Bit = 0
RET


#16
!InternalMachineLoadBuffer

global int LoadPanelStateError
LoadPanelStateError = 0

global int LoadPanelAlignBeforeSlowSensorError,LoadPanelSecureError,LoadPanelAlignError,LoadPanelSlowSensorError
LoadPanelSlowSensorError = 4
LoadPanelAlignBeforeSlowSensorError = 5
LoadPanelAlignError = 6
LoadPanelSecureError = 7

int WaitTimeToSlow
int WaitTimeToAlign
int SlowDelayTime

real SlowPosition


TILL RPOS(CONVEYOR_AXIS) < SlowPosition
CALL AdjustConveyorBeltSpeedToSlow
TILL IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit,WaitTimeToAlign
if IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit = 1
	CALL TurnOffConveyorBeltMotor
	START SecurePanelBufferIndex,1
	TILL ^ PST(SecurePanelBufferIndex).#RUN
	if PanelSecured = 1
		CURRENT_STATUS = LOADED_STATUS
	else
		LoadPanelStateError = LoadPanelSecureError
		CALL ErrorExit
	end
else
	LoadPanelStateError = LoadPanelAlignError
	CALL ErrorExit
end

STOP



ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

AdjustConveyorBeltSpeedToSlow:
	JOG/v CONVEYOR_AXIS,ConveyorBeltSlowSpeed
RET

TurnOffConveyorBeltMotor:
	DISABLE CONVEYOR_AXIS
RET


#17
!LoadPanelBuffer

global int LoadPanelStateError
LoadPanelStateError = 0

global int LoadPanelNotReleasedError,LoadPanelSensorBlockedError,LoadPanelAcqError,LoadPanelSlowSensorError

LoadPanelNotReleasedError = 1
LoadPanelSensorBlockedError = 2
LoadPanelAcqError = 3
LoadPanelSlowSensorError = 4

int WaitTimeToAcq

if CURRENT_STATUS = RELEASED_STATUS
	if (IN(EntryOpto_Port).EntryOpto_Bit = 0 & IN(ExitOpto_Port).ExitOpto_Bit = 0 & IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit = 0)
		CURRENT_STATUS = LOADING_STATUS
		CALL UpstreamSmemaMachineReady
		TILL IN(UpstreamBoardAvailableSignal_Port).UpstreamBoardAvailableSignal_Bit = 1
		CALL RaiseBoardStop
		CALL StartConveyorBeltsDownstream
		TILL IN(EntryOpto_Port).EntryOpto_Bit = 1,WaitTimeToAcq
		if IN(EntryOpto_Port).EntryOpto_Bit = 1
IfSlowDown:	TILL IN(EntryOpto_Port).EntryOpto_Bit = 0,WaitTimeToAcq
				if IN(EntryOpto_Port).EntryOpto_Bit = 0 !Unblocked
				CALL ClearUpstreamSmemaMachineReady
				CALL AdjustConveyorBeltSpeedToInternalSpeed
				START InternalMachineLoadBufferIndex,1
				TILL ^ PST(InternalMachineLoadBufferIndex).#RUN
			else
			   GOTO IfSlowDown
			end
		else
			LoadPanelStateError = LoadPanelAcqError
			CALL ErrorExit
		end
	else
		LoadPanelStateError = LoadPanelSensorBlockedError
		CALL ErrorExit
	end
else
	LoadPanelStateError = LoadPanelNotReleasedError
	CALL ErrorExit
end

STOP

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

UpstreamSmemaMachineReady:
	OUT(SmemaUpStreamMachineReady_Port).SmemaUpStreamMachineReady_Bit = 1
RET

ClearUpstreamSmemaMachineReady:
	OUT(SmemaUpStreamMachineReady_Port).SmemaUpStreamMachineReady_Bit = 0
RET


RaiseBoardStop:
	OUT(RaiseBoardStopStopper_Port).RaiseBoardStopStopper_Bit = 1
RET

StartConveyorBeltsDownstream:
	JOG/v CONVEYOR_AXIS,ConveyorBeltSlowSpeed
RET

AdjustConveyorBeltSpeedToInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed
RET


#18
!PowerOnRecoverFromEmergencyStopBuffer


int WaitTimeToSearch
int WaitTimeToExit

int WidthToW_0_Position

CALL InitializeACS
CALL InitializeMotors

CALL EnableOptos
CALL ErrorExit

if IN(EstopAndDoorOpenFeedback_Port).EstopAndDoorOpenFeedback_Bit = 1
	CURRENT_STATUS = SAFE_STATUS
else
	EMO_RECOVERY:
	if IN(BypassNormal_Port).BypassNormal_Bit = 1
		START BypassModeBufferIndex,1
	else
		CURRENT_STATUS = SEARGING_STATUS
		if (IN(EntryOpto_Port).EntryOpto_Bit = 1 & IN(ExitOpto_Port).ExitOpto_Bit = 1 & IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit = 1)
			CALL ContinueFindPanel	
		else
			CALL StartConveyorBeltsDownstream
			TILL (IN(EntryOpto_Port).EntryOpto_Bit = 1 & IN(ExitOpto_Port).ExitOpto_Bit = 1 & IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit = 1) ,WaitTimeToSearch
			if (IN(EntryOpto_Port).EntryOpto_Bit = 1 & IN(ExitOpto_Port).ExitOpto_Bit = 1 & IN(BoardStopPanelAlignSensor_Port).BoardStopPanelAlignSensor_Bit = 1)
				CALL ContinueFindPanel	
			else
				CALL StopConveyorBelts
				CALL HomeWidth
			end
		end
	end
end

STOP


ContinueFindPanel:
	if ConveyorWidthHomed = 1
	    TILL IN(ExitOpto_Port).ExitOpto_Bit = 1,WaitTimeToExit
		if IN(ExitOpto_Port).ExitOpto_Bit = 1
			CALL StopConveyorBelts
			CURRENT_STATUS = PRERELEASED_STATUS
		else
			CALL ErrorExit
			CURRENT_STATUS = ERROR_STATUS
		end
	else
		CALL ErrorExit
		CURRENT_STATUS = ERROR_STATUS
	end

RET

HomeWidth:

	if ConveyorWidthHomed = 1
		CURRENT_STATUS = RELEASED_STATUS
	else
		CURRENT_STATUS = WIDTH_HOMING_STATUS
		CALL HomeConveyorWidthMotor
		if ConveyorWidthHomed = 1
			CALL AdjustConveyorWidthToW_0
			CURRENT_STATUS = RELEASED_STATUS
		else
			CALL ErrorExit
			CURRENT_STATUS = ERROR_STATUS
		end
	end
RET

HomeConveyorWidthMotor:
	START HomeConveyorBufferIndex,1
	TILL ^ PST(HomeConveyorBufferIndex).#RUN
RET

AdjustConveyorWidthToW_0:
	PTP/em CONVEYOR_WIDTH_AXIS, WidthToW_0_Position
RET



ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

InitializeACS:
	ENABLE ALL
RET

InitializeMotors:

RET

EnableOptos:
	OUT(StopSensor_Port).StopSensor_Bit = 1
RET

StartConveyorBeltsDownstream:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed
RET

StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET





#19
!PreReleasePanelBuffer

global int PreReleaseStateError
PreReleaseStateError = 0

int ReleaseCommandReceived
int WaitTimeToExit


if CURRENT_STATUS = LOADED_STATUS
	CURRENT_STATUS = PRERELEASING_STATUS
	PanelFreed = 0
	START FreePanelBufferIndex,1
	TILL ^ PST(FreePanelBufferIndex).#RUN

	if PanelFreed = 0
		PreReleaseStateError = 2
		CALL ErrorExit	
		CURRENT_STATUS = ERROR_STATUS
	else
	    ReleaseCommandReceived = -1
		CALL StartConveyorBeltsDownstream
		TILL ReleaseCommandReceived <> -1,WaitTimeToExit
		if ReleaseCommandReceived <> 1
			if IN(ExitOpto_Port).ExitOpto_Bit = 1
				CALL TurnOffConveyor
				CURRENT_STATUS = PRERELEASED_STATUS
			else
				PreReleaseStateError = 3
				CALL ErrorExit	
				CURRENT_STATUS = ERROR_STATUS
			end
		end
	end

else
	PreReleaseStateError = 1
	CALL ErrorExit	
	CURRENT_STATUS = ERROR_STATUS
end

STOP

StartConveyorBeltsDownstream:
	JOG/v CONVEYOR_AXIS,ConveyorBeltReleaseSpeed
RET

TurnOffConveyor:
	HALT CONVEYOR_AXIS
RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

#20
!ReleasePanelBuffer

global int ReleasePanelError
ReleasePanelError = 0

global int ReleasePanelStateError,ReleasePanelFreeError,ReleasePanelExitError,ReleasePanelReleaseError,ReleasePanelSmemaError

ReleasePanelStateError = 1
ReleasePanelFreeError = 2
ReleasePanelExitError = 3
ReleasePanelReleaseError = 4
ReleasePanelSmemaError = 5

int WaitTimeToExit
int WaitTimeToRelease
int WaitTimeToSmema
int WaitTimeToCutout

if CURRENT_STATUS = PRERELEASED_STATUS
	CURRENT_STATUS = RELEASING_STATUS
	CALL SetDownstreamBoardAvailable
	TILL IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 1
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
				ReleasePanelError = ReleasePanelFreeError
				CALL ErrorExit	
			end
		else
			ReleasePanelError = ReleasePanelStateError
			CALL ErrorExit	
		end
	end
end

STOP

SetDownstreamBoardAvailable:
	OUT(DownStreamBoardAvailable_Port).DownStreamBoardAvailable_Bit = 1
RET

ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease:
		CALL SetConveyorBeltsDownstreamSpeedToRelease
RET3:	TILL IN(ExitOpto_Port).ExitOpto_Bit = 0,WaitTimeToRelease
		if IN(ExitOpto_Port).ExitOpto_Bit = 0
			TILL IN(ExitOpto_Port).ExitOpto_Bit = 1,WaitTimeToCutout
			if IN(ExitOpto_Port).ExitOpto_Bit = 1
				GOTO RET3
			else
				CALL ClearDownstreamSmemaBoardAvailable
				TILL IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 0,WaitTimeToSmema
				if IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 1
					ReleasePanelError = ReleasePanelSmemaError
					CALL ErrorExit	
				else
					CALL TurnOffConveyorBelts
					CURRENT_STATUS = RELEASED_STATUS
				end
			end
		else
			ReleasePanelError = ReleasePanelReleaseError
			CALL ErrorExit	
		end
RET

TurnOffConveyorBelts:
	HALT CONVEYOR_AXIS
RET

ClearDownstreamSmemaBoardAvailable:
	OUT(DownStreamBoardAvailable_Port).DownStreamBoardAvailable_Bit = 0
RET

SetConveyorBeltsDownstreamSpeedToRelease:
	JOG/v CONVEYOR_AXIS,ConveyorBeltReleaseSpeed
RET

ContinueFrom_SetDownstreamSmemaBoardAvailable:
	CALL SetDownstreamSmemaBoardAvailable
	TILL IN(ExitOpto_Port).ExitOpto_Bit = 1,WaitTimeToExit
	if IN(ExitOpto_Port).ExitOpto_Bit = 1
		if IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 1
			CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease
		else
			CALL StopConveyorBelts
			TILL IN(DownstreamMachineReadySignal_Port).DownstreamMachineReadySignal_Bit = 1
			CALL ContinueFrom_SetConveyorBeltsDownstreamSpeedToRelease
		end
	else
		ReleasePanelError = ReleasePanelExitError
		CALL ErrorExit	
	end
RET


StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET

SetDownstreamSmemaBoardAvailable:
	OUT(DownStreamBoardAvailable_Port).DownStreamBoardAvailable_Bit = 1
RET


StartConveyorBeltsDownstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltReleaseSpeed
RET

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET


#21
!ReloadPanelBuffer

global int ReloadPanelError
ReloadPanelError = 0

global int ReloadPanelStateError,ReloadPanelFreeError,ReloadPanelSearchError,ReloadPanelSlowSensorError

ReloadPanelStateError = 1
ReloadPanelFreeError = 2
ReloadPanelSearchError = 3
ReloadPanelSlowSensorError = 4

int WaitTimeToSearch
int ReloadDelayTime

if CURRENT_STATUS = LOADED_STATUS
	CURRENT_STATUS = RELOADING_STATUS
	START FreePanelBufferIndex,1
	TILL ^ PST(FreePanelBufferIndex).#RUN
	if PanelFreed = 1
		CALL ContinueReloading
	else
		ReloadPanelError = ReloadPanelFreeError
		CALL ErrorExit	
	end
else
	if CURRENT_STATUS = PRERELEASED_STATUS
		CURRENT_STATUS = RELOADING_STATUS
		CALL ContinueReloading
	else
		ReloadPanelError = ReloadPanelStateError
		CALL ErrorExit	
	end

end


STOP

ContinueReloading:
	CALL StartConveyorBeltsUpstreamInternalSpeed
	TILL IN(EntryOpto_Port).EntryOpto_Bit = 1,WaitTimeToSearch
	if IN(EntryOpto_Port).EntryOpto_Bit = 1
		CALL StopConveyorBelts
		WAIT ReloadDelayTime
		CALL RaiseBoardStop
		CALL StartConveyorBeltsDownstreamInternalSpeed
		START InternalMachineLoadBufferIndex,1
		TILL ^ PST(InternalMachineLoadBufferIndex).#RUN
	else
		ReloadPanelError = ReloadPanelSearchError
		CALL ErrorExit	
	end
RET


StartConveyorBeltsDownstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,ConveyorBeltLoadingSpeed
RET

RaiseBoardStop:
	OUT(LockStopper_Port).LockStopper_Port = 1
	OUT(RaiseBoardStopStopper_Port).RaiseBoardStopStopper_Bit = 1
RET

StopConveyorBelts:
	HALT CONVEYOR_AXIS
RET

StartConveyorBeltsUpstreamInternalSpeed:
	JOG/v CONVEYOR_AXIS,-ConveyorBeltLoadingSpeed
RET


ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET


#22
!SecurePanelBuffer

int ClampLiftDelayTime
int WaitTimeToPanelClamped
int WaitTimeToLifted
int WaitTimeToUnstop
real Stage_1_LifterOnlyDistance
real Stage_2_LifterAndClamperDistance

global int SecurePanelStateError
SecurePanelStateError = 0

global int SecurePanelToClampedError,SecurePanelToLiftedError,SecurePanelToUnstopError

SecurePanelToClampedError=1
SecurePanelToLiftedError=2
SecurePanelToUnstopError=3

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
		TILL (IN(StopperUnlocked_Port).StopperUnlocked_Bit & IN(StopperArmDown_Port).StopperArmDown_Bit),WaitTimeToUnstop
		if (IN(StopperUnlocked_Port).StopperUnlocked_Bit & IN(StopperArmDown_Port).StopperArmDown_Bit)
			PanelSecured = 1
		else
			SecurePanelStateError = SecurePanelToUnstopError
		end
	end
end
STOP

TurnOffPanelSensingOptos:
	OUT(StopSensor_Port).StopSensor_Bit = 0
RET


Stage_1_LifterOnly:
	absPosTemp = RPOS(LIFTER_AXIS)+Stage_1_LifterOnlyDistance
	ptp (LIFTER_AXIS), absPosTemp
	till ^AST(LIFTER_AXIS).#MOVE,WaitTimeToLifted
	if (AST(LIFTER_AXIS).#MOVE | RPOS(LIFTER_AXIS) <> absPosTemp)
		HALT LIFTER_AXIS
		SecurePanelStateError = SecurePanelToLiftedError
		StageLifterResult = 0
	else
		StageLifterResult = 1
	end
RET


Stage_2_LifterAndClamper:
	CALL ClampPanel
	absPosTemp = RPOS(LIFTER_AXIS)+Stage_2_LifterAndClamperDistance
	ptp (LIFTER_AXIS), absPosTemp
	TILL (IN(RearClampUp_Port).RearClampUp_Bit & IN(FrontClampUp_Port).FrontClampUp_Bit),ClampLiftDelayTime
	if (IN(RearClampUp_Port).RearClampUp_Bit & IN(FrontClampUp_Port).FrontClampUp_Bit)
		till ^AST(LIFTER_AXIS).#MOVE,WaitTimeToLifted
		if (AST(LIFTER_AXIS).#MOVE | RPOS(LIFTER_AXIS) <> absPosTemp)
			HALT LIFTER_AXIS
			SecurePanelStateError = SecurePanelToLiftedError
			StageLifterResult = 0
		else
			StageLifterResult = 1
		end
	else
		HALT LIFTER_AXIS
		SecurePanelStateError = SecurePanelToClampedError
		StageLifterResult = 0
	end

RET

ClampPanel:
	OUT(ClampPanel_Port).ClampPanel_Bit = 1
RET

LowerPanelStopper:
	OUT(LockStopper_Port).LockStopper_Port = 0
	OUT(RaiseBoardStopStopper_Port).RaiseBoardStopStopper_Bit = 0
RET



#23
!InternalErrorExitBuffer

CALL ErrorExit
STOP



ErrorExit:
	CALL TurnOffMotors	
	CALL LowerLifter	
	CALL Unclamp	
	CALL LowerStopper	
	CALL ClearBoardAvailable	
	CALL ClearMachineReady	
	CURRENT_STATUS = ERROR_STATUS
RET

TurnOffMotors:
	DISABLEALL
RET

LowerLifter:
	JOG LIFTER_AXIS,-
	TILL IN(LifterLowered_Port).LifterLowered_Bit = 1
	HALT LIFTER_AXIS
RET

Unclamp:
	OUT(ClampPanel_Port).ClampPanel_Bit = 0
RET

LowerStopper:
	OUT(LockStopper_Port).LockStopper_Bit = 0
	OUT(RaiseBoardStopStopper_Port).RaiseBoardStopStopper_Bit = 0
RET

ClearBoardAvailable:
	OUT(DownStreamBoardAvailable_Port).DownStreamBoardAvailable_Bit = 0
RET

ClearMachineReady:
	OUT(SmemaUpStreamMachineReady_Port).SmemaUpStreamMachineReady_Bit = 0
RET



#24

!ConveyorWidthHomingBuffer

int AXIS


FDEF(AXIS).#LL=0       ! Disable the axis left limit default response
FDEF(AXIS).#RL=0       ! Disable the axis left limit default response
FDEF(AXIS).#SLL=0
FDEF(AXIS).#SRL=0




HALT(AXIS)
MFLAGS(AXIS).#DEFCON = 1 !just in case
MFLAGS(AXIS).#HOME = 0
ConveyorWidthHomed = 0

!the motion is 
!	1) in fast 
!	2) out fast
!	3) in slow
!	4) out slow

! 1) in fast
JOG/v(AXIS), -HOME_VEL_IN(AXIS) !Move to the left limit switch
TILL FAULT(6).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 2) out fast
JOG/v(AXIS), HOME_VEL_IN(AXIS) !Move to the left limit switch
TILL ^FAULT(6).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 3) in slow
JOG/v(AXIS), -HOME_VEL_OUT(AXIS) !Move to the left limit switch
TILL FAULT(6).#SLL   ! Wait for the left limit switch activation
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

! 4) out slow
JOG/v(AXIS),HOME_VEL_OUT(AXIS) !Move out of the left limit   
TILL ^FAULT(6).#SLL  !Wait for the left limit release
KILL(AXIS)
TILL ^AST(AXIS).#MOVE

SET APOS(AXIS) = HOME_OFFSET(AXIS)

MFLAGS(AXIS).#HOME = 1
ConveyorWidthHomed = 1
STOP

RESTORE_SETTINGS:
	FDEF(AXIS).#LL=1       ! enable the axis left limit default response
	FDEF(AXIS).#RL=1
STOP

on ^PST(24).#RUN
	call RESTORE_SETTINGS
ret

on MFLAGS(AXIS).#HOME
	block; if MFLAGS(AXIS).#HOME
		FDEF(AXIS).#SLL=1
		FDEF(AXIS).#SRL=1	
	end; end
ret

on ^MFLAGS(AXIS).#HOME
	block; if ^MFLAGS(AXIS).#HOME
		FDEF(AXIS).#SLL=0
		FDEF(AXIS).#SRL=0	
	end; end
ret
#A
global int I(100),I0,I1,I2,I3,I4,I5,I6,I7,I8,I9,I90,I91,I92,I93,I94,I95,I96,I97,I98,I99
global real V(100),V0,V1,V2,V3,V4,V5,V6,V7,V8,V9,V90,V91,V92,V93,V94,V95,V96,V97,V98,V99

!homing variables
global real HOME_VEL_IN(10)
global real HOME_VEL_OUT(10)
global real HOME_OFFSET(10)

global int X_AXIS
global int Y_AXIS
global int Z_AXIS

global int CONVEYOR_AXIS
global int CONVEYOR_WIDTH_AXIS
global int LIFTER_AXIS

global real movine_angle,angle_step

global int ECOFFSETM(3)
global int WAGO_IN1,WAGO_IN2
global int WAGO_OUT1,WAGO_OUT2
global int WAGO_ANAIN_1,WAGO_ANAIN_2,WAGO_ANAIN_3,WAGO_ANAIN_4, ControlWordGen
global int StatusWord_MT1, ControlWord_MT1
global int StatusWord_MT2, ControlWord_MT2
global int StatusWord_Mecapion, ControlWord_Mecapion
global int ActualPos_MT1,ActualPos_MT2,ActualPos_Mecapion
global int MecapionHome
global int Axis_0,Axis_1,Axis_2,Axis_3,Axis_4,Axis_5,Axis_6, Fault_Clr
!global int TargetPos
global int ControlWord, StatusWord, TargetPosition, ActualPosition
global real Slave_number_Mec, vel_search_limit_switch_Mec,vel_search_index_Mec,homing_type_Mec,homing_Acc_Mec
global real Slave_number_MT1,vel_search_limit_switch_MT1,vel_search_index_MT1,homing_type_MT1,homing_Acc_MT1
global real Slave_number_MT2,vel_search_limit_switch_MT2, vel_search_index_MT2,homing_type_MT2,homing_Acc_MT2
global int Elevator_Mode

global int SAFE_STATUS,LOADED_STATUS,ERROR_STATUS,PRERELEASING_STATUS
global int PRERELEASED_STATUS,RELEASED_STATUS,CHANGING_WIDTH_STATUS
global int SEARGING_STATUS,WIDTH_HOMING_STATUS,LOADING_STATUS,RELOADING_STATUS
global int RELEASING_STATUS,BYPASS_STATUS

SAFE_STATUS = 0
LOADED_STATUS = 1
ERROR_STATUS = 2
PRERELEASED_STATUS = 3
RELEASED_STATUS = 4
CHANGING_WIDTH_STATUS = 5
SEARGING_STATUS = 6
WIDTH_HOMING_STATUS = 7
LOADING_STATUS = 8
RELOADING_STATUS = 9
PRERELEASING_STATUS = 10
RELEASING_STATUS = 11
BYPASS_STATUS = 12

global int CURRENT_STATUS

!///////////////////////////////////////////////////////////
!Inputs

global int EntryOpto_Port
global int EntryOpto_Bit

global int ExitOpto_Port
global int ExitOpto_Bit

global int LifterLowered_Port
global int LifterLowered_Bit

global int BoardStopPanelAlignSensor_Port
global int BoardStopPanelAlignSensor_Bit

global int StopperArmUp_Port
global int StopperArmUp_Bit

global int StopperArmDown_Port
global int StopperArmDown_Bit

global int StopperLocked_Port
global int StopperLocked_Bit

global int StopperUnlocked_Port
global int StopperUnlocked_Bit

global int RearClampUp_Port
global int RearClampUp_Bit

global int FrontClampUp_Port
global int FrontClampUp_Bit


global int RearClampDown_Port
global int RearClampDown_Bit

global int FrontClampDown_Port
global int FrontClampDown_Bit


global int WidthHomeSwitch_Port
global int WidthHomeSwitch_Bit

global int WidthLimitSwitch_Port
global int WidthLimitSwitch_Bit

global int UpstreamBoardAvailableSignal_Port
global int UpstreamBoardAvailableSignal_Bit

global int UpstreamFailedBoardAvailableSignal_Port
global int UpstreamFailedBoardAvailableSignal_Bit




global int DownstreamMachineReadySignal_Port
global int DownstreamMachineReadySignal_Bit

global int BypassNormal_Port
global int BypassNormal_Bit


global int EstopAndDoorOpenFeedback_Port
global int EstopAndDoorOpenFeedback_Bit


!///////////////////////////////////////////////////////////
!Outputs

global int ClampPanel_Port
global int ClampPanel_Bit

global int LockStopper_Port
global int LockStopper_Bit

global int RaiseBoardStopStopper_Port
global int RaiseBoardStopStopper_Bit

global int BeltShroudVaccumON_Port
global int BeltShroudVaccumON_Bit

global int TowerLightRed_Port
global int TowerLightRed_Bit

global int TowerLightYellow_Port
global int TowerLightYellow_Bit

global int TowerLightGreen_Port
global int TowerLightGreen_Bit

global int TowerLightBlue_Port
global int TowerLightBlue_Bit

global int TowerLightBuzzer_Port
global int TowerLightBuzzer_Bit

global int StopSensor_Port
global int StopSensor_Bit

global int SmemaUpStreamMachineReady_Port
global int SmemaUpStreamMachineReady_Bit

global int DownStreamBoardAvailable_Port
global int DownStreamBoardAvailable_Bit

global int SmemaDownStreamFailedBoardAvailable_Port
global int SmemaDownStreamFailedBoardAvailable_Bit


!//////////////////////////////////////////////////////////


global int PanelFreed

global int PanelSecured

global int ConveyorWidthHomed

global int InternalMachineLoadBufferIndex
global int InternalErrorExitBufferIndex
global int FreePanelBufferIndex
global int SecurePanelBufferIndex
global int BypassModeBufferIndex
global int HomeConveyorBufferIndex

global real ConveyorBeltAcquireSpeed
global real ConveyorBeltLoadingSpeed
global real ConveyorBeltSlowSpeed
global real ConveyorBeltReleaseSpeed
global real ConveyorBeltUnloadingSpeed

!//////////////////////////////////////////////////////////

global int Gantry_Bottom_Pos(20), Gantry_Top_Pos(20), Gantry_Z_Pos(20), Combined_Vel(20)

global int Gantry_Mode

STOP
