AUTOEXEC:

int Z_ECOffset_DigitalInput

Z_ECOffset_DigitalInput = ECGETOFFSET ("Digital inputs" , 1)
ecin(Z_ECOffset_DigitalInput, Z_Digital_Input)

int WAGO_IO_Offset
WAGO_IO_Offset = ECGETOFFSET ("Channel 1 Data" , 5)


!ECUNMAPOUT(396)
!ECUNMAPOUT(397)
!ECUNMAPOUT(398)
!ECUNMAPOUT(399)
!
!ECUNMAPIN(396)
!ECUNMAPIN(397)
!ECUNMAPIN(398)
!ECUNMAPIN(399)

!Outputs

ECOUT/b((WAGO_IO_Offset*8),ResetButtonLight_Bit)!0 0.0
ECOUT/b((WAGO_IO_Offset*8)+1,StartButtonLight_Bit)!0 0.1
ECOUT/b((WAGO_IO_Offset*8)+2,StopButtonLight_Bit)!0 0.2
!ECOUT/b((WAGO_IO_Offset*8)+3,Spare)!0 0.3
ECOUT/b((WAGO_IO_Offset*8)+4,TowerLightRed_Bit)!0 0.4
ECOUT/b((WAGO_IO_Offset*8)+5,TowerLightYellow_Bit)!0 0.5
ECOUT/b((WAGO_IO_Offset*8)+6,TowerLightGreen_Bit)!0 0.6
ECOUT/b((WAGO_IO_Offset*8)+7,TowerLightBlue_Bit)!0 0.7

ECOUT/b((WAGO_IO_Offset*8)+8,TowerLightBuzzer_Bit)!0 1.0
ECOUT/b((WAGO_IO_Offset*8)+9,SensorPowerOnOff_Bit)!0 1.1
ECOUT/b((WAGO_IO_Offset*8)+10,StopSensor_Bit)!0 1.2
ECOUT/b((WAGO_IO_Offset*8)+11,SmemaUpStreamMachineReady_Bit)!0 1.3
ECOUT/b((WAGO_IO_Offset*8)+12,DownStreamBoardAvailable_Bit)!0 1.4
ECOUT/b((WAGO_IO_Offset*8)+13,SmemaDownStreamFailedBoardAvailable_Bit)!0 1.5
ECOUT/b((WAGO_IO_Offset*8)+14,CustomerDO1Signal_Bit)!0 1.6
ECOUT/b((WAGO_IO_Offset*8)+15,CustomerDO2Signal_Bit)!0 1.7

ECOUT/b((WAGO_IO_Offset*8)+16,ClampPanel_Bit)!0 2.0
ECOUT/b((WAGO_IO_Offset*8)+17,LockStopper_Bit)!0 2.1
ECOUT/b((WAGO_IO_Offset*8)+18,RaiseBoardStopStopper_Bit)!0 2.2
ECOUT/b((WAGO_IO_Offset*8)+19,BeltShroudVaccumON_Bit)!0 2.3
ECOUT/b((WAGO_IO_Offset*8)+20,VacuumChuckEjector_Bit)!0 2.4
ECOUT/b((WAGO_IO_Offset*8)+21,VacuumChuckGeneratorOnOff_Bit)!0 2.5 Option
ECOUT/b((WAGO_IO_Offset*8)+22,VacuumReleaseChuckOnOff_Bit)!0 2.6 Option
ECOUT/b((WAGO_IO_Offset*8)+23,HighVacummValve)!0 2.7

!ECOUT/b((WAGO_IO_Offset*8)+24,Spare)!0 3.0
!ECOUT/b((WAGO_IO_Offset*8)+25,Spare)!0 3.1
!ECOUT/b((WAGO_IO_Offset*8)+26,Spare)!0 3.2
!ECOUT/b((WAGO_IO_Offset*8)+27,Spare)!0 3.3
!ECOUT/b((WAGO_IO_Offset*8)+28,Spare)!0 3.4
!ECOUT/b((WAGO_IO_Offset*8)+29,Spare)!0 3.5
!ECOUT/b((WAGO_IO_Offset*8)+30,Spare)!0 3.6
!ECOUT/b((WAGO_IO_Offset*8)+31,Spare)!0 3.7




!Inputs

