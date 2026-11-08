using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace RedRivalry {
public sealed class Controls {
    [StructLayout(LayoutKind.Sequential)] struct Pad { public uint packet;public ushort buttons;public byte lt,rt;public short lx,ly,rx,ry; }
    [StructLayout(LayoutKind.Sequential)] struct Vibration {public ushort left,right;}
    [DllImport("xinput1_4.dll",EntryPoint="XInputGetState")]static extern uint GetState(uint index,out Pad state);
    [DllImport("xinput1_4.dll",EntryPoint="XInputSetState")]static extern uint SetState(uint index,ref Vibration vibration);
    Pad[] pads=new Pad[4],last=new Pad[4];public bool[] connected=new bool[4];public int[] assigned={-1,-1};bool native=true;
    public bool AnyPad=>assigned[0]>=0;
    public void Poll(){
        Array.Copy(pads,last,4);for(uint i=0;i<4;i++)try{connected[i]=native&&GetState(i,out pads[i])==0;}catch{native=false;connected[i]=false;}
        assigned[0]=assigned[1]=-1;int n=0;for(int i=0;i<4&&n<2;i++)if(connected[i])assigned[n++]=i;
    }
    bool Press(int i,int mask){return i>=0&&(pads[i].buttons&mask)!=0&&(last[i].buttons&mask)==0;}
    bool Hold(int i,int mask){return i>=0&&(pads[i].buttons&mask)!=0;}
    static float Dead(short s){float v=s/32767f;return Mathf.Abs(v)<.22f?0:v;}
    public bool Confirm=>Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space)||Press(assigned[0],0x1000);
    public bool Back=>Input.GetKeyDown(KeyCode.Escape)||Press(assigned[0],0x2000);
    public bool Pause=>Input.GetKeyDown(KeyCode.Escape)||Press(assigned[0],0x10);
    public bool Up=>Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.W)||Press(assigned[0],1)||StickCross(1);
    public bool Down=>Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.S)||Press(assigned[0],2)||StickCross(-1);
    public bool Left=>Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.A)||Press(assigned[0],4)||StickX(-1);
    public bool Right=>Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.D)||Press(assigned[0],8)||StickX(1);
    bool StickCross(int sign){int i=assigned[0];return i>=0&&pads[i].ly*sign>18000&&last[i].ly*sign<=18000;}
    bool StickX(int sign){int i=assigned[0];return i>=0&&pads[i].lx*sign>18000&&last[i].lx*sign<=18000;}
    public Command Read(int slot,bool local){
        int i=assigned[slot];var c=new Command();
        if(slot==0){c.move=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);c.jump=Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.Space);c.crouch=Input.GetKey(KeyCode.S);c.light=Input.GetKeyDown(KeyCode.J)||Input.GetMouseButtonDown(0);c.heavy=Input.GetKeyDown(KeyCode.K)||Input.GetMouseButtonDown(1);c.block=Input.GetKey(KeyCode.LeftShift)||Input.GetMouseButton(2);c.special=Input.GetKeyDown(KeyCode.U)||Input.GetKeyDown(KeyCode.Q);c.skill=Input.GetKeyDown(KeyCode.I)||Input.GetKeyDown(KeyCode.E);c.ultimate=Input.GetKeyDown(KeyCode.O)||Input.GetKeyDown(KeyCode.R);}
        else if(local){c.move=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);c.jump=Input.GetKeyDown(KeyCode.UpArrow);c.crouch=Input.GetKey(KeyCode.DownArrow);c.light=Input.GetKeyDown(KeyCode.Keypad1);c.heavy=Input.GetKeyDown(KeyCode.Keypad2);c.block=Input.GetKey(KeyCode.RightShift);c.special=Input.GetKeyDown(KeyCode.Keypad4);c.skill=Input.GetKeyDown(KeyCode.Keypad5);c.ultimate=Input.GetKeyDown(KeyCode.Keypad6);}
        if(i>=0){float mx=Dead(pads[i].lx);if(Hold(i,4))mx=-1;if(Hold(i,8))mx=1;if(Mathf.Abs(mx)>.1f)c.move=mx;c.jump|=Press(i,0x1000)||Press(i,1);c.crouch|=Hold(i,2)||pads[i].ly<-16000;c.light|=Press(i,0x4000);c.heavy|=Press(i,0x8000);c.special|=Press(i,0x2000);c.skill|=Press(i,0x200);c.ultimate|=pads[i].rt>100&&last[i].rt<=100;c.block|=Hold(i,0x100)||pads[i].lt>60;}
        return c;
    }
    public void Rumble(int slot,bool heavy){int i=assigned[slot];if(i<0||!native)return;var v=new Vibration{left=(ushort)(heavy?26000:10000),right=(ushort)(heavy?32000:16000)};try{SetState((uint)i,ref v);}catch{}}
    public void StopRumble(){if(!native)return;for(uint i=0;i<4;i++){var v=new Vibration();try{SetState(i,ref v);}catch{}}}
    // Test a virtual controller state through the same mapping as physical XInput.
    public void InjectForTest(int index,ushort buttons,short lx=0){assigned[0]=index;last[index]=pads[index];pads[index].buttons=buttons;pads[index].lx=lx;connected[index]=true;}
}
}
