using System;
using UnityEngine;
namespace RedRivalry {
public enum ActionState { Idle,Light,Heavy,Special,Skill,Ultimate,Hurt,KO,Victory }
public struct Command {
    public float move;public bool jump,crouch,light,heavy,special,skill,ultimate,block;
}
[Serializable] public sealed class Fighter {
    public int slot,kind,facing=1,wins;public float x,height,vy,hp=100,displayHp=100,energy=30;
    public float stateTime,stateDuration,stun,flash,invulnerable,walkPhase,comboTime,specialCooldown,skillCooldown;
    public int combo,bookCycle;public bool walking,crouching,blocking,activated;public ActionState state;
    public float AttackProgress=>stateDuration>0?Mathf.Clamp01(stateTime/stateDuration):0;
    public bool Busy=>state!=ActionState.Idle;
    public void Set(ActionState s,float duration){state=s;stateTime=0;stateDuration=duration;activated=false;blocking=false;}
    public void Reset(float px){x=px;height=vy=0;hp=displayHp=100;energy=30;state=ActionState.Idle;stateTime=stun=flash=invulnerable=comboTime=specialCooldown=skillCooldown=0;walkPhase=0;combo=bookCycle=0;blocking=crouching=walking=activated=false;}
}
public sealed class Projectile {
    public Fighter owner;public float x,y,vx,life,delay,damage;public int type,book;public bool hit,heavy;public float age;
}
public sealed class CombatWorld {
    public static readonly string[] BookTitles={"ЧТО ДЕЛАТЬ?","ГОСУДАРСТВО И РЕВОЛЮЦИЯ","ИМПЕРИАЛИЗМ"};
    public Fighter[] fighters={new Fighter{slot=0,kind=0,facing=1},new Fighter{slot=1,kind=1,facing=-1}};
    public readonly System.Collections.Generic.List<Projectile> projectiles=new System.Collections.Generic.List<Projectile>();
    public event Action<string,Fighter> Effect;
    public float hitstop,shake;public int totalHits,blockedHits;
    public void Emit(string s,Fighter f){Effect?.Invoke(s,f);}
    public void Reset(){fighters[0].Reset(205);fighters[1].Reset(435);fighters[0].facing=1;fighters[1].facing=-1;projectiles.Clear();hitstop=shake=0;}
    public void Step(float dt,Command a,Command b){
        shake=Mathf.Max(0,shake-dt*20);if(hitstop>0){hitstop-=dt;return;}
        float previousA=fighters[0].x,previousB=fighters[1].x;
        StepFighter(fighters[0],fighters[1],a,dt);StepFighter(fighters[1],fighters[0],b,dt);
        foreach(var f in fighters)f.x=Mathf.Clamp(f.x,52,588);
        var l=fighters[0];var r=fighters[1];float gap=r.x-l.x;
        if(Mathf.Abs(gap)<37&&Mathf.Abs(l.height-r.height)<52&&l.hp>0&&r.hp>0){float center=Mathf.Clamp((l.x+r.x)*.5f,70.5f,569.5f);float sign=gap>=0?1:-1;l.x=center-18.5f*sign;r.x=center+18.5f*sign;}
        for(int i=0;i<2;i++){var f=fighters[i];float travelled=Mathf.Abs(f.x-(i==0?previousA:previousB));f.walking=f.walking&&travelled>.01f&&f.state==ActionState.Idle&&f.height==0;if(f.walking)f.walkPhase+=Mathf.Abs(i==0?a.move:b.move)*dt*12;}
        for(int i=projectiles.Count-1;i>=0;i--){
            var p=projectiles[i];if(p.delay>0){p.delay-=dt;continue;}p.life-=dt;p.age+=dt;p.x+=p.vx*dt;if(p.type==1)p.y-=6*dt;
            var target=fighters[1-p.owner.slot];
            float radius=p.type==1?22+p.age*14:24;
            bool vertical=Mathf.Abs((300-target.height+(target.crouching?14:0)-92)-p.y)<(p.type==1?42+p.age*14:44);
            if(!p.hit&&Mathf.Abs(target.x-p.x)<radius&&vertical&&target.hp>0){float before=target.hp;Hit(p.owner,target,p.damage,p.heavy,p.x<target.x?1:-1);p.hit=true;if(p.type==0)p.life=0;if(p.type==1&&before-target.hp>2&&target.state==ActionState.Hurt){target.stateDuration=.5f;Emit("cough",target);}}
            if(p.life<=0||p.x<-60||p.x>700)projectiles.RemoveAt(i);
        }
    }
    void StepFighter(Fighter f,Fighter enemy,Command c,float dt){
        f.flash=Mathf.Max(0,f.flash-dt);f.invulnerable=Mathf.Max(0,f.invulnerable-dt);f.comboTime=Mathf.Max(0,f.comboTime-dt);f.specialCooldown=Mathf.Max(0,f.specialCooldown-dt);f.skillCooldown=Mathf.Max(0,f.skillCooldown-dt);
        f.displayHp=Mathf.MoveTowards(f.displayHp,f.hp,dt*32);if(f.comboTime==0)f.combo=0;
        f.stateTime+=dt;
        if(f.height>0||f.vy>0){f.height+=f.vy*dt;f.vy-=620*dt;if(f.height<=0){f.height=0;f.vy=0;Emit("land",f);}}
        if(f.state==ActionState.KO||f.state==ActionState.Victory)return;
        if(f.state!=ActionState.Idle){
            float active=f.state==ActionState.Light?.12f:f.state==ActionState.Heavy?.28f:f.state==ActionState.Special?.32f:.30f;
            if(!f.activated&&f.stateTime>=active){f.activated=true;Activate(f,enemy);}
            if(f.stateTime>=f.stateDuration)f.Set(ActionState.Idle,0);
        }
        f.walking=false;
        if(f.state!=ActionState.Idle)return;
        f.facing=enemy.x>=f.x?1:-1;f.crouching=c.crouch&&f.height==0;f.blocking=c.block&&f.height==0;
        f.energy=Mathf.Min(100,f.energy+dt*2.3f);
        if(c.ultimate&&f.energy>=100){f.energy=0;f.Set(ActionState.Ultimate,1.20f);f.invulnerable=.45f;Emit("ultimate",f);return;}
        if(c.special&&f.specialCooldown<=0&&f.energy>=20){f.energy-=20;f.specialCooldown=1.3f;f.Set(ActionState.Special,.80f);Emit("cast",f);return;}
        if(c.skill&&f.skillCooldown<=0&&f.energy>=30){f.energy-=30;f.skillCooldown=2.8f;f.Set(ActionState.Skill,.75f);if(f.kind==1)f.invulnerable=.95f;Emit("skill",f);return;}
        if(c.heavy){f.Set(ActionState.Heavy,.68f);Emit("swing",f);return;}
        if(c.light){f.Set(ActionState.Light,.32f);Emit("swing",f);return;}
        if(c.jump&&f.height==0&&!f.blocking){f.vy=280;f.crouching=false;Emit("jump",f);}
        if(!f.blocking&&!f.crouching){f.x+=c.move*(f.height>0?106:122)*dt;f.walking=Mathf.Abs(c.move)>.1f;}
    }
    void Activate(Fighter f,Fighter enemy){
        if(f.state==ActionState.Hurt)return;
        if(f.state==ActionState.Special){
            if(f.kind==0)ThrowBook(f,13,0,0);else projectiles.Add(new Projectile{owner=f,x=f.x+f.facing*22,y=300-f.height-110,vx=f.facing*155,life=1.5f,type=1,damage=11});
            Emit(f.kind==0?"book":"smoke",f);return;
        }
        if(f.state==ActionState.Skill){
            if(f.kind==0){projectiles.Add(new Projectile{owner=f,x=f.x+f.facing*55,y=300-f.height-80,life=.48f,type=2,hit=true,book=f.bookCycle++%3});if((enemy.x-f.x)*f.facing>0&&Mathf.Abs(f.x-enemy.x)<145&&Mathf.Abs(f.height-enemy.height)<60){Hit(f,enemy,17,false,f.facing);if(enemy.state==ActionState.Hurt)enemy.stateDuration=.55f;}Emit("argument",f);}
            else {projectiles.Add(new Projectile{owner=f,x=f.x,y=300-f.height-65,life=1.1f,type=3,hit=true});Emit("smoke",f);}
            return;
        }
        if(f.state==ActionState.Ultimate){
            if(f.kind==0){for(int i=0;i<3;i++)ThrowBook(f,12,i*.18f,(i-1)*9);Emit("library",f);}
            else {projectiles.Add(new Projectile{owner=f,x=f.x+f.facing*25,y=300-f.height-100,vx=f.facing*180,life=1.8f,type=1,damage=34,heavy=true});Emit("smoke",f);}
            return;
        }
        bool heavy=f.state==ActionState.Heavy,ult=f.state==ActionState.Ultimate;
        float range=ult?175:heavy?94:74;
        if((enemy.x-f.x)*f.facing>-7&&Mathf.Abs(f.x-enemy.x)<range&&Mathf.Abs(f.height-enemy.height)<(ult?100:60))Hit(f,enemy,ult?34:heavy?16:8,heavy||ult,f.facing);
        if(ult)Emit("burst",f);
    }
    void ThrowBook(Fighter f,float damage,float delay,float dy){projectiles.Add(new Projectile{owner=f,x=f.x+f.facing*30,y=300-f.height-87+dy,vx=f.facing*285,life=2.3f,type=0,damage=damage,delay=delay,book=f.bookCycle++%3});}
    public void Hit(Fighter attacker,Fighter victim,float damage,bool heavy,int direction){
        if(victim.hp<=0||victim.invulnerable>0)return;
        bool blocked=victim.blocking&&direction==-victim.facing;
        if(blocked){victim.hp=Mathf.Max(1,victim.hp-damage*.12f);victim.energy=Mathf.Min(100,victim.energy+6);victim.x+=direction*5;blockedHits++;hitstop=.035f;Emit("block",victim);return;}
        victim.hp=Mathf.Max(0,victim.hp-damage);victim.x+=direction*(heavy?19:9);victim.flash=.1f;victim.invulnerable=.12f;
        victim.Set(victim.hp<=0?ActionState.KO:ActionState.Hurt,heavy?.42f:.24f);victim.crouching=victim.blocking=false;
        attacker.energy=Mathf.Min(100,attacker.energy+(heavy?14:8));victim.energy=Mathf.Min(100,victim.energy+5);attacker.combo++;attacker.comboTime=1.25f;
        totalHits++;hitstop=heavy?.075f:.045f;shake=heavy?4:2;Emit(heavy?"heavyhit":"hit",victim);
    }
}
public sealed class OpponentAI {
    float timer,jumpTimer;int seed=1917;Command held;
    float Next(){seed=unchecked(seed*1664525+1013904223);return (seed&0x7fffffff)/(float)int.MaxValue;}
    public Command Think(Fighter f,Fighter e,float dt,int difficulty){
        timer-=dt;jumpTimer-=dt;Command c=held;c.light=c.heavy=c.special=c.skill=c.ultimate=c.jump=false;
        if(timer<=0){
            timer=difficulty==0?.30f:difficulty==1?.19f:.12f;float dist=Mathf.Abs(f.x-e.x),r=Next();
            c.move=dist>73?Mathf.Sign(e.x-f.x):dist<47?-Mathf.Sign(e.x-f.x)*.5f:0;
            c.block=dist<110&&e.Busy&&Next()<(difficulty==0?.2f:.52f);c.crouch=false;
            if(dist<85&&!c.block){c.light=r<.6f;c.heavy=r>=.6f;}
            if(dist>120&&r<.25f)c.special=true;
            if(dist<190&&r>.8f)c.skill=true;
            if(f.energy>=100&&dist<160)c.ultimate=true;
            if(jumpTimer<=0&&dist>90&&Next()<.12f){c.jump=true;jumpTimer=2.2f;}
            held=c;
        }
        return c;
    }
}
}
