using System;
using UnityEngine;
using static RedRivalry.PixelCanvas;
namespace RedRivalry {
public sealed class ArenaArt {
    public const int Count=6;
    static readonly string[] titles={"ЗИМНЯЯ ПЛОЩАДЬ","ЛИТЕЙНЫЙ ЦЕХ","КОМСОМОЛЬСКАЯ","ВОКЗАЛ","НАБЕРЕЖНАЯ","КРЫШИ ГОРОДА"};
    static readonly string[] resources={"WinterArena","FoundryArena","KomsomolskayaArena","RailwayArena","HarborArena","RooftopArena"};
    static readonly string[] captions={"СНЕГ И НОЧНЫЕ ОГНИ","ИСКРЫ И ЖАР ПЕЧЕЙ","МОСКВА / КОЛЬЦЕВАЯ ЛИНИЯ","ДОЖДЬ И ПАРОВОЗНЫЙ ПАР","РЕКА И ОГНИ МОСТА","ВЕТЕР НАД ГОРОДОМ"};
    public readonly Color32[][] backgrounds=new Color32[Count][];
    public int LoadedArtwork {get;private set;}
    public static int Normalize(int index)=>((index%Count)+Count)%Count;
    public static string Title(int index)=>titles[Normalize(index)];
    public static string Caption(int index)=>captions[Normalize(index)];
    public static string NumberedTitle(int index)=>(Normalize(index)+1).ToString("00")+" / "+Title(index);
    readonly ArenaMotion motion;
    readonly System.Random rng=new System.Random(1917);
    PixelCanvas p;
    public ArenaArt(PixelCanvas canvas){p=canvas;for(int i=0;i<Count;i++){backgrounds[i]=SpriteArt.Background(resources[i]);if(backgrounds[i]!=null){LoadedArtwork++;continue;}if(i==0)Winter();else Foundry();backgrounds[i]=(Color32[])p.pixels.Clone();}motion=new ArenaMotion(p);}
    public bool TrainLoaded=>motion.TrainLoaded;
    public void Thumbnail(int index,int x,int y,int w,int h){var src=backgrounds[Normalize(index)];for(int yy=0;yy<h;yy++)for(int xx=0;xx<w;xx++)p.P(x+xx,y+yy,src[(H-1-yy*H/h)*W+xx*W/w]);}
    void R(int x,int y,int w,int h,uint c){p.Rect(x,y,w,h,C(c));}
    void L(int x,int y,int xx,int yy,uint c,int s=1){p.Line(x,y,xx,yy,C(c),s);}
    void Tower(int x,int baseY,int width,int height,bool clock=false){
        int top=baseY-height;
        R(x,top,width,height,0x253141);R(x+3,top+3,width-6,height-3,0x344051);
        for(int y=top+8;y<baseY;y+=7){L(x+2,y,x+width-3,y,0x263347);for(int xx=x+((y/7%2)*6);xx<x+width-2;xx+=12)L(xx,y,xx,y+6,0x2a3546);}
        R(x-3,top,width+6,5,0x59616a);R(x-2,top-6,width+4,6,0x1d293b);
        for(int i=0;i<width;i+=7)R(x+i,top-11,4,8,0x566071);
        p.Poly(C(0x142536),new Vector2(x-4,top-9),new Vector2(x+width/2,top-70),new Vector2(x+width+4,top-9));
        L(x+width/2,top-70,x+width+4,top-9,0x596d7b,2);
        for(int i=0;i<4;i++)L(x+width/2-i*3,top-62+i*13,x+width/2+i*3,top-62+i*13,0x395264);
        Star(x+width/2,top-77,5,0xf55e5b);L(x+width/2,top-77,x+width/2,top-66,0xf1a673);
        if(clock){int cx=x+width/2,cy=top+19;p.Ellipse(cx,cy,12,12,C(0x0b1424));p.Arc(cx,cy,12,0,Mathf.PI*2,C(0xc4a975),1);for(int i=0;i<12;i++){float a=i*Mathf.PI/6;p.P(cx+(int)(9*Mathf.Cos(a)),cy+(int)(9*Mathf.Sin(a)),C(0xddbd80));}L(cx,cy,cx,cy-7,0xf6d9a0);L(cx,cy,cx+5,cy+3,0xf6d9a0);}
        else {R(x+width/2-3,top+12,6,15,0x111e31);R(x+width/2-2,top+14,2,11,0xffb966);}
    }
    void Star(int x,int y,int r,uint c){for(int i=0;i<5;i++){float a=-Mathf.PI/2+i*Mathf.PI*2/5;p.Line(x,y,x+(int)(Mathf.Cos(a)*r),y+(int)(Mathf.Sin(a)*r),C(c),2);}}
    void Winter(){
        for(int y=0;y<H;y++){float t=y/(float)H;p.Rect(0,y,W,1,Color32.Lerp(C(0x0a142b),C(0x536373),t));}
        for(int i=0;i<130;i++)p.P(rng.Next(W),rng.Next(180),C(i%5==0?0xc1c2c0u:0x5a718cu));
        p.Ellipse(469,71,28,28,C(0x718395));p.Ellipse(465,67,24,24,C(0xadb9bc));p.Ellipse(456,62,6,9,C(0x9eacb1));p.Ellipse(475,79,6,4,C(0x919fa8));
        for(int i=0;i<14;i++){int x=rng.Next(640),y=rng.Next(75,155);p.Ellipse(x,y,50+rng.Next(60),3,C(0x34435b));}
        // Distant onion domes and rooftops, deliberately layered in cool silhouettes.
        for(int i=0;i<9;i++){int x=250+i*29,y=150+rng.Next(30);R(x,y,23,80,0x27374c);p.Ellipse(x+11,y-8,13,16,C(0x34495e));p.Poly(C(0x34495e),new Vector2(x,y-11),new Vector2(x+11,y-37),new Vector2(x+22,y-11));L(x+11,y-41,x+11,y-29,0x84919a);L(x+7,y-37,x+15,y-37,0x84919a);}
        R(0,177,640,78,0x303a4a);
        for(int y=184;y<254;y+=8){L(0,y,640,y,0x242f40);for(int x=(y/8%2)*10;x<640;x+=20)L(x,y,x,y+7,0x293445);}
        R(0,173,640,4,0x909ba2);for(int x=0;x<640;x+=15){R(x,164,9,11,0x4b5563);R(x,163,9,2,0xa3aeb4);}
        Tower(78,253,47,116,true);Tower(528,253,35,80);Tower(188,253,26,61);
        // Central gate and snow-capped masonry.
        p.Ellipse(321,231,24,36,C(0x1b273b));R(297,230,49,25,0x1b273b);p.Ellipse(321,233,18,30,C(0x111d30));R(303,233,37,23,0x111d30);
        for(int x=306;x<340;x+=5)L(x,210,x,255,0x49515e);L(302,241,340,241,0x60636c);
        // Red fabric panels.
        Banner(156,178,18,62);Banner(431,178,18,62);
        R(0,254,640,106,0x394654);R(0,254,640,5,0x6b7d88);
        for(int row=0;row<9;row++){int y=263+row*11;L(0,y,640,y,0x273441);for(int x=-30+(row%2)*20;x<640;x+=46){L(x,y,x-12,y+10,0x263541);L(x+2,y+1,x+30,y+1,0x52616a);}}
        for(int i=0;i<180;i++){int x=rng.Next(W),y=rng.Next(258,360);R(x,y,rng.Next(2,16),1,i%3==0?0x95a5abu:0x566d7bu);}
        p.Poly(C(0x84959f),new Vector2(0,255),new Vector2(97,258),new Vector2(66,266),new Vector2(0,270));p.Poly(C(0x7a8d99),new Vector2(640,255),new Vector2(556,258),new Vector2(579,266),new Vector2(640,267));
        Lamp(38,250);Lamp(599,250);
        // Foreground rail and crates frame the playfield without covering the fighters.
        Crate(1,284,33,37);Crate(602,294,38,33);R(0,349,640,11,0x101b2a);L(0,349,640,349,0x526575);
    }
    void Banner(int x,int y,int w,int h){R(x+2,y+2,w,h,0x161e30);R(x,y,w,h,0x842d40);R(x+2,y,w-5,h,0xa33b49);R(x+5,y,2,h,0xb6474d);R(x+1,y,w-2,2,0xe5ae73);Star(x+w/2,y+18,5,0xe8b879);p.Poly(C(0x344052),new Vector2(x,y+h),new Vector2(x+w/2,y+h-6),new Vector2(x+w,y+h));}
    void Lamp(int x,int y){R(x-6,y-2,13,4,0x142134);R(x-2,y-83,4,82,0x121d2e);R(x+2,y-82,1,80,0x75818b);R(x-7,y-100,15,17,0x162437);R(x-5,y-98,11,13,0xf6bb78);R(x-2,y-97,3,12,0xffe3a6);L(x-8,y-101,x+8,y-101,0xa3acaf,2);p.Poly(C(0x182436),new Vector2(x-10,y-103),new Vector2(x,y-110),new Vector2(x+10,y-103));for(int i=0;i<28;i++){int xx=x+rng.Next(-26,27),yy=y+rng.Next(-12,5);R(xx,yy,rng.Next(2,7),1,0x9a815f);}}
    void Crate(int x,int y,int w,int h){R(x,y,w,h,0x1b2836);R(x+2,y+2,w-4,h-4,0x3c4850);for(int xx=x+4;xx<x+w-2;xx+=6)L(xx,y+3,xx,y+h-3,0x263540);L(x+2,y+3,x+w-3,y+h-3,0x6e756f,3);R(x,y,w,3,0xb5c1c5);R(x,y+h-4,w,3,0x202c39);}
    void Foundry(){
        for(int y=0;y<H;y++)p.Rect(0,y,W,1,Color32.Lerp(C(0x171424),C(0x523a40),y/(float)H));
        for(int x=0;x<640;x+=40){R(x,40,4,210,0x302a39);R(x+5,41,31,96,0x1a2230);for(int y=44;y<134;y+=13){R(x+7,y,26,2,0x5a4243);R(x+19,y,2,12,0x5a4243);}}
        for(int x=25;x<640;x+=124){R(x,146,66,102,0x262735);p.Ellipse(x+33,154,33,36,C(0x262735));p.Ellipse(x+33,168,23,34,C(0xe56b43));p.Ellipse(x+33,172,17,25,C(0xffb660));R(x+10,172,46,65,0xe56b43);for(int xx=x+12;xx<x+56;xx+=9)R(xx,145,4,91,0x302b33);R(x-5,231,76,7,0x8f6c5b);}
        for(int x=0;x<640;x+=80){R(x,0,10,250,0x172331);R(x+2,0,2,250,0x615052);for(int y=12;y<250;y+=24){p.Ellipse(x+5,y,1,1,C(0xb27a60));}L(x,140,x+80,222,0x352f3b,6);}
        R(0,248,640,112,0x39343d);for(int y=253;y<360;y+=14){L(0,y,640,y,0x1a2330);for(int x=(y/14%2)*20;x<640;x+=40)L(x,y,x-3,y+13,0x202832);}
        R(0,270,640,4,0xd0804b);R(0,274,640,2,0x1e202c);R(0,335,640,5,0x171d2b);R(0,340,640,2,0x996042);
        for(int x=0;x<640;x+=22){p.Poly(C(0xc38a4b),new Vector2(x,336),new Vector2(x+9,336),new Vector2(x+4,339),new Vector2(x-5,339));}
        R(260,110,120,30,0x161e2b);p.Frame(260,110,120,30,C(0x7e6656));p.Center("ЛИТЕЙНЫЙ ЦЕХ",320,121,C(0xd49c6c));
        for(int x=0;x<640;x+=170){R(x,238,140,7,0x56515a);R(x+3,236,134,2,0xb07a61);}
        Crate(3,292,32,39);Crate(606,286,35,45);
    }
    public void Draw(int index,float time){
        index=Normalize(index);
        Array.Copy(backgrounds[index],p.pixels,p.pixels.Length);
        if(index==0){for(int i=0;i<58;i++){int x=(int)((i*83+Mathf.Sin(time*.35f+i)*15+time*8)%660)-10;int y=(int)((i*41+time*(8+i%9))%355);p.Rect(x,y,i%7==0?2:1,1,C(i%3==0?0xd2dcdfu:0x7d98aau));}}
        else if(index==1)for(int i=0;i<40;i++){int x=(int)((i*67+Mathf.Sin(time+i)*10)%640),y=245-(int)((i*17+time*(15+i%12))%160);p.Rect(x,y,1,i%5==0?3:1,C(i%3==0?0xffcc79u:0xb56a55u));}
        motion.Draw(index,time);
    }
}
}
