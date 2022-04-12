!ScanningBuffer

global int CameraBusyMissing, CameraBusyStuck, GantryError, ConveyorAxisError
global int X_EncoderError, X_OverHeat, X_OverCurrent, X_CriticalPositionError, X_HitLimit
global int Y_EncoderError, Y_OverHeat, Y_OverCurrent, Y_CriticalPositionError, Y_HitLimit
global int Z_Error

CameraBusyMissing = 901
CameraBusyStuck = 902
ConveyorAxisError = 903

X_EncoderError = 904
X_OverHeat = 905
X_OverCurrent = 906
X_CriticalPositionError = 907
X_HitLimit = 908
Y_EncoderError = 909
Y_OverHeat = 910
Y_OverCurrent = 911
Y_CriticalPositionError = 912
Y_HitLimit = 913
Z_Error = 914

real SCAN_POINTS(_NR_SCAN_POINTS_)(3)
real SCAN_POINTS_VELOCITY(_NR_SCAN_POINTS_)(3)
real SCAN_POINTS_DELAY(_NR_SCAN_POINTS_)(3)

!wait 10000

int START_SCAN_POINT_INDEX
int END_SCAN_POINT_INDEX

int IS_NEED_WAIT_CONTINUE_COMMAND

int START_NEXT_STEP_COMMAND
int WAIT_CONTINUE_COMMAND_MONITOR

int CURRENT_STEP_INDEX
int IS_ERROR

int MOVE_MOTION_COMPLETE_RECVD
int MOVE_PSX_ACK_RECVD

MOVE_MOTION_COMPLETE_RECVD = 0
MOVE_PSX_ACK_RECVD = 0

DO_SCAN:
!ENABLE ALL

!ISSAC TEST

ENABLE (X_AXIS,Y_AXIS,Z_AXIS)
CURRENT_STEP_INDEX = START_SCAN_POINT_INDEX


