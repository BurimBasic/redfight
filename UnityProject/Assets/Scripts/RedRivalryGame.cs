using System;
using System.Collections.Generic;
using UnityEngine;
using static RedRivalry.PixelCanvas;
namespace RedRivalry {
public enum ScreenMode { Menu,Select,Fight,Pause,Guide,Result,Controllers }
public sealed class RedRivalryGame:MonoBehaviour {
    public PixelCanvas canvas;public ArenaArt arena;public CombatWorld world;public Controls controls;public SynthAudio audioFx;
    public ScreenMode mode;ScreenMode guideReturn=ScreenMode.Menu;
    public bool local,training;public int selectedFighter,stage,difficulty=1,round=1;public float remaining=90,countdown=3.3f,roundEnd=-1;
    public string announcement="";float announceTime,clockTime,rumbleTime;int focus;Vector2 previousMouse;
    readonly List<UiButton> buttons=new List<UiButton>();readonly List<Particle> particles=new List<Particle>();readonly List<Speech> speeches=new List<Speech>();
    OpponentAI ai=new OpponentAI();bool matchFinished,qa,showcase;float qaTime;int qaPhase,qaCaptureCount;string qaDirectory;
    ControllerProbe controllerProbe;string disconnectNotice="";
    static Color32 White=C(0xf0e7cf),Gold=C(0xd9b781),Muted=C(0x8a9ba8),Red=C(0xe85c65),Teal=C(0x73d4cf);
    sealed class UiButton{public Rect rect;public string text;public Action action;}
    sealed class Particle{public float x,y,vx,vy,life,max;public Color32 color;public int size;}
    sealed class Speech{public Fighter actor;public string text;public float life;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Bootstrap(){if(FindFirstObjectByType<RedRivalryGame>()==null)new GameObject("RED RIVALRY").AddComponent<RedRivalryGame>();}
    void Awake(){
        Application.targetFrameRate=60;QualitySettings.vSyncCount=1;Application.runInBackground=true;
        canvas=new PixelCanvas();arena=new ArenaArt(canvas);world=new CombatWorld();controls=new Controls();audioFx=gameObject.AddComponent<SynthAudio>();world.Effect+=OnEffect;world.Reset();mode=ScreenMode.Menu;
        var camera=new GameObject("Pixel Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.gameObject.AddComponent<AudioListener>();
        difficulty=Mathf.Clamp(PlayerPrefs.GetInt("difficulty",1),0,2);stage=Mathf.Clamp(PlayerPrefs.GetInt("stage",0),0,1);if(PlayerPrefs.GetInt("muted",0)==1)audioFx.Toggle();
        var args=Environment.GetCommandLineArgs();showcase=Array.IndexOf(args,"--showcase")>=0;int ix=Array.IndexOf(args,"--qa");if(ix>=0){qa=true;qaDirectory=ix+1<args.Length?args[ix+1]:Application.persistentDataPath;System.IO.Directory.CreateDirectory(qaDirectory);if(!audioFx.muted)audioFx.Toggle();stage=0;Debug.Log("QA_RUNTIME_STARTED");}
        int probe=Array.IndexOf(args,"--controller-check");if(probe>=0){controllerProbe=new ControllerProbe(probe+1<args.Length?args[probe+1]:Application.persistentDataPath);mode=ScreenMode.Controllers;}
        Render();
    }
    void OnApplicationQuit(){controls.StopRumble();controllerProbe?.Finish(controls);if(qa)return;PlayerPrefs.SetInt("difficulty",difficulty);PlayerPrefs.SetInt("stage",stage);PlayerPrefs.SetInt("muted",audioFx.muted?1:0);PlayerPrefs.Save();}
    void OnApplicationFocus(bool hasFocus){controls?.Resync();if(!hasFocus&&mode==ScreenMode.Fight&&!qa){mode=ScreenMode.Pause;focus=0;}}
    Rect Viewport(){float s=Mathf.Min(Screen.width/(float)W,Screen.height/(float)H);return new Rect((Screen.width-W*s)/2,(Screen.height-H*s)/2,W*s,H*s);}
    Vector2 Mouse(){var v=Viewport();return new Vector2((Input.mousePosition.x-v.x)/v.width*W,((Screen.height-Input.mousePosition.y)-v.y)/v.height*H);}
    void Update(){
        float dt=Mathf.Min(Time.unscaledDeltaTime,.034f);clockTime+=dt;controls.Poll();
        controllerProbe?.Update(controls,canvas);
        if(!Application.isFocused&&!qa&&controllerProbe==null)return;
        if(mode==ScreenMode.Fight&&!qa&&(controls.DisconnectedThisFrame&(local?3:1))!=0){disconnectNotice="КОНТРОЛЛЕР ОТКЛЮЧЁН";mode=ScreenMode.Pause;focus=0;controls.StopRumble();}
        if(Input.GetKeyDown(KeyCode.F11))Screen.fullScreen=!Screen.fullScreen;
        if(Input.GetKeyDown(KeyCode.M)){audioFx.Toggle();}
        if(Input.GetKeyDown(KeyCode.F12))SaveShot(System.IO.Path.Combine(Application.persistentDataPath,"Screenshot.png"));
        if(rumbleTime>0){rumbleTime-=dt;if(rumbleTime<=0)controls.StopRumble();}
        if(mode==ScreenMode.Fight){
            if(controls.Pause&&!qa){mode=ScreenMode.Pause;focus=0;controls.StopRumble();}
            else TickFight(dt);
        } else TickMenus();
        for(int i=particles.Count-1;i>=0;i--){var p=particles[i];p.life-=dt;p.x+=p.vx*dt;p.y+=p.vy*dt;p.vy+=90*dt;if(p.life<=0)particles.RemoveAt(i);}
        announceTime=Mathf.Max(0,announceTime-dt);
        for(int i=speeches.Count-1;i>=0;i--){speeches[i].life-=dt;if(speeches[i].life<=0)speeches.RemoveAt(i);}
        if(qa)TickQa(dt);
        Render();
    }
    void TickMenus(){
        if(mode==ScreenMode.Controllers){
            for(int slot=0;slot<2;slot++)if(controls.Pressed(slot,Controls.Buttons.Light)){controls.Rumble(slot,true);rumbleTime=.35f;controllerProbe?.RumbleRequested(slot);}
            if(controls.Back||(Input.GetMouseButtonDown(0)&&new Rect(39,307,138,23).Contains(Mouse()))){mode=ScreenMode.Guide;focus=0;}return;
        }
        if(mode==ScreenMode.Select){if(controls.Left||controls.Right){selectedFighter=1-selectedFighter;audioFx.Play("confirm");}if(controls.Back){mode=ScreenMode.Menu;focus=0;return;}}
        if(mode==ScreenMode.Guide&&controls.Back){mode=guideReturn;focus=0;return;}
        if(mode==ScreenMode.Pause&&controls.Pause){disconnectNotice="";mode=ScreenMode.Fight;return;}
        if(buttons.Count==0)return;
        if(controls.Down){focus=(focus+1)%buttons.Count;audioFx.Play("confirm");}
        if(controls.Up){focus=(focus+buttons.Count-1)%buttons.Count;audioFx.Play("confirm");}
        Vector2 mouse=Mouse();bool moved=(mouse-previousMouse).sqrMagnitude>.5f;previousMouse=mouse;
        int hit=-1;for(int i=0;i<buttons.Count;i++)if(buttons[i].rect.Contains(mouse)){hit=i;if(moved)focus=i;}
        focus=Mathf.Clamp(focus,0,buttons.Count-1);
        if(controls.Confirm||(Input.GetMouseButtonDown(0)&&hit>=0)){int index=Input.GetMouseButtonDown(0)&&hit>=0?hit:focus;var action=buttons[index].action;audioFx.Play("confirm");action();}
    }
    void TickFight(float dt){
        if(countdown>0){countdown-=dt;return;}
        if(roundEnd>=0){world.Step(dt,new Command(),new Command());roundEnd+=dt;if(roundEnd>2.6f){if(matchFinished){mode=ScreenMode.Result;focus=0;}else{round++;ResetRound();}}return;}
        Command a=controls.Read(0,local),b=local?controls.Read(1,true):training?new Command():ai.Think(world.fighters[1],world.fighters[0],dt,difficulty);
        if(qa&&showcase){float t=qaTime-1.3f;a=new Command{move=t<.8f?1:t<1.2f?-1:t>4.2f&&t<5.6f?1:0,special=Beat(t,1.6f)||Beat(t,6.8f),skill=Beat(t,5.8f),ultimate=Beat(t,8.1f)};b=new Command{move=t<.8f?-1:t<1.2f?1:0,special=Beat(t,3.2f),skill=Beat(t,6.8f),ultimate=Beat(t,10.4f)};}
        else if(qa) {a=new Command{move=Mathf.Sign(world.fighters[1].x-world.fighters[0].x),light=qaPhase%28==0,heavy=qaPhase%67==0,special=qaPhase%125==0,skill=qaPhase%190==0};qaPhase++;}
        world.Step(dt,a,b);
        if(training){remaining=90;foreach(var f in world.fighters){f.energy=100;if(f.hp<30&&f.state==ActionState.Idle)f.hp=100;}}
        else remaining=Mathf.Max(0,remaining-dt);
        if(world.fighters[0].hp<=0||world.fighters[1].hp<=0||remaining<=0){
            if(training){world.Reset();return;}
            roundEnd=0;world.projectiles.Clear();int winner=world.fighters[0].hp==world.fighters[1].hp?-1:world.fighters[0].hp>world.fighters[1].hp?0:1;
            if(winner>=0){var f=world.fighters[winner];f.wins++;f.Set(ActionState.Victory,999);announcement=(f.kind==0?"ЛЕНИН":"СТАЛИН")+" ПОБЕЖДАЕТ";matchFinished=f.wins>=2;}
            else {announcement="НИЧЬЯ";matchFinished=false;}
            announceTime=3;audioFx.Play("ko");
        }
    }
    public void BeginMatch(int kind,bool isLocal=false,bool practice=false){disconnectNotice="";selectedFighter=kind;local=isLocal;training=practice;world.fighters[0].kind=kind;world.fighters[1].kind=1-kind;foreach(var f in world.fighters)f.wins=0;round=1;matchFinished=false;particles.Clear();ai=new OpponentAI();ResetRound();mode=ScreenMode.Fight;}
    void ResetRound(){world.Reset();remaining=90;countdown=3.3f;roundEnd=-1;announceTime=0;particles.Clear();speeches.Clear();}
    static bool Beat(float t,float when){return t>=when&&t<when+.08f;}
    void Select(bool two,bool practice){local=two;training=practice;mode=ScreenMode.Select;focus=0;}
    void Guide(ScreenMode back){guideReturn=back;mode=ScreenMode.Guide;focus=0;}
    void OnEffect(string name,Fighter f){
        if(name=="hit"||name=="heavyhit"||name=="block"||name=="burst"||name=="ice"){
            int n=name=="burst"?60:name=="heavyhit"?30:18;var random=new System.Random(world.totalHits*37+(int)(clockTime*300));
            for(int i=0;i<n;i++){float a=(float)random.NextDouble()*Mathf.PI*2;float speed=30+(float)random.NextDouble()*140;particles.Add(new Particle{x=f.x+f.facing*12,y=300-f.height-90,vx=Mathf.Cos(a)*speed,vy=Mathf.Sin(a)*speed-20,life=.18f+(float)random.NextDouble()*.4f,max=.6f,color=name=="block"||name=="ice"?Teal:i%3==0?White:Gold,size=i%6==0?3:1});}
            controls.Rumble(f.slot,name=="heavyhit");rumbleTime=.12f;
        }
        if(name=="ultimate"){announcement=f.kind==0?"ПОЛНОЕ СОБРАНИЕ СОЧИНЕНИЙ":"БОЛЬШАЯ ЗАТЯЖКА";announceTime=1.0f;}
        if(name=="cough"||name=="argument"||name=="smoke"&&f.state==ActionState.Skill)speeches.Add(new Speech{actor=f,text=name=="cough"?"КХЕ-КХЕ!":name=="argument"?"ТЕОРИЯ - СИЛА!":"ПЕРЕКУР!",life=1.0f});
        if(name!="land"&&name!="jump")audioFx.Play(name);
    }
    public void SaveShot(string path){Render();System.IO.File.WriteAllBytes(path,canvas.texture.EncodeToPNG());}
    void Render(){
        buttons.Clear();arena.Draw(stage,clockTime);
        bool fighting=mode==ScreenMode.Fight||mode==ScreenMode.Pause||mode==ScreenMode.Result;
        if(fighting){DrawBattle();DrawHud();}
        else if(mode==ScreenMode.Menu)DrawMenu();
        else if(mode==ScreenMode.Select)DrawSelect();
        else if(mode==ScreenMode.Guide)DrawGuide();
        else if(mode==ScreenMode.Controllers)DrawControllers();
        if(mode==ScreenMode.Pause)DrawPause();
        if(mode==ScreenMode.Result)DrawResult();
        canvas.Upload();
    }
    void OnGUI(){GUI.color=Color.white;GUI.DrawTexture(Viewport(),canvas.texture,ScaleMode.StretchToFill,false);}
    void R(int x,int y,int w,int h,uint c){canvas.Rect(x,y,w,h,C(c));}
    void Panel(int x,int y,int w,int h){for(int j=0;j<h;j++)canvas.Rect(x,y+j,w,1,Color32.Lerp(C(0x192334),C(0x0c1423),j/(float)h));canvas.Frame(x,y,w,h,C(0x635d59));R(x+1,y+1,w-2,1,0x9c896d);R(x,y,4,1,0xd9b781);R(x+w-4,y+h-1,4,1,0xd9b781);}
    void Button(string text,int x,int y,int w,Action action,bool small=false){
        int i=buttons.Count,h=small?23:27;bool active=i==focus;
        for(int row=0;row<h;row++)canvas.Rect(x,y+row,w,1,Color32.Lerp(C(active?0x813244u:0x1d293bu),C(active?0x422136u:0x111b2cu),row/(float)h));
        canvas.Frame(x,y,w,h,active?Gold:C(0x4b4e56));R(x+1,y+1,w-2,1,active?0xe1ad7bu:0x394555u);if(active){R(x,y,3,h,0xf0c285);R(x+5,y+5,1,h-10,0xb16c63);}
        canvas.Text(text,x+14,y+(h-7)/2,active?White:Muted,1,true);if(active)canvas.Text(">",x+w-15,y+(h-7)/2,Gold);
        buttons.Add(new UiButton{rect=new Rect(x,y,w,h),text=text,action=action});
    }
    void DrawMenu(){
        var f0=new Fighter{kind=0,x=418,facing=1,slot=0};var f1=new Fighter{kind=1,x=548,facing=-1,slot=1};
        canvas.Ellipse(418,313,34,5,C(0x172435));canvas.Ellipse(548,313,34,5,C(0x172435));FighterArt.Draw(canvas,f0,clockTime,1.72f,false,309);FighterArt.Draw(canvas,f1,clockTime+1,1.72f,false,309);
        for(int y=0;y<H;y++)for(int x=0;x<355;x++){int k=(H-1-y)*W+x;float alpha=x<315?.94f:Mathf.Lerp(.94f,0,(x-315)/40f);canvas.pixels[k]=Color32.Lerp(canvas.pixels[k],C(0x0b1322),alpha);}
        R(26,30,4,16,0xde5b62);canvas.Text("ЮМОРИСТИЧЕСКИЙ ФАЙТИНГ / 03",39,34,Gold);canvas.Text("КРАСНЫЙ",26,65,White,6,true);canvas.Text("ПОЕДИНОК",26,116,Red,6,true);
        canvas.Text("КНИГИ, ТРУБКА И НЕМНОГО ХАОСА",29,172,Gold);
        Button("ОДИНОЧНЫЙ БОЙ",29,198,273,()=>Select(false,false));Button("ДВОЕ У ОДНОГО ЭКРАНА",29,230,273,()=>Select(true,false));Button("ТРЕНИРОВКА",29,262,273,()=>Select(false,true));
        Button("УПРАВЛЕНИЕ",29,294,169,()=>Guide(ScreenMode.Menu),true);Button(audioFx.muted?"ЗВУК: ВЫКЛ":"ЗВУК: ВКЛ",203,294,99,()=>audioFx.Toggle(),true);
        canvas.Text("ENTER / "+controls.ConfirmKey+": ВЫБРАТЬ",29,337,Muted);canvas.Text("ESC / "+controls.BackKey+": НАЗАД",217,337,Muted);
        canvas.Text(controls.AnyPad?"P1: "+controls.DeviceName(0):"КЛАВИАТУРА / МЫШЬ / ГЕЙМПАД",29,349,controls.AnyPad?Teal:Muted);
        canvas.Text("1917",365,33,Red,2);canvas.Text("АЛЬТЕРНАТИВНАЯ ИСТОРИЯ",365,54,Muted,1,true);
        canvas.Text("ЛЕНИН",382,322,White,2,true);canvas.Text("СТАЛИН",513,322,White,2,true);canvas.Text("КНИГИ",390,343,Gold,1,true);canvas.Text("ТРУБКА",520,343,Teal,1,true);
        Button("ВЫХОД",541,25,76,()=>Application.Quit(),true);
        canvas.Text(stage==0?"01 / ЗИМНЯЯ ПЛОЩАДЬ":"02 / ЛИТЕЙНЫЙ ЦЕХ",365,91,Gold,1,true);
        canvas.Text("ДО ДВУХ ПОБЕД",365,105,Muted,1,true);R(365,122,246,1,0x5c5360);canvas.Center("VS",486,203,Gold,2,true);
    }
    void DrawSelect(){
        canvas.Dim(.35f);canvas.Text("ВЫБЕРИ СВОЕГО БОЙЦА",35,26,White,3);canvas.Text(training?"ТРЕНИРОВКА / БЕСКОНЕЧНАЯ ЭНЕРГИЯ":local?"ЛОКАЛЬНЫЙ БОЙ / ДО ДВУХ ПОБЕД":"ОДИНОЧНЫЙ БОЙ / ДО ДВУХ ПОБЕД",36,58,Muted);
        for(int i=0;i<2;i++){
            int x=35+i*294;Panel(x,80,276,198);canvas.Frame(x,80,276,198,i==selectedFighter?Gold:C(0x354453),i==selectedFighter?2:1);
            canvas.Text(i==0?"01 / ЛЕНИН":"02 / СТАЛИН",x+17,94,i==selectedFighter?Gold:Muted,2);
            canvas.Ellipse(x+65,272,31,5,C(0x070f1c));FighterArt.Draw(canvas,new Fighter{kind=i,x=x+65,facing=1},clockTime,1.35f,false,268);
            canvas.Text(i==0?"МОЛОТ + КНИГИ":"СЕРП + ТРУБКА",x+127,136,White);
            canvas.Text(i==0?"КНИЖНЫЙ БРОСОК":"ДЫМ ИЗ ТРУБКИ",x+127,157,i==0?Red:Teal);
            canvas.Text(i==0?"СИЛА АРГУМЕНТА":"ДЫМОВАЯ ЗАВЕСА",x+127,177,Muted);
            canvas.Text("СУПЕРПРИЁМ",x+127,208,Gold);canvas.Text(i==0?"ПОЛНОЕ":"БОЛЬШАЯ",x+127,225,White);canvas.Text(i==0?"СОБРАНИЕ":"ЗАТЯЖКА",x+127,240,White);
            if(Input.GetMouseButtonDown(0)&&new Rect(x,80,276,198).Contains(Mouse()))selectedFighter=i;
        }
        Button("НАЧАТЬ БОЙ",35,294,196,()=>BeginMatch(selectedFighter,local,training));
        Button(stage==0?"АРЕНА: ПЛОЩАДЬ":"АРЕНА: ЦЕХ",240,294,195,()=>stage=1-stage);
        Button("ИИ: "+(difficulty==0?"ЛЕГКО":difficulty==1?"НОРМА":"СЛОЖНО"),444,294,161,()=>difficulty=(difficulty+1)%3);
        canvas.Text("A / D ИЛИ СТИК: БОЕЦ   ENTER / "+controls.ConfirmKey+": СТАРТ   ESC / "+controls.BackKey+": НАЗАД",35,341,Muted);
    }
    void DrawBattle(){
        foreach(var f in world.fighters)canvas.Ellipse((int)f.x,305,(int)(30-f.height*.09f),5,C(0x152333));
        foreach(var f in world.fighters){
            FighterArt.Draw(canvas,f,clockTime);
        }
        foreach(var q in world.projectiles){
            ComicArt.Projectile(canvas,q);
        }
        foreach(var p in particles)canvas.Rect((int)p.x,(int)p.y,p.size,p.size,p.color);
        foreach(var speech in speeches){int w=canvas.TextWidth(speech.text)+12,x=Mathf.Clamp((int)speech.actor.x-w/2,4,W-w-4),y=Mathf.Clamp((int)(300-speech.actor.height-157),107,190);R(x,y,w,17,0xefdcb5);canvas.Frame(x,y,w,17,C(0x3a2c36));canvas.Text(speech.text,x+6,y+5,C(0x3a2c36));canvas.Poly(C(0xefdcb5),new Vector2(x+w/2-3,y+16),new Vector2(x+w/2+3,y+16),new Vector2(x+w/2,y+21));}
        if(world.shake>0){int sx=(int)(Mathf.Sin(clockTime*130)*world.shake);if(sx!=0){var copy=(Color32[])canvas.pixels.Clone();for(int y=90;y<325;y++)for(int x=0;x<W;x++)if(x+sx>=0&&x+sx<W)canvas.pixels[(H-1-y)*W+x]=copy[(H-1-y)*W+x+sx];}}
        for(int i=0;i<2;i++){var f=world.fighters[i];if(f.combo>1){int x=i==0?37:495;string word=f.combo%10>=2&&f.combo%10<=4&&(f.combo%100<12||f.combo%100>14)?" УДАРА":" УДАРОВ";canvas.Text(f.combo+word,x,111,i==0?Red:Teal,2,true);canvas.Text("КОМБО",x,130,Gold,1,true);}}
        if(countdown>0){string text=countdown>1.1f?"РАУНД "+round:"В БОЙ!";canvas.Center(text,320,122,countdown>1.1f?White:Gold,3,true);}
        if(announceTime>0){canvas.Center(announcement,320,123,Gold,1,true);}
        R(0,326,640,34,0x0d1828);R(0,326,640,1,0x4c5260);
        canvas.Text(training?"ТРЕНИРОВКА":"РАУНД "+round,19,336,Gold);
        canvas.Text(controls.IsConnected(0)?controls.LightKey(0)+": УДАР   "+controls.HeavyKey(0)+": МОЩНЫЙ   "+controls.BlockKey(0)+": БЛОК":"J / ЛКМ: УДАР   K / ПКМ: МОЩНЫЙ   SHIFT: БЛОК",143,336,Muted);
        canvas.Text(controls.IsConnected(0)?controls.SpecialKey(0)+": КНИГА/ДЫМ   "+controls.SkillKey(0)+": ПРИЁМ   "+controls.UltimateKey(0)+": СУПЕР   "+controls.PauseKey+": ПАУЗА":"Q: КНИГА/ДЫМ   E: ПРИЁМ   R: СУПЕР   ESC: ПАУЗА",143,348,Muted);canvas.Text("F11: ЭКРАН",556,348,Muted);
    }
    void DrawHud(){
        for(int y=0;y<88;y++)for(int x=0;x<W;x++){int k=(H-1-y)*W+x;canvas.pixels[k]=Color32.Lerp(canvas.pixels[k],C(0x0a1220),y<76?.94f:Mathf.Lerp(.94f,.45f,(y-76)/12f));}R(0,87,640,1,0x796a5d);
        for(int i=0;i<2;i++){
            var f=world.fighters[i];int x=i==0?19:370;Color32 accent=i==0?Red:Teal;
            Panel(x,16,44,48);FighterArt.Portrait(canvas,f.kind,x+22,39,i==0?1:-1);
            int bx=i==0?72:421;canvas.Text(f.kind==0?"ЛЕНИН":"СТАЛИН",bx,14,White,2);
            canvas.Text(i==0?"P1":local?"P2":"CPU",i==0?265:592,18,accent);
            R(bx,35,200,15,0x303445);int delayed=Mathf.RoundToInt(f.displayHp*1.96f),hp=Mathf.RoundToInt(f.hp*1.96f);
            canvas.Rect(bx+2,37,delayed,11,Gold);canvas.Rect(bx+2,37,hp,11,accent);canvas.Rect(bx+2,37,hp,3,i==0?C(0xf79b83):C(0xb2e3d6));
            for(int j=1;j<4;j++)R(bx+j*50,36,1,13,0x253043);
            R(bx,56,200,6,0x273247);canvas.Rect(bx+1,57,(int)(f.energy*1.98f),4,f.energy>=100?Gold:accent);
            canvas.Text(f.energy>=100?"СУПЕР ГОТОВ / "+(controls.IsConnected(i)?controls.UltimateKey(i):i==0?"R":"NUM6"):"ЭНЕРГИЯ "+(int)f.energy,bx,70,f.energy>=100?Gold:Muted);
            for(int j=0;j<2;j++)canvas.Ellipse(i==0?258+j*11:599+j*11,74,3,3,j<f.wins?Gold:C(0x344052));
        }
        Panel(288,13,64,61);canvas.Center(training?"--":Mathf.CeilToInt(remaining).ToString("00"),320,24,White,3);canvas.Center("BEST OF 3",320,59,Gold);
        canvas.Center(stage==0?"ЗИМНЯЯ ПЛОЩАДЬ":"ЛИТЕЙНЫЙ ЦЕХ",320,95,Gold,1,true);
    }
    void DrawPause(){canvas.Dim(.35f);Panel(170,78,300,209);canvas.Center("ПАУЗА",320,98,White,3);if(disconnectNotice.Length>0)canvas.Center(disconnectNotice,320,123,Gold);Button("ПРОДОЛЖИТЬ",196,138,248,()=>{disconnectNotice="";mode=ScreenMode.Fight;});Button("УПРАВЛЕНИЕ И ПРИЁМЫ",196,171,248,()=>Guide(ScreenMode.Pause));Button("НАЧАТЬ ЗАНОВО",196,204,248,()=>BeginMatch(selectedFighter,local,training));Button("ГЛАВНОЕ МЕНЮ",196,237,248,()=>{mode=ScreenMode.Menu;focus=0;});}
    void DrawResult(){canvas.Dim(.45f);Panel(137,88,366,190);int win=world.fighters[0].wins>=2?0:1;canvas.Center("ПОБЕДИТЕЛЬ",320,106,Gold,2);canvas.Center(world.fighters[win].kind==0?"ЛЕНИН":"СТАЛИН",320,133,White,4);canvas.Center(world.fighters[0].wins+" : "+world.fighters[1].wins,320,178,Gold,2);Button("РЕВАНШ",161,211,149,()=>BeginMatch(selectedFighter,local,training));Button("В МЕНЮ",322,211,157,()=>{mode=ScreenMode.Menu;focus=0;});canvas.Center("ДО ДВУХ ПОБЕД",320,257,Muted);}
    void DrawGuide(){
        int guideSlot=controls.MenuSlot;
        canvas.Dim(.25f);Panel(20,15,600,329);canvas.Text("УПРАВЛЕНИЕ / ШКОЛА БОЯ",38,32,White,2);
        canvas.Text("ДЕЙСТВИЕ",39,65,Gold);canvas.Text("КЛАВИАТУРА + МЫШЬ",217,65,Gold);canvas.Text(controls.PlayStation(guideSlot)?"PLAYSTATION":"ГЕЙМПАД XBOX",459,65,Gold);
        string[] names={"ДВИЖЕНИЕ","ПРЫЖОК / ПРИСЕД","ЛЁГКИЙ УДАР","МОЩНЫЙ УДАР","БЛОК","СПЕЦПРИЁМ (20)","ОСОБЫЙ ПРИЁМ (30)","СУПЕРУДАР (100)"};
        string[] keys={"A / D","W / SPACE / S","J / ЛКМ","K / ПКМ","SHIFT / СКМ","Q / U","E / I","R / O"};string[] pad={"СТИК / D-PAD",(controls.PlayStation(guideSlot)?"×":"A")+" / ВНИЗ",controls.LightKey(guideSlot),controls.HeavyKey(guideSlot),controls.BlockKey(guideSlot),controls.SpecialKey(guideSlot),controls.SkillKey(guideSlot),controls.UltimateKey(guideSlot)};
        for(int i=0;i<names.Length;i++){int y=85+i*17;if(i%2==0)R(32,y-4,576,16,0x172337);canvas.Text(names[i],39,y,Muted);canvas.Text(keys[i],217,y,White);canvas.Text(pad[i],459,y,Teal);}
        canvas.Text("P2: СТРЕЛКИ / NUM1 / NUM2 / RSHIFT / NUM4 / NUM5 / NUM6",39,230,Gold);
        canvas.Text("ЛЕНИН: Q - КНИГА. E - АРГУМЕНТ. R - ПОЛНОЕ СОБРАНИЕ.",39,249,Muted);
        canvas.Text("СТАЛИН: Q - ДЫМ. E - ЗАВЕСА. R - БОЛЬШАЯ ЗАТЯЖКА.",39,265,Muted);
        canvas.Text("БЛОК СНИЖАЕТ УРОН. КНИГИ МОЖНО ПЕРЕПРЫГИВАТЬ.",39,281,Muted);
        Button("НАЗАД",39,305,138,()=>{mode=guideReturn;focus=0;},true);Button(audioFx.muted?"ЗВУК: ВЫКЛ":"ЗВУК: ВКЛ",190,305,145,()=>audioFx.Toggle(),true);
        Button("ПРОВЕРИТЬ ГЕЙМПАД",350,305,248,()=>{mode=ScreenMode.Controllers;focus=0;},true);
    }
    void DrawControllers(){
        canvas.Dim(.3f);Panel(20,15,600,329);canvas.Text("ПРОВЕРКА КОНТРОЛЛЕРОВ",38,31,White,2);
        canvas.Text("НАЖИМАЙ КНОПКИ И ДВИГАЙ ОБА СТИКА",39,57,Muted);
        for(int slot=0;slot<2;slot++){
            int x=39+slot*294;canvas.Text("P"+(slot+1)+": "+controls.DeviceName(slot),x,79,controls.IsConnected(slot)?Teal:Muted);
            string[] labels={controls.PlayStation(slot)?"×":"A",controls.SpecialKey(slot),controls.LightKey(slot),controls.HeavyKey(slot),controls.PlayStation(slot)?"L1":"LB",controls.SkillKey(slot),controls.PlayStation(slot)?"OPTIONS":"START"};
            Controls.Buttons[] masks={Controls.Buttons.Jump,Controls.Buttons.Special,Controls.Buttons.Light,Controls.Buttons.Heavy,Controls.Buttons.Block,Controls.Buttons.Skill,Controls.Buttons.Start};
            for(int i=0;i<labels.Length;i++){int bx=x+i%4*65,by=105+i/4*30;bool held=controls.Held(slot,masks[i]);R(bx,by,60,24,held?0x377e78u:0x1b293bu);canvas.Frame(bx,by,60,24,held?Teal:Muted);canvas.Center(labels[i],bx+30,by+8,held?White:Muted);}
            for(int stick=0;stick<2;stick++){
                var value=stick==0?controls.Stick(slot):controls.RightStick(slot);int cx=x+35+stick*72,cy=205;canvas.Frame(cx-25,cy-25,51,51,Muted);canvas.Line(cx-24,cy,cx+24,cy,C(0x344459));canvas.Line(cx,cy-24,cx,cy+24,C(0x344459));canvas.Ellipse(cx+(int)(value.x*22),cy-(int)(value.y*22),4,4,Teal);canvas.Center(stick==0?"ЛЕВЫЙ":"ПРАВЫЙ",cx,237,Muted);
            }
            for(int trigger=0;trigger<2;trigger++){int tx=x+153+trigger*54;canvas.Frame(tx,180,34,51,Muted);int h=(int)(controls.Trigger(slot,trigger==1)*47);R(tx+2,229-h,30,h,0x73d4cf);canvas.Center(controls.PlayStation(slot)?(trigger==0?"L2":"R2"):(trigger==0?"LT":"RT"),tx+17,237,Muted);}
            string direction="";if(controls.Held(slot,Controls.Buttons.Up))direction+="ВВЕРХ ";if(controls.Held(slot,Controls.Buttons.Down))direction+="ВНИЗ ";if(controls.Held(slot,Controls.Buttons.Left))direction+="ВЛЕВО ";if(controls.Held(slot,Controls.Buttons.Right))direction+="ВПРАВО";
            canvas.Text("КРЕСТОВИНА: "+direction,x,259,Gold);
            canvas.Text(controls.LightKey(slot)+": ПРОВЕРИТЬ ВИБРАЦИЮ",x,278,Muted);
        }
        Button("НАЗАД",39,307,138,()=>{mode=ScreenMode.Guide;focus=0;},true);canvas.Text("ESC / "+controls.BackKey+": НАЗАД",199,315,Muted);
    }
    void TickQa(float dt){
        qaTime+=dt;
        if(qaTime>(showcase?1.5f:3)&&qaTime<(showcase?13:7)&&Time.frameCount%(showcase?1:5)==0){
            byte[] rgb=new byte[W*H*3];for(int y=0;y<H;y++)for(int x=0;x<W;x++){var c=canvas.pixels[(H-1-y)*W+x];int k=(y*W+x)*3;rgb[k]=c.r;rgb[k+1]=c.g;rgb[k+2]=c.b;}
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(qaDirectory,"frame-"+(qaCaptureCount++).ToString("0000")+".rgb"),rgb);
        }
        if(qaPhase==0&&qaTime>.8f){SaveShot(System.IO.Path.Combine(qaDirectory,"01-menu.png"));mode=ScreenMode.Select;qaPhase=1;}
        if(qaPhase==1&&qaTime>1.3f){SaveShot(System.IO.Path.Combine(qaDirectory,"02-select.png"));BeginMatch(0,false,showcase);if(showcase){world.fighters[0].x=150;world.fighters[1].x=490;}countdown=0;qaPhase=2;}
        if(qaTime>4&&qaTime<4.1f)SaveShot(System.IO.Path.Combine(qaDirectory,"03-fight.png"));
        if(qaTime>(showcase?13.2f:7)&&qaTime<(showcase?13.3f:7.1f)){stage=1;SaveShot(System.IO.Path.Combine(qaDirectory,"04-foundry.png"));}
        if(showcase&&qaTime>3.3f&&qaTime<3.4f)SaveShot(System.IO.Path.Combine(qaDirectory,"07-book.png"));
        if(showcase&&qaTime>6.1f&&qaTime<6.2f)SaveShot(System.IO.Path.Combine(qaDirectory,"08-smoke.png"));
        if(qaTime>(showcase?14:10)){
            try {
                BeginMatch(1);countdown=0;world.fighters[1].hp=0;TickFight(.016f);
                Require(world.fighters[0].wins==1&&roundEnd==0,"first round win");TickFight(2.7f);
                Require(round==2&&world.fighters[0].hp==100&&world.fighters[0].wins==1,"next round reset preserves score");
                countdown=0;world.fighters[1].hp=0;TickFight(.016f);TickFight(2.7f);
                Require(mode==ScreenMode.Result&&world.fighters[0].wins==2,"match result after two wins");SaveShot(System.IO.Path.Combine(qaDirectory,"05-result.png"));
                BeginMatch(0);countdown=0;remaining=0;world.fighters[1].hp=50;TickFight(.016f);Require(world.fighters[0].wins==1,"timeout compares health");
                BeginMatch(0,true);Require(local&&mode==ScreenMode.Fight,"local match starts");
                BeginMatch(1,false,true);countdown=0;world.fighters[1].hp=10;TickFight(.016f);Require(world.fighters[1].hp==100&&world.fighters[0].energy==100,"training restores health and energy");
                Guide(ScreenMode.Menu);SaveShot(System.IO.Path.Combine(qaDirectory,"06-controls.png"));
                Debug.Log("QA_RUNTIME_COMPLETE frames="+Time.frameCount);System.IO.File.WriteAllText(System.IO.Path.Combine(qaDirectory,"runtime.txt"),"Windows runtime completed without errors. 6 match-flow checks passed. Both stages, menu, selection, result and controls rendered. Frames="+Time.frameCount);Application.Quit(0);
            }catch(Exception ex){Debug.LogException(ex);System.IO.File.WriteAllText(System.IO.Path.Combine(qaDirectory,"runtime.txt"),"FAILED: "+ex);Application.Quit(1);}
        }
    }
    static void Require(bool condition,string name){if(!condition)throw new Exception("Runtime check failed: "+name);Debug.Log("QA_PASS "+name);}
}
}
