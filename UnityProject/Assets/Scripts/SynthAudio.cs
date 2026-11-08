using System;
using UnityEngine;
namespace RedRivalry {
public sealed class SynthAudio:MonoBehaviour {
    AudioSource music,sfx;AudioClip hit,heavy,swing,block,cast,confirm,ko,paper,smoke,cough;public bool muted;public float volume=.65f;
    void Awake(){music=gameObject.AddComponent<AudioSource>();sfx=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.45f;sfx.volume=.5f;
        hit=Sound("impact",.13f,90,40,.5f);heavy=Sound("heavy",.27f,64,28,.65f);swing=Sound("swing",.1f,340,70,.18f);block=Sound("guard",.16f,840,380,.1f);cast=Sound("cast",.25f,180,880,.1f);confirm=Sound("confirm",.13f,440,880,0);ko=Sound("ko",.7f,130,32,.15f);
        paper=Noise("pages",.22f,1);smoke=Noise("smoke",.62f,.14f);cough=Noise("cough",.38f,.5f);
        music.clip=Resources.Load<AudioClip>("Audio/PaperAndSteam");if(music.clip==null)music.clip=Music();music.Play();Debug.Log("MUSIC_READY "+music.clip.name+" duration="+music.clip.length);
    }
    AudioClip Sound(string name,float duration,float f0,float f1,float noise){int n=(int)(22050*duration);float[] a=new float[n];var r=new System.Random(name.GetHashCode());float phase=0;for(int i=0;i<n;i++){float t=i/(float)n,f=Mathf.Lerp(f0,f1,t);phase+=f/22050f;float tone=(phase%1<.5f?1:-1)*.32f;a[i]=(tone+(float)(r.NextDouble()*2-1)*noise)*Mathf.Pow(1-t,2)*Mathf.Min(1,t*40);}var clip=AudioClip.Create(name,n,1,22050,false);clip.SetData(a,0);return clip;}
    AudioClip Music(){const int rate=22050;float beat=.42f;int n=(int)(rate*beat*32);float[] a=new float[n];int[] bass={38,38,41,36,38,38,43,36},mel={62,0,69,65,62,60,58,60,62,0,72,69,65,62,60,57,62,65,69,74,72,69,65,60,62,0,60,58,57,0,58,60};var r=new System.Random(55);
        for(int i=0;i<n;i++){float t=i/(float)rate;int b=(int)(t/beat);float u=t%beat,env=Mathf.Exp(-u*9);float bf=440*Mathf.Pow(2,(bass[(b/4)%8]-69)/12f);float sample=Mathf.Sin(t*bf*Mathf.PI*2)*.22f;
            if(mel[b%32]>0){float f=440*Mathf.Pow(2,(mel[b%32]-69)/12f);sample+=(Mathf.Repeat(t*f,1)<.3f?1:-1)*.065f*env;}
            if(b%4==0)sample+=Mathf.Sin(u*(90-u*120)*Mathf.PI*2)*Mathf.Exp(-u*25)*.28f;
            if(b%4==2)sample+=(float)(r.NextDouble()*2-1)*Mathf.Exp(-u*36)*.15f;
            sample+=(float)(r.NextDouble()*2-1)*Mathf.Exp(-(u%(beat/2))*70)*.025f;a[i]=sample;
        }var clip=AudioClip.Create("Original - Winter Duel",n,1,rate,false);clip.SetData(a,0);return clip;
    }
    AudioClip Noise(string name,float duration,float brightness){int n=(int)(22050*duration);float[] data=new float[n];var r=new System.Random(name.GetHashCode());float smooth=0;for(int i=0;i<n;i++){float t=i/(float)n;float white=(float)r.NextDouble()*2-1;smooth=Mathf.Lerp(smooth,white,brightness);float env=Mathf.Sin(t*Mathf.PI)*.55f;if(name=="cough")env*=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI*4)),2);data[i]=smooth*env;}var clip=AudioClip.Create(name,n,1,22050,false);clip.SetData(data,0);return clip;}
    public void Play(string name){if(muted)return;AudioClip c=name=="hit"?hit:name=="heavyhit"||name=="burst"?heavy:name=="block"?block:name=="swing"?swing:name=="ko"?ko:name=="confirm"?confirm:name=="book"||name=="library"||name=="argument"?paper:name=="smoke"?smoke:name=="cough"?cough:cast;sfx.PlayOneShot(c,volume);}
    public void Toggle(){muted=!muted;music.mute=muted;sfx.mute=muted;}
}
}
