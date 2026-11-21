using System;
using System.Windows.Media;

namespace XiaoheiDesktop {
public sealed partial class PetArt {
    const string HumanInk="#171817", HumanSkin="#E1CBBA", CollarGreen="#788757";
    const string LongSleeve="#768856", Tee="#24272E", Trousers="#4F4B48";
    const string ShoeUpper="#323134", ShoeStrap="#B1962B", ShoeSole="#D3CBBE";

    void DrawBoy(DrawingContext d, bool split) {
        bool running=Animation=="running-left" || Animation=="running-right";
        bool wave=Animation=="waving" && !Sleeping;
        bool thinking=(Animation=="review" || Animation=="running") && !Sleeping;
        double stride=running?Math.Sin(AnimationElapsed*12):0;
        double leap=Animation=="jumping"?Math.Sin(Math.PI*Unit(AnimationElapsed/.78)):0;
        double snackAge=Clock-snackSince;
        bool eating=!Sleeping && snackAge>=0 && snackAge<4;
        double first=split?TailProgress(0):0, second=split?TailProgress(1):0;
        if(split) DrawSeparateTail(d,first,second,true);
        if(Sleeping) E(d,96,197,43,6,"#A5B99B","#596A51",1.15);

        // The reference pose is constructed in a single coordinate space. The
        // crown is at y=96, the collar at 405 and the soles at 1104.
        d.PushTransform(new TranslateTransform(96,198-leap*17-Math.Abs(stride)*2));
        if(running && Animation=="running-left") d.PushTransform(new ScaleTransform(-1,1));
        d.PushTransform(new ScaleTransform(.19,.19));
        d.PushTransform(new TranslateTransform(-400,-1104+(Sleeping?275:0)));
        if(Sleeping) DrawCrossedLegs(d);
        else if(running || leap>.001) {
            DrawMovingLeg(d,false,-stride,leap);
            DrawMovingLeg(d,true,stride,leap);
        } else DrawStandingLegs(d);

        double breath=Math.Sin(Clock*2.2);
        d.PushTransform(new ScaleTransform(1,1+.0045*breath,397,710));
        // A small forward lean helps the running legs read at desktop size.
        double lean=running?4.3:0;
        d.PushTransform(new RotateTransform(lean,398,706));
        double frontShoulder=running?stride*23:0, backShoulder=running?-stride*22:0;
        double frontElbow=running?25+stride*12:0, backElbow=running?20-stride*11:0;
        if(wave){frontShoulder=57;frontElbow=86+Math.Sin(AnimationElapsed*9)*10;backShoulder=2;}
        if(leap>.001){frontShoulder=37*leap;backShoulder=-39*leap;frontElbow=35*leap;backElbow=-25*leap;}
        if(Sleeping){frontShoulder=-13;frontElbow=-34;backShoulder=10;backElbow=21;}
        if(eating || thinking){frontShoulder=-9;frontElbow=-111+(eating?5*Math.Sin(snackAge*5):0);}
        DrawLayeredArm(d,false,backShoulder,backElbow,false);
        if(!thinking && !eating && !Sleeping) DrawLayeredArm(d,true,frontShoulder,frontElbow,wave);
        DrawTee(d);
        if(thinking || eating || Sleeping) DrawLayeredArm(d,true,frontShoulder,frontElbow,false);

        double tilt=Sleeping?6:thinking?3:eating?2*Math.Sin(snackAge*6):wave?-3:Animation=="waiting"?3:0;
        d.PushTransform(new RotateTransform(tilt+breath*.26,400,392));
        DrawReferenceHead(d,eating);
        d.Pop();d.Pop();d.Pop();
        d.Pop();d.Pop();
        if(running && Animation=="running-left")d.Pop();
        d.Pop();
        if(split) DrawClones(d,first,second,true);
        if(eating) {
            E(d,60,195,17,3,"#91B5A0","#4B5F50",1.2);
            if(snackAge<3) {
                d.PushTransform(new ScaleTransform(1-.25*Math.Floor(snackAge),1,60,191));
                Path(d,"M49,190 L54,191 Q60,185 69,190 Q63,194 55,192 L50,194 Z","#DEA061","#704C37",1.3);
                d.Pop();
            }
        }
    }

