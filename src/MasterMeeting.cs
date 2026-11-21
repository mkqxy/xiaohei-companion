using System;
using System.Windows;
using System.Windows.Media;

namespace XiaoheiDesktop {
public sealed partial class PetWindow {
    public void VisitMaster() {
        if(Timer.Running){Say("等专注结束，再找师父练习吧。",3,true);return;}
        if(art.UsingCustomSkin && !art.HumanForm){Say("换回小黑的形象，就能和师父一起练习。",3,true);return;}
        StopGuestPlay();art.CancelTail();
        PlayAnimation(art.HumanForm?"waving":"idle",null);
        art.BeginMasterMeeting();
        UpdateFormLayout(true);
        if(art.HumanForm)CueAfter("human-master",.76);
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

        // The metal ring floats from his wrist. The cat follows its arc,
        // settles in the crook of his arm, then springs back to the ground.
        double up=Ease((t-1.22)/.84),down=Ease((t-3.55)/.82),held=up*(1-down);
        double arc=Math.Sin(Math.PI*up)+Math.Sin(Math.PI*down);
        double catX=142-40*held,catY=200-80*held-29*arc;
        if(HumanForm){
            d.PushTransform(new TranslateTransform(80,52));
            d.PushTransform(new ScaleTransform(.74,.74));
            DrawBoy(d,false);d.Pop();d.Pop();
        } else {
            double squash=1-.055*Math.Sin(Math.PI*Unit((t-2.06)/.17))-.073*Math.Sin(Math.PI*Unit((t-4.37)/.20));
            d.PushTransform(new TranslateTransform(catX,catY));
            d.PushTransform(new RotateTransform(-6*arc+4.2*held));
            double scale=.48-.09*held;
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
        if(!HumanForm && held>.05) {
            d.PushOpacity(held);
            Path(d,"M79,109 Q84,113 92,111 L99,108 Q102,107 102,109 Q99,113 94,115 Q85,118 78,114 Z","#EDCDBB","#262D36",1.25);
            d.Pop();
        }
        double travel=Ease((t-.76)/.52)*(1-Ease((t-3.2)/1.04));
        if(t>.75 && t<4.25){
            double ringX=72+50*travel,ringY=97-21*Math.Sin(Math.PI*travel)-6*travel;
            d.PushTransform(new RotateTransform(-24+87*travel,ringX,ringY));
            E(d,ringX,ringY,6.8,2.7,null,"#314854",2.4);
            E(d,ringX,ringY-.5,6.1,2.3,null,"#D5E6E5",1.1);
            d.Pop();
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

    // Three-quarter pose facing the pet: centre part, indigo hair, crossed
    // pale collar, double waist cord, wide sleeves and ankle-length robe.
    static void DrawMaster(DrawingContext d,double t,double catchPose,bool greeting) {
        const string edge="#282D36",skin="#EFCCB8",blue="#719AAB";
        double breath=Math.Sin(t*3)*.46;
        d.PushTransform(new RotateTransform(-.9+2.6*catchPose,53,191));
        Path(d,"M34,118 Q48,113 68,117 L72,162 Q76,178 65,186 L54,186 L51,146 Q50,174 45,187 L32,188 Q20,179 24,160 Z",blue,edge,1.8);
        Path(d,"M31,184 L45,184 L44,196 Q39,200 27,199 Q24,197 29,193 Z M54,183 L66,182 L67,191 Q71,194 75,195 Q77,198 74,199 L55,198 Z","#414C55",edge,1.7);
        Path(d,"M31,185 L44,184 L43,190 L30,192 Z M55,183 L66,183 L67,190 L55,190 Z","#DDE4DF",edge,1.1);
        d.PushTransform(new TranslateTransform(0,breath));
        const string navy="#394E71", robe="#7698A2", inner="#CFD8DE", ink="#242E36";
        Path(d,"M30,26 Q29,12 41,9 Q56,4 63,14 Q69,21 66,40 Q65,58 60,72 Q57,83 49,86 Q39,87 26,80 Q30,66 29,53 Z",navy,ink,1.55);
        Path(d,"M27,80 Q38,86 49,84 L48,87 Q36,88 27,84 Z",inner,ink,.7);
        Path(d,"M31,86 Q36,88 40,88 Q34,122 23,144 Q28,126 31,86 Z",navy,ink,1);

        // Long outer panels and the relaxed arm folded behind his back.
        Path(d,"M32,61 Q21,62 17,78 Q11,99 18,113 Q23,124 29,125 L22,186 Q34,192 47,188 L62,188 L73,186 L66,113 L68,74 Q64,63 54,60 Z",robe,ink,1.7);
        Path(d,"M36,64 L47,59 L59,62 L65,71 L60,109 L65,186 Q52,189 40,187 L43,111 Z",inner,ink,1.25);
        Path(d,"M42,55 L55,53 L58,65 L49,77 L40,65 Z",skin,ink,1);
        Path(d,"M38,61 L42,59 L55,76 L51,83 Z M58,61 L63,65 Q59,79 42,98 L38,100 Q48,82 58,61 Z",inner,ink,1.35);
        Path(d,"M28,110 Q44,106 66,108 L66,112 Q45,110 28,114 Z","#364857",ink,1);
        Path(d,"M28,114 Q46,110 66,112 L66,115 Q48,113 28,117 Z",inner,ink,.8);
        Path(d,"M62,113 Q65,111 65,116 L67,140 M63,115 L63,134 M21,87 Q21,104 29,108",null,ink,1);
        d.PushTransform(new RotateTransform(18*catchPose+(greeting?4*Math.Sin(t*5):0),58,84));
        Path(d,"M60,65 Q70,64 72,76 L73,90 L81,88 L86,99 Q76,106 62,108 Q54,107 53,101 Q53,96 57,88",robe,ink,1.6);
        Path(d,"M80,89 Q82,87 84,87 L88,97 L85,100 Z",inner,ink,1);
        Path(d,"M84,88 L90,85 Q93,84 91,88 L89,90 L96,88 Q99,89 96,92 L89,96 L87,97 Z",skin,ink,1);
        d.Pop();
        d.PushTransform(new RotateTransform(-1+10*catchPose,49,58));
        Path(d,"M36,23 Q43,18 50,21 L54,23 L61,20 Q65,24 65,32 L64,43 Q61,52 53,55 Q46,55 39,48 Q32,41 36,23 Z","#EBD0C2",ink,1.15);
        Path(d,"M30,38 Q30,15 40,12 Q48,9 52,12 Q56,11 61,15 Q65,18 66,25 L62,21 L54,23 Q48,19 42,21 Q40,31 40,43 Q40,54 43,61 L39,59 L40,62 Q31,58 31,50 Z",navy,ink,1.15);
        Path(d,"M61,20 Q67,20 67,32 Q68,51 61,60 L62,54 L59,58 Q64,41 61,20 Z",navy,ink,1);
        Path(d,"M42,30 Q46,29 51,31 L51,32 Q46,31 42,31 Z M55,31 Q59,29 62,29 L62,30.6 L55,32 Z",ink,ink,.55);
        Path(d,"M42,34 L51,34.8 Q49,39 46,38 Q43,37 42,34 Z M55,34.6 L62,33.8 Q61,37.5 58,38 Q56,37.8 55,34.6 Z","#F2EAE1",null,0);
        Path(d,"M45,34.5 L50,34.8 Q49.8,38.2 47.3,38 Q45,38 45,34.5 Z M57,34.5 L61,34 Q61,37.6 58.8,37.6 Q57,37.6 57,34.5 Z","#637F80",ink,.6);
        Path(d,"M42,34 Q46,33.7 51,34.8 M55,34.6 L62,33.8 M53,38 L54,41 L53,42 M49,46 Q52,47 55,45.8",null,ink,.9);
        d.Pop();d.Pop();d.Pop();
    }
}
}
