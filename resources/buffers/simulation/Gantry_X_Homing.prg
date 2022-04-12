!Gantry X Homing
ERRORUNMAP 0,0

int Axis_X1, Axis_X2

Axis_X1 = 0
Axis_X2 = 2

!MFLAGS(Axis_X1).#HOME = 0

disable Axis_X1
disable Axis_X2

global real CnvT(5), CnvA(5)
disable Axis_X1, Axis_X2

CALL INPUTSHAPING_OFF
CALL NONGANTRY_SETTINGS
CALL NONGANTRY_PARAMTERS


!Simulation
WAIT 5000
enable Axis_X1
enable Axis_X2
ptp/ve Axis_X1,0,100
ptp/ve Axis_X2,0,100
MFLAGS(Axis_X1).#HOME = 1
MFLAGS(Axis_X2).#HOME = 1
MFLAGS(5).#HOME = 1
MFLAGS(6).#HOME = 1
MFLAGS(7).#HOME = 1
STOP


Gantry_Mode = 0
CALL X1_IND_HOMING
CALL X2_IND_HOMING

WAIT 500
real MiddlePoint
MiddlePoint = (FPOS(Axis_X1)+FPOS(Axis_X2))/2
set FPOS(0) = MiddlePoint
set FPOS(2) = MiddlePoint

CALL GANTRY_PARAMETERS
Gantry_Mode = 1

set FPOS(2) = 0
set FPOS(0) = 0
CALL GANTRY_SETTINGS 

enable Axis_X1

CALL INPUTSHAPING_ON
WAIT 500
!START 62, 1
!TILL ^ PST(62).#RUN
MFLAGS(Axis_X1).#HOME = 1

stop


!FUNCTIONS

INPUTSHAPING_OFF:
	InShapeOFF 0
RET

INPUTSHAPING_ON:
!-------------------------------------------
CnvT(0)= 0*0.5;      CnvA(0)= 24813/1e5
CnvT(1)= 17*0.5;     CnvA(1)= 8441/1e5
CnvT(2)= 22*0.5;     CnvA(2)= 1639/1e5
CnvT(3)= 27*0.5;    CnvA(3)= 42464/1e5
CnvT(4)= 51*0.5;    CnvA(4)= 22643/1e5  
!--------------------------------------------
	InShapeOn 0, CnvT, CnvA
RET

NONGANTRY_SETTINGS:
	!MFLAGS(Axis_X1).#HOME = 0

	FMASK(Axis_X1).#RL = 1
	FMASK(Axis_X1).#LL = 1
	FMASK(Axis_X2).#RL = 1
	FMASK(Axis_X2).#LL = 1

	FDEF(Axis_X1).#RL = 0
	FDEF(Axis_X1).#LL = 0
	FDEF(Axis_X2).#RL = 0
	FDEF(Axis_X2).#LL = 0	
RET

GANTRY_SETTINGS:
	FMASK(Axis_X1).#RL = 1
	FMASK(Axis_X1).#LL = 1
	FMASK(Axis_X2).#RL = 0
	FMASK(Axis_X2).#LL = 0

	FDEF(Axis_X1).#RL = 1
	FDEF(Axis_X1).#LL = 1

	VEL (Axis_X1) = 1000
	ACC (Axis_X1) = 6000
	DEC (Axis_X1) = 6000
	KDEC(Axis_X1) = 10000
	JERK (Axis_X1) = 170000
RET

NONGANTRY_PARAMTERS:
	SLSBORD(Axis_X1) =0
	SLSBORD(Axis_X2) =0

	ACC (Axis_X1) = 1000
	DEC (Axis_X1) = 1000
	KDEC(Axis_X1) = 1000
	JERK (Axis_X1) = 10000

	ACC (Axis_X2) = 1000
	DEC (Axis_X2) = 1000
	KDEC(Axis_X2) = 1000
	JERK (Axis_X2) = 10000

!X1

	MFLAGS(Axis_X1).25 = 0

	SLPKP(Axis_X1) = 110
	SLVKP(Axis_X1) = 500
	SLVKI(Axis_X1) = 200
	SLVSOF(Axis_X1) = 500
	SLVSOFD(Axis_X1) = 0.8

	MFLAGS(Axis_X1).14 = 1
	SLVNATT(Axis_X1) = 7
	SLVNFRQ(Axis_X1) = 110
	SLVNWID(Axis_X1) = 15
	
	MFLAGS(Axis_X1).16 = 0
	MFLAGS(Axis_X1).26 = 0

	XCURV(Axis_X1) = 20
	XCURI(Axis_X1) = 15

	SLAFF(Axis_X1) = 150

