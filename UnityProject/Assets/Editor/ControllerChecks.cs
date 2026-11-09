using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using RedRivalry;

public static class ControllerChecks {
    // Wire reports, rather than a second copy of the game's button mapping.
    [StructLayout(LayoutKind.Explicit,Size=64)] struct UsbReport:IInputStateTypeInfo {
        public FourCC format=>new FourCC('H','I','D');
        [FieldOffset(0)]public byte id;[FieldOffset(1)]public byte lx;[FieldOffset(2)]public byte ly;[FieldOffset(3)]public byte rx;[FieldOffset(4)]public byte ry;
        [FieldOffset(5)]public byte buttons;[FieldOffset(6)]public byte shoulders;[FieldOffset(8)]public byte lt;[FieldOffset(9)]public byte rt;
        public static UsbReport Neutral=>new UsbReport{id=1,lx=128,ly=128,rx=128,ry=128,buttons=8};
    }
    [StructLayout(LayoutKind.Explicit,Size=78)] struct BluetoothReport:IInputStateTypeInfo {
        public FourCC format=>new FourCC('H','I','D');
        [FieldOffset(0)]public byte id;[FieldOffset(1)]public byte flags;[FieldOffset(3)]public byte lx;[FieldOffset(4)]public byte ly;[FieldOffset(5)]public byte rx;[FieldOffset(6)]public byte ry;
        [FieldOffset(7)]public byte buttons;[FieldOffset(8)]public byte shoulders;[FieldOffset(10)]public byte lt;[FieldOffset(11)]public byte rt;
    }
    static void Send<T>(Gamepad pad,T report,Controls controls)where T:struct,IInputStateTypeInfo{InputSystem.QueueStateEvent(pad,report);InputSystem.Update();controls.Poll();}
    public static void Run(Action<bool,string> check){
        var physical=new List<Gamepad>();foreach(var pad in Gamepad.all)if(pad.enabled)physical.Add(pad);
        var mode=InputSystem.settings.updateMode;Gamepad ds=null,xbox=null;
        try{
            foreach(var pad in physical)InputSystem.DisableDevice(pad);
            InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
            ds=InputSystem.AddDevice<DualShock4GamepadHID>();var controls=new Controls();var neutral=UsbReport.Neutral;Send(ds,neutral,controls);
            check(controls.IsConnected(0)&&controls.PlayStation(0)&&!controls.IsConnected(1),"native DualShock 4 occupies exactly one player slot");
            check(!controls.Read(0,false).block&&!controls.Read(0,false).ultimate&&Mathf.Abs(controls.Read(0,false).move)<.01f,"neutral DualShock sticks and triggers do not drift");
            var report=neutral;report.buttons=0x28;Send(ds,report,controls);check(controls.Confirm&&controls.Read(0,false).jump,"DualShock USB cross confirms menu and jumps");
            Send(ds,report,controls);check(!controls.Confirm&&!controls.Read(0,false).jump,"held cross does not create repeated presses");
            Send(ds,neutral,controls);report=neutral;report.buttons=0x18;Send(ds,report,controls);check(controls.Read(0,false).light&&controls.LightKey(0)=="□","DualShock USB square is light attack with PlayStation hint");
            Send(ds,neutral,controls);report=neutral;report.buttons=0x88;Send(ds,report,controls);check(controls.Read(0,false).heavy,"DualShock USB triangle is heavy attack");
            Send(ds,neutral,controls);report=neutral;report.buttons=0x48;Send(ds,report,controls);check(controls.Back&&controls.Read(0,false).special,"DualShock USB circle goes back and casts special");
            Send(ds,neutral,controls);report=neutral;report.shoulders=2;Send(ds,report,controls);check(controls.Read(0,false).skill,"DualShock USB R1 activates skill");
            Send(ds,neutral,controls);report=neutral;report.shoulders=1;Send(ds,report,controls);check(controls.Read(0,false).block,"DualShock USB L1 holds block");
            Send(ds,neutral,controls);report=neutral;report.lt=180;report.rt=220;Send(ds,report,controls);check(controls.Read(0,false).block&&controls.Read(0,false).ultimate,"independent DualShock L2 and R2 triggers");
            Send(ds,report,controls);check(!controls.Read(0,false).ultimate,"held R2 does not repeat super attack");
            Send(ds,neutral,controls);report=neutral;report.shoulders=32;Send(ds,report,controls);check(controls.Pause,"DualShock Options pauses game");
            Send(ds,neutral,controls);report=neutral;report.buttons=2;Send(ds,report,controls);check(controls.Right&&controls.Read(0,false).move==1,"DualShock dpad navigates menu and moves fighter");
            Send(ds,neutral,controls);report=neutral;report.buttons=4;Send(ds,report,controls);check(controls.Down&&controls.Read(0,false).crouch,"DualShock dpad down crouches");
            Send(ds,neutral,controls);report=neutral;report.buttons=0;Send(ds,report,controls);check(controls.Up&&controls.Read(0,false).jump,"DualShock dpad up jumps");
            Send(ds,neutral,controls);report=neutral;report.lx=245;Send(ds,report,controls);check(controls.Right&&controls.Read(0,false).move>.8f,"DualShock analog stick controls menu and fighter");
            Send(ds,neutral,controls);var bt=new BluetoothReport{id=0x11,flags=0x80,lx=0,ly=128,rx=128,ry=128,buttons=0x18};Send(ds,bt,controls);check(controls.Read(0,false).light&&controls.Read(0,false).move<-.9f,"DualShock Bluetooth wire report maps square and left stick");
            Send(ds,neutral,controls);xbox=InputSystem.AddDevice<Gamepad>();Send(xbox,new GamepadState(),controls);Send(xbox,new GamepadState().WithButton(GamepadButton.X),controls);check(controls.Read(1,true).light&&!controls.Read(0,false).light,"mixed Xbox and DualShock inputs remain independent");
            Send(xbox,new GamepadState(),controls);Send(xbox,new GamepadState().WithButton(GamepadButton.South),controls);check(controls.Confirm&&controls.Read(1,true).jump&&!controls.Read(0,false).jump,"second controller can operate menus without controlling P1");
            InputSystem.RemoveDevice(ds);controls.Poll();check(controls.DisconnectedThisFrame==1&&!controls.IsConnected(0)&&controls.IsConnected(1),"disconnecting P1 keeps P2 assigned and clears stale inputs");
            ds=InputSystem.AddDevice<DualShock4GamepadHID>();report=neutral;report.buttons=0x28;Send(ds,report,controls);check(controls.IsConnected(0)&&controls.IsConnected(1)&&!controls.Read(0,false).jump,"hotplug fills free slot without phantom button press");
            Send(ds,neutral,controls);report=neutral;report.buttons=0x28;InputSystem.QueueStateEvent(ds,report);InputSystem.Update();controls.Resync();controls.Poll();check(!controls.Read(0,false).jump,"returning focus suppresses held button edges");
            controls.StopRumble();
        }finally{
            if(ds!=null&&ds.added)InputSystem.RemoveDevice(ds);if(xbox!=null&&xbox.added)InputSystem.RemoveDevice(xbox);
            foreach(var pad in physical)if(pad.added)InputSystem.EnableDevice(pad);InputSystem.settings.updateMode=mode;
        }
    }
}