    static void DrawTee(DrawingContext d) {
        // Neck and collar meet at the reference neckline. Exposed skin is
        // visible only between the chin and the narrow green crewneck.
        Path(d,"M372,372 Q389,380 425,374 L426,394 Q427,402 444,407 L447,419 L345,420 L344,409 Q362,404 370,397 Z",HumanSkin,HumanInk,3.4);
        Path(d,"M342,405 Q361,418 398,418 Q431,419 449,405 Q466,411 484,420 Q499,428 507,447 L498,449 L485,513 Q484,567 487,620 Q488,629 494,640 Q502,654 493,668 Q489,674 492,681 L494,701 Q472,712 404,714 Q344,717 289,707 Q287,697 290,679 Q291,673 288,664 Q286,647 292,618 Q297,594 301,521 L289,456 L278,453 Q285,432 301,423 Q319,414 342,405 Z",Tee,null,0);
        // Only the exposed body outline is inked. The sleeves share the
        // shoulder with this shape and can turn independently without a gap.
        Path(d,"M342,405 Q321,413 302,422 Q289,429 282,441 M448,405 Q471,413 488,422 Q499,429 505,443 M291,446 Q289,480 301,522 Q302,563 292,618 Q286,650 289,669 Q292,680 289,695 L289,707 Q341,717 405,713 Q464,713 494,702 L492,681 Q490,673 495,662 Q500,652 495,640 Q487,623 487,616 Q485,578 484,518 Q493,478 495,452",null,HumanInk,3.8);
        Path(d,"M338,406 Q350,397 354,400 Q370,407 397,407 Q429,407 440,399 Q447,401 452,407 Q439,421 406,424 Q366,428 338,412 Z",CollarGreen,HumanInk,3.6);
        Path(d,"M312,676 Q352,694 432,688 M358,662 Q393,665 465,643",null,"#101419",3.6);
    }

    // Geometry is authored for the left sleeve and mirrored for the right.
    // The under-sleeve articulates at the hem of the shorter outer sleeve.
    static void DrawLayeredArm(DrawingContext d,bool left,double shoulder,double elbow,bool palm) {
        double pivot=left?295:494;
        d.PushTransform(new RotateTransform(shoulder,pivot,449));
        if(!left)d.PushTransform(new ScaleTransform(-1,1,394,0));
        d.PushTransform(new RotateTransform(left?elbow:-elbow,261,613));
        if(palm) {
            Path(d,"M235,691 Q247,686 269,696 L276,710 Q281,718 283,728 Q284,734 279,735 Q276,735 270,727 L266,723 L270,752 Q271,761 265,761 Q261,761 258,750 L255,736 L255,761 Q254,767 249,766 Q245,765 245,758 L243,740 L241,757 Q240,763 235,761 Q232,760 232,753 L231,738 L227,747 Q224,754 220,749 Q219,747 221,740 L224,719 Q226,705 235,691 Z",HumanSkin,HumanInk,3.6);
        } else {
            Path(d,"M238,693 Q255,688 283,698 L282,731 Q281,747 276,753 Q270,757 264,751 Q253,753 248,745 L239,736 Q233,728 235,713 Z",HumanSkin,HumanInk,3.8);
            Path(d,"M261,724 Q265,733 264,741 Q261,744 258,742 M264,741 Q268,750 274,750",null,HumanInk,3.2);
        }
        Path(d,"M233,602 Q257,601 296,609 L291,640 Q287,660 290,672 Q292,680 289,690 L287,707 Q258,707 230,700 Q229,693 231,681 Q233,675 227,670 Q221,665 225,649 Z",LongSleeve,HumanInk,3.8);
        Path(d,"M235,667 Q243,662 255,670 Q268,676 280,679",null,"#293025",3.6);
        d.Pop();

        Path(d,"M317,418 Q288,415 276,451 L226,580 L225,601 Q229,612 240,615 Q265,619 293,618 L309,525 L325,466 Q331,438 317,418 Z",Tee,null,0);
        Path(d,"M312,419 Q283,427 276,451 L226,580 L225,601 Q229,612 240,615 Q265,619 293,618 M302,476 L302,514",null,HumanInk,3.8);
        Path(d,"M228,578 L239,587 L236,610 Q229,606 225,601 Z","#C0C4A9",HumanInk,2.7);
        if(!left)d.Pop();
        d.Pop();
    }

