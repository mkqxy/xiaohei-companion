using System;
using System.Windows;
using System.Windows.Media;

namespace XiaoheiDesktop {
public sealed partial class PetArt {
    const string HumanHair = "#11161C", HumanEdge = "#20201B";
    const string HumanComplexion = "#E1CBBA", HumanEar = "#98AA67";
    static Geometry HumanEyeIris { get { return Geo("M0,-24 C12,-24 17,-13 17,1 C17,17 12,25 0,25 C-12,25 -18,17 -18,2 C-18,-13 -12,-24 0,-24 Z"); } }

    // Coordinates follow the 800 by 1200 front-view model used for the body.
    // The gap in the bottom outline leaves the neck visible during a head turn.
    void DrawReferenceHead(DrawingContext d, bool eating) {
        Path(d,
            "M264,203 C279,160 331,146 395,145 C479,141 535,180 544,251 " +
            "C546,278 530,298 544,318 C552,334 544,348 526,349 " +
            "C528,363 522,377 509,374 C507,383 497,389 486,383 " +
            "C482,392 474,400 462,393 C457,401 450,404 444,397 " +
            "C438,405 430,404 424,399 L423,376 Q399,365 374,376 L373,399 " +
            "Q368,407 361,398 Q354,407 345,400 Q337,405 333,397 " +
            "Q319,404 315,392 Q303,397 298,385 Q285,388 282,374 " +
            "Q267,381 266,350 Q246,350 250,329 Q252,316 260,310 " +
            "Q247,275 256,237 Z",HumanHair,HumanEdge,3.3);
        Path(d,"M289,324 Q278,348 281,371 M303,361 Q298,378 306,385 M495,363 Q502,379 494,382 M510,352 Q510,364 508,372",
            null,"#242523",2.0);

        PaintReferenceEar(d,false);
        PaintReferenceEar(d,true);
        const string faceContour = "M395,381 C361,379 332,369 317,351 C310,343 310,334 311,317 " +
            "L313,284 C314,244 334,219 365,208 Q408,180 465,212 " +
            "C482,229 484,259 486,283 L488,322 C489,339 484,351 473,360 " +
            "C458,373 430,380 407,381";
        Path(d,faceContour+" Z",HumanComplexion,null,0);
        Path(d,faceContour,null,HumanEdge,3.2);

        double blink=Sleeping?1:Math.Max(0,Math.Min(1,Blink));
        if(!Sleeping && blink<.01) {
            // Standalone previews use the same closing eyelids as the live pet.
            double cycle=(Clock+1.85)%4.93;
            if(cycle<.16)blink=Math.Sin(Math.PI*cycle/.16);
        }
        double glanceX = 1.9*Math.Max(-1,Math.Min(1,GazeX));
        double glanceY = 1.4*Math.Max(-1,Math.Min(1,GazeY));
        PaintReferenceEye(d,355,292,glanceX,glanceY,blink,false);
        PaintReferenceEye(d,444,292.5,glanceX,glanceY,blink,true);

        Path(d,"M397,310 Q395,314 396,320",null,"#392D24",2.9);
        if (MouthActive) {
            double open=MouthAmount;
            if(open>.04) E(d,399,353,7+6*open,1.2+8*open,"#945D59",HumanEdge,2.2);
            else Path(d,"M385,352 Q397,356 411,352",null,HumanEdge,2.4);
        } else if (eating) {
            double size=1.1+Math.Abs(Math.Sin(Clock*5));
            E(d,400,354,3.5+size,size,"#795849",null,0);
        } else if (Animation=="waving"||Animation=="jumping") {
            Path(d,"M382,350 C390,355 407,355 417,350 C414,358 408,362 399,362 Q388,362 382,350 Z",
                "#AA796A",HumanEdge,2.8);
        } else if (Animation=="failed") {
            Path(d,"M385,356 C393,350 406,350 415,355",null,HumanEdge,2.8);
        } else {
            Path(d,"M383,351 Q400,362 416,351",null,HumanEdge,3.2);
        }

        // A low fringe, with one lock hanging on each side of the face.
        Path(d,
            "M281,153 C277,140 281,119 292,114 C304,108 308,121 320,123 " +
            "C326,123 337,115 350,110 C377,97 398,94 420,99 " +
            "C445,102 463,113 473,130 C474,117 478,105 488,100 " +
            "C497,93 507,96 511,105 C518,119 509,138 500,147 " +
            "Q517,142 522,158 C536,166 540,186 531,203 Q528,211 520,217 " +
            "C530,224 535,242 533,259 C532,278 528,282 506,283 " +
            "C493,284 477,283 468,278 Q452,268 447,248 " +
            "C442,259 432,265 417,267 Q385,270 367,250 " +
            "Q361,249 358,244 Q351,250 355,239 Q359,234 364,236 " +
            "Q361,226 362,216 C353,225 344,234 337,245 " +
            "C326,263 323,283 319,295 Q315,302 300,296 " +
            "C282,291 273,274 272,254 Q269,241 273,225 " +
            "Q259,241 253,234 Q252,223 255,213 " +
            "Q251,200 265,187 Q263,178 276,166 Q283,157 295,152 Z",
            HumanHair,HumanEdge,3.15);
        // Subtle interior curves keep the forelock one dark mass at pet scale.
        Path(d,"M282,155 Q296,149 307,148 M500,147 Q489,157 485,158 M487,158 Q509,157 521,166 " +
            "M278,206 Q273,217 273,228 M531,203 Q526,216 515,213 " +
            "M364,236 Q361,219 366,191 M446,249 Q450,237 448,224",
            null,"#212522",2.9);
        Path(d,"M367,249 C358,250 353,245 355,239 C358,232 368,233 375,240 Q380,247 374,248 " +
            "M442,231 C435,228 428,231 424,237 Q419,248 427,249 Q437,249 441,240",null,"#232724",3.2);
    }
    void PaintReferenceEar(DrawingContext d,bool right) {
        d.PushTransform(new ScaleTransform(right?-1:1,1,400,220));
        double twitch=Clock%7;
        bool flick=!Sleeping && twitch>4.9 && twitch<5.16;
        d.PushTransform(new RotateTransform(flick?2.5*Math.Sin(Math.PI*(twitch-4.9)/.26):0,280,227));
        Path(d,"M285,155 C265,152 245,153 231,159 C221,163 220,186 223,202 " +
            "Q224,217 229,232 C217,233 214,245 220,254 Q224,262 238,265 " +
            "C225,271 223,281 230,288 Q239,297 254,291 " +
            "C243,305 248,319 261,322 Q273,325 280,316 " +
            "Q291,295 303,276 L318,234 L320,179 Z",HumanEar,HumanEdge,3.5);
        Path(d,"M232,160 Q254,149 284,155 L282,160 Q271,167 265,178 Q249,168 231,164 Z",
            HumanHair,HumanEdge,2.3);
        Path(d,"M258,195 C245,197 246,210 253,219 C242,228 242,244 264,253 " +
            "Q256,282 279,282 M250,297 Q255,292 257,289",
            null,"#4D583B",2.8);
        d.Pop();d.Pop();
    }
    static void PaintReferenceEye(DrawingContext d,double x,double y,double dx,double dy,double blink,bool right) {
        d.PushTransform(new TranslateTransform(x,y));
        if (blink>.95) {
            Path(d,"M-21,3 Q0,15 21,3",null,HumanEdge,3.4);
        } else {
            d.PushTransform(new ScaleTransform(1,1-.96*blink,0,3));
            Path(d,"M-29,-10 C-27,-24 -19,-32 -3,-32 C13,-32 22,-23 23,-6 " +
                "Q26,18 16,28 Q7,35 -11,32 C-23,29 -29,18 -29,6 Z",
                "#F2F1E8",null,0);
            d.PushTransform(new TranslateTransform(dx,dy));
            d.DrawGeometry(B("#497566"),null,HumanEyeIris);
            d.PushClip(HumanEyeIris);
            Path(d,"M-24,-29 H24 V1 Q11,12 -5,9 Q-17,8 -23,13 Z","#356056",null,0);
            Path(d,"M-23,11 C-12,4 -4,6 2,9 C11,13 17,7 22,10 L23,29 H-23 Z","#598671",null,0);
            E(d,.1,-.6,8.2,12.6,"#153431","#102925",1.9);
            E(d,-12.7,6.2,2.3,3.4,"#DEEAE2",null,0);
            d.Pop();
            d.DrawGeometry(null,P("#142425",3.1),HumanEyeIris);
            d.Pop();
            if(right) Path(d,"M-20,-20 C-10,-34 5,-35 16,-25 Q25,-17 28,-7",null,HumanEdge,3.8);
            else Path(d,"M-28,-10 C-24,-26 -14,-33 0,-33 Q14,-34 22,-20",null,HumanEdge,3.8);
            d.Pop();
        }
        d.Pop();
    }
}
}
