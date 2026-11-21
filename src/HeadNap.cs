using System;
using System.Windows;
using System.Windows.Media;

namespace XiaoheiDesktop {
public sealed partial class PetWindow {
    public void HeadNap() {
        if(Timer.Running) {Say("等这一段专注结束，再叫嘿咻上来吧。",3,true);return;}
        if(art.HumanForm || art.UsingCustomSkin) {Say("换回小黑的猫形，就能让嘿咻趴在头上。",3,true);return;}
        if(art.HeadNapActive) {art.WakeHeadNap();return;}
        StopGuestPlay();
        art.CancelTail();
        PlayAnimation("idle",null);
        art.BeginHeadNap();
        PlaySound("heyxiu");
    }
}

public sealed partial class PetArt {
    const double NapLands=.85, NapWakes=5.5, HopDown=5.9, NapEnds=6.9;
    double headNapAt=Double.NegativeInfinity;
    double NapAge {get{return Clock-headNapAt;}}
    public bool HeadNapActive {get{return NapAge>=0 && NapAge<NapEnds;}}
    public void BeginHeadNap() {
        if(HumanForm || UsingCustomSkin)return;
        CancelTail();headNapAt=Clock;InvalidateVisual();
    }
    public void CancelHeadNap() {headNapAt=Double.NegativeInfinity;}
    public void WakeHeadNap() {
        if(NapAge>NapLands && NapAge<NapWakes)headNapAt=Clock-NapWakes;
        InvalidateVisual();
    }
    bool HitNappingBuddy(Point p) {
        if(NapAge<NapLands || NapAge>HopDown)return false;
        Rect r=SpriteBounds();
        return Math.Abs(p.X-(r.X+r.Width*.52))<r.Width*.095 &&
               Math.Abs(p.Y-(r.Y+r.Height*.08))<r.Height*.065;
    }
    void DrawHeadNap(DrawingContext dc,SpriteAtlas atlas) {
        Rect r=SpriteBounds();
        if(r.Width<=0 || r.Height<=0)return;
        dc.PushTransform(new TranslateTransform(r.X,r.Y));
        dc.PushTransform(new ScaleTransform(r.Width/192,r.Height/208));
        DrawSoloCat(dc,atlas,false);
        double age=NapAge;
        double progress=age<HopDown?Ease(age/NapLands):1-Ease((age-HopDown)/(NapEnds-HopDown));
        double arc=Math.Sin(Math.PI*progress);
        // Travel along the outer ear rather than straight through the face.
        Point position=Between(new Point(144,188),new Point(100,16.9),progress);
        position.X+=55*arc;
        position.Y-=23*arc;
        double settle=Unit((age-NapLands)/.23);
        double landing=Math.Sin(settle*Math.PI);
        double doze=Ease((age-1.12)/.65)*(1-Ease((age-NapWakes)/.32));
        double breath=Math.Sin((age-1.1)*3.4)*doze;
        position.Y+=landing*2.5+doze*.8+breath*.6;
        double tilt=(age<HopDown?-18:14)*arc+doze*5;
        double width=1+.09*landing+.10*doze;
        double height=1-.19*landing-.21*doze+.026*breath;
        DrawHeyxiuBody(dc,position,width,height,1-doze,-.5,tilt);
        if(doze>0) {
            dc.PushTransform(new TranslateTransform(position.X,position.Y));
            dc.PushTransform(new RotateTransform(tilt));
            dc.PushTransform(new ScaleTransform(width,width*height));
            dc.PushOpacity(doze);
            Path(dc,"M-7.4,1 Q-5,3.4 -2.6,1.3 M2,1.2 Q4.4,3.5 6.7,1.1",null,"#ECE1B8",1.4);
            dc.Pop();dc.Pop();dc.Pop();dc.Pop();
        }
        dc.Pop();dc.Pop();
    }
}
}
