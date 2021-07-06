!Homing WIDTH

int Axis
int Slave_Number

Axis= 6
Slave_Number = 3

ConveyorWidthHomed = 0
MFLAGS(Axis).#HOME = 0

!******Motion parameters for homing***********
VEL(Axis)=10
ACC(Axis)=1000
DEC(Axis)=1000
KDEC(Axis)=10000
JERK(Axis)=10000

int EC_Offset, V_Limit_Search, V_Index_Search

V_Limit_Search = -50
V_Index_Search = 5
EC_Offset = 364
!*********************************************

disable Axis
till ^MST(Axis).#ENABLED
wait 200

!*********Unmapping ethercat offset for control word******
ecunmapin(EC_Offset)
ecunmapout(EC_Offset)
!***************************************

!***********Fault Clear***********
ecout(EC_Offset,ControlWord_Width)
ControlWord_Width = 0x8F
wait 500
ControlWord_Width = 0x0F
coewrite/1(Slave_Number,0x6060,0,8)
ecunmapin(EC_Offset)
ecunmapout(EC_Offset)
!***********Fault Clear***********


!*****Disable default limit response******
FDEF(Axis).#LL=0
FDEF(Axis).#RL=0
!*****************************************

!********Search for the negetive limit******
enable Axis
wait 100
! Simulation
!jog/v Axis , V_Limit_Search
!till SAFINI(Axis).#LL=1
halt Axis
till ^MST(Axis).#MOVE
wait 10
!*******************************************

int Touch_Probe_Status
real Index_Position

!********Initiating touch probe******************
coewrite/2 (Slave_Number,0x60B8,0,0)
wait 200
coewrite/2 (Slave_Number,0x60B8,0,21)
Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
!**********************************************************

!*************Looking for index*****************
jog/v Axis,V_Index_Search
while (Touch_Probe_Status=65)
Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
if ^(Touch_Probe_Status = 65 )
end
end
kill Axis
wait 200
!************************************************

ecin (366, ActualPos_Width)
set FPOS(Axis)=ActualPos_Width/1000
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
ConveyorWidthHomed = 1
stop