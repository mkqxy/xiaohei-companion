using System;
using System.Windows;
using System.Windows.Media;

namespace XiaoheiDesktop {
internal sealed partial class GuestArt {
    static readonly Pen Dark = Stroke("#412F28",1.95);
    static readonly Pen Thin = Stroke("#49352C",1.22);
    const string TabbyCoat="#D5D4CE", TabbyMark="#777772", EarGreen="#A4BA89";
    const string EyeWhite="#FFFEFA", EyeInk="#4E6560";
    static double Unit(double value) { return Math.Max(0,Math.Min(1,value)); }
    static double Ease(double t) {t=Unit(t);return t*t*(3-2*t);}
    static double Pulse(double elapsed,double start,double length) {
        return Math.Sin(Math.PI*Unit((elapsed-start)/length));
    }

    static void DrawTabbyBody(DrawingContext d,double leap,double breath) {
        d.PushTransform(new RotateTransform(2*breath+10*leap,46,78));
        Shape(d,"M44,75 C39,75 36,73 32,75 C28,77 28,81 32,82 C37,84 43,82 49,81 Z",TabbyCoat,Dark);
        d.Pop();
        d.PushTransform(new RotateTransform(27*leap,60,65));
        Shape(d,"M51,52 C48,57 43,62 41,69 C39,75 39,81 45,83 Q50,86 61,84 Q68,85 72,82 C76,79 77,66 73,56 Z",TabbyCoat,Dark);
        Shape(d,"M47,58 Q44,61 44,63 Q48,65 50,61 Q51,59 49,58 Z M43,65 Q41,67 41,69 Q45,71 48,67 Q48,64 46,64 Z M41,72 Q40,74 41,76 Q44,77 46,74 Q46,72 44,71 Z",TabbyMark,null);
        Shape(d,"M47,77 Q49,79 48,81",null,Thin);
        d.Pop();
        Shape(d,"M63,69 C62,74 62,81 66,83 Q71,85 74,81 Q77,76 75,66",TabbyCoat,Dark);
        Shape(d,"M71,81 L71,82",null,Thin);
    }

    static void DrawTabbyPaw(DrawingContext d,double reach) {
        d.PushTransform(new RotateTransform(-115*reach,57,69));
        Shape(d,"M53,70 Q50,76 51,81 Q52,85 57,85 Q63,86 64,81 L65,73",TabbyCoat,Dark);
        if(reach<.28) Shape(d,"M55,82 L55.6,84",null,Thin);
        d.Pop();
    }

    void DrawTabbyFace(DrawingContext d,double joy,double leap) {
        double earTick=5*Math.Pow(Math.Max(0,Math.Sin(Clock*2.3)),16);
        d.PushTransform(new RotateTransform(-earTick,42,39));
        Shape(d,"M45,13 Q32,13 16,23 Q14,25 17,30 Q21,38 28,44 Q29,48 26,49 Q33,52 41,47 L49,34 Z",TabbyCoat,Dark);
        Shape(d,"M16.7,25 Q25,26 38,34 L35,46 Q31,49 28,49 Q30,46 28,44 Q19,34 16.7,25 Z",EarGreen,Thin);
        d.Pop();
        Shape(d,"M74,16 Q85,13 92,15 Q96,23 94,32 L88,41 Z",TabbyCoat,Dark);
        Shape(d,"M90.5,17.6 Q93,23 93,29 L90,36 L84,25 Z",EarGreen,Thin);

        // Keep the underside open where the neck joins the body.
        const string head="M51,59 C38,57 30,48 31,34 C32,17 45,7 60,7 C77,6 90,15 94,29 C99,46 92,57 73,61";
        Shape(d,head+" Z",TabbyCoat,null);
        Shape(d,"M43.4,12.6 Q47.1,9.7 53.2,8.5 Q52,12.8 48.7,14.4 Q45.1,15.9 43.4,12.6 Z M56.3,7.9 Q61.5,6.7 68,8 Q66,11.9 63,14 Q59,16 56.5,13.7 Q54.6,11.7 56.3,7.9 Z M74,9.1 Q78,10.6 81.1,12.5 Q79.2,14.3 76.6,13.6 Q73.8,12.7 74,9.1 Z",TabbyMark,null);
        Shape(d,head,null,Dark);

        double blink=Pulse((Clock+.7)%3.8,0,.17);
        double eyeOpen=1-Math.Max(blink,.99*joy);
        if(eyeOpen<.28) {
            Shape(d,"M36,34 Q46,32.5 60,39 M75,39 Q83,32.8 92,34",null,Dark);
        } else {
            double gx=(FaceRight?LookX:-LookX)*1.5+.5*Math.Sin(Clock*.82)+.7*leap;
            double gy=LookY*1.4-1.5*leap;
            d.PushTransform(new ScaleTransform(1,eyeOpen,64,35));
            Oval(d,50.3,34.5,16.4,19.1,EyeWhite,Dark);
            Oval(d,80.4,35.0,12.4,17.6,EyeWhite,Dark);
            Oval(d,52.6+gx,35.2+gy,9.6,13.2,EyeInk,Thin);
            Oval(d,81.2+gx*.75,35.2+gy,7.6,12.0,EyeInk,Thin);
            d.Pop();
        }
        d.PushTransform(new ScaleTransform(1+.60*joy,1+1.6*joy,68.5,48.9));
        Shape(d,"M68.4,45.7 L71.9,48.8 Q71.4,50.2 69.1,53.4 L65.5,48.7 Z","#DD8279",Thin);
        d.Pop();
    }

