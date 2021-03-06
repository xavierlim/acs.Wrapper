WidthLifterConveyor_Reset_Completed = 0

!TURN OFF CONVEYOR BELT SOFT LIMIT
FMASK5.#SRL = 0
FMASK5.#SLL = 0


!Reset Width

int Axis
int Slave_Number

Axis= 6
Slave_Number = 3

int EC_Offset

EC_Offset = 364

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






!Reset Lifter


Axis= 7
Slave_Number = 4


EC_Offset = 378

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






!Reset Conveyor


Axis= 5
Slave_Number = 2


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

wait 2000
!***********2nd Fault Clear***********
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




WidthLifterConveyor_Reset_Completed = 1

stop