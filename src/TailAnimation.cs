using System;
using System.Windows;
using System.Windows.Media;

namespace XiaoheiDesktop {
public sealed partial class PetArt {
    const string TailInk="#382823", TailFur="#100B0A";
    static Point Between(Point from,Point to,double amount) {
        return new Point(from.X+(to.X-from.X)*amount,from.Y+(to.Y-from.Y)*amount);
    }
    double TailUncurl() {
        if(!TailActive)return 0;
        double back=Math.Min(recallAt,splitAt+AutoRecall);
        return Ease((Math.Min(Clock,back)-splitAt)/.48)*(1-Ease((Clock-back-.64)/.31));
    }
    static readonly Point[] Landings={new Point(13,197),new Point(41,191),new Point(175,197)};
    struct TailPose {
        public Point Root, Bend, Tip, C1,C2,C3,C4;
        public double Thickness;
    }
    TailPose MakeTailPose(double first,double second,bool human) {
        double open=TailUncurl();
        double spent=TailActive?(Ease(first)+Ease(second)+Ease(TailProgress(2)))/3:0;
        Point root=human?new Point(80,150):new Point(65,183);
        Point resting=human?new Point(43,123):new Point(29,150);
        double lift=human?9:0;
        Point bend=Between(Between(root,resting,.60),new Point(11,177-lift),open);
        Point tip=Between(resting,new Point(34,194-lift),open);
        var p=new TailPose();
        p.Root=root;
        p.Bend=Between(bend,root,.73*spent);
        p.Tip=Between(tip,root,.73*spent);
        p.C1=Between(Between(new Point(root.X-12,root.Y+6),new Point(39,root.Y+5),open),root,.73*spent);
        p.C2=Between(Between(new Point(bend.X+4,bend.Y+8),new Point(19,169-lift),open),root,.73*spent);
        p.C3=Between(Between(new Point(bend.X-6,bend.Y-5),new Point(0,192-lift),open),root,.73*spent);
        p.C4=Between(Between(new Point(resting.X,resting.Y+5),new Point(20,197-lift),open),root,.73*spent);
        p.Thickness=(human?9:16)-open*(human?1:3)-spent*4;
        return p;
    }
    void DrawSeparateTail(DrawingContext dc,double a,double b,bool human) {
        TailPose tail=MakeTailPose(a,b,human);
        var curve=new StreamGeometry();
        using(var g=curve.Open()){
            g.BeginFigure(tail.Root,false,false);
            g.BezierTo(tail.C1,tail.C2,tail.Bend,true,true);
            g.BezierTo(tail.C3,tail.C4,tail.Tip,true,true);
        }
        dc.DrawGeometry(null,P(TailInk,tail.Thickness+3.0),curve);
        dc.DrawGeometry(null,P(TailFur,tail.Thickness),curve);
    }
    // The tip bulges before the new body separates. Near the floor, the
    // round backs and slightly different landing times carry the movement.
    void DrawClones(DrawingContext dc,double a,double b,bool human) {
        TailPose tail=MakeTailPose(a,b,human);
        double age=Clock-splitAt;
        if(human || !UseSprite || SpriteAtlas.Current==null){
            dc.PushOpacity(TailUncurl());
            DrawHeyxiuBody(dc,new Point(144,188),.90,1,1,-.65,0);
            dc.Pop();
        }
        for(int i=0;i<3;i++){
            double p=i==0?a:i==1?b:TailProgress(i);
            if(p<=0)continue;
            double bud=Ease(p/.24), flight=Unit((p-.20)/.63);
            double motion=Ease(flight),landing=Unit((p-.83)/.17);
            Point from=new Point(tail.Tip.X+(i==2?3:-3),tail.Tip.Y-1);
            Point to=Landings[i];
            // Index 2 rolls across the foreground. Its low arc keeps both
            // the cat's face and the scale of the tail visible.
            double lift=i==0?21:i==1?12:14;
            Point pos=Between(from,to,motion);
            pos.Y-=4*bud+lift*Math.Sin(Math.PI*flight);
            pos.Y+=3.1*Math.Sin(Math.PI*landing);
            double stretch=Math.Sin(flight*2*Math.PI);
            double squash=Math.Sin(Math.PI*landing);
            double sx=.99-.10*stretch+.21*squash;
            double sy=1+.21*stretch-.27*squash;
            double size=(i==1?.78:i==2?.90:1)*(.28+.72*bud);
            if(p<.40) {
                double neck=1-Ease((p-.16)/.24);
                if(neck>0) {
                    var line=new StreamGeometry();
                    using(var g=line.Open()){
                        g.BeginFigure(tail.Tip,false,false);
                        g.QuadraticBezierTo(new Point((tail.Tip.X+pos.X)*.5,tail.Tip.Y-2),pos,true,false);
                    }
                    dc.DrawGeometry(null,P(TailInk,6.5*neck+.6),line);
                    dc.DrawGeometry(null,P(TailFur,5.5*neck+.2),line);
                }
            }
            double look=i==2?-.74:.74;
            // A dark bud has no ears or eyes until it has separated. One
            // turns around after landing, as when the four gather on grass.
            double face=Ease((p-.36)/.35);
            if(i==1)face*=.5+.5*Math.Cos(Math.Max(0,age-1.8)*2.3+1.8);
            double tilt=(i==2?11:-9)*Math.Sin(Math.PI*flight);
            DrawHeyxiuBody(dc,pos,size*sx,sy,face,look,tilt);
        }
    }
    static void DrawHeyxiuBody(DrawingContext dc,Point center,double widthScale,double heightScale,double face,double look,double tilt) {
        dc.PushTransform(new TranslateTransform(center.X,center.Y));
        dc.PushTransform(new RotateTransform(tilt));
        dc.PushTransform(new ScaleTransform(widthScale,widthScale*heightScale));
        Path(dc,"M-11.1,2.9 C-11.2,-.7 -9.5,-4.4 -7.3,-5.7 L-7.3,-7.7 Q-5.5,-7.8 -4.3,-6.8 Q-.2,-8 3.6,-6.8 Q5.3,-8.1 6.4,-8 L7,-6.3 Q11.3,-3.6 11.4,2 C11.6,6.9 8,8.1 .4,8.1 Q-10.9,8.1 -11.1,2.9 Z",TailFur,TailInk,1.4);
        if(face>.055){
            dc.PushOpacity(Ease(face));
            if(look<0)dc.PushTransform(new ScaleTransform(-1,1));
            double offset=1.0*Math.Abs(look);
            E(dc,-3.8+offset,.7,4.25,5.27,"#F7EFCB",null,0);
            E(dc,-3.1+offset,.65,2.67,4.23,"#0E0A0B",null,0);
            E(dc,5.05+offset,.55,3.38,4.9,"#F7EFCB",null,0);
            E(dc,5.40+offset,.51,2.22,3.93,"#0E0A0B",null,0);
            if(look<0)dc.Pop();
            dc.Pop();
        }
        dc.Pop();dc.Pop();dc.Pop();
    }
}
}
