!Gantry X Homing

int Axis_X1, Axis_X2

Axis_X1 = 0
Axis_X2 = 2

MFLAGS(Axis_X1).#HOME = 0

disable Axis_X1
disable Axis_X2

global real CnvT(5), CnvA(5)
disable Axis_X1, Axis_X2

CALL INPUTSHAPING_OFF
CALL NONGANTRY_SETTINGS
CALL NONGANTRY_PARAMTERS
Gantry_Mode = 0
CALL X1_IND_HOMING
CALL X2_IND_HOMING
CALL GANTRY_PARAMETERS
Gantry_Mode = 1

set FPOS(2) = 0
!set FPOS(2) = FPOS(2) - 1.2336
CALL GANTRY_SETTINGS

enable Axis_X1
ptp/ve Axis_X1,0,100

CALL INPUTSHAPING_ON

MFLAGS(Axis_X1).#HOME = 1

stop


!FUNCTIONS

INPUTSHAPING_OFF:
	InShapeOFF 0
RET

INPUTSHAPING_ON:
!-------------Regular convalution-----------
	CnvT(0)= 0*0.5;		CnvA(0)=30391/1e5
	CnvT(1)= 40*0.5;	CnvA(1)= 1893/1e5
	CnvT(2)= 51*0.5;	CnvA(2)= 8109/1e5
	CnvT(3)= 60*0.5;	CnvA(3)= 40192/1e5
	CnvT(4)= 116*0.5;	CnvA(4)= 19415/1e5
!-------------------------------------------
	InShapeOn 0, CnvT, CnvA
RET

NONGANTRY_SETTINGS:
	MFLAGS(Axis_X1).#HOME = 0

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

	VEL (Axis_X1) = 100
	ACC (Axis_X1) = 6000
	DEC (Axis_X1) = 6000
	KDEC(Axis_X1) = 10000
	JERK (Axis_X1) = 200000
RET

NONGANTRY_PARAMTERS:
	SLSBORD(Axis_X1) =0

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

	SLPKP(Axis_X1) = 23.5
	SLVKP(Axis_X1) = 243
	SLVKI(Axis_X1) = 154
	SLVSOF(Axis_X1) = 150
	SLVSOFD(Axis_X1) = 0.707

	MFLAGS(Axis_X1).14 = 0
	MFLAGS(Axis_X1).16 = 0
	MFLAGS(Axis_X1).26 = 0

	XCURV(Axis_X1) = 20
	XCURI(Axis_X1) = 15

	SLAFF(Axis_X1) = 366.01

!X2
	MFLAGS(Axis_X2).25 = 0
	MFLAGS(Axis_X2).1 = 0

	SLPKP(Axis_X2) = 23.5
	SLVKP(Axis_X2) = 243
	SLVKI(Axis_X2) = 154
	SLVSOF(Axis_X2) = 150
	SLVSOFD(Axis_X2) = 0.707

	MFLAGS(Axis_X2).14 = 0
	MFLAGS(Axis_X2).16 = 0
	MFLAGS(Axis_X2).26 = 0

	XCURV(Axis_X2) = 20
	XCURI(Axis_X2) = 15

	SLAFF(Axis_X2) = 366.01
wait 100
RET

GANTRY_PARAMETERS:
	MFLAGS(Axis_X1).25 = 1
	MFLAGS(Axis_X2).25 = 1

	MFLAGS(Axis_X2).1 = 1

!X1

	SLPKP(Axis_X1) = 250
	SLVKP(Axis_X1) = 250
	SLVKI(Axis_X1) = 0
	SLVSOF(Axis_X1) = 2500
	SLVSOFD(Axis_X1) = 0.8
	MFLAGS(Axis_X1).15 = 1

!Velocity notch filter
	MFLAGS(Axis_X1).14 = 1
	SLVNATT(Axis_X1) = 0.5
	SLVNFRQ(Axis_X1) = 17
	SLVNWID(Axis_X1) = 10

!Velocity BQF 1 
	MFLAGS(Axis_X1).16 = 0
	SLVB0DD(Axis_X1) = 0.8
	SLVB0DF(Axis_X1) = 180
	SLVB0ND(Axis_X1) = 0.3
	SLVB0NF(Axis_X1) = 300

!Velocity BQF 2
	MFLAGS(Axis_X1).26 = 0
	SLVB1DD(Axis_X1) = 0.4
	SLVB1DF(Axis_X1) = 60
	SLVB1ND(Axis_X1) = 0.6
	SLVB1NF(Axis_X1) = 60

!Current limit restoration 
	XCURV(Axis_X1) = 100
	XCURI(Axis_X1) = 50

!X2

	SLPKP(Axis_X2) = 30
	SLVKP(Axis_X2) = 100
	SLVKI(Axis_X2) = 100
	SLVSOF(Axis_X2) = 200
	SLVSOFD(Axis_X2) = 0.707

	MFLAGS(Axis_X2).14 = 0
	MFLAGS(Axis_X2).16 = 0
	MFLAGS(Axis_X2).26 = 0


	XCURV(Axis_X2) = 20
	XCURI(Axis_X2) = 10

	SLAFF(0) = 170
	SLSBORD(Axis_X1) = 1
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
	SET FPOS(Axis_X2)=FPOS(Axis_X2)-IND(Axis_X2) + 85

	disable Axis_X2
RET


