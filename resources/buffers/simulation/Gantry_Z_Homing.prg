!Gantry Z Homing

int Axis_Z

Axis_Z = 4

FMASK(Axis_Z).#RL = 1
FMASK(Axis_Z).#LL = 1

FDEF(Axis_Z).#RL = 0
FDEF(Axis_Z).#LL = 0

MFLAGS(Axis_Z).#HOME = 0

ACC (Axis_Z) = 2000
DEC (Axis_Z) = 2000
KDEC(Axis_Z) = 3000
JERK (Axis_Z) = 10000

enable Axis_Z

WAIT 5000
MFLAGS(Axis_Z).#HOME = 1
STOP

XCURV(Axis_Z) = 20
XCURI(Axis_Z) = 10

jog/v Axis_Z, -1
till FAULT(Axis_Z).#LL = 1

disable Axis_Z

wait 1000

enable Axis_Z

IST(Axis_Z).#IND=0 
jog/v Axis_Z,5
wait 100
TILL IST(Axis_Z).#IND 
kill Axis_Z
wait 100

SET FPOS(Axis_Z)=FPOS(Axis_Z)-IND(Axis_Z)

XCURV(Axis_Z) = 45
XCURI(Axis_Z) = 16

ptp/ve Axis_Z,0,100

ACC (Axis_Z) = 5000
DEC (Axis_Z) = 5000
KDEC(Axis_Z) = 10000
JERK (Axis_Z) = 50000

FMASK(Axis_Z).#RL = 1
FMASK(Axis_Z).#LL = 1

FDEF(Axis_Z).#RL = 1
FDEF(Axis_Z).#LL = 1

MFLAGS(Axis_Z).#HOME = 1

stop 