OUT(_TRIGGER_PORT_TO_CAMERA_START_)._TRIGGER_BIT_TO_CAMERA_START_ = 1

	int ii
	ii = START_SCAN_POINT_INDEX
	int lastIndex
	lastIndex = END_SCAN_POINT_INDEX
	if(lastIndex > _NR_SCAN_POINTS_)
		lastIndex = _NR_SCAN_POINTS_
	end

	while ii <= lastIndex
		!ISSAC TEST
		!OUT(0).1 = 1
		MOVE_MOTION_COMPLETE_RECVD = 0
		MOVE_PSX_ACK_RECVD = 0

		!Edit by ISSAC to keep vel at ACS default 1m/s
		VEL(X_AXIS)=SCAN_POINTS_VELOCITY(ii)(_X_AXIS_INDEX_)/10000
		VEL(Y_AXIS)=SCAN_POINTS_VELOCITY(ii)(_Y_AXIS_INDEX_)/10000
		!VEL(Z_AXIS)=SCAN_POINTS_VELOCITY(ii)(_Z_AXIS_INDEX_)/1000
				
		WAIT BeforeMoveDelay 
		
		!ISSAC TEST
		PTP Z_AXIS, (SCAN_POINTS(ii)(_Z_AXIS_INDEX_))/1000
		PTP/em (X_AXIS,Y_AXIS), (SCAN_POINTS(ii)(_X_AXIS_INDEX_))/10000, (SCAN_POINTS(ii)(_Y_AXIS_INDEX_))/10000
		IF FAULT(0) <> 0 | FAULT(1) <> 0 | FAULT(4) <> 0
		CALL CheckGantryAxisError
		CALL ErrorExit
		STOP 9
		END
		
		if ConveyorInSimulationMode = 0
		IF FAULT(5) <> 0 | FAULT(6) <> 0 | FAULT(7) <> 0
		CALL ErrorExit
		ERROR_CODE = ConveyorAxisError
		STOP 9
		END
		end 
		
		!PTP/em (X_AXIS,Y_AXIS,Z_AXIS), (SCAN_POINTS(ii)(_X_AXIS_INDEX_))/1000, (SCAN_POINTS(ii)(_Y_AXIS_INDEX_))/1000, (SCAN_POINTS(ii)(_Z_AXIS_INDEX_))/1000
		
		TILL ^MST(4).#MOVE & ^MST(1).#MOVE & ^MST(0).#MOVE

		WAIT SCAN_POINTS_DELAY(ii)(_X_AXIS_INDEX_)
		WAIT MotionSettlingTimeBeforeScan

		OUT(_TRIGGER_PORT_TO_CAMERA_START_)._TRIGGER_BIT_TO_CAMERA_START_ = 0
		
		!ISSAC TEST
		MOVE_MOTION_COMPLETE_RECVD = ii + 1
		!WAIT (SCAN_POINTS_DELAY(ii)(_X_AXIS_INDEX_)+SCAN_POINTS_DELAY(ii)(_Y_AXIS_INDEX_)+SCAN_POINTS_DELAY(ii)(_Z_AXIS_INDEX_))
		!TILL MOVE_MOTION_COMPLETE_RECVD = 0
		
		!added by issac for busy interlock
		TILL IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 1,_TRIGGER_FROM_CAMERA_TIME_OUT_
		
		!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		IF IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 1
			TILL IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 0,_TRIGGER_FROM_CAMERA_TIME_OUT_
			
			IF IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 0			
				!ISSAC TEST
				MOVE_PSX_ACK_RECVD = ii + 1
				!IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 0
				!TILL MOVE_PSX_ACK_RECVD = 0
				OUT(_TRIGGER_PORT_TO_CAMERA_START_)._TRIGGER_BIT_TO_CAMERA_START_ = 1

				CURRENT_STEP_INDEX=ii

				ii = ii + 1
				IS_ERROR=MERR(X_AXIS) + MERR(Y_AXIS) + MERR(Z_AXIS)
				!IS_ERROR=MERR(X_AXIS) + MERR(Y_AXIS)

				if IS_ERROR = 0
					if IS_NEED_WAIT_CONTINUE_COMMAND = 1
						START_NEXT_STEP_COMMAND = 0
						WAIT_CONTINUE_COMMAND_MONITOR = 1
						TILL START_NEXT_STEP_COMMAND = 1
						WAIT_CONTINUE_COMMAND_MONITOR = 0
					end
				else
					ii=lastIndex+10
				end
			ELSE IF IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 1
			CALL ErrorExit
			ERROR_CODE = CameraBusyStuck
			STOP 9
			END
			END
			
		ELSE IF IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 0
		CALL ErrorExit
		ERROR_CODE = CameraBusyMissing
		STOP 9
		END
		END
		!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	
	END

STOP

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET

CheckGantryAxisError:

if FAULT(0) <> 0

	if FAULT(0).#ENC = 1
	GANTRY_ERROR = X_EncoderError
	GANTRY_STATUS = 1
	elseif FAULT(0).#HOT = 1
	GANTRY_ERROR = X_OverHeat
	GANTRY_STATUS = 1
	elseif FAULT(0).#CL = 1
	GANTRY_ERROR = X_OverCurrent
	GANTRY_STATUS = 1
	elseif FAULT(0).#CPE = 1
	GANTRY_ERROR = X_CriticalPositionError
	GANTRY_STATUS = 1
	elseif FAULT(0).#SRL = 1 | FAULT(0).#SLL = 1 | FAULT(0).#RL = 1 | FAULT(0).#LL = 1
	GANTRY_ERROR = X_HitLimit
	GANTRY_STATUS = 1
	end
	
elseif FAULT(1) <> 0

	if FAULT(1).#ENC = 1
	GANTRY_ERROR = Y_EncoderError
	GANTRY_STATUS = 1
	elseif FAULT(1).#HOT = 1
	GANTRY_ERROR = Y_OverHeat
	GANTRY_STATUS = 1
	elseif FAULT(1).#CL = 1
	GANTRY_ERROR = Y_OverCurrent
	GANTRY_STATUS = 1
	elseif FAULT(1).#CPE = 1
	GANTRY_ERROR = Y_CriticalPositionError
	GANTRY_STATUS = 1
	elseif FAULT(1).#SRL = 1 | FAULT(1).#SLL = 1 | FAULT(1).#RL = 1 | FAULT(1).#LL = 1
	GANTRY_ERROR = Y_HitLimit
	GANTRY_STATUS = 1
	end

elseif FAULT(4) <> 0
GANTRY_ERROR = Z_Error
GANTRY_STATUS = 1

end

RET