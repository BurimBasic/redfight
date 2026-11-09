using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
namespace RedRivalry {
public sealed class Controls {
    [Flags] public enum Buttons { Up=1,Down=2,Left=4,Right=8,Start=16,Block=256,Skill=512,Jump=4096,Special=8192,Light=16384,Heavy=32768 }
    struct Sample {public Buttons buttons;public Vector2 stick,rightStick;public float lt,rt;}
    readonly Gamepad[] devices=new Gamepad[2];
    readonly Sample[] current=new Sample[2],previous=new Sample[2];
    bool resync;int menuSlot;
    public int DisconnectedThisFrame {get;private set;}
    public bool AnyPad=>IsConnected(0)||IsConnected(1);
    public bool IsConnected(int slot)=>devices[slot]!=null&&devices[slot].added&&devices[slot].enabled;
    public bool PlayStation(int slot)=>devices[slot] is DualShockGamepad;
    public bool MenuPlayStation=>PlayStation(menuSlot);
    public int MenuSlot=>menuSlot;
    public string ConfirmKey=>MenuPlayStation?"×":"A";
    public string BackKey=>MenuPlayStation?"○":"B";
    public string LightKey(int slot)=>PlayStation(slot)?"□":"X";
    public string HeavyKey(int slot)=>PlayStation(slot)?"△":"Y";
    public string SpecialKey(int slot)=>PlayStation(slot)?"○":"B";
    public string SkillKey(int slot)=>PlayStation(slot)?"R1":"RB";
    public string UltimateKey(int slot)=>PlayStation(slot)?"R2":"RT";
    public string BlockKey(int slot)=>PlayStation(slot)?"L1 / L2":"LB / LT";
    public string PauseKey=>MenuPlayStation?"OPTIONS":"START";
    public string DeviceName(int slot){if(!IsConnected(slot))return "НЕ ПОДКЛЮЧЁН";return PlayStation(slot)?(devices[slot] is DualSenseGamepadHID?"DUALSENSE":"DUALSHOCK 4"):"ГЕЙМПАД XBOX";}
    public Vector2 Stick(int slot)=>current[slot].stick;
    public Vector2 RightStick(int slot)=>current[slot].rightStick;
    public float Trigger(int slot,bool right)=>right?current[slot].rt:current[slot].lt;
    public bool Held(int slot,Buttons button)=>(current[slot].buttons&button)!=0;
    public bool Pressed(int slot,Buttons button)=>Held(slot,button)&&(previous[slot].buttons&button)==0;
    public void Resync(){resync=true;StopRumble();}
    public void Poll(){
        DisconnectedThisFrame=0;
        for(int slot=0;slot<2;slot++){
            previous[slot]=current[slot];
            if(devices[slot]!=null&&!IsConnected(slot)){Stop(devices[slot]);devices[slot]=null;current[slot]=previous[slot]=default;DisconnectedThisFrame|=1<<slot;}
        }
        // Keep P2 in place when P1 disconnects. One device can never fill both slots.
        foreach(var pad in Gamepad.all){
            if(!pad.enabled||ReferenceEquals(pad,devices[0])||ReferenceEquals(pad,devices[1]))continue;
            for(int slot=0;slot<2;slot++)if(devices[slot]==null){devices[slot]=pad;current[slot]=previous[slot]=Capture(pad);SetLight(slot);Debug.Log("CONTROLLER_CONNECTED P"+(slot+1)+" "+pad.layout);break;}
        }
        for(int slot=0;slot<2;slot++){
            current[slot]=IsConnected(slot)?Capture(devices[slot]):default;
            if(resync)previous[slot]=current[slot];
            if(current[slot].buttons!=previous[slot].buttons||current[slot].rt>.4f&&previous[slot].rt<=.4f||(current[slot].stick-previous[slot].stick).sqrMagnitude>.04f)menuSlot=slot;
        }
        resync=false;
        if(!IsConnected(menuSlot))menuSlot=IsConnected(0)?0:1;
    }
    static Sample Capture(Gamepad pad){
        var s=new Sample{stick=pad.leftStick.ReadValue(),rightStick=pad.rightStick.ReadValue(),lt=pad.leftTrigger.ReadValue(),rt=pad.rightTrigger.ReadValue()};
        if(pad.dpad.up.isPressed)s.buttons|=Buttons.Up;if(pad.dpad.down.isPressed)s.buttons|=Buttons.Down;if(pad.dpad.left.isPressed)s.buttons|=Buttons.Left;if(pad.dpad.right.isPressed)s.buttons|=Buttons.Right;
        if(pad.buttonSouth.isPressed)s.buttons|=Buttons.Jump;if(pad.buttonEast.isPressed)s.buttons|=Buttons.Special;if(pad.buttonWest.isPressed)s.buttons|=Buttons.Light;if(pad.buttonNorth.isPressed)s.buttons|=Buttons.Heavy;
        if(pad.leftShoulder.isPressed)s.buttons|=Buttons.Block;if(pad.rightShoulder.isPressed)s.buttons|=Buttons.Skill;if(pad.startButton.isPressed)s.buttons|=Buttons.Start;return s;
    }
    bool MenuPress(Buttons b)=>Pressed(0,b)||Pressed(1,b);
    bool MenuStick(bool horizontal,int sign){for(int i=0;i<2;i++){float a=horizontal?current[i].stick.x:current[i].stick.y,b=horizontal?previous[i].stick.x:previous[i].stick.y;if(a*sign>.55f&&b*sign<=.55f)return true;}return false;}
    public bool Confirm=>Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space)||MenuPress(Buttons.Jump);
    public bool Back=>Input.GetKeyDown(KeyCode.Escape)||MenuPress(Buttons.Special);
    public bool Pause=>Input.GetKeyDown(KeyCode.Escape)||MenuPress(Buttons.Start);
    public bool Up=>Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.W)||MenuPress(Buttons.Up)||MenuStick(false,1);
    public bool Down=>Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.S)||MenuPress(Buttons.Down)||MenuStick(false,-1);
    public bool Left=>Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.A)||MenuPress(Buttons.Left)||MenuStick(true,-1);
    public bool Right=>Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.D)||MenuPress(Buttons.Right)||MenuStick(true,1);
    public Command Read(int slot,bool local){
        var c=new Command();
        if(slot==0){c.move=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);c.jump=Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.Space);c.crouch=Input.GetKey(KeyCode.S);c.light=Input.GetKeyDown(KeyCode.J)||Input.GetMouseButtonDown(0);c.heavy=Input.GetKeyDown(KeyCode.K)||Input.GetMouseButtonDown(1);c.block=Input.GetKey(KeyCode.LeftShift)||Input.GetMouseButton(2);c.special=Input.GetKeyDown(KeyCode.U)||Input.GetKeyDown(KeyCode.Q);c.skill=Input.GetKeyDown(KeyCode.I)||Input.GetKeyDown(KeyCode.E);c.ultimate=Input.GetKeyDown(KeyCode.O)||Input.GetKeyDown(KeyCode.R);}
        else if(local){c.move=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);c.jump=Input.GetKeyDown(KeyCode.UpArrow);c.crouch=Input.GetKey(KeyCode.DownArrow);c.light=Input.GetKeyDown(KeyCode.Keypad1);c.heavy=Input.GetKeyDown(KeyCode.Keypad2);c.block=Input.GetKey(KeyCode.RightShift);c.special=Input.GetKeyDown(KeyCode.Keypad4);c.skill=Input.GetKeyDown(KeyCode.Keypad5);c.ultimate=Input.GetKeyDown(KeyCode.Keypad6);}
        if(IsConnected(slot)){
            float mx=Mathf.Abs(current[slot].stick.x)<.1f?0:current[slot].stick.x;if(Held(slot,Buttons.Left))mx=-1;if(Held(slot,Buttons.Right))mx=1;if(Mathf.Abs(mx)>.1f)c.move=mx;
            c.jump|=Pressed(slot,Buttons.Jump)||Pressed(slot,Buttons.Up);c.crouch|=Held(slot,Buttons.Down)||current[slot].stick.y<-.5f;c.light|=Pressed(slot,Buttons.Light);c.heavy|=Pressed(slot,Buttons.Heavy);c.special|=Pressed(slot,Buttons.Special);c.skill|=Pressed(slot,Buttons.Skill);c.ultimate|=current[slot].rt>.4f&&previous[slot].rt<=.4f;c.block|=Held(slot,Buttons.Block)||current[slot].lt>.24f;
        }
        return c;
    }
    void SetLight(int slot){try{if(devices[slot] is DualShockGamepad ps)ps.SetLightBarColor(slot==0?new Color(.15f,.4f,1):new Color(1,.15f,.2f));}catch(Exception e){Debug.LogWarning("Controller light: "+e.Message);}}
    public void Rumble(int slot,bool heavy){if(!IsConnected(slot))return;try{devices[slot].SetMotorSpeeds(heavy?.6f:.25f,heavy?.8f:.4f);}catch(Exception e){Debug.LogWarning("Controller vibration: "+e.Message);}}
    static void Stop(Gamepad pad){try{pad.ResetHaptics();}catch{}}
    public void StopRumble(){foreach(var pad in devices)if(pad!=null)Stop(pad);}
}
}
