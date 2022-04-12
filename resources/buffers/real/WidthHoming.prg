!Homing WIDTH
global int PanelOnConveyor
PanelOnConveyor = 1001

int WidthECOffset_ControlWord
int WidthECOffset_TouchProbeFunction
int WidthECOffset_ActualPosition
int Axis
int Slave_Number
Axis= 6
Slave_Number = 3

WidthECOffset_ControlWord = ECGETOFFSET ("Control Word" , Slave_Number)
WidthECOffset_TouchProbeFunction = ECGETOFFSET ("Touch Probe Function" , Slave_Number)
WidthECOffset_ActualPosition = ECGETOFFSET ("Actual Position" , Slave_Number)

ConveyorWidthHomed = 0
MFLAGS(Axis).#HOME = 0

!******Motion parameters for homing***********
VEL(Axis)=10
ACC(Axis)=1000
DEC(Axis)=100000
KDEC(Axis)=10000
JERK(Axis)=10000

int V_Limit_Search, V_Index_Search

V_Limit_Search = -80
V_Index_Search = 15
!*********************************************

disable Axis
till ^MST(Axis).#ENABLED
wait 200

!*********Unmapping ethercat offset for control word******
ecunmapin(WidthECOffset_ControlWord)
ecunmapout(WidthECOffset_ControlWord)
!***************************************

!***********Fault Clear***********
ecout(WidthECOffset_ControlWord,ControlWord_Width)
ControlWord_Width = 0x8F
wait 500
ControlWord_Width = 0x0F
ecunmapin(WidthECOffset_ControlWord)
ecunmapout(WidthECOffset_ControlWord)
!***********Fault Clear***********


!*****Disable default limit response******
FDEF(Axis).#LL=0
FDEF(Axis).#RL=0
!*****************************************

if AutoWidthEnable = 1
if (EntryOpto_Bit = 0 & ExitOpto_Bit = 0 & BoardStopPanelAlignSensor_Bit = 0)
!********Search for the negetive limit******
enable Axis
wait 100
!*******************************************
if WidthHomingDirection = 0
jog/v Axis , V_Limit_Search
till SAFINI(Axis).#LL=1
halt Axis
till ^MST(Axis).#MOVE
wait 10
end
!*******************************************

!*******************************************
if WidthHomingDirection = 1
jog/v Axis , 20
till SAFINI(Axis).#RL=1
halt Axis
till ^MST(Axis).#MOVE
wait 10
end
!*******************************************

int Touch_Probe_Status = 0
ecout( WidthECOffset_TouchProbeFunction, Touch_Probe_Function )
real Index_Position

!********Initiating touch probe******************
!while ^(Touch_Probe_Status = 65)
!coewrite/2 (Slave_Number,0x60B8,0,0)
!wait 200
!coewrite/2 (Slave_Number,0x60B8,0,21)
!Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
!end
while ^(Touch_Probe_Status = 65)
Touch_Probe_Function = 0
wait 200
Touch_Probe_Function = 21
wait 200
Touch_Probe_Status = coeread/2(Slave_Number, 0x60B9, 0)
end
!**********************************************************

!*************Looking for index*****************\
if WidthHomingDirection = 0
jog/v Axis,V_Index_Search
end
if WidthHomingDirection = 1
jog/v Axis,-15
end

while (Touch_Probe_Status=65)
Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
if ^(Touch_Probe_Status = 65 )
kill Axis
wait 200
end
end
!************************************************

!2nd index find if index fall withlim LL
if SAFINI(Axis).#LL=1

while ^(Touch_Probe_Status = 65)
Touch_Probe_Function = 0
wait 200
Touch_Probe_Function = 21
wait 200
Touch_Probe_Status = coeread/2(Slave_Number, 0x60B9, 0)
end
!**********************************************************

!*************Looking for index*****************\
if WidthHomingDirection = 0
jog/v Axis,V_Index_Search
end
if WidthHomingDirection = 1
jog/v Axis,-15
end
while (Touch_Probe_Status=65)
Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
if ^(Touch_Probe_Status = 65 )
kill Axis
wait 200
end
end

end

ecin (WidthECOffset_ActualPosition, ActualPos_Width)
set FPOS(Axis)=ActualPos_Width/1000
Index_Position=(coeread/4 (Slave_Number,0x60BA,0))/1000

set FPOS(Axis)=(FPOS(Axis)-Index_Position)

ptp/v Axis,0,10
till ^MST(Axis).#MOVE
wait 200
MFLAGS(Axis).#HOME = 1
ConveyorWidthHomed = 1

elseif BoardStopPanelAlignSensor_Bit = 1
ERROR_CODE = PanelOnConveyor
CALL ErrorExit
end
end


!*****Enable default limit response******
FDEF(Axis).#LL=1
FDEF(Axis).#RL=1
!*****************************************

stop

ErrorExit:
	START InternalErrorExitBufferIndex,1
	TILL ^ PST(InternalErrorExitBufferIndex).#RUN
RET