    void DrawTabby(DrawingContext d) {
        bool rub=Interactions>0 && Interactions%2==0;
        double age=ReactionAge;
        double leap=rub?0:Pulse(age,.24,.73);
        double reach=rub?0:Pulse(age,.30,.72);
        double crouch=rub?0:Pulse(age,0,.27)+.62*Pulse(age,.95,.27);
        double nuzzle=rub?Ease(age/.37)*(1-Ease((age-1.78)/.59)):0;
        double trot=Arriving?Math.Sin(Clock*21):0;
        double breath=Math.Sin(Clock*2.55);
        double smile=rub?nuzzle:Ease((reach-.30)/.49);
        if(!Arriving && (Interactions==0 || age>3))
            smile=Math.Max(smile,Ease(((Clock%8.6)-4.6)/.24)*(1-Ease(((Clock%8.6)-5.75)/.24)));
        Oval(d,59,86,20-7*leap,1.5,"#20403229",null);
        d.PushTransform(new TranslateTransform(-3*leap+2*nuzzle,-9*leap-1.8*Math.Abs(trot)));
        d.PushTransform(new ScaleTransform(1+.06*crouch,1-.16*crouch,60,85));
        d.PushTransform(new RotateTransform(33*leap+2*trot,59,62));
        DrawTabbyBody(d,leap,breath);
        DrawTabbyPaw(d,reach);
        if(reach>.28) {
            d.PushOpacity(Ease((reach-.28)/.3));
            Shape(d,"M70,63 C75,61 80,55 83,54 C88,53 91,56 89,59 Q86,63 80,65 L74,68",TabbyCoat,Dark);
            d.Pop();
        }
        d.PushTransform(new TranslateTransform(nuzzle*1.5,.4*breath+1.2*crouch));
        d.PushTransform(new RotateTransform(-breath*.85-5*leap+nuzzle*(5+4*Math.Sin(age*5.3)),63,57));
        DrawTabbyFace(d,smile,leap);
        d.Pop();d.Pop();
        if(reach>.4) {
            // An independently curling near paw reads in front of the cheek during a pounce.
            d.PushOpacity(Ease((reach-.4)/.27));
            d.PushTransform(new TranslateTransform(1.5*reach,-6*reach));
            Shape(d,"M53,72 C57,69 58,67 61,64 C63,61 63,57 60,55 C56,53 52,55 52,59 Q52,62 55,62.5 M52,60 Q47,60 44,65",TabbyCoat,Dark);
            d.Pop();d.Pop();
        }
        d.Pop();d.Pop();d.Pop();

        if(!rub && Interactions>0 && age>=0 && age<1.47) {
            double t=Unit((age-.05)/.77), caught=Ease((age-.89)/.3);
            double x=109-8*t-12*caught, y=58-2*t-29*Math.Sin(Math.PI*t)+11*caught;
            d.PushOpacity(1-Ease((age-1.2)/.26));
            d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new RotateTransform(-80+200*t+33*caught));
            Shape(d,"M-8,2 Q-3,-7 8,-4 Q7,4 0,6 Q-5,6 -8,2 Z","#B5CB78",Thin);
            Shape(d,"M-8,3 L5,-3",null,Thin);d.Pop();d.Pop();d.Pop();
        }
    }

