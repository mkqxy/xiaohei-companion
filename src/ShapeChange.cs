using System;
using System.Windows;
using System.Windows.Media;

namespace XiaoheiDesktop {
public sealed partial class PetWindow {
    public void ResetFormAfterSkinSelection() {
        StopGuestPlay(); art.CancelTail();
        Pref.HumanForm=false; art.SetHumanForm(false,false);
        UpdateFormLayout(false);
        art.InvalidateVisual(); Save(); RefreshPocket();
    }
    public void ToggleForm() {
        StopGuestPlay();
        gestures.CancelPendingTap();
        if (!Timer.Running) PlayAnimation("waving", null);
        Pref.HumanForm = !art.HumanForm;
        art.SetHumanForm(Pref.HumanForm, true);
        UpdateFormLayout(art.HumanForm||art.IsChangingForm);
        if(art.UsingCustomSkin)
            Say(Pref.HumanForm?"换成人形了，再点「变身」回到你的形象。":"你的形象回来啦。",4,true);
        Save(); RefreshPocket();
    }

    public void SplitTail() {
        if (Timer.Running) { Say("等这段专注结束，再叫嘿咻出来吧。", 3, true); return; }
        gestures.CancelPendingTap();
        if (art.TailActive) { art.RecallTail(); return; }
        StopGuestPlay();
        PlayAnimation("idle", null);
        art.BeginTail();
        if(art.UsingCustomSkin&&!art.HumanForm)Say("小黑来分尾巴啦，收回后换回你的形象。",4,true);
    }
}

public sealed partial class PetArt {
    public bool HumanForm { get; private set; }
    public bool UsingCustomSkin {get{return UseSprite&&SpriteAtlas.Current!=null&&SpriteAtlas.Current.IsCustomSkin;}}
    public bool GuestIsHeyxiu { get; set; }
    const double FormDuration = .68, AutoRecall = 5.35, RecallDuration = .96;
    double changedAt = Double.NegativeInfinity;
    double FormProgress { get { return (Clock-changedAt)/FormDuration; } }
    internal bool IsChangingForm { get { return FormProgress>=0 && FormProgress<1; } }
    bool PaintedHuman { get { return IsChangingForm && FormProgress<.46 ? !HumanForm : HumanForm; } }
    double splitAt = Double.NegativeInfinity;
    double recallAt = Double.PositiveInfinity;
    readonly double[] recallFrom = new double[3];

