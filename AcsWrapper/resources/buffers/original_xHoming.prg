#/ Controller version = 3.01
#/ Date = 01-Feb-21 11:43 AM
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
