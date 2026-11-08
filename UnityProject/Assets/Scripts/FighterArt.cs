using UnityEngine;
using static RedRivalry.PixelCanvas;
namespace RedRivalry {
public static class FighterArt {
    static PixelCanvas p;static float ox,oy,sc;static int face;static bool flash;
    static Color32 Out=C(0x0a1220),Skin=C(0xd59a72),SkinLight=C(0xf5c49a),SkinDark=C(0x996551);
    static Vector2 V(float x,float y){return new Vector2(Mathf.Round(ox+x*face*sc),Mathf.Round(oy+y*sc));}
    static Color32 Col(uint c){return flash?C(0xfff3ce):C(c);}
    static void R(float x,float y,float w,float h,uint c){var a=V(face==1?x:x+w,y);p.Rect((int)a.x,(int)a.y,Mathf.Max(1,Mathf.RoundToInt(w*sc)),Mathf.Max(1,Mathf.RoundToInt(h*sc)),Col(c));}
    static void L(float x,float y,float xx,float yy,uint c,int width=1){var a=V(x,y);var b=V(xx,yy);p.Line((int)a.x,(int)a.y,(int)b.x,(int)b.y,Col(c),Mathf.Max(1,(int)(width*sc)));}
    static void E(float x,float y,float rx,float ry,uint c){var a=V(x,y);p.Ellipse((int)a.x,(int)a.y,(int)(rx*sc),(int)(ry*sc),Col(c));}
    static void Poly(uint c,params Vector2[] pts){for(int i=0;i<pts.Length;i++)pts[i]=V(pts[i].x,pts[i].y);p.Poly(Col(c),pts);}
    static Vector2 Pt(float x,float y){return new Vector2(x,y);}
    static void Limb(Vector2 a,Vector2 b,uint baseC,uint hi,int width){L(a.x,a.y,b.x,b.y,0x0a1220,width+3);L(a.x,a.y,b.x,b.y,baseC,width);L(a.x-1,a.y,b.x-1,b.y,hi,2);}
    public static void Draw(PixelCanvas canvas,Fighter f,float time,float scale=1.35f,bool ghost=false,float ground=300){
        if(SpriteArt.Draw(canvas,f,time,scale,ground,ghost))return;
        p=canvas;ox=f.x;oy=ground-f.height;sc=scale;face=f.facing;flash=f.flash>0;
        bool len=f.kind==0;uint cloth=len?0x343e55u:0x68735bu,light=len?0x626e85u:0xa1a17bu,dark=len?0x202c42u:0x3b4a43u;
        float bob=Mathf.Sin(time*3.8f+f.slot)*1.2f,walk=f.walking?Mathf.Sin(f.walkPhase):0;
        float crouch=f.crouching?15:0,lean=0;
        Vector2 leftFoot=Pt(-11+walk*8,0),rightFoot=Pt(15-walk*8,-Mathf.Max(0,walk)*5);
        Vector2 hip=Pt(0,-38+crouch),shoulder=Pt(0,-62+crouch+bob),head=Pt(3,-78+crouch+bob);
        Vector2 rearHand=Pt(-14,-43+crouch+bob),hand=Pt(22,-45+crouch+bob);
        float weaponAngle=-1.4f;
        if(f.height>0){leftFoot=Pt(-15,-5);rightFoot=Pt(13,-12);}
        if(f.state==ActionState.Light||f.state==ActionState.Heavy||f.state==ActionState.Special||f.state==ActionState.Skill||f.state==ActionState.Ultimate){
            float a=f.AttackProgress;float swing=Mathf.Sin(Mathf.Clamp01(a/.7f)*Mathf.PI);
            if(f.state==ActionState.Heavy){lean=swing*7;hand=Pt(12+31*swing,-69+23*swing);weaponAngle=-2.4f+swing*3.5f;}
            else if(f.state==ActionState.Special||f.state==ActionState.Skill){hand=Pt(22+15*swing,-53);weaponAngle=-2.2f+swing*1.8f;lean=swing*5;}
            else if(f.state==ActionState.Ultimate){hand=Pt(20+25*swing,-60+swing*10);weaponAngle=-2+Mathf.Sin(a*18)*2;lean=swing*5;}
            else {hand=Pt(21+26*swing,-54+swing*3);weaponAngle=-1.2f+swing*1.9f;lean=swing*5;}
        }
        if(f.blocking){hand=Pt(15,-69+crouch);rearHand=Pt(8,-63+crouch);weaponAngle=-1.75f;lean=-3;}
        if(f.state==ActionState.Hurt){lean=-7;head.x-=5;hand=Pt(9,-39);}
        if(f.state==ActionState.KO){
            float t=Mathf.Clamp01(f.stateTime*2);oy+=5;sc*=1-.08f*t;
            // Fall transforms the silhouette into a readable prone pose.
            if(t>.65f){L(-28,-6,25,-6,dark,17);L(-34,-7,-12,-7,cloth,10);E(32,-10,11,9,0xd59a72);R(28,-17,10,4,len?0xc6987bu:0x27323bu);L(-35,-2,-18,-2,0x111b2a,7);return;}
            lean=-15*t;head.x-=10*t;shoulder.x-=12*t;
        }
        if(f.state==ActionState.Victory){hand=Pt(21,-89+Mathf.Sin(time*4)*2);weaponAngle=-1.4f;rearHand=Pt(-18,-62);}
        shoulder.x+=lean;head.x+=lean;
        // Back arm, trousers, shoes, then torso: consistent hand-painted light direction.
        Limb(Pt(-9+lean,-59+crouch+bob),Pt(-17,-51+crouch),dark,cloth,9);Limb(Pt(-17,-51+crouch),rearHand,dark,cloth,8);E(rearHand.x,rearHand.y,4,4,0xb57c5e);
        Limb(Pt(-6,-37+crouch),Pt(-10+walk*4,-20+crouch*.45f),dark,cloth,11);Limb(Pt(-10+walk*4,-20+crouch*.45f),leftFoot,dark,cloth,9);
        Limb(Pt(7,-37+crouch),Pt(11-walk*4,-19+crouch*.45f),cloth,light,11);Limb(Pt(11-walk*4,-19+crouch*.45f),rightFoot,cloth,light,9);
        R(leftFoot.x-6,leftFoot.y-2,18,6,0x0d1728);R(leftFoot.x-6,leftFoot.y-2,16,2,0x4d5c69);R(rightFoot.x-4,rightFoot.y-2,19,6,0x0b1422);R(rightFoot.x-3,rightFoot.y-2,15,2,0x65717b);
        Poly(0x0a1220,Pt(-14+lean,-65+crouch+bob),Pt(10+lean,-66+crouch+bob),Pt(16,-34+crouch),Pt(-14,-34+crouch));
        Poly(cloth,Pt(-12+lean,-63+crouch+bob),Pt(9+lean,-64+crouch+bob),Pt(13,-36+crouch),Pt(-12,-36+crouch));
        Poly(dark,Pt(-12+lean,-61+crouch+bob),Pt(-3+lean,-57+crouch),Pt(-3,-36+crouch),Pt(-12,-36+crouch));
        L(10+lean,-61+crouch+bob,12,-38+crouch,light,2);
        if(len){Poly(0xe6d4b5,Pt(-3+lean,-64+crouch+bob),Pt(6+lean,-64+crouch+bob),Pt(2+lean,-48+crouch));L(2+lean,-60+crouch,2+lean,-49+crouch,0xbc4a4e,3);Poly(light,Pt(-7+lean,-63+crouch),Pt(-2+lean,-51+crouch),Pt(-8+lean,-55+crouch));L(-1,-42+crouch,-1,-36+crouch,0x172337);}
        else {R(-11,-41+crouch,24,4,0x332b29);R(1,-42+crouch,5,5,0xd5b46d);R(2,-41+crouch,3,3,0x5b5345);R(-8+lean,-62+crouch,7,3,0xc99a65);R(6+lean,-62+crouch,7,3,0xc99a65);R(5+lean,-53+crouch,5,2,0xd4a75b);R(5+lean,-49+crouch,2,3,0xc15447);}
        // Front sleeve and forearm follow the attack pose.
        Vector2 elbow=Pt((hand.x+7+lean)/2+4,(hand.y-59+crouch)/2+8);
        Limb(Pt(9+lean,-59+crouch+bob),elbow,cloth,light,10);Limb(elbow,hand,cloth,light,8);R(hand.x-4,hand.y-3,7,7,0x8d5c49);R(hand.x-3,hand.y-3,6,4,0xe1a87c);
        Head(head.x,head.y,len);
        Weapon(canvas,hand.x,hand.y,weaponAngle,len,ghost);
        if(f.blocking){var a=V(30,-51+crouch);p.Arc((int)a.x,(int)a.y,26*scale,face==1?-1.1f:2.04f,face==1?1.1f:4.24f,C(0x8ce2e2),2);}
    }
    static void Head(float x,float y,bool len){
        E(x,y,11,13,0x0a1220);E(x,y-1,10,12,0xb57e60);E(x+2,y-3,8,10,0xdba078);R(x-1,y-11,7,5,0xf4c59c);R(x+6,y-6,4,10,0xeab388);R(x+10,y-2,4,4,0xf1bd91);R(x+11,y+1,2,2,0xa26e55);R(x+8,y-4,2,1,0x172032);R(x+4,y-7,6,2,0x735247);E(x-7,y+1,2,3,0xdca17d);R(x-7,y,1,2,0x8d5d4b);
        if(len){R(x-9,y-6,3,8,0x775143);R(x-8,y-8,3,3,0x8d6651);R(x+2,y+6,8,3,0x5d4037);R(x+3,y+9,5,4,0x4c3633);R(x+4,y+12,3,2,0x372e2d);R(x+8,y+4,4,2,0x67443a);R(x-2,y+6,4,3,0xa27359);}
        else {R(x-7,y-13,15,4,0x1b2630);R(x-9,y-10,18,4,0x26323b);R(x-10,y-7,4,8,0x29343a);R(x-8,y-7,2,6,0x73817d);R(x+7,y-9,3,4,0x39413f);R(x+2,y+4,11,3,0x20272d);R(x+1,y+5,4,3,0x283034);R(x+8,y+5,4,3,0x283034);R(x+4,y+9,6,1,0x865e4c);}
    }
    static void Weapon(PixelCanvas canvas,float hx,float hy,float a,bool len,bool ghost){
        float dx=Mathf.Cos(a),dy=Mathf.Sin(a),length=len?24:17,tx=hx+dx*length,ty=hy+dy*length;
        L(hx-dx*8,hy-dy*8,tx,ty,0x101926,5);L(hx-dx*8,hy-dy*8,tx,ty,0x987153,3);L(hx-dx*7-1,hy-dy*7,tx-1,ty,0xe1ac68);
        if(len){float px=-dy*10,py=dx*10;L(tx-px,ty-py,tx+px,ty+py,0x0c1725,13);L(tx-px,ty-py,tx+px,ty+py,0x6e8999,10);L(tx-px-1,ty-py-3,tx+px-1,ty+py-3,0xd1e2df,3);L(tx-px+1,ty-py+3,tx+px+1,ty+py+3,0x3b596f,3);}
        else {var c=V(tx-dy*8,ty+dx*8);float angle=face==1?a:a*-1+Mathf.PI;canvas.Arc((int)c.x,(int)c.y,15*sc,angle-2.3f,angle+.65f,Col(0x101c2b),7);canvas.Arc((int)c.x,(int)c.y,15*sc,angle-2.3f,angle+.65f,Col(0xa0bfc0),4);canvas.Arc((int)c.x,(int)c.y,16*sc,angle-2.3f,angle+.65f,Col(0xf0e7c7),1);}
    }
    public static void Portrait(PixelCanvas canvas,int kind,int x,int y,int dir){if(SpriteArt.Portrait(canvas,kind,x,y,dir))return;p=canvas;ox=x;oy=y;sc=1.65f;face=dir;flash=false;R(-13,5,27,18,kind==0?0x3b4860u:0x65755du);Head(0,0,kind==0);}
}
}