ECIN/b((WAGO_IO_Offset*8),Estop_R_Bit)!I 0.0 OK
ECIN/b((WAGO_IO_Offset*8)+1,Estop_L_Bit)!I 0.1 OK
ECIN/b((WAGO_IO_Offset*8)+2,EstopAndDoorOpenFeedback_Bit)!I 0.2
ECIN/b((WAGO_IO_Offset*8)+3,Reset_Button_Bit)!I 0.3 OK
ECIN/b((WAGO_IO_Offset*8)+4,Start_Button_Bit)!I 0.4 OK
ECIN/b((WAGO_IO_Offset*8)+5,Stop_Button_Bit)!I 0.5 OK
ECIN/b((WAGO_IO_Offset*8)+6,AlarmCancelPushButton_Bit)!I 0.6 OK
ECIN/b((WAGO_IO_Offset*8)+7,ByPassR2L)!I 0.7


ECIN/b((WAGO_IO_Offset*8)+8,MainPressureSwitchFeedback_Bit)!I 1.0
ECIN/b((WAGO_IO_Offset*8)+9,ByPassL2R)!I 1.1
!ECIN/b((WAGO_IO_Offset*8)+10,TwentyFourVoltPowerSuppyAndFuse_Bit)!I 1.2
!ECIN/b((WAGO_IO_Offset*8)+11,BeltShroudManifoldPressureSwitchFeedback_Bit)!I 1.3
ECIN/b((WAGO_IO_Offset*8)+12,UpstreamBoardAvailableSignal_Bit)!I 1.4
ECIN/b((WAGO_IO_Offset*8)+13,UpstreamFailedBoardAvailableSignal_Bit)!I 1.5
ECIN/b((WAGO_IO_Offset*8)+14,DownstreamMachineReadySignal_Bit)!I 1.6
ECIN/b((WAGO_IO_Offset*8)+15,CustomerDISignal_Bit)!I 1.7


ECIN/b((WAGO_IO_Offset*8)+16,EntryOpto_Bit)!I 2.0
ECIN/b((WAGO_IO_Offset*8)+17,ExitOpto_Bit)!I 2.1
ECIN/b((WAGO_IO_Offset*8)+18,LifterLowered_Bit)!I 2.2
ECIN/b((WAGO_IO_Offset*8)+19,BoardStopPanelAlignSensor_Bit)!I 2.3
ECIN/b((WAGO_IO_Offset*8)+20,StopperArmUp_Bit)!I 2.4
ECIN/b((WAGO_IO_Offset*8)+21,StopperArmDown_Bit)!I 2.5
ECIN/b((WAGO_IO_Offset*8)+22,RearClampUp_Bit)!I 2.6
ECIN/b((WAGO_IO_Offset*8)+23,RearClampDown_Bit)!I 2.7


ECIN/b((WAGO_IO_Offset*8)+24,FrontClampUp_Bit)!I 3.0
ECIN/b((WAGO_IO_Offset*8)+25,FrontClampDown_Bit)!I 3.1
ECIN/b((WAGO_IO_Offset*8)+26,Width_LL)!I 3.2
ECIN/b((WAGO_IO_Offset*8)+27,Width_RL)!I 3.3
ECIN/b((WAGO_IO_Offset*8)+28,StopperLocked_Bit)!I 3.4
ECIN/b((WAGO_IO_Offset*8)+29,StopperUnlocked_Bit)!I 3.5
ECIN/b((WAGO_IO_Offset*8)+30,ConveyorPressureSwitchFeedback_Bit)!I 3.6
!ECIN/b((WAGO_IO_Offset*8)+31,Spare)!I 3.7

VacuumChuckEjector_Bit = 1
BeltShroudVaccumON_Bit = 1
SensorPowerOnOff_Bit = 1
!ResetButtonLight_Bit = 1

IF EstopAndDoorOpenFeedback_Bit = 0
ResetButtonLight_Bit = 1
END
IF EstopAndDoorOpenFeedback_Bit = 1
ResetButtonLight_Bit = 0
END

stop

! Z left limit
ON Z_Digital_Input.17 = 0
SAFINI(4).#LL=0
RET
ON Z_Digital_Input.17 = 1
SAFINI(4).#LL= 1
RET

! Z right limit
ON Z_Digital_Input.16 = 0
SAFINI(4).#RL=0
RET
ON Z_Digital_Input.16 = 1
SAFINI(4).#RL= 1
RET

! Width left limit
ON Width_LL = 0
SAFINI(6).#LL=0
RET
ON Width_LL = 1
SAFINI(6).#LL= 1
RET

! Width right limit
ON Width_RL = 0
SAFINI(6).#RL=0
RET
ON Width_RL = 1
SAFINI(6).#RL= 1
RET

