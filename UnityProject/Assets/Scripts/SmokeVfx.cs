using System;
using UnityEngine;
using static RedRivalry.PixelCanvas;
namespace RedRivalry {
public static class SmokeVfx {
    static Color32[] pixels;static int width,height;static float anchor;static bool attempted;
    static readonly int[] bayer={0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
    static readonly Color32[] palette={C(0x455064),C(0x5c687b),C(0x748094),C(0x8c96a6),C(0xa8afba),C(0xc0c3c5)};
    public static bool Loaded {get{Ensure();return pixels!=null;}}
    static void Ensure(){
        if(attempted)return;attempted=true;var t=Resources.Load<Texture2D>("Art/PixelSmoke");if(t==null)return;var raw=t.GetPixels32();int l=t.width,r=0,top=t.height,bottom=0;
        for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(raw[(t.height-1-y)*t.width+x].a>40){l=Math.Min(l,x);r=Math.Max(r,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
        if(r<=l||bottom<=top)return;width=r-l+1;height=bottom-top+1;pixels=new Color32[width*height];float weight=0,sum=0;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){var c=raw[(t.height-1-top-y)*t.width+l+x];pixels[y*width+x]=c;if(x<width*.06f&&c.a>40){weight+=c.a;sum+=y*c.a;}}
        anchor=weight>0?sum/weight/height:.7f;Debug.Log("PIXEL_SMOKE_READY "+width+"x"+height);
    }
    public static void Draw(PixelCanvas p,Projectile q){
        Ensure();if(pixels==null||q.delay>0)return;
        if(q.type==3){float fade=Mathf.SmoothStep(0,1,Mathf.Min(q.age*5,q.life*2));Blit(p,q.x-51,q.y-17,102,87,1,q.age,.42f*fade);Blit(p,q.x+51,q.y-30,102,83,-1,q.age+.7f,.40f*fade);return;}
        int direction=q.vx>=0?1:-1;float speed=Mathf.Abs(q.vx),start=q.x-q.vx*q.age,tail=speed*Mathf.Max(0,q.age-.48f)*.85f;
        float origin=start+tail*direction,len=Mathf.Max(2,speed*q.age-tail),scale=q.heavy?1.55f:1;
        float h=(8+Mathf.Min(q.age,1.4f)*38)*Mathf.SmoothStep(0,1,Mathf.Clamp01(q.age/.12f))*scale;
        float originY=q.y+6*q.age-Mathf.Pow(Mathf.Max(0,q.age-.48f),2)*14;
        float opacity=(q.heavy?.82f:.72f)*Mathf.SmoothStep(0,1,Mathf.Min(q.age*7,q.life*2));Blit(p,origin,originY,len,Mathf.Max(2,h),direction,q.age,opacity);
    }
    static void Blit(PixelCanvas p,float ox,float oy,float w,float h,int direction,float time,float opacity){
        int left=Mathf.Max(0,Mathf.FloorToInt(direction>0?ox:ox-w)),right=Mathf.Min(W-1,Mathf.CeilToInt(direction>0?ox+w:ox));
        int top=Mathf.Max(0,Mathf.FloorToInt(oy-anchor*h-4)),bottom=Mathf.Min(H-1,Mathf.CeilToInt(oy+(1-anchor)*h+4));if(right<left)return;
        int span=right-left+1;float[] us=new float[span],curls=new float[span];for(int i=0;i<span;i++){float u=(left+i-ox)*direction/w;us[i]=u;curls[i]=Mathf.Sin(u*11-time*3)*.025f*u;}
        for(int y=top;y<=bottom;y++){
            float v=(y-oy)/h+anchor,rowCurl=Mathf.Sin(v*10+time*3)*.015f;
            for(int x=left;x<=right;x++){
                int col=x-left;float u=us[col],su=u+rowCurl*(.2f+u),sv=v+curls[col];if(su<0||su>=1||sv<0||sv>=1)continue;
                var src=pixels[Mathf.Min(height-1,(int)(sv*height))*width+Mathf.Min(width-1,(int)(su*width))];if(src.a<12)continue;
                float a=src.a/255f*opacity,levels=a*4,whole=Mathf.Floor(levels);a=(whole+(levels-whole>bayer[(x&3)+(y&3)*4]/16f?1:0))/4f;if(a<=0)continue;
                int shade=Mathf.Clamp((src.r+src.g+src.b)/3*6/256,0,5),k=(H-1-y)*W+x;p.pixels[k]=Color32.Lerp(p.pixels[k],palette[shade],a);
            }
        }
    }
}
}
