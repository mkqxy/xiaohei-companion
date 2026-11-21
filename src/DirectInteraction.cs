using System;
namespace XiaoheiDesktop {
public enum PetGestureKind { None, SingleTap, DoubleTap, Hold }
public struct PetGesture {
    public PetGestureKind Kind; public bool Buddy;
    public PetGesture(PetGestureKind kind,bool buddy){Kind=kind;Buddy=buddy;}
}
// A clock supplied by the caller keeps Windows click timing and held drags testable.
public sealed class PetGestureController {
    public const double HoldSeconds=.65,DragDistance=6;
    readonly double doubleClickSeconds;
    bool pressed,moved,held,buddy,pending,pendingBuddy;
    double pressedAt,singleAt;
    public bool Pressed {get{return pressed;}}
    public PetGestureController(double doubleClickSeconds){this.doubleClickSeconds=Math.Max(.1,doubleClickSeconds);}
    public PetGesture Down(double now,bool isBuddy,bool isDoubleClick){
        PetGesture previous=new PetGesture();
        if(isDoubleClick){Cancel();return new PetGesture(PetGestureKind.DoubleTap,isBuddy);}
        if(pending)previous=new PetGesture(PetGestureKind.SingleTap,pendingBuddy);
        pending=false;pressed=true;moved=false;held=false;buddy=isBuddy;pressedAt=now;return previous;
    }
    public bool Move(double distance){if(pressed&&distance>DragDistance){moved=true;CancelPendingTap();}return moved;}
    public void Up(double now){if(pressed&&!moved&&!held){pending=true;pendingBuddy=buddy;singleAt=now+doubleClickSeconds;}pressed=false;}
    public PetGesture Tick(double now){
        if(pressed&&!moved&&!held&&now-pressedAt>=HoldSeconds){held=true;CancelPendingTap();return new PetGesture(PetGestureKind.Hold,buddy);}
        if(pending&&now>=singleAt){pending=false;return new PetGesture(PetGestureKind.SingleTap,pendingBuddy);}
        return new PetGesture();
    }
    public void CancelPendingTap(){pending=false;}
    public void Cancel(){pressed=false;moved=false;held=false;pending=false;}
}
public sealed class QuickToolbarVisibility {
    public const double LeaveGraceSeconds=.8;
    double hideAt=Double.NegativeInfinity; bool inside;
    public void Enter(){inside=true;hideAt=Double.PositiveInfinity;}
    public void Leave(double now){inside=false;hideAt=now+LeaveGraceSeconds;}
    public bool Visible(double now,bool toolbarHovered,bool pressing){return inside||toolbarHovered||pressing||now<hideAt;}
}
public sealed class CompanionReactions {
    static readonly string[] states={"waving","waiting","review","jumping"};
    static readonly string[] lines={"嗨，我在呢。","嗯？在听你说。","让我瞧瞧，今天也很棒。","和你一起，开心一下！"};
    int tapIndex;string last="";
    public string NextTap(out string line){int i=tapIndex++%states.Length;if(states[i]==last)i=tapIndex++%states.Length;last=states[i];line=lines[i];return last;}
    public string NextAmbient(Random random){int lastIndex=Array.IndexOf(states,last),i=random.Next(lastIndex<0?states.Length:states.Length-1);if(lastIndex>=0&&i>=lastIndex)i++;last=states[i];return last;}
    public static bool CanAutoReact(bool enabled,bool quiet,bool sleeping,bool focusing,bool hovered,bool pressing,bool activeAction,bool pocketVisible){return enabled&&!quiet&&!sleeping&&!focusing&&!hovered&&!pressing&&!activeAction&&!pocketVisible;}
}
}