    static readonly Color[] Tones={
        Color.FromRgb(246,195,90),Color.FromRgb(148,196,73),Color.FromRgb(169,128,189)
    };
    string CoatColour(double age) {
        int to=Interactions==0 ? 0 : 1+(Interactions-1)%2;
        int from=Interactions<=1 ? 0 : 1+(Interactions-2)%2;
        double t=Math.Round(20*Ease((age-.55)/.49))/20.0;
        Color a=Tones[from],b=Tones[to];
        return String.Format("#{0:X2}{1:X2}{2:X2}",(int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));
    }

    static void RestingBidiu(DrawingContext d,string fur,double breath) {
        d.PushTransform(new ScaleTransform(1-.008*breath,1+.021*breath,62,84));
        Shape(d,"M54,80 Q32,90 24,81 C17,72 25,60 36,61 Q46,61 50,68 Z",fur,Dark);
        Shape(d,"M37,76 C34,70 39,60 47,55 C58,48 77,50 86,58 Q94,64 95,75 Q98,82 93,84 C87,86 78,83 75,84 Q55,87 42,83 Z",fur,Dark);
        Shape(d,"M59,78 Q67,68 79,73 Q85,78 88,82 Q75,85 59,83 Z",EyeWhite,null);
        Shape(d,"M45,77 C44,73 50,69 54,72 Q58,75 58,82 Q53,86 46,82 M84,78 Q87,72 91,73 Q97,74 95,82",fur,Dark);
        Shape(d,"M61,69 Q63,67.2 65,67 M77,66.5 Q78,66.5 80,68 M69,70 L71,68.2 L73,70",null,Thin);
        d.Pop();
    }

    void SeatedBidiu(DrawingContext d,string fur,double open,double gulp,double breath) {
        // An offset tail is mostly hidden behind the taper of the back in the front view.
        d.PushTransform(new RotateTransform(4*breath+12*gulp,44,74));
        Shape(d,"M45,62 C38,58 32,60 30,66 C27,74 29,81 35,83 Q40,85 48,82 Z",fur,Dark);
        d.Pop();
        Shape(d,"M36,65 C37,55 38,45 44,38 C50,31 57,30 64,30 C78,30 86,40 89,52 C90,60 92,72 90,79 C89,85 82,85 77,85 C64,86 45,86 38,84 C34,82 34,76 36,65 Z",fur,Dark);
        Shape(d,"M42,78 C43,70 50,60 57,58 Q60,57 61,58 L63,57 L65,58 L68,57 Q74,58 77,62 Q84,71 85,81 Q82,84 75,84 Q51,85 43,83 Z",EyeWhite,null);
        double blink=1-.88*Pulse((Clock+.3)%3.66,0,.17);
        double gx=(FaceRight?LookX:-LookX)*.65;
        double gy=LookY*.7+.32*breath;
        Oval(d,54+gx,48.3+gy,2.35,2.46*blink,"#413129",null);
        Oval(d,77.7+gx,48.6+gy,2.25,2.39*blink,"#413129",null);
        if(open<.12) Shape(d,"M62.8,49.2 L65.4,46 L68.0,49.2",null,Thin);
        else {
            d.PushTransform(new ScaleTransform(1+.30*open,.57+.92*open,65.4,48.5));
            Shape(d,"M65.4,45.1 L68.7,48.8 L65.8,52.6 L62.35,48.7 Z","#ED9458",Thin);
            d.Pop();
        }
        // Paw contours turn into the cheeks at the top and meet the belly at the base.
        d.PushTransform(new RotateTransform(12*open-8*gulp,46,78));
        Shape(d,"M36.3,65 C36.5,60 39,56 42,54.8 Q41,53.3 43,53.6 L46,54.1 C53,52.2 57,55.4 57,61.6 C57.3,68 53,73 51,76 L51.3,84 Q43,85 38.8,83.3 Q33,80.5 36.3,65 Z",fur,null);
        Shape(d,"M36.3,65 C36.5,60 39,56 42,54.8 Q40.9,53.1 43,53.5 L45.5,54.2 C52.8,52.3 57,55.4 57,61.6 C57.3,68 53.3,72.1 50.5,73.4",null,Dark);
        d.Pop();
        d.PushTransform(new RotateTransform(-13*open+8*gulp,81,77));
        Shape(d,"M90.5,64 Q89.5,59 85,56 L84,54.3 L81.8,55 C75.8,52.9 71.4,55.4 71.2,61 C70.7,67 74.3,72.1 78,74 L78.6,84 Q85.8,84 89,81.7 Q92,79 90.5,64 Z",fur,null);
        Shape(d,"M90.5,64 Q89.5,59 85,56 L85.4,54.2 L82.1,55 C75.8,52.9 71.4,55.4 71.2,61 C70.7,67 74.3,72.1 78,74",null,Dark);
        d.Pop();
    }