! Lifter left limit
ON LifterLowered_Bit = 0
SAFINI(7).#LL=0
RET
ON LifterLowered_Bit = 1
SAFINI(7).#LL= 1
RET

ON EstopAndDoorOpenFeedback_Bit = 0
ResetButtonLight_Bit = 1
RET
ON EstopAndDoorOpenFeedback_Bit = 1
wait 2000
call z_reset
ResetButtonLight_Bit = 0
RET

ON AlarmCancelPushButton_Bit = 1
TowerLightBuzzer_Bit = 0
RET


ON TowerLightRedFlashing_Bit = 1 | TowerLightYellowFlashing_Bit = 1 | TowerLightGreenFlashing_Bit = 1 |TowerLightBlueFlashing_Bit = 1

	While TowerLightRedFlashing_Bit = 1 | TowerLightYellowFlashing_Bit = 1|  TowerLightGreenFlashing_Bit = 1 | TowerLightBlueFlashing_Bit = 1

		if(TowerLightRedFlashing_Bit = 1)
			TowerLightRed_Bit = 1
		end
		if(TowerLightYellowFlashing_Bit = 1)
			TowerLightYellow_Bit = 1
		end
		if(TowerLightGreenFlashing_Bit = 1)
			TowerLightGreen_Bit = 1
		end
		if(TowerLightBlueFlashing_Bit = 1)
			TowerLightBlue_Bit = 1
		end

		wait 500

		if(TowerLightRedFlashing_Bit = 1)
			TowerLightRed_Bit = 0
		end
		if(TowerLightYellowFlashing_Bit = 1)
			TowerLightYellow_Bit = 0
		end
		if(TowerLightGreenFlashing_Bit = 1)
			TowerLightGreen_Bit = 0
		end
		if(TowerLightBlueFlashing_Bit = 1)
			TowerLightBlue_Bit = 0
		end

		wait 500

	End

RET

ON TowerLightRedFlashing_Bit = 0
TowerLightRed_Bit = 0
RET

ON TowerLightYellowFlashing_Bit = 0
TowerLightYellow_Bit = 0
RET

ON TowerLightGreenFlashing_Bit = 0
TowerLightGreen_Bit = 0
RET

ON TowerLightBlueFlashing_Bit = 0
TowerLightBlue_Bit = 0
RET

ON ConveyorDirection = 1
CALL L2R
RET

ON ConveyorDirection = -1
CALL R2L
RET

STOP

L2R:
ecunmapin(WAGO_IO_Offset+2)
ECIN/b((WAGO_IO_Offset*8)+16,EntryOpto_Bit)!I 2.0
ECIN/b((WAGO_IO_Offset*8)+17,ExitOpto_Bit)!I 2.1

ECIN/b((WAGO_IO_Offset*8)+18,LifterLowered_Bit)!I 2.2
ECIN/b((WAGO_IO_Offset*8)+19,BoardStopPanelAlignSensor_Bit)!I 2.3
ECIN/b((WAGO_IO_Offset*8)+20,StopperArmUp_Bit)!I 2.4
ECIN/b((WAGO_IO_Offset*8)+21,StopperArmDown_Bit)!I 2.5
ECIN/b((WAGO_IO_Offset*8)+22,RearClampUp_Bit)!I 2.6
ECIN/b((WAGO_IO_Offset*8)+23,RearClampDown_Bit)!I 2.7

MFLAGS(5).12 = 0

RET

R2L:
ecunmapin(WAGO_IO_Offset+2)
ECIN/b((WAGO_IO_Offset*8)+17,EntryOpto_Bit)!I 2.0
ECIN/b((WAGO_IO_Offset*8)+16,ExitOpto_Bit)!I 2.1

ECIN/b((WAGO_IO_Offset*8)+18,LifterLowered_Bit)!I 2.2
ECIN/b((WAGO_IO_Offset*8)+19,BoardStopPanelAlignSensor_Bit)!I 2.3
ECIN/b((WAGO_IO_Offset*8)+20,StopperArmUp_Bit)!I 2.4
ECIN/b((WAGO_IO_Offset*8)+21,StopperArmDown_Bit)!I 2.5
ECIN/b((WAGO_IO_Offset*8)+22,RearClampUp_Bit)!I 2.6
ECIN/b((WAGO_IO_Offset*8)+23,RearClampDown_Bit)!I 2.7

MFLAGS(5).12 = 1

RET

z_reset:

int Axis
int Slave_Number
Axis= 4
Slave_Number = 1
int ZECOffset_ControlWord

ZECOffset_ControlWord = ECGETOFFSET ("Control Word" , Slave_Number)

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

RET