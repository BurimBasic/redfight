using UnityEngine;
using static RedRivalry.PixelCanvas;
namespace RedRivalry {
// Animated scenery is painted before fighters, HUD and combat effects.
public sealed class ArenaMotion {
    readonly PixelCanvas p;readonly Color32[] train;readonly int trainW,trainH;
    public bool TrainLoaded=>train!=null;
    public ArenaMotion(PixelCanvas canvas){
        p=canvas;var texture=Resources.Load<Texture2D>("Art/MetroTrain");if(texture==null)return;
        var raw=texture.GetPixels32();int l=texture.width,r=-1,b=texture.height,t=-1;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(raw[y*texture.width+x].a>32){l=Mathf.Min(l,x);r=Mathf.Max(r,x);b=Mathf.Min(b,y);t=Mathf.Max(t,y);}
        if(r<l||t<b)return;trainW=r-l+1;trainH=t-b+1;train=new Color32[trainW*trainH];for(int y=0;y<trainH;y++)for(int x=0;x<trainW;x++)train[y*trainW+x]=raw[(t-y)*texture.width+l+x];
    }
    public void Draw(int index,float time){
        if(index==0){Glint(66,192,time,0);Glint(595,191,time,2);}
        else if(index==1)Furnace(time);
        else if(index==2)Metro(time);
        else if(index==3)Railway(time);
        else if(index==4)Harbor(time);
        else Rooftop(time);
    }
    void Blend(int x,int y,uint color,float alpha){if((uint)x>=W||(uint)y>=H)return;int k=(H-1-y)*W+x;p.pixels[k]=Color32.Lerp(p.pixels[k],C(color),alpha);}
    void Glint(int x,int y,float time,float seed){float alpha=.12f+.08f*Mathf.Sin(time*1.7f+seed);for(int dy=-5;dy<=5;dy++)for(int dx=-7;dx<=7;dx++)if(Mathf.Abs(dx)+Mathf.Abs(dy)<8)Blend(x+dx,y+dy,0xffd692,alpha);}
    void Furnace(float time){
        for(int y=125;y<246;y++)for(int x=12;x<628;x++){int k=(H-1-y)*W+x;var c=p.pixels[k];if(c.r>110&&c.r>c.g*1.3f){float a=(Mathf.Sin(time*3+x*.07f+y*.11f)+1)*.055f;p.pixels[k]=Color32.Lerp(c,C(0xffc372),a);}}
    }
    static bool TrackOpening(int x,int y){if(y<209||y>281)return false;if(y>=266)return x>69&&x<201||x>253&&x<389||x>441&&x<573;return x>62&&x<205||x>249&&x<394||x>439&&x<578;}
    void Metro(float time){
        float phase=time%22;int offset=Mathf.RoundToInt(-930+phase*135);
        if(phase<13&&train!=null)for(int car=0;car<4;car++){
            int left=offset+car*228;for(int y=0;y<63;y++)for(int x=0;x<222;x++){
                int px=left+x,py=219+y;if(!TrackOpening(px,py))continue;var c=train[(y*trainH/63)*trainW+x*trainW/222];if(c.a<40)continue;int k=(H-1-py)*W+px;p.pixels[k]=Color32.Lerp(p.pixels[k],c,c.a/255f);
            }
        }
        Glint(145,171,time,0);Glint(320,171,time,1);Glint(495,171,time,2);
    }
    void Railway(float time){
        SmokeVfx.DrawAmbient(p,75,170,43,94,time,.30f);
        for(int i=0;i<90;i++){float speed=110+i%9*7;int x=(int)Mathf.Repeat(i*71-time*19,W+20)-10,y=(int)Mathf.Repeat(i*47+time*speed,324);p.Line(x,y,x-2,y+5,C(i%4==0?0x8095a4u:0x526679u));}
        for(int i=0;i<22;i++){int x=i*91%630,y=278+i*13%43;float pulse=(Mathf.Sin(time*2+i*2.3f)+1)*.16f;for(int k=0;k<3+i%6;k++)Blend(x+k,y,0xc69a70,pulse);}
    }
    void Harbor(float time){
        for(int i=0;i<95;i++){int x=70+(int)Mathf.Repeat(i*67+time*(i%2==0?3:-2),494),y=176+i*17%43;float alpha=(Mathf.Sin(time*1.5f+i)+1)*.2f;for(int k=0;k<2+i%7;k++)Blend(x+k,y,i%3==0?0xe9b376u:0x819db2u,alpha);}
        int boat=(int)Mathf.Repeat(time*7+90,680)-35;float fade=Mathf.Clamp01((boat-55)/35f)*Mathf.Clamp01((555-boat)/35f);
        for(int y=196;y<=207;y++)for(int x=0;x<29;x++){
            bool cabin=y<202&&x>=8&&x<22,hull=y>=202&&x>=y-202&&x<29-(y-202);if(!cabin&&!hull)continue;
            Blend(boat+x,y,cabin&&y>=198&&y<200&&x>=11&&x<15?0xd3ad78u:cabin?0x1c2d3du:0x152433u,fade);
        }
        Glint(49,196,time,0);Glint(593,196,time,2);
    }
    void Rooftop(float time){
        SmokeVfx.DrawAmbient(p,456,107,23,68,time,.24f);SmokeVfx.DrawAmbient(p,604,128,30,75,time+2,.25f);
        for(int i=0;i<42;i++){int x=(int)Mathf.Repeat(i*89+time*(18+i%7),W),y=100+(int)Mathf.Repeat(i*43+time*(6+i%4),214);p.Rect(x,y,i%5==0?2:1,1,C(i%3==0?0xc8bac9u:0x938dacu));}
    }
}
}
