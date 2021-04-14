
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
		MOVE_MOTION_COMPLETE_RECVD = 0
		MOVE_PSX_ACK_RECVD = 0

		VEL(X_AXIS)=SCAN_POINTS_VELOCITY(ii)(_X_AXIS_INDEX_)/1000
		VEL(Y_AXIS)=SCAN_POINTS_VELOCITY(ii)(_Y_AXIS_INDEX_)/1000
		VEL(Z_AXIS)=SCAN_POINTS_VELOCITY(ii)(_Z_AXIS_INDEX_)/1000

		PTP/em (X_AXIS,Y_AXIS,Z_AXIS), (SCAN_POINTS(ii)(_X_AXIS_INDEX_))/1000, (SCAN_POINTS(ii)(_Y_AXIS_INDEX_))/1000, (SCAN_POINTS(ii)(_Z_AXIS_INDEX_))/1000
		!!PTP/em (X_AXIS,Y_AXIS), SCAN_POINTS(ii)(_X_AXIS_INDEX_), SCAN_POINTS(ii)(_Y_AXIS_INDEX_)

		WAIT MotionSettlingTimeBeforeScan

		OUT(_TRIGGER_PORT_TO_CAMERA_START_)._TRIGGER_BIT_TO_CAMERA_START_ = 0
		MOVE_MOTION_COMPLETE_RECVD = 1
		!WAIT (SCAN_POINTS_DELAY(ii)(_X_AXIS_INDEX_)+SCAN_POINTS_DELAY(ii)(_Y_AXIS_INDEX_)+SCAN_POINTS_DELAY(ii)(_Z_AXIS_INDEX_))
		!TILL MOVE_MOTION_COMPLETE_RECVD = 0
		WAIT 60
		
		!TILL IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 0,_TRIGGER_FROM_CAMERA_TIME_OUT_
		MOVE_PSX_ACK_RECVD = 1
		!IN(_TRIGGER_PORT_FROM_CAMERA_CONTINUE_)._TRIGGER_BIT_FROM_CAMERA_CONTINUE_ = 0
		!TILL MOVE_PSX_ACK_RECVD = 0
		WAIT 60
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
	end



STOP

