using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XiaoheiDesktop {
public sealed partial class PetWindow {
    public void VisitMaster() {
        if(Timer.Running){Say("等专注结束，再找师父练习吧。",3,true);return;}
        if(art.UsingCustomSkin && !art.HumanForm){Say("换回小黑的形象，就能和师父一起练习。",3,true);return;}
        StopGuestPlay();art.CancelTail();
        PlayAnimation(art.HumanForm?"waving":"idle",null);
        art.BeginMasterMeeting();
        UpdateFormLayout(true);
        if(art.HumanForm){Say("师父第一！",2,false);CueAfter("human-master",.76);}
        else CueAfter("master-sleeve",2.06);
    }
}

public sealed partial class PetArt {
    double masterAt=double.NegativeInfinity,callAt=double.NegativeInfinity,mouthAt=double.NegativeInfinity;
    double[] mouth;
    public bool MasterActive {get{return Clock-masterAt>=0 && Clock-masterAt<5.8;}}
    bool CatCallActive {get{return !HumanForm && Clock-callAt>=0 && (Clock-callAt<1.84 || MouthActive);}}
    internal bool Performing {get{return MasterActive || CatCallActive || MouthActive;}}
    int MouthFrame {get{return mouth==null?-1:(int)((Clock-mouthAt)*25);}}
    bool MouthActive {get{return mouth!=null && Clock>=mouthAt && MouthFrame<mouth.Length;}}
    double MouthAmount {get{return MouthActive?mouth[MouthFrame]:0;}}
    internal void BeginMouth(double[] track){mouth=track;mouthAt=Clock;}
    internal bool ClearMouth(){bool playing=MouthActive;mouth=null;return playing;}
    public void BeginMasterMeeting(){masterAt=Clock;InvalidateVisual();}
    public void BeginCatCall(){callAt=Clock;InvalidateVisual();}
    internal void CancelActing(){masterAt=callAt=double.NegativeInfinity;ClearMouth();}
    void CopyActing(PetArt other){masterAt=other.masterAt;callAt=other.callAt;mouthAt=other.mouthAt;mouth=other.mouth;}

    void DrawCatCall(DrawingContext d,SpriteAtlas atlas) {
        Rect r=SpriteBounds();
        double t=Clock-callAt,lift=Math.Sin(Math.PI*Unit(t/.45));
        d.PushTransform(new TranslateTransform(r.X,r.Y));
        d.PushTransform(new ScaleTransform(r.Width/192,r.Height/208));
        double tuck=Ease((t-.29)/.40)*(1-Ease((t-1.12)/.53));
        double side=Ease((t-.27)/.45)*(1-Ease((t-1.18)/.51));
        // The little tail clone watches from the ground while Xiaohei rolls.
        DrawHeyxiuBody(d,new Point(144,188),.96,1,1,0,0);
        d.PushTransform(new TranslateTransform(9*side,0));
        d.PushTransform(new RotateTransform(-3.4*lift+23*side,96,192));
        d.PushTransform(new ScaleTransform(1+.07*tuck,1-.32*tuck,96,195));
        d.PushOpacity(1-Ease((tuck-.25)/.57));
        DrawSoloCat(d,atlas,false);
        double open=MouthAmount;
        if(open>.03) {
            d.PushTransform(new ScaleTransform(1,1+open*.53,96,113));
            Path(d,"M88,114 Q96,113 104,114 Q102,121 96,126 Q91,122 88,114 Z","#5F8991","#362220",2.7);
            d.Pop();
        }
        d.Pop();
        double turn=Ease((tuck-.20)/.58);
        if(turn>0){
            d.PushOpacity(turn);
            // A tucked body, a back-facing ear and a loose tail show the
            // direction of the turn even after the eyes disappear.
            Path(d,"M119,168 Q151,172 157,147 Q158,138 164,140 Q172,143 167,158 Q156,187 127,188 Z","#090605","#392923",3);
            Path(d,"M53,158 Q63,134 100,140 Q130,145 138,171 Q143,187 130,195 Q114,202 85,196 Q58,196 49,180 Z","#0A0706","#392923",3.4);
            Path(d,"M89,178 Q106,153 126,163 M101,189 Q121,173 134,181",null,"#392923",2.2);
            Path(d,"M43,164 C24,160 19,144 23,125 Q28,99 58,92 Q84,85 99,108 Q110,127 101,145 Q83,172 58,167 Z","#080604","#392923",3.5);
            Path(d,"M53,113 L67,98 L81,133 Z","#B2C78E","#392923",2.5);
            d.Pop();
        }
        d.Pop();d.Pop();d.Pop();d.Pop();d.Pop();
    }

    void DrawMasterMeeting(DrawingContext d,SpriteAtlas atlas) {
        double t=Clock-masterAt;
        double size=Math.Min(ActualWidth/200,ActualHeight/208);
        if(size<=0)return;
        d.PushTransform(new TranslateTransform((ActualWidth-200*size)/2,ActualHeight-208*size));
        d.PushTransform(new ScaleTransform(size,size));
        double arrive=Ease(t/.63),leave=Ease((t-5.08)/.72),opacity=arrive*(1-leave);
        double catchPose=Ease((t-1.35)/.5)*(1-Ease((t-3.45)/.55));
        d.PushOpacity(opacity);
        d.PushTransform(new TranslateTransform(-16*(1-arrive)-13*leave,0));
        DrawMaster(d,t,catchPose,HumanForm);
        d.Pop();d.Pop();

        // The cat springs towards his open hand,
        // settles in the crook of his arm, then springs back to the ground.
        double up=Ease((t-1.22)/.84),down=Ease((t-3.55)/.82),held=up*(1-down);
        double arc=Math.Sin(Math.PI*up)+Math.Sin(Math.PI*down);
        double catX=142-58*held,catY=200-123*held-24*arc;
        if(HumanForm){
            d.PushTransform(new TranslateTransform(86,77));
            d.PushTransform(new ScaleTransform(.61,.61));
            DrawBoy(d,false);d.Pop();d.Pop();
        } else {
            double squash=1-.055*Math.Sin(Math.PI*Unit((t-2.06)/.17))-.073*Math.Sin(Math.PI*Unit((t-4.37)/.20));
            d.PushTransform(new TranslateTransform(catX,catY));
            d.PushTransform(new RotateTransform(-6*arc+4.2*held));
            double scale=.39-.17*held;
            d.PushTransform(new ScaleTransform(scale,scale*squash));
            d.PushTransform(new TranslateTransform(-96,-199));
            double tucked=Ease((held-.27)/.67);
            d.PushOpacity(1-tucked);
            DrawSoloCat(d,atlas,false);
            d.Pop();
            if(tucked>0) {
                d.PushOpacity(tucked);
                DrawHeldCat(d,t);
                d.Pop();
            }
            d.Pop();d.Pop();d.Pop();d.Pop();
            DrawHeyxiuBody(d,new Point(162,196),.43,.94,1,0,0);
        }
        d.Pop();d.Pop();
    }

    static void DrawHeldCat(DrawingContext d,double t) {
        const string fur="#0A0807",ink="#3B2926";
        double breath=.85*Math.Sin(4*t);
        d.PushTransform(new ScaleTransform(1,1+.012*breath,87,177));
        Path(d,"M109,164 Q138,172 146,153 C152,137 151,125 156,124 Q166,125 165,142 Q165,174 144,184 Q130,191 110,182 Z",fur,ink,3.8);
        Path(d,"M47,145 Q60,128 83,128 Q106,130 125,143 Q145,153 142,168 Q138,184 117,187 Q94,195 72,185 Q52,181 45,166 Z",fur,ink,4.1);
        Path(d,"M85,158 Q93,146 107,148 Q119,153 117,165 Q116,177 108,182 M112,176 Q121,170 128,175",null,ink,2.9);
        // Head turns into his sleeve: a sliver of the far eye remains visible
        // just below the raised ear. The forepaws fold against the chest.
        Path(d,"M44,145 Q29,140 24,129 Q17,115 22,103 Q29,85 50,81 Q68,77 83,88 Q99,98 100,114 Q104,133 89,148 Q73,159 55,153 Z",fur,ink,4.1);
        Path(d,"M27,107 Q24,96 30,84 Q44,84 57,95 L61,106 Z","#ACCA98",ink,3.3);
        Path(d,"M68,86 L77,76 Q86,81 90,94 Z",fur,ink,2.5);
        Path(d,"M26,116 Q22,119 24,126 Q28,129 29,121 Z","#F4EBC5",ink,1.7);
        Path(d,"M42,147 Q55,143 62,154 Q68,164 78,165 M48,154 Q54,165 64,170",null,ink,3);
        d.Pop();
    }

    static readonly Lazy<BitmapSource[]> mentorPoses = new Lazy<BitmapSource[]>(LoadMentorPoses);

    static BitmapSource[] LoadMentorPoses() {
        using(var stream=typeof(PetArt).Assembly.GetManifestResourceStream("Xiaohei.Mentor.png")) {
            var atlas=new BitmapImage();
            atlas.BeginInit();
            atlas.CacheOption=BitmapCacheOption.OnLoad;
            atlas.StreamSource=stream;
            atlas.EndInit();
            atlas.Freeze();
            var poses=new BitmapSource[6];
            int width=atlas.PixelWidth/poses.Length;
            for(int i=0;i<poses.Length;i++) {
                var cel=new CroppedBitmap(atlas,new Int32Rect(i*width,0,width,atlas.PixelHeight));
                cel.Freeze();
                poses[i]=cel;
            }
            return poses;
        }
    }

    static void DrawMaster(DrawingContext d,double t,double catchPose,bool greeting) {
        var poses=mentorPoses.Value;
        double tilt=greeting?.68*Ease((t-.48)/.66)*(1-Ease((t-3)/.85)):catchPose;
        int frame=(int)Math.Round(tilt*(poses.Length-1));
        double breath=.34*Math.Sin(t*2.7);
        d.DrawImage(poses[frame],new Rect(16,2+breath,85.8,197-breath));
    }
}
}
