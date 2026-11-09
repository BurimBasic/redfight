using System.IO;
using UnityEngine;
using RedRivalry;
public static class ArenaReview {
    public static void Export(){
        string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Review/Arenas"));Directory.CreateDirectory(directory);var canvas=new PixelCanvas();var arena=new ArenaArt(canvas);
        for(int i=0;i<ArenaArt.Count;i++){Frame(canvas,arena,i,7.7f);canvas.Upload();File.WriteAllBytes(Path.Combine(directory,"arena-"+i+".png"),canvas.texture.EncodeToPNG());}
        byte[] rgb=new byte[640*360*3];using(var stream=File.Create(Path.Combine(directory,"metro.rgb")))for(int i=0;i<72;i++){
            Frame(canvas,arena,2,2+i/12f);for(int y=0;y<360;y++)for(int x=0;x<640;x++){var c=canvas.pixels[(359-y)*640+x];int k=(y*640+x)*3;rgb[k]=c.r;rgb[k+1]=c.g;rgb[k+2]=c.b;}stream.Write(rgb,0,rgb.Length);
        }
        Debug.Log("ARENA_REVIEW_EXPORTED "+directory);
    }
    static void Frame(PixelCanvas p,ArenaArt art,int stage,float time){
        art.Draw(stage,time);p.Ellipse(210,305,30,5,PixelCanvas.C(0x152333));p.Ellipse(430,305,30,5,PixelCanvas.C(0x152333));FighterArt.Draw(p,new Fighter{kind=0,x=210,facing=1},time);FighterArt.Draw(p,new Fighter{kind=1,x=430,facing=-1},time);
        p.Rect(18,15,330,29,PixelCanvas.C(0x101b2c));p.Text(ArenaArt.NumberedTitle(stage),27,25,PixelCanvas.C(0xd9b781));
    }
}
