using System;
using System.IO;
using UnityEngine;
namespace RedRivalry {
// Optional local diagnostic, enabled only with --controller-check <folder>.
public sealed class ControllerProbe {
    readonly string directory;readonly Controls.Buttons[] seen=new Controls.Buttons[2];readonly bool[] stick=new bool[2],rightStick=new bool[2],lt=new bool[2],rt=new bool[2],rumble=new bool[2];float next;bool captured;
    public ControllerProbe(string path){directory=path;Directory.CreateDirectory(path);}
    public void Update(Controls controls,PixelCanvas canvas){
        for(int i=0;i<2;i++){
            foreach(Controls.Buttons b in Enum.GetValues(typeof(Controls.Buttons)))if(controls.Held(i,b))seen[i]|=b;
            stick[i]|=controls.Stick(i).magnitude>.6f;rightStick[i]|=controls.RightStick(i).magnitude>.6f;lt[i]|=controls.Trigger(i,false)>.6f;rt[i]|=controls.Trigger(i,true)>.6f;
        }
        if(Time.unscaledTime>=next){next=Time.unscaledTime+1;Finish(controls);}
        if(!captured&&Time.unscaledTime>2){captured=true;File.WriteAllBytes(Path.Combine(directory,"controllers.png"),canvas.texture.EncodeToPNG());}
    }
    public void RumbleRequested(int slot){rumble[slot]=true;}
    public void Finish(Controls controls){string report="Version "+Application.version+"\n";for(int i=0;i<2;i++)report+="P"+(i+1)+" "+controls.DeviceName(i)+"; buttons="+seen[i]+"; leftStick="+stick[i]+"; rightStick="+rightStick[i]+"; L2="+lt[i]+"; R2="+rt[i]+"; rumbleRequested="+rumble[i]+"\n";File.WriteAllText(Path.Combine(directory,"controller-check.txt"),report);}
}
}
