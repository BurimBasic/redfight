using System;
using System.Collections.Generic;
using UnityEngine;
using static RedRivalry.PixelCanvas;
namespace RedRivalry {
// Atlas components are isolated by alpha, then registered to the same floor and cell pivot.
// This avoids cropping overhead weapons and prevents feet from drifting between frames.
public static class SpriteArt {
    public sealed class Frame { public Color32[] pixels;public int w,h;public float pivotX; }
    sealed class Atlas {public Frame[] frames;public int referenceHeight;}
    sealed class Component {public int left,right,top,bottom,count;public List<int> members=new List<int>();}
    static Atlas[] atlases=new Atlas[2],casts=new Atlas[2],walks=new Atlas[2];static bool loaded;
    public static int LoadedFrames {get{Ensure();return (atlases[0]?.frames.Length??0)+(atlases[1]?.frames.Length??0);}}
    public static int CastFrames {get{Ensure();return (casts[0]?.frames.Length??0)+(casts[1]?.frames.Length??0);}}
    public static int WalkFrames {get{Ensure();return (walks[0]?.frames.Length??0)+(walks[1]?.frames.Length??0);}}
    static void Ensure(){if(loaded)return;loaded=true;atlases[0]=Load("Art/LeninAtlas");atlases[1]=Load("Art/StalinAtlas");casts[0]=Load("Art/LeninBookCast",4,2,2);casts[1]=Load("Art/StalinPipeCast",4,2,2);walks[0]=Load("Art/LeninWalkBetween",8,4,2);walks[1]=Load("Art/StalinWalkBetween",8,4,2);RegisterWalk(0);RegisterWalk(1);}
    static Atlas Load(string path,int expected=12,int columns=4,int rows=3){
        var texture=Resources.Load<Texture2D>(path);if(texture==null)return null;
        int w=texture.width,h=texture.height;var raw=texture.GetPixels32();var visited=new bool[w*h];var list=new List<Component>();var queue=new Queue<int>();
        for(int start=0;start<raw.Length;start++){
            if(visited[start]||raw[start].a<100)continue;var c=new Component{left=w,right=0,top=h,bottom=0};queue.Enqueue(start);visited[start]=true;
            while(queue.Count>0){int k=queue.Dequeue(),x=k%w,y=h-1-k/w;c.count++;c.members.Add(k);c.left=Math.Min(c.left,x);c.right=Math.Max(c.right,x);c.top=Math.Min(c.top,y);c.bottom=Math.Max(c.bottom,y);
                for(int n=0;n<4;n++){int nx=x+(n==0?-1:n==1?1:0),ny=y+(n==2?-1:n==3?1:0);if(nx<0||nx>=w||ny<0||ny>=h)continue;int ni=(h-1-ny)*w+nx;if(!visited[ni]&&raw[ni].a>=100){visited[ni]=true;queue.Enqueue(ni);}}
            }
            if(c.count>800)list.Add(c);
        }
        list.Sort((a,b)=>b.count.CompareTo(a.count));if(list.Count<expected){Debug.LogError("Atlas requires "+expected+" complete sprite components: "+path+" got "+list.Count);return null;}if(list.Count>expected)list.RemoveRange(expected,list.Count-expected);
        list.Sort((a,b)=>((a.top+a.bottom)*.5f).CompareTo((b.top+b.bottom)*.5f));
        for(int row=0;row<rows;row++)list.Sort(row*columns,columns,Comparer<Component>.Create((a,b)=>a.left.CompareTo(b.left)));
        var atlas=new Atlas{frames=new Frame[expected],referenceHeight=list[0].bottom-list[0].top+1};
        for(int i=0;i<expected;i++){
            var c=list[i];var f=new Frame{w=c.right-c.left+1,h=c.bottom-c.top+1,pivotX=(i%columns+.5f)*(w/(float)columns)-c.left};f.pixels=new Color32[f.w*f.h];
            foreach(int k in c.members){int x=k%w-c.left,y=h-1-k/w-c.top;f.pixels[y*f.w+x]=raw[k];}
            atlas.frames[i]=f;
        }
        Debug.Log("SPRITE_ATLAS_READY "+path+" frames="+expected+" reference="+atlas.referenceHeight);return atlas;
    }
    public static int FrameIndex(Fighter f,float time){
        float t=f.AttackProgress;
        if(f.state==ActionState.KO||f.state==ActionState.Hurt)return 10;
        if(f.state==ActionState.Victory)return 11;
        if(f.state==ActionState.Light)return t<.35f?6:t<.75f?7:0;
        if(f.state==ActionState.Heavy)return t<.40f?8:t<.78f?9:0;
        if(f.state==ActionState.Special)return t<.33f?6:t<.78f?7:0;
        if(f.state==ActionState.Skill)return f.kind==0?7:t<.4f?8:9;
        if(f.state==ActionState.Ultimate)return t<.2f?8:t<.7f?(f.kind==0?9:((int)(t*20)%2==0?7:9)):11;
        if(f.height>0)return 5;
        if(f.crouching)return 4;
        return (int)(time*2.3f+f.slot)%2;
    }
    public static bool Draw(PixelCanvas canvas,Fighter f,float time,float scale,float ground=300,bool ghost=false){
        Ensure();var a=atlases[Mathf.Clamp(f.kind,0,1)];if(a==null)return false;
        int index=FrameIndex(f,time);var frame=a.frames[index];float zoom=scale*104/a.referenceHeight;
        float x=f.x,y=ground-f.height;
        if(f.state==ActionState.Idle&&f.height==0&&!f.crouching&&f.walking&&!f.blocking)WalkSample(f.kind,WalkIndex(f.walkPhase),scale,out frame,out zoom);
        if((f.state==ActionState.Special||f.state==ActionState.Skill||f.state==ActionState.Ultimate)&&casts[f.kind]!=null){var cast=casts[f.kind];float t=f.AttackProgress;float release=f.state==ActionState.Ultimate?.25f:.40f;int castIndex=t<.14f?0:t<release?1:t<.82f?2:3;frame=cast.frames[castIndex];zoom=scale*104/cast.referenceHeight;}
        if(f.height>0&&index==5)y-=(a.referenceHeight-frame.h)*zoom;
        if(f.state==ActionState.Light||f.state==ActionState.Heavy){float lunge=Mathf.Sin(f.AttackProgress*Mathf.PI)*(f.state==ActionState.Heavy?12:3);x+=f.facing*lunge;}
        if(f.state==ActionState.Hurt)x-=f.facing*Mathf.Sin(f.AttackProgress*Mathf.PI)*3;
        if(f.state==ActionState.KO){float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(f.stateTime/.6f));DrawFrame(canvas,frame,x-f.facing*t*15,y+2,zoom,f.facing,f.flash>0,ghost,-f.facing*t*Mathf.PI*.5f);return true;}
        DrawFrame(canvas,frame,x,y,zoom,f.facing,f.flash>0,ghost,0);
        if(f.blocking){int cx=(int)(x+f.facing*26),cy=(int)(y-72);canvas.Arc(cx,cy,32,f.facing==1?-1.1f:2.04f,f.facing==1?1.1f:4.24f,C(0x9cddd9),2);}
        return true;
    }
    static readonly int[] walkKeys={0,2,1,3};
    public static int WalkIndex(float phase){return ((int)(phase*3.3f))%12;}
    public static int WalkKeyAt(int index){return walkKeys[(index%12)/3];}
    static float HeadX(Frame frame){
        float sum=0,count=0;for(int y=0;y<frame.h*.24f;y++)for(int x=0;x<frame.w;x++){var c=frame.pixels[y*frame.w+x];if(c.a>=100&&c.r>95&&c.r>c.g*1.1f&&c.g>c.b*1.05f){sum+=x;count++;}}return count>0?sum/count:frame.pivotX;
    }
    static void RegisterWalk(int kind){
        var a=atlases[kind];var b=walks[kind];if(a==null||b==null)return;
        for(int group=0;group<4;group++){var previous=a.frames[walkKeys[group]];var next=a.frames[walkKeys[(group+1)%4]];
            for(int part=1;part<=2;part++){var frame=b.frames[group*2+part-1];float t=part/3f,height=Mathf.Lerp(previous.h,next.h,t)/a.referenceHeight,head=Mathf.Lerp((HeadX(previous)-previous.pivotX)/a.referenceHeight,(HeadX(next)-next.pivotX)/a.referenceHeight,t);frame.pivotX=HeadX(frame)-head*frame.h/height;}
        }
    }
    static void WalkSample(int kind,int index,float scale,out Frame frame,out float zoom){
        var a=atlases[kind];int group=index/3,part=index%3;var previous=a.frames[walkKeys[group]];var next=a.frames[walkKeys[(group+1)%4]];
        if(part==0||walks[kind]==null){frame=previous;zoom=scale*104/a.referenceHeight;return;}
        frame=walks[kind].frames[group*2+part-1];float targetHeight=Mathf.Lerp(previous.h,next.h,part/3f);zoom=scale*104*targetHeight/a.referenceHeight/frame.h;
    }
    public static void DrawFrame(PixelCanvas p,Frame f,float x,float ground,float zoom,int direction,bool flash=false,bool ghost=false,float rotation=0){
        int sw=Mathf.CeilToInt(f.w*zoom),sh=Mathf.CeilToInt(f.h*zoom);float anchor=f.pivotX*zoom;float cs=Mathf.Cos(rotation),sn=Mathf.Sin(rotation);
        // Destination-driven sampling retains crisp pixels and does not leave holes at sub-pixel scales.
        if(Mathf.Abs(rotation)<.001f){
            for(int yy=0;yy<sh;yy++){int sy=Mathf.Min(f.h-1,(int)(yy/zoom));for(int xx=0;xx<sw;xx++){int sx=Mathf.Min(f.w-1,(int)(xx/zoom));var c=f.pixels[sy*f.w+sx];if(c.a<110)continue;if(flash)c=Color32.Lerp(c,C(0xffe4b1),.6f);else if(ghost)c=Color32.Lerp(c,C(0x9b3c55),.7f);int dx=(int)(x+(xx-anchor)*direction),dy=(int)(ground-sh+yy);p.P(dx,dy,c);}}
        }else{
            for(int yy=0;yy<sh;yy++)for(int xx=0;xx<sw;xx++){var c=f.pixels[Mathf.Min(f.h-1,(int)(yy/zoom))*f.w+Mathf.Min(f.w-1,(int)(xx/zoom))];if(c.a<110)continue;float px=(xx-anchor)*direction,py=yy-sh;int dx=(int)(x+px*cs-py*sn),dy=(int)(ground+px*sn+py*cs);p.Rect(dx,dy,2,2,c);}
        }
    }
    public static bool Portrait(PixelCanvas p,int kind,int x,int y,int dir){
        Ensure();var a=atlases[Mathf.Clamp(kind,0,1)];if(a==null)return false;var f=a.frames[0];
        int cx=kind==0?(int)(f.w*.57f):(int)(f.w*.56f),cw=(int)(a.referenceHeight*.30f),ch=(int)(a.referenceHeight*.28f),left=cx-cw/2;
        const int dw=32,dh=35;
        for(int yy=0;yy<dh;yy++)for(int xx=0;xx<dw;xx++){int sx=left+xx*cw/dw,sy=yy*ch/dh;if(sx<0||sx>=f.w||sy>=f.h)continue;var c=f.pixels[sy*f.w+sx];if(c.a>=110)p.P(x+(xx-dw/2)*dir,y+yy-18,c);}
        return true;
    }
    public static Color32[] Background(string name){
        var t=Resources.Load<Texture2D>("Art/"+name);if(t==null)return null;var src=t.GetPixels32();var dst=new Color32[W*H];
        for(int y=0;y<H;y++)for(int x=0;x<W;x++){int sx=x*t.width/W,sy=y*t.height/H;dst[y*W+x]=src[sy*t.width+sx];}return dst;
    }
}
}
