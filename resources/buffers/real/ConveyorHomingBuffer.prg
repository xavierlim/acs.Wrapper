!Reset CONVEYOR
int ConveyorECOffset_ControlWord
int Axis
int Slave_Number
Axis= 5
Slave_Number = 2

ConveyorECOffset_ControlWord = ECGETOFFSET ("ControlWord" , Slave_Number)
MFLAGS(Axis).#HOME = 0

ACC(Axis) = 10000
DEC(Axis) = 16000

disable Axis
till ^MST(Axis).#ENABLED
wait 200

!*********Unmapping ethercat offset for control word******
ecunmapin(ConveyorECOffset_ControlWord)
ecunmapout(ConveyorECOffset_ControlWord)
!***************************************

!***********Fault Clear***********
ecout(ConveyorECOffset_ControlWord,ControlWord_Conveyor)
ControlWord_Conveyor = 0x8F
wait 500
ControlWord_Conveyor = 0x0F
ECUNMAPOUT (ConveyorECOffset_ControlWord)
ECUNMAPIN (ConveyorECOffset_ControlWord)
!***********Fault Clear***********

!***********2ND TIME Fault Clear***********
ecout(ConveyorECOffset_ControlWord,ControlWord_Conveyor)
ControlWord_Conveyor = 0x8F
wait 500
ControlWord_Conveyor = 0x0F
ECUNMAPOUT (ConveyorECOffset_ControlWord)
ECUNMAPIN (ConveyorECOffset_ControlWord)
!***********Fault Clear***********


enable Axis
till MST(Axis).#ENABLED
wait 100

set FPOS(Axis)= 0 

MFLAGS(Axis).#HOME = 1

stop