!X2
	MFLAGS(Axis_X2).25 = 0
	MFLAGS(Axis_X2).1 = 0

	SLPKP(Axis_X2) = 80
	SLVKP(Axis_X2) = 300
	SLVKI(Axis_X2) = 280
	SLVSOF(Axis_X2) = 300
	SLVSOFD(Axis_X2) = 0.8

	MFLAGS(Axis_X2).14 = 0
	SLVNATT(Axis_X2) = 7
	SLVNFRQ(Axis_X2) = 110
	SLVNWID(Axis_X2) = 15
	
	MFLAGS(Axis_X2).16 = 0
	MFLAGS(Axis_X2).26 = 0

	XCURV(Axis_X2) = 20
	XCURI(Axis_X2) = 15

	SLAFF(Axis_X2) = 150
	
wait 100
RET

GANTRY_PARAMETERS:
	MFLAGS(Axis_X1).25 = 1
	MFLAGS(Axis_X2).25 = 1
	MFLAGS(Axis_X2).1 = 1

	SLSBORD(Axis_X1) = 1
	SLSBBW(Axis_X1) = 55
	SLSBF(Axis_X1) = 800

!X1

	SLPKP(Axis_X1) = 300
	SLVKP(Axis_X1) = 320 !430 will got sound, 200 no sound
	SLVKI(Axis_X1) = 5
	SLVSOF(Axis_X1) = 2500
	SLVSOFD(Axis_X1) = 1
	MFLAGS(Axis_X1).15 = 0
	SLAFF(Axis_X1) = 150

!Velocity notch filter
	MFLAGS(Axis_X1).14 = 1
	SLVNATT(Axis_X1) = 7
	SLVNFRQ(Axis_X1) = 140
	SLVNWID(Axis_X1) = 15

!Velocity BQF 1 
	MFLAGS(Axis_X1).16 = 1
	SLVB0DD(Axis_X1) = 0.8
	SLVB0DF(Axis_X1) = 435
	SLVB0ND(Axis_X1) = 0.3
	SLVB0NF(Axis_X1) = 435

!Velocity BQF 2
	MFLAGS(Axis_X1).26 = 0
	SLVB1DD(Axis_X1) = 0.4
	SLVB1DF(Axis_X1) = 60
	SLVB1ND(Axis_X1) = 0.6
	SLVB1NF(Axis_X1) = 60
	SLAFF(Axis_X1) = 150

!Current limit restoration 
	XCURV(Axis_X1) = 100
	XCURI(Axis_X1) = 50

!X2

	SLPKP(Axis_X2) = 80
	SLVKP(Axis_X2) = 300
	SLVKI(Axis_X2) = 280
	SLVSOF(Axis_X2) = 300
	SLVSOFD(Axis_X2) = 0.8

	MFLAGS(Axis_X2).14 = 0
	SLVNATT(Axis_X2) = 7
	SLVNFRQ(Axis_X2) = 110
	SLVNWID(Axis_X2) = 15
	
	MFLAGS(Axis_X2).16 = 0
	MFLAGS(Axis_X2).26 = 0

	XCURV(Axis_X2) = 20
	XCURI(Axis_X2) = 15

	SLAFF(Axis_X2) = 150
	
	SLSBORD(Axis_X2) = 1
	SLSBBW(Axis_X2) = 30
	SLSBF(Axis_X2) = 200
RET

X1_IND_HOMING:
	enable Axis_X1

	jog/v Axis_X1, -50
	till FAULT(Axis_X1).#LL = 1
	kill Axis_X1
	TILL ^AST(Axis_X1).#MOVE

	IST(Axis_X1).#IND=0 
	jog/v Axis_X1,20
	wait 100
	TILL IST(Axis_X1).#IND 
	kill Axis_X1
	TILL ^AST(Axis_X1).#MOVE
	wait 100
	SET FPOS(Axis_X1)=FPOS(Axis_X1)-IND(Axis_X1)

	disable Axis_X1
RET

X2_IND_HOMING:
	enable Axis_X2

	jog/v Axis_X2, -50
	till FAULT(Axis_X2).#LL = 1
	kill Axis_X2
	TILL ^AST(Axis_X2).#MOVE

	IST(Axis_X2).#IND=0 
	jog/v Axis_X2,20
	wait 100
	TILL IST(Axis_X2).#IND 
	kill Axis_X2
	TILL ^AST(Axis_X2).#MOVE
	wait 100
	SET FPOS(Axis_X2)=FPOS(Axis_X2)-IND(Axis_X2)

	disable Axis_X2
RET