!Reset CONVEYOR

int Axis
int Slave_Number

MFLAGS(Axis).#HOME = 0

Axis= 5
Slave_Number = 2
ACC(Axis) = 10000
DEC(Axis) = 16000

int EC_Offset

EC_Offset = 350

disable Axis
till ^MST(Axis).#ENABLED
wait 200

!*********Unmapping ethercat offset******
ecunmapin(EC_Offset)
ecunmapout(EC_Offset)
!***************************************

!***********Fault Clear***********
ecout(EC_Offset,ControlWord_Conveyor)
ControlWord_Conveyor = 0x8F
wait 500
ControlWord_Conveyor = 0x0F
coewrite/1(Slave_Number,0x6060,0,8)
ecunmapin(EC_Offset)
ecunmapout(EC_Offset)
!***********Fault Clear***********

enable Axis
till MST(Axis).#ENABLED
wait 100

set FPOS(Axis)= 0 

MFLAGS(Axis).#HOME = 1

stop