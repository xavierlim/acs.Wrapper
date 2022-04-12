!Homing LIFTER

int LifterECOffset_ControlWord
int LifterECOffset_TouchProbeFunction
int LifterECOffset_ActualPosition
int Axis
int Slave_Number
Axis= 7
Slave_Number = 4

LifterECOffset_ControlWord = ECGETOFFSET ("Control Word" , Slave_Number)
LifterECOffset_TouchProbeFunction = ECGETOFFSET ("Touch Probe Function" , Slave_Number)
LifterECOffset_ActualPosition = ECGETOFFSET ("Actual Position" , Slave_Number)

ConveyorLifterHomed = 0
MFLAGS(Axis).#HOME = 0

!******Motion parameters for homing***********
VEL(Axis)=10
ACC(Axis)=1000
DEC(Axis)=1000
KDEC(Axis)=10000
JERK(Axis)=10000

int EC_Offset, V_Limit_Search, V_Index_Search

V_Limit_Search = -25
V_Index_Search = 2
EC_Offset = 378
!*********************************************

disable Axis
till ^MST(Axis).#ENABLED
wait 200

!*********Unmapping ethercat offset for control word******
ecunmapin(LifterECOffset_ControlWord)
ecunmapout(LifterECOffset_ControlWord)
!***************************************

!***********Fault Clear***********
ecout(LifterECOffset_ControlWord,ControlWord_Lifter)
ControlWord_Lifter = 0x8F
wait 500
ControlWord_Lifter = 0x0F
ecunmapin(LifterECOffset_ControlWord)
ecunmapout(LifterECOffset_ControlWord)
!***********Fault Clear***********


!*****Disable default limit response******
FDEF(Axis).#LL=0
FDEF(Axis).#RL=0
!*****************************************

!********Search for the negetive limit******
enable Axis
wait 100
jog/v Axis , V_Limit_Search
till SAFINI(Axis).#LL=1
halt Axis
till ^MST(Axis).#MOVE
wait 10
!*******************************************

int Touch_Probe_Status = 0
ecout( LifterECOffset_TouchProbeFunction, Touch_Probe_Function )
real Index_Position

!********Initiating touch probe******************
!coewrite/2 (Slave_Number,0x60B8,0,0)
!wait 200
!coewrite/2 (Slave_Number,0x60B8,0,21)
!Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
while ^(Touch_Probe_Status = 65)
Touch_Probe_Function = 0
wait 200
Touch_Probe_Function = 21
wait 200
Touch_Probe_Status = coeread/2(Slave_Number, 0x60B9, 0)
end
!**********************************************************

!*************Looking for index*****************
jog/v Axis,V_Index_Search
while (Touch_Probe_Status=65)
Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
if ^(Touch_Probe_Status = 65 )
kill Axis
wait 200
end
end
!************************************************

!********Initiating 2nd touch probe******************
while ^(Touch_Probe_Status = 65)
Touch_Probe_Function = 0
wait 200
Touch_Probe_Function = 21
wait 200
Touch_Probe_Status = coeread/2(Slave_Number, 0x60B9, 0)
end
!**********************************************************

!*************Looking for 2nd index*****************
jog/v Axis,V_Index_Search
while (Touch_Probe_Status=65)
Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
if ^(Touch_Probe_Status = 65 )
kill Axis
wait 200
end
end
!************************************************

ecin (LifterECOffset_ActualPosition, ActualPos_Lifter)
set FPOS(Axis)=ActualPos_Lifter/1000
Index_Position=(coeread/4 (Slave_Number,0x60BA,0))/1000
set FPOS(Axis)=(FPOS(Axis)-Index_Position)

ptp/v Axis,0,10
till ^MST(Axis).#MOVE
wait 200



!*****Enable default limit response******
FDEF(Axis).#LL=1
FDEF(Axis).#RL=1
!*****************************************

MFLAGS(Axis).#HOME = 1
ConveyorLifterHomed = 1
stop