    void DrawBidiu(DrawingContext d) {
        double age=ReactionAge;
        double open=Interactions==0?0:Pulse(age,.10,.57);
        double gulp=Interactions==0?0:Pulse(age,.52,.45);
        double bounce=Interactions==0?0:Pulse(age,.91,.49);
        double enter=Arriving?Math.Abs(Math.Sin(Clock*9))*.8:0;
        double breath=Math.Sin(Clock*2.6);
        double cycle=Clock%11.4;
        double resting=Arriving || (Interactions>0 && age<2.4)?0:
            Ease((cycle-5.5)/.42)*(1-Ease((cycle-8.9)/.42));
        string fur=CoatColour(age);
        Oval(d,61,85.7,23-4*bounce,1.4,"#20403229",null);
        d.PushTransform(new TranslateTransform(0,-8*bounce-4.5*enter));
        if(resting<1) {
            d.PushTransform(new ScaleTransform(1+.12*gulp+.19*resting,1-.15*gulp+.045*bounce-.34*resting,62,84));
            d.PushTransform(new RotateTransform(.5*breath-4*open,62,81));
            d.PushOpacity(1-Ease((resting-.42)/.32));
            SeatedBidiu(d,fur,open,gulp,breath);
            d.Pop();d.Pop();d.Pop();
        }
        if(resting>.42) {
            d.PushOpacity(Ease((resting-.42)/.32));
            RestingBidiu(d,fur,breath);
            d.Pop();
        }
        d.Pop();
        if(Interactions==0||age>=.65)return;
        double progress=Ease(age/.65);
        double px=101-35.6*progress, py=31+17.5*progress-19*Math.Sin(progress*Math.PI);
        d.PushTransform(new TranslateTransform(px,py));d.PushTransform(new ScaleTransform(1-progress*.9,1-progress*.9));
        d.PushTransform(new RotateTransform(-80+130*progress));
        if(Interactions%2==1) {
            Oval(d,0,0,7.4,7.4,"#92C579",Thin);
            Shape(d,"M-5,-4 Q1,-1 4,5",null,Thin);
        } else {
            Shape(d,"M-8,2 Q-9,-6 -2,-5 Q4,-5 8,-1 Q7,5 1,6 Q-5,7 -8,2 Z","#8763A0",Thin);
            Shape(d,"M4,-3 L7,-5 L8,-8 L10,-7 L10,-4 L8,0 Z","#88A66B",Thin);
        }
        d.Pop();d.Pop();d.Pop();
    }
}
}

