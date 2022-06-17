WidthLifterConveyor_Reset_Completed = 0

!TURN OFF CONVEYOR BELT SOFT LIMIT
FMASK5.#SRL = 0
FMASK5.#SLL = 0


!Reset Width

int Axis
int Slave_Number
int WidthECOffset_ControlWord

Axis= 6
Slave_Number = 3

WidthECOffset_ControlWord = ECGETOFFSET ("ControlWord" , Slave_Number)

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

enable Axis
till MST(Axis).#ENABLED
wait 100






!Reset Lifter


Axis= 7
Slave_Number = 4

int LifterECOffset_ControlWord
LifterECOffset_ControlWord = ECGETOFFSET ("ControlWord" , Slave_Number)

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

enable Axis
till MST(Axis).#ENABLED
wait 100






!Reset Conveyor


Axis= 5
Slave_Number = 2

int ConveyorECOffset_ControlWord
ConveyorECOffset_ControlWord = ECGETOFFSET ("ControlWord" , Slave_Number)

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

wait 2000

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

!Reset Z

Axis= 4
Slave_Number = 1
int ZECOffset_ControlWord
ZECOffset_ControlWord = ECGETOFFSET ("Controlword" , Slave_Number)

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

WidthLifterConveyor_Reset_Completed = 1

stop
