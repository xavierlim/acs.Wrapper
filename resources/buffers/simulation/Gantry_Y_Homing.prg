!Gantry Y homing

int Axis_Y

global real CnvTY(5), CnvAY(5) !CnvB(612)!CnvB(420)

Axis_Y = 1

MFLAGS(Axis_Y).#HOME = 0

disable Axis_Y

CALL RESTORE_TUNING_PARA
CALL INPUTSHAPING_OFF
enable Axis_Y

FMASK(Axis_Y).#RL = 1
FMASK(Axis_Y).#LL = 1

FDEF(Axis_Y).#RL = 0
FDEF(Axis_Y).#LL = 0

SLSBORD(Axis_Y) =0

ACC (Axis_Y) = 1000
DEC (Axis_Y) = 1000
KDEC(Axis_Y) = 1000
JERK (Axis_Y) = 10000

enable Axis_Y


!Simulation
WAIT 5000
ptp/ve Axis_Y,0,100
MFLAGS(Axis_Y).#HOME = 1
STOP


XCURV(Axis_Y) = 10
XCURI(Axis_Y) = 5

jog/v Axis_Y, -50
till FAULT(Axis_Y).#LL = 1
kill Axis_Y
TILL ^AST(Axis_Y).#MOVE
wait 100

IST(Axis_Y).#IND=0 
jog/v Axis_Y,20
wait 100
TILL IST(Axis_Y).#IND 
kill Axis_Y
TILL ^AST(Axis_Y).#MOVE
wait 100
SET FPOS(Axis_Y)=FPOS(Axis_Y)-IND(Axis_Y)

XCURV(Axis_Y) = 100
XCURI(Axis_Y) = 50

SLSBORD(Axis_Y) =1

ptp/ve Axis_Y,0,100

FMASK(Axis_Y).#RL = 1
FMASK(Axis_Y).#LL = 1

FDEF(Axis_Y).#RL = 1
FDEF(Axis_Y).#LL = 1

VEL (Axis_Y) = 100
ACC (Axis_Y) = 6000
DEC (Axis_Y) = 6000
KDEC(Axis_Y) = 10000
JERK (Axis_Y) = 200000

CALL INPUTSHAPING_ON
MFLAGS(Axis_Y).#HOME = 1

STOP

INPUTSHAPING_ON:
!-------------Regular convalution-----------
!-------------------------------------------
CnvTY(0)= 0*0.5;      CnvAY(0)= 27207/1e5
CnvTY(1)= 46*0.5;     CnvAY(1)= 9289/1e5
CnvTY(2)= 58*0.5;     CnvAY(2)= 41417/1e5
CnvTY(3)= 111*0.5;    CnvAY(3)= 3093/1e5
CnvTY(4)= 112*0.5;    CnvAY(4)= 18994/1e5  
!--------------------------------------------

InShapeOn 1, CnvTY, CnvAY
!InShapeOn x, CnvT1, CnvA1
RET

INPUTSHAPING_OFF:
InShapeOFF 1
RET

RESTORE_TUNING_PARA:
	SLPKP(Axis_Y) = 230
	SLVKP(Axis_Y) = 230
	SLVKI(Axis_Y) = 0
	SLVSOF(Axis_Y) = 1600
	SLVSOFD(Axis_Y) = 0.3
	MFLAGS(Axis_Y).15 = 1

!Velocity notch filter
	MFLAGS(Axis_Y).14 = 1
	SLVNATT(Axis_Y) = 4
	SLVNFRQ(Axis_Y) = 200
	SLVNWID(Axis_Y) = 50

!Velocity BQF 1 
	MFLAGS(Axis_Y).16 = 1
	SLVB0DD(Axis_Y) = 0.8
	SLVB0DF(Axis_Y) = 700
	SLVB0ND(Axis_Y) = 0.3
	SLVB0NF(Axis_Y) = 700

!Velocity BQF 2
	MFLAGS(Axis_Y).26 = 1
	SLVB1DD(Axis_Y) = 0.1
	SLVB1DF(Axis_Y) = 1000
	SLVB1ND(Axis_Y) = 0.4
	SLVB1NF(Axis_Y) = 1000
	
!Current limit restoration 
	XCURV(Axis_Y) = 100
	XCURI(Axis_Y) = 50

	SLAFF(Axis_Y) = 460
	SLSBORD(Axis_Y) = 1
RET

stop 