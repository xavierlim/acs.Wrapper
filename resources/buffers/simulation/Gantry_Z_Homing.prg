!Gantry Z Homing

int ZECOffset_ControlWord
int ZECOffset_TouchProbeFunction
int ZECOffset_ActualPosition
int Slave_Number = 1

int Axis_Z
Axis_Z = 4

ZECOffset_ControlWord = ECGETOFFSET ("Control Word" , Slave_Number)
ZECOffset_TouchProbeFunction = ECGETOFFSET ("Touch Probe Function" , Slave_Number)
ZECOffset_ActualPosition = ECGETOFFSET ("Actual Position" , Slave_Number)

disable Axis_Z

FMASK(Axis_Z).#RL = 1
FMASK(Axis_Z).#LL = 1

FDEF(Axis_Z).#RL = 0
FDEF(Axis_Z).#LL = 0

MFLAGS(Axis_Z).#HOME = 0

ACC (Axis_Z) = 2000
DEC (Axis_Z) = 2000
KDEC(Axis_Z) = 3000
JERK (Axis_Z) = 10000

!*********Unmapping ethercat offset for control word******
ecunmapin(ZECOffset_ControlWord)
ecunmapout(ZECOffset_ControlWord)
!***************************************

!***********Fault Clear***********
ecout(ZECOffset_ControlWord,ControlWord_Z)
ControlWord_Z = 0x8F
wait 500
ControlWord_Z = 0x0F
ecunmapin(ZECOffset_ControlWord)
ecunmapout(ZECOffset_ControlWord)
!***********Fault Clear***********

wait 1000

!*********Unmapping ethercat offset for control word******
ecunmapin(ZECOffset_ControlWord)
ecunmapout(ZECOffset_ControlWord)
!***************************************

!***********2nd time Fault Clear***********
ecout(ZECOffset_ControlWord,ControlWord_Z)
ControlWord_Z = 0x8F
wait 500
ControlWord_Z = 0x0F
ecunmapin(ZECOffset_ControlWord)
ecunmapout(ZECOffset_ControlWord)
!***********Fault Clear***********

enable Axis_Z


!Simulation
WAIT 5000
ptp/ve Axis_Z,0,100
MFLAGS(Axis_Z).#HOME = 1
STOP


XCURV(Axis_Z) = 45
XCURI(Axis_Z) = 16

jog/v Axis_Z, -1
till SAFINI(4).#LL = 1

disable Axis_Z

wait 1000

enable Axis_Z

int Touch_Probe_Status = 0
ecout( ZECOffset_TouchProbeFunction, Touch_Probe_Function )
real Index_Position

!********Initiating touch probe******************
while ^(Touch_Probe_Status = 65)
Touch_Probe_Function = 0
wait 200
Touch_Probe_Function = 21
wait 200
Touch_Probe_Status = coeread/2(Slave_Number, 0x60B9, 0)
end
!**********************************************************

!*************Looking for index*****************\
jog/v Axis_Z,1
while (Touch_Probe_Status=65)
Touch_Probe_Status=coeread/2 (Slave_Number,0x60B9,0)
if ^(Touch_Probe_Status = 65 )
kill Axis_Z
wait 200
end
end
!************************************************

ecin (ZECOffset_ActualPosition, ActualPos_Z)
set FPOS(Axis_Z)=ActualPos_Z/1000
Index_Position=(coeread/4 (Slave_Number,0x60BA,0))/1000
set FPOS(Axis_Z)=(FPOS(Axis_Z)-Index_Position)

ptp/v Axis_Z,0,10
till ^MST(Axis_Z).#MOVE
wait 200

ptp/v Axis_Z,-5,10
till ^MST(Axis_Z).#MOVE
wait 200
set FPOS(Axis_Z) = 0

VEL (Axis_Z) = 50
ACC (Axis_Z) = 1500
DEC (Axis_Z) = 1500
KDEC(Axis_Z) = 10000
JERK (Axis_Z) = 50000

FMASK(Axis_Z).#RL = 1
FMASK(Axis_Z).#LL = 1

FDEF(Axis_Z).#RL = 1
FDEF(Axis_Z).#LL = 1

MFLAGS(Axis_Z).#HOME = 1

stop 