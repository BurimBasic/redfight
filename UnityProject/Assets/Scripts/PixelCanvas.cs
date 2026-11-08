using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedRivalry {
// A fixed-resolution, point-filtered canvas. All artwork is drawn in actual pixels.
public sealed class PixelCanvas {
    public const int W=640, H=360;
    public readonly Color32[] pixels = new Color32[W*H];
    public readonly Texture2D texture;
    static readonly Dictionary<char,string[]> glyphs = new Dictionary<char,string[]>();
    public PixelCanvas() {
        texture=new Texture2D(W,H,TextureFormat.RGBA32,false); texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;
        if(glyphs.Count==0) InitFont();
    }
    public static Color32 C(uint rgb) { return new Color32((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb,255); }
    public void Clear(Color32 c) { for(int i=0;i<pixels.Length;i++) pixels[i]=c; }
    public void P(int x,int y,Color32 c) { if((uint)x<W&&(uint)y<H) pixels[(H-1-y)*W+x]=c; }
    public void Rect(int x,int y,int w,int h,Color32 c) {
        int l=Math.Max(0,x),r=Math.Min(W,x+w),t=Math.Max(0,y),b=Math.Min(H,y+h);
        for(int j=t;j<b;j++){ int k=(H-1-j)*W+l; for(int i=l;i<r;i++) pixels[k++]=c; }
    }
    public void Frame(int x,int y,int w,int h,Color32 c,int thick=1) { Rect(x,y,w,thick,c);Rect(x,y+h-thick,w,thick,c);Rect(x,y,thick,h,c);Rect(x+w-thick,y,thick,h,c); }
    public void Line(int x0,int y0,int x1,int y1,Color32 c,int size=1) {
        int dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,err=dx+dy;
        for(;;) { Rect(x0-size/2,y0-size/2,size,size,c);if(x0==x1&&y0==y1)break;int e=err*2;if(e>=dy){err+=dy;x0+=sx;}if(e<=dx){err+=dx;y0+=sy;} }
    }
    public void Ellipse(int cx,int cy,int rx,int ry,Color32 c) {
        if(rx<=0||ry<=0)return;
        for(int y=-ry;y<=ry;y++){int w=(int)(rx*Math.Sqrt(Math.Max(0,1-(double)y*y/(ry*ry))));Rect(cx-w,cy+y,w*2+1,1,c);}
    }
    public void Poly(Color32 c,params Vector2[] v) {
        float min=H,max=0;foreach(var p in v){min=Math.Min(min,p.y);max=Math.Max(max,p.y);}
        float[] nodes=new float[v.Length];
        for(int y=Math.Max(0,(int)min);y<=Math.Min(H-1,(int)max);y++) {
            int n=0;for(int i=0,j=v.Length-1;i<v.Length;j=i++)if((v[i].y<y&&v[j].y>=y)||(v[j].y<y&&v[i].y>=y))nodes[n++]=v[i].x+(y-v[i].y)/(v[j].y-v[i].y)*(v[j].x-v[i].x);
            Array.Sort(nodes,0,n);for(int i=0;i+1<n;i+=2) Rect((int)nodes[i],y,(int)nodes[i+1]-(int)nodes[i]+1,1,c);
        }
    }
    public void Arc(int cx,int cy,float radius,float a,float b,Color32 c,int thick=1) {
        int steps=Math.Max(2,(int)(Math.Abs(b-a)*radius/2));
        int px=cx+(int)(Math.Cos(a)*radius),py=cy+(int)(Math.Sin(a)*radius);
        for(int i=1;i<=steps;i++){float t=a+(b-a)*i/steps;int nx=cx+(int)(Math.Cos(t)*radius),ny=cy+(int)(Math.Sin(t)*radius);Line(px,py,nx,ny,c,thick);px=nx;py=ny;}
    }
    public int TextWidth(string text,int scale=1) {return text.Length*6*scale-(text.Length>0?scale:0);}
    public void Text(string text,int x,int y,Color32 c,int scale=1,bool shadow=false) {
        if(shadow)Text(text,x+scale,y+scale,C(0x080e19),scale,false);
        foreach(char ch in text.ToUpperInvariant()) {
            if(glyphs.TryGetValue(ch,out var rows))for(int j=0;j<7;j++)for(int i=0;i<rows[j].Length;i++)if(rows[j][i]!='.')Rect(x+i*scale,y+j*scale,scale,scale,c);
            x+=6*scale;
        }
    }
    public void Center(string text,int x,int y,Color32 c,int scale=1,bool shadow=false){ Text(text,x-TextWidth(text,scale)/2,y,c,scale,shadow); }
    public void Upload(){texture.SetPixels32(pixels);texture.Apply(false,false);}
    public void Dim(float a) {for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32((byte)(p.r*a),(byte)(p.g*a),(byte)(p.b*a),255);}}
    static void G(char c,string s){glyphs[c]=s.Split('/');}
    static void InitFont(){
        G('A',".###./#...#/#...#/#####/#...#/#...#/#...#");G('B',"####./#...#/#...#/####./#...#/#...#/####.");
        G('C',".####/#..../#..../#..../#..../#..../.####");G('D',"####./#...#/#...#/#...#/#...#/#...#/####.");
        G('E',"#####/#..../#..../####./#..../#..../#####");G('F',"#####/#..../#..../####./#..../#..../#....");
        G('G',".####/#..../#..../#.###/#...#/#...#/.###.");G('H',"#...#/#...#/#...#/#####/#...#/#...#/#...#");
        G('I',"#####/..#../..#../..#../..#../..#../#####");G('J',"..###/...#./...#./...#./...#./#..#./.##..");
        G('K',"#...#/#..#./#.#../##.../#.#../#..#./#...#");G('L',"#..../#..../#..../#..../#..../#..../#####");
        G('M',"#...#/##.##/#.#.#/#...#/#...#/#...#/#...#");G('N',"#...#/##..#/#.#.#/#..##/#...#/#...#/#...#");
        G('O',".###./#...#/#...#/#...#/#...#/#...#/.###.");G('P',"####./#...#/#...#/####./#..../#..../#....");
        G('Q',".###./#...#/#...#/#...#/#.#.#/#..#./.##.#");G('R',"####./#...#/#...#/####./#.#../#..#./#...#");
        G('S',".####/#..../#..../.###./....#/....#/####.");G('T',"#####/..#../..#../..#../..#../..#../..#..");
        G('U',"#...#/#...#/#...#/#...#/#...#/#...#/.###.");G('V',"#...#/#...#/#...#/#...#/#...#/.#.#./..#..");
        G('W',"#...#/#...#/#...#/#...#/#.#.#/##.##/#...#");G('X',"#...#/#...#/.#.#./..#../.#.#./#...#/#...#");
        G('Y',"#...#/#...#/.#.#./..#../..#../..#../..#..");G('Z',"#####/....#/...#./..#../.#.../#..../#####");
        G('0',".###./#...#/#..##/#.#.#/##..#/#...#/.###.");G('1',"..#../.##../..#../..#../..#../..#../.###.");
        G('2',".###./#...#/....#/...#./..#../.#.../#####");G('3',"####./....#/....#/.###./....#/....#/####.");
        G('4',"...#./..##./.#.#./#..#./#####/...#./...#.");G('5',"#####/#..../#..../####./....#/....#/####.");
        G('6',".###./#..../#..../####./#...#/#...#/.###.");G('7',"#####/....#/...#./..#../.#.../.#.../.#...");
        G('8',".###./#...#/#...#/.###./#...#/#...#/.###.");G('9',".###./#...#/#...#/.####/....#/....#/.###.");
        G('-',"...../...../...../#####/...../...../.....");G('+',"...../..#../..#../#####/..#../..#../.....");
        G(':',"...../..#../..#../...../..#../..#../.....");G('.',"...../...../...../...../...../.##../.##..");
        G('/',"....#/....#/...#./..#../.#.../#..../#....");G('!',"..#../..#../..#../..#../..#../...../..#..");
        G('?',".###./#...#/....#/...#./..#../...../..#..");G('>',".#.../..#../...#./....#/...#./..#../.#...");
        G('<',"...#./..#../.#.../#..../.#.../..#../...#.");G('=',"...../...../#####/...../#####/...../.....");
        G('×',"...../#...#/.#.#./..#../.#.#./#...#/.....");G('(',"...#./..#../.#.../.#.../.#.../..#../...#.");G(')',".#.../..#../...#./...#./...#./..#../.#...");
        string ru="АВЕКМНОРСТХ",en="ABEKMHOPCTX";for(int i=0;i<ru.Length;i++)glyphs[ru[i]]=glyphs[en[i]];
        G('Б',"#####/#..../#..../####./#...#/#...#/####.");G('Г',"#####/#..../#..../#..../#..../#..../#....");
        G('Д',"..##./.#.#./.#.#./.#.#./#..#./#####/#...#");G('Ж',"#.#.#/#.#.#/.###./..#../.###./#.#.#/#.#.#");
        G('З',"####./....#/....#/.###./....#/....#/####.");G('И',"#...#/#...#/#..##/#.#.#/##..#/#...#/#...#");
        G('Й',".#.#./..#../#...#/#..##/#.#.#/##..#/#...#");G('Л',"..###/.#..#/.#..#/.#..#/.#..#/#...#/#...#");
        G('П',"#####/#...#/#...#/#...#/#...#/#...#/#...#");G('У',"#...#/#...#/#...#/.####/....#/....#/.###.");
        G('Ф',"..#../.###./#.#.#/#.#.#/.###./..#../..#..");G('Ц',"#..#./#..#./#..#./#..#./#..#./#####/....#");
        G('Ч',"#...#/#...#/#...#/.####/....#/....#/....#");G('Ш',"#.#.#/#.#.#/#.#.#/#.#.#/#.#.#/#.#.#/#####");
        G('Щ',"#.#.#/#.#.#/#.#.#/#.#.#/#.#.#/#####/....#");G('Ь',"#..../#..../#..../####./#...#/#...#/####.");
        G('Ы',"#...#/#...#/#...#/###.#/#.#.#/#.#.#/###.#");G('Ъ',"##.../.#.../.#.../.###./.#..#/.#..#/.###.");
        G('Э',"####./....#/....#/.####/....#/....#/####.");G('Ю',"#.##./##..#/#...#/##..#/#...#/##..#/#.##.");
        G('Я',".####/#...#/#...#/.####/..#.#/.#..#/#...#");glyphs['Ё']=glyphs['Е'];
    }
}
}
