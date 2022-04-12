!Gantry Y homing

ERRORUNMAP 1,0

int Axis_Y

global real CnvTY(5), CnvAY(5) !CnvB(612)!CnvB(420)

Axis_Y = 1

!MFLAGS(Axis_Y).#HOME = 0

disable Axis_Y
FMASK(Axis_Y).#SRL = 0
FMASK(Axis_Y).#SLL = 0

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

VEL (Axis_Y) = 1000
ACC (Axis_Y) = 6000
DEC (Axis_Y) = 6000
KDEC(Axis_Y) = 10000
JERK (Axis_Y) = 170000

CALL INPUTSHAPING_ON
WAIT 500
!START 60, 1
!TILL ^ PST(60).#RUN
MFLAGS(Axis_Y).#HOME = 1
FMASK(Axis_Y).#SRL = 1
FMASK(Axis_Y).#SLL = 1

FMASK(Axis_Y).0=1
FMASK(Axis_Y).1=1
FMASK(Axis_Y).4=1
FMASK(Axis_Y).5=1
FMASK(Axis_Y).6=1
FMASK(Axis_Y).7=0
FMASK(Axis_Y).8=0
FMASK(Axis_Y).9=1
FMASK(Axis_Y).12=0
FMASK(Axis_Y).14=1
FMASK(Axis_Y).18=1

STOP

INPUTSHAPING_ON:
!-------------Regular convalution-----------
!-------------------------------------------
CnvTY(0)= 0*0.5;      CnvAY(0)= 24813/1e5
CnvTY(1)= 17*0.5;     CnvAY(1)= 8441/1e5
CnvTY(2)= 22*0.5;     CnvAY(2)= 1639/1e5
CnvTY(3)= 27*0.5;    CnvAY(3)= 42464/1e5
CnvTY(4)= 51*0.5;    CnvAY(4)= 22643/1e5  
!--------------------------------------------

InShapeOn 1, CnvTY, CnvAY
!InShapeOn x, CnvT1, CnvA1
RET

INPUTSHAPING_OFF:
InShapeOFF 1
RET

RESTORE_TUNING_PARA:
	SLPKP(Axis_Y) = 200
	SLVKP(Axis_Y) = 180
	SLVKI(Axis_Y) = 0
	SLVSOF(Axis_Y) = 600
	SLVSOFD(Axis_Y) = 0.9
	MFLAGS(Axis_Y).15 = 0

!Velocity notch filter
	MFLAGS(Axis_Y).14 = 0
	SLVNATT(Axis_Y) = 18
	SLVNFRQ(Axis_Y) = 380
	SLVNWID(Axis_Y) = 28

!Velocity BQF 1 
	MFLAGS(Axis_Y).16 = 0
	SLVB0DD(Axis_Y) = 0.2
	SLVB0DF(Axis_Y) = 2870
	SLVB0ND(Axis_Y) = 0.03
	SLVB0NF(Axis_Y) = 2870

!Velocity BQF 2
	MFLAGS(Axis_Y).26 = 1
	SLVB1DD(Axis_Y) = 0.9
	SLVB1DF(Axis_Y) = 435
	SLVB1ND(Axis_Y) = 0.4
	SLVB1NF(Axis_Y) = 435
	
!Current limit restoration 
	XCURV(Axis_Y) = 100
	XCURI(Axis_Y) = 50

	SLAFF(Axis_Y) = 220
	SLSBORD(Axis_Y) = 1
	SLSBBW(Axis_Y) = 30
	SLSBF(Axis_Y) = 900
RET

stop 