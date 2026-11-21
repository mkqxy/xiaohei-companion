using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace XiaoheiDesktop {
public sealed partial class PetWindow {
    const double ToolbarSpace = 80;
    const double DockGap = 1;
    bool tallLayout;
    readonly DispatcherTimer scaleSave = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };

    public double PawBaseline {get{return Top + Height;}}
    public void SetDocked(bool docked) {
        Pref.DockToTaskbar=docked;
        if (IsLoaded) ClampToScreen();
        Save();
    }
    public void FinishPetDrag() {
        var area=WorkArea();
        Pref.DockToTaskbar=PawBaseline >= area.Bottom-22;
        ClampToScreen();
        Save();
    }
    public void SetScale(double scale) { ResizePet(scale,true); }
    void ResizePet(double scale,bool saveNow) {
        if (Double.IsNaN(scale)||Double.IsInfinity(scale)) scale=.36;
        Pref.Scale=Math.Min(1,Math.Max(.28,scale));
        Profile.scale=Pref.Scale;
        UpdateFormLayout(art.HumanForm||art.IsChangingForm);
        if (!initialized) return;
        if(saveNow) Save();
        else {scaleSave.Stop();scaleSave.Start();}
    }
    Size FormWindowSize(bool tall) {
        double cat=320*Pref.Scale;
        double frame=tall?cat*PetArt.HumanHeightFactor:cat;
        // Leave room for the head at the top of a jump and all three split tails at the sides.
        // Both forms have the same horizontal reach so transforming at a screen
        // edge does not move the character sideways to fit its companions.
        return new Size(Math.Max(190,cat*PetArt.HumanHeightFactor*192/208),
                        ToolbarSpace+frame+(tall?frame*14/208:0));
    }
    void MigrateFormPosition() {
        if(Pref.LayoutVersion>=4)return;
        var previous=new Size(Math.Max(190,400*Pref.Scale),ToolbarSpace+320*Pref.Scale);
        var current=FormWindowSize(Pref.HumanForm);
        if(Pref.X!=-99999)Pref.X+=(previous.Width-current.Width)/2;
        if(Pref.Y!=-99999)Pref.Y+=previous.Height-current.Height;
        Pref.LayoutVersion=4;
    }
    void UpdateFormLayout(bool tall) {
        tallLayout=tall;
        art.CatFrameHeight=320*Pref.Scale;
        var size=FormWindowSize(tall);
        double oldLeft=Left,oldTop=Top,center=Left+Width/2,feet=PawBaseline;
        Rect area=initialized?WorkArea():Rect.Empty;
        Width=size.Width; Height=size.Height;
        art.InvalidateVisual();
        if(!initialized)return;
        Left=center-Width/2; Top=feet-Height;
        FitToArea(area);
        if(pressing)original+=new Vector(Left-oldLeft,Top-oldTop);
    }
    void FitToArea(Rect area) {
        Left=Math.Max(area.Left,Math.Min(Left,area.Right-Width));
        double bottom=area.Bottom-Height;
        Top=Math.Max(area.Top,Pref.DockToTaskbar?bottom-DockGap:Math.Min(Top,bottom));
    }
    internal StackPanel SizeChooser() {
        var box=new StackPanel{Width=185,Margin=new Thickness(6,4,6,4)};
        var label=UI.Text("",13,UI.Ink);
        var slider=new Slider{Minimum=.28,Maximum=1,TickFrequency=.01,IsSnapToTickEnabled=true,IsMoveToPointEnabled=true,Value=Pref.Scale,Margin=new Thickness(0,8,0,4)};
        AutomationProperties.SetName(slider,"桌宠大小");
        Action display=delegate {label.Text="桌宠大小　"+Math.Round(slider.Value*100)+"%";};
        slider.ValueChanged+=delegate {ResizePet(slider.Value,false);display();};
        display();box.Children.Add(label);box.Children.Add(slider);
        return box;
    }
    // All icons use fixed device-independent targets. Scaling the pet does not
    // make its hover controls harder to hit.
    static FrameworkElement ActionIcon(string label) {
        string path;
        switch(label) {
            case "变身":path="M4,8 H17 L14,5 M20,16 H7 L10,19";break;
            case "分身":path="M4,19 C13,20 9,10 14,9 M10,16 C12,14 14,16 18,14 M13,5 A2.4,2.4 0 1 0 17.8,5 A2.4,2.4 0 1 0 13,5 M17,12 A2.1,2.1 0 1 0 21.2,12 A2.1,2.1 0 1 0 17,12";break;
            case "皇受":path="M4,11 L4,4 L9,7 Q12,6 15,7 L20,4 L20,12 Q20,19 12,20 Q4,19 4,11 Z M8,12 L9,12 M15,12 L16,12 M11,15 L13,15";break;
            case "比丢":path="M4,15 C4,9 7,5 12,5 C17,5 20,9 20,15 Q20,20 12,20 Q4,20 4,15 Z M8,11 L8.5,11 M15.5,11 L16,11 M10,14 L12,12.5 L14,14 L12,16 Z";break;
            default:path="M9,7 V5 Q9,3 11,3 H13 Q15,3 15,5 V7 M4,7 H20 V19 H4 Z M4,12 Q12,15 20,12 M12,12 V15";break;
        }
        var drawing=new System.Windows.Shapes.Path{Data=Geometry.Parse(path),Stroke=PetArt.B("#395144"),StrokeThickness=1.65,StrokeLineJoin=PenLineJoin.Round,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,IsHitTestVisible=false};
        var canvas=new Canvas{Width=24,Height=24,IsHitTestVisible=false};
        canvas.Children.Add(drawing);return canvas;
    }
}
}