    static void DrawShoe(DrawingContext d,bool left) {
        if(!left)d.PushTransform(new ScaleTransform(-1,1,389,0));
        Path(d,"M278,1004 Q282,984 311,989 Q336,988 341,1006 L339,1024 L357,1014 Q367,1016 368,1037 L369,1066 Q365,1077 344,1081 Q331,1097 308,1100 L270,1100 Q247,1100 244,1084 Q243,1069 251,1053 L265,1037 Q274,1024 278,1004 Z",ShoeUpper,HumanInk,3.9);
        Path(d,"M263,1038 Q282,1022 306,1026 Q323,1028 331,1038 Q334,1044 326,1051 L318,1055 Q294,1041 259,1053 Q255,1051 263,1038 Z",ShoeStrap,HumanInk,3.4);
        Path(d,"M246,1072 Q263,1059 290,1063 Q309,1065 329,1076 Q335,1079 340,1074 Q349,1059 366,1050 L369,1052 L370,1069 Q365,1080 344,1083 Q332,1097 309,1099 Q276,1092 249,1097 Q244,1091 244,1082 Z",ShoeSole,HumanInk,3.5);
        Path(d,"M250,1094 Q273,1085 298,1090 L315,1098 Q279,1106 252,1098 Z","#17191A",null,0);
        if(!left)d.Pop();
    }

    static void DrawStandingLegs(DrawingContext d) {
        DrawShoe(d,false);DrawShoe(d,true);
        Path(d,"M291,693 Q338,684 493,696 Q495,740 504,785 Q513,843 521,899 Q519,909 522,924 Q522,945 504,960 Q523,980 504,1001 L498,1008 Q480,989 461,993 Q435,994 437,1013 L440,1021 Q422,1035 407,1024 L401,1014 Q394,1007 398,996 L405,986 L390,973 Q384,983 377,986 Q389,999 379,1011 Q377,1023 364,1029 Q345,1027 335,1020 Q346,995 316,991 Q294,987 279,1003 Q259,989 269,967 L274,961 Q255,938 258,908 Q262,862 272,808 Q284,742 291,693 Z",Trousers,HumanInk,4);
        Path(d,"M370,724 Q364,748 376,796 L387,833 M393,776 Q389,835 392,881 L393,952 M393,961 Q393,969 390,975 M289,980 Q307,971 359,995 M425,996 Q463,973 497,979",null,"#272624",3.7);
    }

    static void DrawMovingLeg(DrawingContext d,bool left,double stride,double leap) {
        double hip=left?344:443;
        d.PushTransform(new RotateTransform((left?1:-1)*1.5-stride*31+leap*(left?15:-15),hip,704));
        double bend=.32*Math.Max(0,-stride)+.14*leap;
        d.PushTransform(new ScaleTransform(1,1-bend,hip,704));
        DrawShoe(d,left);
        if(!left)d.PushTransform(new ScaleTransform(-1,1,392,0));
        Path(d,"M293,694 Q347,685 395,701 L392,789 Q394,849 391,905 L390,968 Q385,981 377,987 Q390,999 376,1016 L367,1028 Q345,1032 335,1020 Q344,998 319,992 Q294,986 279,1004 Q259,987 269,968 L274,961 Q253,936 259,902 Q265,845 274,795 Z",Trousers,HumanInk,3.8);
        Path(d,"M290,979 Q309,974 358,995 M369,725 Q366,747 381,800",null,"#282724",3.4);
        if(!left)d.Pop();d.Pop();d.Pop();
    }

    static void DrawCrossedLegs(DrawingContext d) {
        Path(d,"M294,679 Q348,672 398,684 Q444,673 490,680 Q524,704 549,733 Q571,759 558,782 Q546,800 516,805 Q469,811 424,822 L380,826 Q354,828 333,815 Q294,815 259,802 Q232,794 229,774 Q228,746 253,721 Z",Trousers,HumanInk,4);
        Path(d,"M246,768 Q258,753 280,757 Q325,764 363,789 M510,755 Q528,752 542,764 M315,802 Q359,796 402,777 L436,766",null,"#302E2B",3.4);
        d.PushTransform(new TranslateTransform(312,787));
        d.PushTransform(new RotateTransform(-13));
        d.PushTransform(new ScaleTransform(.49,.36));
        d.PushTransform(new TranslateTransform(-300,-1032));
        DrawShoe(d,true);d.Pop();d.Pop();d.Pop();d.Pop();
        d.PushTransform(new TranslateTransform(471,796));
        d.PushTransform(new RotateTransform(12));
        d.PushTransform(new ScaleTransform(.53,.39));
        d.PushTransform(new TranslateTransform(-474,-1032));
        DrawShoe(d,false);d.Pop();d.Pop();d.Pop();d.Pop();
    }
}
}
