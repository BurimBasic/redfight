using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RedRivalry;
public static class BuildGame {
    static int passed;
    static void Check(bool ok,string name){if(!ok)throw new Exception("CHECK FAILED: "+name);passed++;Debug.Log("PASS: "+name);}
    public static void Build(){
        try {
            ConfigureArt();if(Array.IndexOf(Environment.GetCommandLineArgs(),"--art-review")>=0)ExportWalkReview();RunChecks();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.SaveScene(scene,"Assets/Scenes/RedRivalry.unity");
            PlayerSettings.companyName="Pixel Duel Studio";PlayerSettings.productName="Красный поединок";PlayerSettings.bundleVersion="3.0.0";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.colorSpace=ColorSpace.Gamma;
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var handler=settings.FindProperty("activeInputHandler");if(handler!=null){handler.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();}
            string target=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Windows/RedRivalry.exe"));Directory.CreateDirectory(Path.GetDirectoryName(target));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/RedRivalry.unity"},locationPathName=target,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Проверки.txt")),"Проверки логики: "+passed+" успешно.\nСборка: Unity "+Application.unityVersion+" / Windows x64.\n");
            Debug.Log("BUILD_COMPLETE "+target+" CHECKS="+passed);EditorApplication.Exit(0);
        }catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static void ConfigureArt(){
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"})){
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio"})){var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();}
    }
    static void ExportWalkReview(){
        string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Review"));Directory.CreateDirectory(dir);var p=new PixelCanvas();var art=new ArenaArt(p);byte[] rgb=new byte[640*360*3];
        using(var stream=File.Create(Path.Combine(dir,"walk-review.rgb")))for(int frame=0;frame<12;frame++){
            art.Draw(0,0);for(int kind=0;kind<2;kind++)FighterArt.Draw(p,new Fighter{kind=kind,x=210+kind*220,facing=kind==0?1:-1,walking=true,walkPhase=(frame+.01f)/3.3f},0);
            for(int y=0;y<360;y++)for(int x=0;x<640;x++){var c=p.pixels[(359-y)*640+x];int k=(y*640+x)*3;rgb[k]=c.r;rgb[k+1]=c.g;rgb[k+2]=c.b;}stream.Write(rgb,0,rgb.Length);
        }
        Debug.Log("WALK_REVIEW_EXPORTED "+dir);
    }
    static void Advance(CombatWorld w,int frames,Command a=default,Command b=default){for(int i=0;i<frames;i++)w.Step(1f/60,a,b);}
    public static void RunChecks(){
        passed=0;var w=new CombatWorld();w.Reset();var a=w.fighters[0];var b=w.fighters[1];
        Check(a.hp==100&&b.hp==100&&a.x<b.x,"round reset");
        Advance(w,60,new Command{move=-1});Check(a.x>=52,"left arena boundary");Advance(w,400,new Command{move=1});Check(a.x<=588&&Mathf.Abs(a.x-b.x)>=36,"right boundary and body collision");
        w.Reset();a.x=280;b.x=340;Advance(w,1,new Command{light=true});Advance(w,28);Check(b.hp==92&&w.totalHits==1,"light strike hits once");
        w.Reset();a.x=280;b.x=340;Advance(w,1,new Command{heavy=true});Advance(w,50);Check(b.hp==84,"heavy strike active window");
        w.Reset();a.x=180;b.x=440;Advance(w,1,new Command{light=true});Advance(w,40);Check(b.hp==100,"out of range strike misses");
        w.Reset();a.x=280;b.x=340;Advance(w,1,new Command(),new Command{block=true});Advance(w,1,new Command{heavy=true},new Command{block=true});Advance(w,50,new Command(),new Command{block=true});Check(b.hp>97&&b.hp<100&&w.blockedHits>0,"frontal block reduces damage");
        w.Reset();a.x=280;b.x=340;b.facing=1;b.blocking=true;w.Hit(a,b,16,true,1);Check(b.hp==84,"block does not protect back");
        w.Reset();Advance(w,1,new Command{jump=true});Advance(w,15);Check(a.height>20,"jump rises");Advance(w,100);Check(a.height==0&&a.vy==0,"jump lands");
        w.Reset();a.energy=0;Advance(w,1,new Command{special=true,skill=true,ultimate=true});Check(a.state==ActionState.Idle,"abilities require energy");
        w.Reset();a.kind=0;a.x=220;b.x=375;Advance(w,1,new Command{special=true});Advance(w,70);Check(b.hp==87&&w.projectiles.Count==0,"Lenin book travels and hits once");
        w.Reset();a.x=220;b.x=375;w.projectiles.Add(new Projectile{owner=a,x=350,y=213,vx=285,life=2,type=0,damage=13});b.height=58;b.vy=0;Advance(w,8);Check(b.hp==100,"jump clears a thrown book");
        int coughs=0;w.Effect+=(name,f)=>{if(name=="cough")coughs++;};
        w.Reset();a.kind=1;a.x=220;b.x=380;Advance(w,1,new Command{special=true});Advance(w,140);Check(b.hp==89&&w.projectiles.Count==0&&coughs==1,"Stalin smoke damages once and makes opponent cough");
        w.Reset();a.kind=0;a.x=260;b.x=340;a.energy=100;Advance(w,1,new Command{skill=true});Advance(w,45);Check(b.hp==83&&b.state==ActionState.Hurt,"Lenin argument stuns at short range");
        w.Reset();a.kind=1;a.energy=100;Advance(w,1,new Command{skill=true});Advance(w,5);w.Hit(b,a,20,true,-1);Check(a.hp==100&&a.invulnerable>0&&b.hp==100,"smoke curtain protects without phantom damage");
        w.Reset();a.kind=0;b.kind=1;a.x=280;b.x=400;a.energy=b.energy=100;Advance(w,1,new Command{special=true},new Command{skill=true});Advance(w,65);Check(b.hp==100,"smoke curtain absorbs an incoming book");
        w.Reset();a.kind=0;a.x=240;b.x=340;a.energy=100;Advance(w,1,new Command{ultimate=true});Advance(w,120);Check(b.hp==64&&a.energy<35,"collected works launches three separately damaging books");
        w.Reset();a.kind=1;a.x=240;b.x=340;a.energy=100;Advance(w,1,new Command{ultimate=true});Advance(w,140);Check(b.hp==66,"big puff delivers its full damage");
        w.Reset();a.kind=0;a.energy=100;a.x=52;b.x=588;for(int book=0;book<3;book++){a.energy=100;Advance(w,1,new Command{special=true});Advance(w,22);Check(w.projectiles.Exists(pj=>pj.owner==a&&pj.type==0&&pj.book==book),"real book title rotation "+book);Advance(w,140);}
        w.Reset();a.kind=0;Advance(w,1,new Command{special=true});Advance(w,5);w.Hit(b,a,8,false,-1);Advance(w,65);Check(w.projectiles.Count==0,"interrupted windup does not throw a ghost book");
        w.Reset();a.x=280;b.x=340;b.hp=5;Advance(w,1,new Command{light=true});Advance(w,50);Check(b.hp==0&&b.state==ActionState.KO,"knockout stops opponent");
        w.Reset();b.invulnerable=1;w.Hit(a,b,10,false,1);Check(b.hp==100,"invulnerability prevents repeat hits");
        w.Reset();a.energy=200;Advance(w,1);Check(a.energy<=100,"energy clamped");
        var pad=new Controls();pad.InjectForTest(0,0x4000,24000);var command=pad.Read(0,false);Check(command.light&&command.move>.5f,"XInput X and left stick mapping");pad.InjectForTest(0,0x4000,0);Check(!pad.Read(0,false).light,"held X does not repeat press");pad.InjectForTest(0,0x1000);Check(pad.Read(0,false).jump,"XInput A jump mapping");pad.InjectForTest(0,0x8000);Check(pad.Read(0,false).heavy,"XInput Y heavy mapping");pad.InjectForTest(0,0x2000);Check(pad.Read(0,false).special,"XInput B special mapping");pad.InjectForTest(0,0x200);Check(pad.Read(0,false).skill,"XInput RB skill mapping");pad.InjectForTest(0,0x100);Check(pad.Read(0,false).block,"XInput LB block mapping");
        Check(SpriteArt.LoadedFrames==24,"24 registered character animation frames");
        Check(SpriteArt.CastFrames==8,"eight dedicated book and pipe animation frames");
        Check(SmokeVfx.Loaded,"pixel-art smoke texture is included");
        Check(SpriteArt.WalkFrames==16,"sixteen new hand-drawn inbetween frames");
        Check(SpriteArt.WalkKeyAt(0)==0&&SpriteArt.WalkKeyAt(3)==2&&SpriteArt.WalkKeyAt(6)==1&&SpriteArt.WalkKeyAt(9)==3,"accepted four walk keyframes stay in their original order");
        var idle=new PixelCanvas();var walking=new PixelCanvas();FighterArt.Draw(idle,new Fighter{kind=0,x=220,facing=1},0);FighterArt.Draw(walking,new Fighter{kind=0,x=220,facing=1,walking=true,walkPhase=0},0);bool unchanged=true;for(int i=0;i<idle.pixels.Length;i++)if(!idle.pixels[i].Equals(walking.pixels[i])){unchanged=false;break;}Check(unchanged,"original starting walk drawing is unchanged pixel for pixel");
        w.Reset();float beforePhase=a.walkPhase;Advance(w,30,new Command{move=1});Check(a.walking&&a.walkPhase>beforePhase,"walk animation advances while moving");float stoppedPhase=a.walkPhase;Advance(w,15);Check(!a.walking&&a.walkPhase==stoppedPhase,"stopping does not cycle walk frames");
        var score=Resources.Load<AudioClip>("Audio/PaperAndSteam");Check(score!=null&&score.length>100&&score.length<102&&score.channels==2,"full AY soundtrack is included");
        var ay=Resources.Load<TextAsset>("Audio/PaperAndSteam-AY");Check(ay!=null&&System.Text.Encoding.ASCII.GetString(ay.bytes,0,4)=="YM6!"&&ay.bytes[26]==0&&ay.bytes[27]==50,"native PSG register score is included at 50 Hz");
        var p=new PixelCanvas();var art=new ArenaArt(p);Check(art.backgrounds[0].Length==640*360&&art.backgrounds[1].Length==640*360,"both arenas render");
        foreach(ActionState state in Enum.GetValues(typeof(ActionState))){a.state=state;a.stateTime=.3f;a.stateDuration=.6f;FighterArt.Draw(p,a,1);a.kind=1;FighterArt.Draw(p,a,1);}
        p.Upload();Check(p.texture.width==640,"all animation states render");
        w.Reset();a.kind=0;b.kind=1;var aiA=new OpponentAI();var aiB=new OpponentAI();for(int i=0;i<60*110&&a.hp>0&&b.hp>0;i++)w.Step(1f/60,aiA.Think(a,b,1f/60,2),aiB.Think(b,a,1f/60,2));Check(a.hp<=0||b.hp<=0,"AI can finish a full fight");
        Debug.Log("ALL_CHECKS_PASSED "+passed);
    }
}
