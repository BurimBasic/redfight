using System;
using System.Collections.Generic;
using UnityEngine;
using static RedRivalry.PixelCanvas;
namespace RedRivalry {
public static class ComicArt {
    static readonly Dictionary<char,string> tiny=new Dictionary<char,string>{
        {'Ч',"101101111001001"},{'Т',"111010010010010"},{'О',"111101101101111"},{'Д',"011101101111101"},{'Е',"111100110100111"},{'Л',"011101101101101"},{'А',"010101111101101"},{'Ь',"100100110101110"},{'?',"110001010000010"},{'Г',"111100100100100"},{'С',"011100100100011"},{'В',"110101110101110"},{'И',"101101111101101"},{'Р',"110101110100100"},{'П',"111101101101101"},{'М',"101111111101101"},{'Я',"011101011101101"},{'З',"110001010001110"},{'-',"000000111000000"},{'У',"101101011001110"},{' ',"000000000000000"}
    };
    public static void AlphaEllipse(PixelCanvas p,int cx,int cy,int rx,int ry,Color32 color,float alpha){
        for(int y=Math.Max(0,cy-ry);y<=Math.Min(H-1,cy+ry);y++)for(int x=Math.Max(0,cx-rx);x<=Math.Min(W-1,cx+rx);x++){
            float r=(x-cx)*(x-cx)/(float)(rx*rx)+(y-cy)*(y-cy)/(float)(ry*ry);if(r>1)continue;int k=(H-1-y)*W+x;p.pixels[k]=Color32.Lerp(p.pixels[k],color,alpha*(r>.80f?.65f:1));
        }
    }
    public static void Projectile(PixelCanvas p,RedRivalry.Projectile q){
        if(q.delay>0)return;
        if(q.type==0){Book(p,q.x,q.y,q.age*8*q.owner.facing,q.book);if(q.damage>=13&&q.age>.05f&&q.age<1.25f){string text=CombatWorld.BookTitles[q.book%3];int w=p.TextWidth(text)+10,x=Mathf.Clamp((int)q.x-w/2,3,W-w-3),y=(int)q.y-44;p.Rect(x,y,w,13,C(0x111a29));p.Frame(x,y,w,13,C(0xa58a65));p.Text(text,x+5,y+3,C(0xf0d8ac));}}
        else if(q.type==1)SmokeVfx.Draw(p,q);
        else if(q.type==2){
            int x=(int)q.x,y=(int)q.y;p.Poly(C(0x362634),new Vector2(x-28,y-12),new Vector2(x,y-8),new Vector2(x+28,y-12),new Vector2(x+28,y+17),new Vector2(x,y+21),new Vector2(x-28,y+17));
            p.Poly(C(0xeadbb8),new Vector2(x-25,y-10),new Vector2(x,y-6),new Vector2(x,y+17),new Vector2(x-25,y+13));p.Poly(C(0xf5e8c9),new Vector2(x,y-6),new Vector2(x+25,y-10),new Vector2(x+25,y+13),new Vector2(x,y+17));
            for(int j=0;j<4;j++){p.Line(x-21,y-4+j*4,x-5,y-2+j*4,C(0x9a8a74));p.Line(x+5,y-2+j*4,x+21,y-4+j*4,C(0x9a8a74));}
            for(int i=0;i<10;i++){int px=(int)(q.x+Mathf.Sin(i*4.1f)*q.age*140),py=(int)(q.y-q.age*110+i%3*8);p.Rect(px,py,5,7,C(0xe6dcc3));p.Line(px+1,py+2,px+3,py+2,C(0x9a8a74));}
        }
        else SmokeVfx.Draw(p,q);
    }
    static void Book(PixelCanvas p,float x,float y,float angle,int title){
        float cs=Mathf.Cos(angle),sn=Mathf.Sin(angle);Color32 cover=C(title%3==0?0xa43e47u:title%3==1?0x7d354bu:0x8e653cu);
        for(int yy=-17;yy<=17;yy++)for(int xx=-18;xx<=18;xx++){
            int dx=(int)(x+xx*cs-yy*sn),dy=(int)(y+xx*sn+yy*cs);Color32 c=Math.Abs(xx)==18||Math.Abs(yy)==17?C(0x17202d):xx>=14?C(0xe6d9b7):xx<-14?C(0x542d3a):yy<-14?C(0xd89568):cover;p.P(dx,dy,c);
        }
        string a=title%3==0?"ЧТО":title%3==1?"ГОС-ВО":"ИМПЕР",b=title%3==0?"ДЕЛАТЬ?":title%3==1?"И РЕВ-Я":"ИАЛИЗМ";
        Tiny(p,a,x,y,cs,sn,-7);Tiny(p,b,x,y,cs,sn,3);
    }
    static void Tiny(PixelCanvas p,string text,float x,float y,float cs,float sn,int offset){float left=-text.Length*2+1;foreach(char ch in text){if(tiny.TryGetValue(ch,out string bits))for(int j=0;j<5;j++)for(int i=0;i<3;i++)if(bits[j*3+i]=='1'){float xx=left+i,yy=j+offset;p.P((int)(x+xx*cs-yy*sn),(int)(y+xx*sn+yy*cs),C(0xf3dfaa));}left+=4;}}
}
}