    public bool TailActive {
        get { return !Double.IsNegativeInfinity(splitAt) && Clock >= splitAt &&
                     Clock < Math.Min(recallAt,splitAt+AutoRecall)+RecallDuration; }
    }
    internal void CopyFormFrom(PetArt s) {
        HumanForm=s.HumanForm; GuestIsHeyxiu=s.GuestIsHeyxiu; changedAt=s.changedAt;
        splitAt=s.splitAt; recallAt=s.recallAt; headNapAt=s.headNapAt;
        Array.Copy(s.recallFrom,recallFrom,recallFrom.Length);
    }
    public void SetHumanForm(bool human, bool animate) {
        if (HumanForm != human) {
            bool previous=PaintedHuman;
            HumanForm=human; CancelTail();
            changedAt=animate && previous!=human?Clock:Double.NegativeInfinity;
        } else if(!animate) changedAt=Double.NegativeInfinity;
        InvalidateVisual();
    }
    public void BeginTail() {
        splitAt=Clock; recallAt=Double.PositiveInfinity;
        for(int i=0;i<recallFrom.Length;i++)recallFrom[i]=1;
        InvalidateVisual();
    }
    public void RecallTail() {
        if (!TailActive || Clock>=Math.Min(recallAt,splitAt+AutoRecall)) return;
        for(int i=0;i<recallFrom.Length;i++)recallFrom[i]=TailProgress(i);
        recallAt=Clock; InvalidateVisual();
    }
    public void CancelTail() {
        splitAt=Double.NegativeInfinity; recallAt=Double.PositiveInfinity; CancelHeadNap();
        InvalidateVisual();
    }
    static double Unit(double x) {return Math.Max(0,Math.Min(1,x));}
    static double Ease(double x) {x=Unit(x);return x*x*(3-2*x);}
    double TailProgress(int i) {
        double back=Math.Min(recallAt,splitAt+AutoRecall);
        if(Clock>=back) return recallFrom[i]*(1-Ease((Clock-back-(2-i)*.08)/.67));
        return Unit((Clock-splitAt-.37-i*.33)/(.74+i*.065));
    }
    bool DrawFormEffects(DrawingContext dc,SpriteAtlas atlas) {
        if(HeadNapActive && atlas!=null && !Sleeping) {DrawHeadNap(dc,atlas);return true;}
        double t=FormProgress;
        bool changing=IsChangingForm;
        bool human=PaintedHuman;
        bool tail=TailActive&&!Sleeping;
        bool maskAtlasBuddy=GuestIsHeyxiu&&atlas!=null&&!atlas.IsCustomSkin;
        if (!changing&&!human&&!tail&&!maskAtlasBuddy) return false;
        var r=SpriteBounds();
        if(r.Width<=0||r.Height<=0)return false;
        dc.PushTransform(new TranslateTransform(r.X,r.Y));
        dc.PushTransform(new ScaleTransform(r.Width/192,r.Height/208));
        double squeeze=changing?Math.Pow(Math.Sin(Math.PI*t),4):0;
        dc.PushTransform(new ScaleTransform(1+.13*squeeze,1-.40*squeeze,96,194));
        dc.PushOpacity(1-.64*squeeze);
        if(human)DrawBoy(dc,tail);
        else if(tail||maskAtlasBuddy||atlas==null)
            DrawSoloCat(dc,atlas!=null&&atlas.IsCustomSkin?SpriteAtlas.BuiltIn:atlas,tail);
        else {
            int col=SpriteTimeline.FrameAt("idle",Clock,1);
            dc.DrawImage(atlas.Frame(0,col),new Rect(0,0,192,208));
        }
        dc.Pop();dc.Pop();
        if(changing)DrawTransformArc(dc,t);
        dc.Pop();dc.Pop();return true;
    }
    static void DrawTransformArc(DrawingContext dc,double progress) {
        double alpha=Math.Sin(Math.PI*progress);
        dc.PushOpacity(alpha*.9);
        dc.PushTransform(new RotateTransform(-22+48*progress,96,122));
        double y=178-108*progress;
        var pen=P("#BAE6A3",3.8);
        dc.DrawEllipse(null,pen,new Point(96,y),26+48*alpha,6+9*alpha);
        for(int i=0;i<4;i++) {
            double a=i*Math.PI/2+progress*2;
            E(dc,96+Math.Cos(a)*(31+46*alpha),y+Math.Sin(a)*22,2.9,2.9,"#F6E4A0",null,0);
        }
        dc.Pop();dc.Pop();
    }
    // The default body, tail and little buddy were painted on one atlas cell.
    // Use the neutral pose during a split, crop out the tail/buddy, and restore
    // the outer rear paw that was behind the buddy. The face is the original art.
    void DrawSoloCat(DrawingContext dc,SpriteAtlas atlas,bool splitting) {
        double a=TailProgress(0), b=TailProgress(1);
        if(!splitting)a=b=0;
        double jump=Animation=="jumping"?12*Math.Abs(Math.Sin(AnimationElapsed*5.2)):0;
        dc.PushTransform(new TranslateTransform(0,-jump));
        if(splitting||atlas==null)DrawSeparateTail(dc,a,b,false);
        if(atlas!=null) {
            // The stationary companion is already painted beside the right paw.
            // Keep it in the group when the other three come out of the tail.
            if (splitting) {
                dc.PushClip(Geo("M0,0 L192,0 L192,208 L64,208 C56,201 54,189 56,177 C57,161 61,151 68,139 L0,139 Z"));
            } else {
                Path(dc,"M119,145 Q131,152 135.2,166 Q140.7,186 133,194 Q128,200 117,197 L110,187 Z","#0A0604","#412821",3.0);
                // Only the area occupied by the foot buddy needs replacing.
                // Keep the original tail and its junction with the body intact.
                dc.PushClip(Geo("M0,0 L192,0 L192,157 L132,157 Q134,160 134,164 C131,165 127,166 126,173 L122,194 Q119,197 115,198 L106,208 L0,208 Z"));
            }
            int blink=Clock%4.1<.15?3:0;
            dc.DrawImage(atlas.Frame(0,blink),new Rect(0,0,192,208));
            dc.Pop();
        } else {
            dc.PushTransform(new ScaleTransform(.52,.59));DrawBody(dc,Clock);DrawHead(dc,Clock);dc.Pop();
        }
        if(splitting)DrawClones(dc,a,b,false);
        dc.Pop();
    }

}
}
