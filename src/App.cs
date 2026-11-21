using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace XiaoheiDesktop {
public sealed class Preferences {
    public bool HumanForm = false, DockToTaskbar = true;
    public bool SoundEnabled = true;
    public double SoundVolume = .30;
    public double Scale=.36, X=-99999, Y=-99999;
    public int LayoutVersion=0, FocusDuration=0;
    public long FocusDeadlineTicks=0, FocusPausedTicks=0;
    public bool ShowHeyxiu=true, AutoExpressions=true;
    public bool Topmost=true, Quiet=false, Roaming=false;
    public int WaterMinutes=0, Pets=0, Snacks=0, Affection=35;
}
public static class Store {
    public static readonly string Dir=Environment.GetEnvironmentVariable("XIAOHEI_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"XiaoheiCompanionV3");
    public static string LastError="";
    public static Preferences Load() { try { string p=Path.Combine(Dir,"settings.json"); if(File.Exists(p)) return new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(p,Encoding.UTF8))??new Preferences{LayoutVersion=2}; } catch { } return new Preferences{LayoutVersion=2}; }
    public static bool Write(string name,string text) {
        try { Directory.CreateDirectory(Dir); string file=Path.Combine(Dir,name),tmp=file+".tmp"; File.WriteAllText(tmp,text,new UTF8Encoding(false)); if(File.Exists(file)) File.Replace(tmp,file,null); else File.Move(tmp,file); LastError=""; return true; }
        catch(Exception ex) { LastError=ex.Message; return false; }
    }
    public static string ReadNote() { try { var p=Path.Combine(Dir,"note.txt"); return File.Exists(p)?File.ReadAllText(p,Encoding.UTF8):""; } catch { return ""; } }
}
public sealed class FocusClock {
    public int SelectedMinutes=25;
    public DateTime? Deadline;
    public TimeSpan Remaining=TimeSpan.FromMinutes(25);
    public bool Running { get { return Deadline.HasValue; } }
    public bool Tick(DateTime now) { if(!Deadline.HasValue) return false; Remaining=Deadline.Value-now; if(Remaining>TimeSpan.Zero) return false; Remaining=TimeSpan.Zero; Deadline=null; return true; }
    public void Start(DateTime now) { if(Remaining<=TimeSpan.Zero) Remaining=TimeSpan.FromMinutes(SelectedMinutes); Deadline=now+Remaining; }
    public void Pause(DateTime now) { if(Deadline.HasValue) Remaining=Deadline.Value>now?Deadline.Value-now:TimeSpan.Zero; Deadline=null; }
    public void Reset(int minutes) { SelectedMinutes=minutes; Deadline=null; Remaining=TimeSpan.FromMinutes(minutes); }
    public string Display { get { var sec=(int)Math.Ceiling(Remaining.TotalSeconds); return (sec/60).ToString("00")+":"+(sec%60).ToString("00"); } }
}
public static class Program {
    static Mutex mutex; static EventWaitHandle showSignal,actionSignal;
    [STAThread] public static void Main(string[] args) {
        if(args.Length>2&&args[0]=="--render-state"){RenderState(args[1],args[2]);return;}
        if(args.Length>1&&args[0]=="--render-workbench"){RenderWorkbench(args[1]);return;}
        if(args.Length>1&&args[0]=="--render-quick-preview"){RenderQuickPreview(args[1]);return;}
        if(args.Length>1&&args[0]=="--render-demo"){RenderDemo(args[1]);return;}
        if(args.Length>1&&args[0]=="--render-frames"){RenderDemo(args[1],true);return;}
        if(args.Length>0 && (args[0]=="--render-preview"||args[0]=="--render-proof")) {
            var art=new PetArt { Width=400,Height=320,Clock=1.5,GazeX=-.15,GazeY=-.08,Sleeping=args.Length>2&&args[2]=="sleep",ExpressionAge=.6 };
            PetExpression expression;if(args.Length>2&&Enum.TryParse<PetExpression>(args[2],out expression))art.Expression=expression;
            art.Measure(new Size(400,320));art.Arrange(new Rect(0,0,400,320));art.UpdateLayout();
            bool proof=args[0]=="--render-proof";var bmp=new RenderTargetBitmap(proof?320:400,320,96,96,PixelFormats.Pbgra32);
            if(proof){var visual=new DrawingVisual();using(var d=visual.RenderOpen()){d.DrawRectangle(System.Windows.Media.Brushes.White,null,new Rect(0,0,320,320));d.DrawRectangle(new VisualBrush(art){Stretch=Stretch.Fill,ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(0,0,400,320)},null,new Rect(0,0,400,320));}bmp.Render(visual);}else bmp.Render(art);
            var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(args[1])) enc.Save(f);return;
        }
        ActionCommand command;try{command=ActionCommand.FromArguments(args);}catch(Exception error){Console.Error.WriteLine(error.Message);Environment.ExitCode=2;return;}
        string instance=ActionCommand.InstanceKey;bool created; mutex=new Mutex(true,"Local\\XiaoheiCompanionV3-"+instance,out created);
        showSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\XiaoheiCompanionShowV3-"+instance);
        actionSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\XiaoheiCompanionActionV3-"+instance);
        if(!created) {if(command!=null){try{command.Enqueue();actionSignal.Set();}catch(Exception error){Console.Error.WriteLine(error.Message);Environment.ExitCode=3;}}else showSignal.Set();showSignal.Dispose();actionSignal.Dispose();mutex.Dispose();return; }
        var app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
        app.DispatcherUnhandledException+=delegate(object s,DispatcherUnhandledExceptionEventArgs e) {
            try { Directory.CreateDirectory(Store.Dir); File.AppendAllText(Path.Combine(Store.Dir,"error.log"),DateTime.Now+" "+e.Exception+Environment.NewLine); }catch{}
            MessageBox.Show("小黑遇到了一点问题，请重新打开。随手记仍保存在本机。\n"+e.Exception.Message,"小黑桌面伙伴");
            e.Handled=true;app.Shutdown(1);
        };
        var pet=new PetWindow(showSignal,actionSignal);if(args.Contains("--inspect-pet"))pet.ShowInTaskbar=true;if(args.Contains("--open-pocket"))pet.Loaded+=delegate{pet.OpenPocket();};if(command!=null)pet.Loaded+=delegate{pet.PlayAnimation(command.animation,command.message);};app.Run(pet);mutex.ReleaseMutex();mutex.Dispose();showSignal.Dispose();actionSignal.Dispose();
    }
    static void SavePng(Visual visual,string path,int width,int height){var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var output=File.Create(path))encoder.Save(output);}
    static void RenderState(string state,string path){if(SpriteTimeline.Row(state)<0)throw new ArgumentException("Unknown state");var art=new PetArt{Width=320,Height=346,Animation=state,AnimationElapsed=.3};art.Measure(new Size(320,346));art.Arrange(new Rect(0,0,320,346));art.UpdateLayout();SavePng(art,path,320,346);}
    static void RenderWorkbench(string path){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset)){var pet=new PetWindow(signal);var pocket=new PocketWindow(pet);var content=(FrameworkElement)pocket.Content;content.Measure(new Size(492,710));content.Arrange(new Rect(0,0,492,710));content.UpdateLayout();var visual=new DrawingVisual();using(var context=visual.RenderOpen()){context.DrawRectangle(pocket.Background,null,new Rect(0,0,492,710));context.DrawRectangle(new VisualBrush(content){Stretch=Stretch.Fill},null,new Rect(0,0,492,710));}SavePng(visual,path,492,710);pocket.Close();pet.Close();}app.Shutdown();}
    static void RenderQuickPreview(string path){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset)){var pet=new PetWindow(signal);pet.SetScale(.36);pet.RevealQuickControls();var content=(FrameworkElement)pet.Content;content.Measure(new Size(pet.Width,pet.Height));content.Arrange(new Rect(0,0,pet.Width,pet.Height));content.UpdateLayout();int width=(int)Math.Ceiling(pet.Width*2),height=(int)Math.Ceiling(pet.Height*2);var visual=new DrawingVisual();using(var context=visual.RenderOpen()){context.DrawRectangle(PetArt.B("#F5F7EE"),null,new Rect(0,0,width,height));context.DrawRectangle(new VisualBrush(content){Stretch=Stretch.Fill,ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(0,0,pet.Width,pet.Height)},null,new Rect(0,0,width,height));}SavePng(visual,path,width,height);pet.Close();}app.Shutdown();}
    static void RenderDemo(string file,bool framesOnly=false){
        if(framesOnly)Directory.CreateDirectory(file);
        var encoder=new GifBitmapEncoder();var poses=SpriteTimeline.States;
        int frameIndex=0;foreach(string pose in poses)for(int frameIndexInPose=0;frameIndexInPose<(int)Math.Ceiling(SpriteTimeline.DurationMs(pose)/100.0);frameIndexInPose++){
            var art=new PetArt{Width=400,Height=320,Clock=frameIndex*.1,Animation=pose,AnimationElapsed=frameIndexInPose*.1};
            art.Measure(new Size(400,320));art.Arrange(new Rect(0,0,400,320));art.UpdateLayout();
            var visual=new DrawingVisual();using(var d=visual.RenderOpen()){d.DrawRectangle(PetArt.B("#F5F7EE"),null,new Rect(0,0,320,280));d.DrawRectangle(new VisualBrush(art){Stretch=Stretch.Fill,ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(0,0,400,320)},null,new Rect(0,0,320,256));var text=new FormattedText(SpriteTimeline.Labels[SpriteTimeline.Row(pose)],System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),13,UI.Green,1);d.DrawText(text,new Point((320-text.Width)/2,260));}
            var bmp=new RenderTargetBitmap(320,280,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var metadata=new BitmapMetadata("gif");metadata.SetQuery("/grctlext/Delay",(ushort)10);metadata.SetQuery("/grctlext/Disposal",(byte)2);
            if(frameIndex==0){metadata.SetQuery("/appext/application",Encoding.ASCII.GetBytes("NETSCAPE2.0"));metadata.SetQuery("/appext/data",new byte[]{3,1,0,0,0});}
            if(framesOnly){var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var output=File.Create(Path.Combine(file,frameIndex.ToString("000")+".png")))png.Save(output);}else encoder.Frames.Add(BitmapFrame.Create(bmp,null,metadata,null));frameIndex++;
        }
        if(!framesOnly){using(var stream=File.Create(file))encoder.Save(stream);MediaExport.SetGifTiming(file,10);}
    }
}
public sealed partial class PetWindow : Window {
    public readonly Preferences Pref=Store.Load();
    public readonly CompanionModel Companion=new CompanionModel(delegate{return DateTime.UtcNow;});
    public readonly FocusClock Timer=new FocusClock();
    readonly PetArt art=new PetArt();
    public PetArt Art {get{return art;}}
    public CustomizationProfile Profile {get;private set;}
    public string ActiveAnimation {get;private set;}
    string requestedAnimation="idle";double animationUntil,stateStarted;int movementDirection=1;
    readonly Grid root=new Grid(); readonly Border bubble=new Border(); readonly TextBlock bubbleText=new TextBlock();
    readonly StackPanel toolbar=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top };
    readonly PetGestureController gestures=new PetGestureController(Forms.SystemInformation.DoubleClickTime/1000.0);
    readonly QuickToolbarVisibility toolbarVisibility=new QuickToolbarVisibility();
    readonly CompanionReactions reactions=new CompanionReactions();
    readonly DispatcherTimer frame=new DispatcherTimer(); readonly DispatcherTimer tick=new DispatcherTimer();
    readonly Stopwatch clock=Stopwatch.StartNew(); readonly Random random=new Random(); readonly EventWaitHandle showSignal,actionSignal;
    Forms.NotifyIcon tray; Forms.ContextMenuStrip trayMenu; PocketWindow pocket;
    DateTime bubbleUntil=DateTime.MinValue,lastInteraction=DateTime.UtcNow,nextWater=DateTime.MaxValue,nextAmbient=DateTime.UtcNow.AddMinutes(18);
    double reactUntil,nextBlink=3,blinkStart=-10,lastFrame,roamDestination,expressionStart,expressionUntil,heyxiuUntil,nextExpression=24;
    double roamTime;
    public PetExpression CurrentExpression {get;private set;}
    bool dragging,pressing,hovered,autoSleeping;
    Point down,original,lastDrag; bool initialized;
    [DllImport("user32.dll")] static extern bool GetCursorPos(out NativePoint point);
    [StructLayout(LayoutKind.Sequential)] struct NativePoint { public int X,Y; }
    public PetWindow(EventWaitHandle signal,EventWaitHandle commandSignal=null) {
        Profile=CustomizationProfile.Load();ActiveAnimation="idle";
        art.SetHumanForm(Pref.HumanForm,false);
        showSignal=signal;actionSignal=commandSignal;Title="小黑桌面伙伴";Width=300;Height=374;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;
        AllowsTransparency=true;Background=null;ShowInTaskbar=false;Topmost=Pref.Topmost;
        FontFamily=new FontFamily("Microsoft YaHei UI"); UseLayoutRounding=true;
        if(Pref.LayoutVersion<3){Pref.Scale=Profile.scale;Pref.LayoutVersion=3;}
        Pref.Scale=Math.Max(.28,Math.Min(1.0,Pref.Scale));Pref.Quiet=Profile.quiet;Pref.Roaming=Profile.roaming;Pref.AutoExpressions=Profile.autoExpressions;Pref.WaterMinutes=Profile.waterMinutes;Timer.Reset(Profile.focusMinutes);RestoreFocus(DateTime.UtcNow);
        MigrateFormPosition();
        Companion.Pets=Pref.Pets;Companion.Snacks=Pref.Snacks;Companion.Affection=Pref.Affection;Companion.Quiet=Pref.Quiet;Companion.Roaming=Pref.Roaming;
        BuildCarryMotion();UpdateAudioSettings();Content=root;art.Margin=new Thickness(0,ToolbarSpace,0,0);root.Children.Add(art);
        bubble.Background=PetArt.B("#FFFDF5");bubble.BorderBrush=PetArt.B("#DCE5D3");bubble.BorderThickness=new Thickness(1);bubble.CornerRadius=new CornerRadius(15,15,3,15);
        bubble.Padding=new Thickness(15,10,15,10);bubble.HorizontalAlignment=HorizontalAlignment.Center;bubble.VerticalAlignment=VerticalAlignment.Top;bubble.Margin=new Thickness(12,7,12,0);bubble.MaxWidth=270;bubble.Visibility=Visibility.Collapsed;
        bubbleText.FontSize=13;bubbleText.Foreground=PetArt.B("#3F5441");bubbleText.TextWrapping=TextWrapping.Wrap;bubble.Child=bubbleText;root.Children.Add(bubble);
        toolbar.Margin=new Thickness(0,ToolbarSpace-34,0,0);toolbar.Opacity=0;toolbar.IsHitTestVisible=false;
        AddTool("变身","切换猫形和人形",ToggleForm);
        AddTool("分身","放出或召回嘿咻",SplitTail);
        AddTool("皇受","叫皇受来玩",VisitTabby);
        AddTool("比丢","叫比丢来玩",VisitBidiu);
        AddTool("工具","专注陪伴与天气",OpenTools);
        root.Children.Add(toolbar);BuildFocusBadge();
        art.Cursor=Cursors.Hand;
        art.MouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e) {
            bool buddy=art.IsHeyxiu(e.GetPosition(art));PetGesture gesture=gestures.Down(clock.Elapsed.TotalSeconds,buddy,e.ClickCount==2);
            if(e.ClickCount==2){pressing=false;art.ReleaseMouseCapture();HandleGesture(gesture);e.Handled=true;return;}
            HandleGesture(gesture);pressing=true;dragging=false;WakeForInteraction();PostponeAmbient();down=PointToScreen(e.GetPosition(this));lastDrag=down;original=new Point(Left,Top);art.CaptureMouse();e.Handled=true;
        };
        art.MouseMove+=delegate(object s,MouseEventArgs e) { if(!pressing) return; Point p=PointToScreen(e.GetPosition(this));Vector delta=p-down;double dpi=GetDpi();dragging=gestures.Move(delta.Length/dpi);if(dragging) {ClearActionSound();art.CancelActing();art.CancelHeadNap();animationUntil=0;double horizontal=p.X-lastDrag.X;if(Math.Abs(horizontal)>=2*dpi){movementDirection=horizontal<0?-1:1;lastDrag=p;}Left=original.X+delta.X/dpi;Top=original.Y+delta.Y/dpi;} };
        art.MouseLeftButtonUp+=delegate { if(!pressing)return;bool moved=dragging;gestures.Up(clock.Elapsed.TotalSeconds);pressing=false;art.ReleaseMouseCapture();if(moved)FinishPetDrag();dragging=false; };
        art.LostMouseCapture+=delegate {if(pressing||dragging)gestures.Cancel();pressing=false;dragging=false;};
        root.MouseEnter+=delegate { hovered=true;toolbarVisibility.Enter();UpdateToolbar(clock.Elapsed.TotalSeconds); };
        root.MouseLeave+=delegate {hovered=false;toolbarVisibility.Leave(clock.Elapsed.TotalSeconds);UpdateToolbar(clock.Elapsed.TotalSeconds);};
        scaleSave.Tick+=delegate {scaleSave.Stop();Save();};
        art.MouseRightButtonUp+=delegate(object s,MouseButtonEventArgs e){gestures.CancelPendingTap();BuildContext().IsOpen=true;e.Handled=true;};
        Deactivated+=delegate{gestures.CancelPendingTap();};
        MouseWheel+=delegate(object s,MouseWheelEventArgs e){ if((Keyboard.Modifiers&ModifierKeys.Control)!=0) SetScale(Pref.Scale+(e.Delta>0?.05:-.05)); };
        Loaded+=delegate { double savedX=Pref.X,savedY=Pref.Y;SetScale(Pref.Scale);var w=SystemParameters.WorkArea;Left=Double.IsNaN(savedX)||Double.IsInfinity(savedX)||savedX==-99999?w.Right-Width-42:savedX;Top=Double.IsNaN(savedY)||Double.IsInfinity(savedY)||savedY==-99999?w.Bottom-Height-10:savedY;ClampToScreen();initialized=true;Save();if(Timer.Running||FocusPaused)UpdateFocusBadge();else Say(Profile.greeting,9,true);if(SpriteAtlas.LoadWarning.Length>0)Say(SpriteAtlas.LoadWarning,12,true);if(CustomizationProfile.LoadWarning.Length>0)Say(CustomizationProfile.LoadWarning,12,true); };
        LocationChanged+=delegate { if(pocket!=null&&pocket.IsVisible&&!pocket.IsActive) pocket.PlaceNearPet(); };
        Closing+=delegate {scaleSave.Stop();audio.Dispose();StopGuestPlay();Save();frame.Stop();tick.Stop();if(tray!=null){tray.Visible=false;tray.Dispose();}if(trayMenu!=null)trayMenu.Dispose();};
        BuildTray(); frame.Interval=TimeSpan.FromMilliseconds(40);frame.Tick+=Animate;frame.Start();tick.Interval=TimeSpan.FromSeconds(1);tick.Tick+=Tick;tick.Start();SetWater(Pref.WaterMinutes,false);
    }
    double GetDpi() { var p=PresentationSource.FromVisual(this); return p==null?1:p.CompositionTarget.TransformToDevice.M11; }
    public Rect WorkArea() {
        double dpi=GetDpi();var screen=Forms.Screen.FromPoint(new System.Drawing.Point((int)((Left+Width/2)*dpi),(int)((Top+Height/2)*dpi)));
        var r=screen.WorkingArea;return new Rect(r.X/dpi,r.Y/dpi,r.Width/dpi,r.Height/dpi);
    }
    public void ClampToScreen() {FitToArea(WorkArea());}
    Button AddTool(string glyph,string tip,Action action) {var b=UI.Button(glyph,delegate{gestures.CancelPendingTap();action();},false);b.Content=ActionIcon(glyph);b.Tag=glyph;b.Width=34;b.Height=30;b.Padding=new Thickness(0);b.Margin=new Thickness(1,0,1,0);b.FontSize=12;b.ToolTip=tip;ToolTipService.SetInitialShowDelay(b,250);System.Windows.Automation.AutomationProperties.SetName(b,tip);toolbar.Children.Add(b);return b;}
    void UpdateToolbar(double now){bool visible=toolbarVisibility.Visible(now,toolbar.IsMouseOver,pressing);toolbar.Opacity=visible?1:0;toolbar.IsHitTestVisible=visible;}
    internal void RevealQuickControls(){toolbarVisibility.Enter();UpdateToolbar(clock.Elapsed.TotalSeconds);}
    
    public void Say(string text,int seconds,bool important) {if((Pref.Quiet||Timer.Running)&&!important)return;if(focusBadge!=null)focusBadge.Visibility=Visibility.Collapsed;bubbleText.Text=text;bubbleUntil=DateTime.UtcNow.AddSeconds(seconds);bubble.Visibility=Visibility.Visible;}
    void WakeForInteraction() {lastInteraction=DateTime.UtcNow;if(autoSleeping){Companion.Sleeping=false;autoSleeping=false;} }
    void PostponeAmbient(){nextExpression=clock.Elapsed.TotalSeconds+random.Next(35,71);}
    void HandleGesture(PetGesture gesture){
        if(gesture.Kind==PetGestureKind.None)return;
        if(gesture.Kind==PetGestureKind.DoubleTap){Play();return;}
        if(gesture.Buddy){HeadNap();return;}
        if(gesture.Kind==PetGestureKind.Hold){PlayAnimation("waiting","爪爪给你，我在这儿呢。");RespondAsCat("pet");return;}
        Companion.Stroke();string line;string animation=reactions.NextTap(out line);PlayAnimation(animation,line);
        if(animation=="waving" && !art.HumanForm && !art.UsingCustomSkin){art.BeginCatCall();CueAfter("cat-answer",.20);}
        Save();RefreshPocket();
    }
    public void QuickRun(){var area=WorkArea();PlayAnimation(Left+Width/2>area.Left+area.Width/2?"running-left":"running-right","跑几步，再回来陪你。");}
    public void Stroke() {WakeForInteraction();string line=Companion.Stroke();reactUntil=clock.Elapsed.TotalSeconds+2.8;if(!Companion.Sleeping){PlayAnimation("waving",line);RespondAsCat("pet");}else Say(line,4,false);Save();RefreshPocket();}
    public void Feed() {WakeForInteraction();int old=Companion.Snacks;string line=Companion.Feed();if(Companion.Snacks>old&&!Companion.Sleeping){PlayAnimation("waiting",line);art.ServeSnack();CueAfter("snack",art.HumanForm?1:.95);}else Say(line,4,false);Save();RefreshPocket();}
    public void Play() {WakeForInteraction();string line=Companion.Play();PlayAnimation("jumping",line);RefreshPocket();}
    public void Sleep() {ClearActionSound();art.CancelActing();StopGuestPlay();art.CancelTail();art.ClearSnack();gestures.CancelPendingTap();autoSleeping=false;WakeForInteraction();PostponeAmbient();Companion.ToggleSleep();UpdateAudioSettings();animationUntil=0;Say(Companion.Sleeping?"安静待一会儿，动作放慢。":"休息好了，继续陪你。",4,false);UpdateToolbar(clock.Elapsed.TotalSeconds);RefreshPocket();}
    public void Heyxiu(){WakeForInteraction();Pref.ShowHeyxiu=true;heyxiuUntil=clock.Elapsed.TotalSeconds+4;PlayAnimation("waving","小黑和嘿咻一起，嗨！");Save();}
    public void PlayAnimation(string animation,string message){
        StartAnimation(animation,message,false);
    }
    void StartAnimation(string animation,string message,bool automatic){
        animation=SpriteTimeline.Normalize(animation);if(SpriteTimeline.Row(animation)<0)throw new ArgumentException("不支持的动画。");gestures.CancelPendingTap();if(!automatic){WakeForInteraction();Companion.Sleeping=false;autoSleeping=false;}PostponeAmbient();
        if(!automatic){ClearActionSound();art.CancelActing();art.CancelHeadNap();art.ClearSnack();}requestedAnimation=animation;double duration=SpriteTimeline.DurationMs(animation)/1000.0/Profile.animationSpeed;
        animationUntil=clock.Elapsed.TotalSeconds+(automatic?duration:animation=="running-right"||animation=="running-left"?2.6:animation=="running"||animation=="waiting"||animation=="review"?4:animation=="idle"?1.2:duration*(animation=="waving"?2:1));
        movementDirection=animation=="running-left"?-1:1;stateStarted=clock.Elapsed.TotalSeconds;ActiveAnimation=animation;art.Animation=animation;art.AnimationElapsed=0;art.TrackGaze=false;art.InvalidateVisual();
        if(!String.IsNullOrEmpty(message))Say(message,4,false);UpdateToolbar(clock.Elapsed.TotalSeconds);RefreshPocket();
    }
    public void RunCustom(CustomAction action){if(action==null) return;PlayAnimation(action.animation,action.message);}
    public bool ApplyProfile(CustomizationProfile next){
        next=CustomizationProfile.Parse(next.Json());
        if(!Store.Write("profile.json",next.Json()))return false;
        Profile=next;Pref.Quiet=next.quiet;Companion.Quiet=next.quiet;Pref.Roaming=next.roaming;Companion.Roaming=next.roaming;Pref.AutoExpressions=next.autoExpressions;
        SetScale(next.scale);if(!Timer.Running&&!FocusPaused)Timer.Reset(next.focusMinutes);SetWater(next.waterMinutes,false);Title=next.name+" · 桌面伙伴";RefreshPocket();return true;
    }
    public void ImportProfile(){var dialog=new Microsoft.Win32.OpenFileDialog{Title="导入个性化配置",Filter="JSON 配置 (*.json)|*.json",CheckFileExists=true};if(dialog.ShowDialog()!=true)return;try{if(new FileInfo(dialog.FileName).Length>65536)throw new ArgumentException("配置文件不能超过64 KiB。");var profile=CustomizationProfile.Parse(File.ReadAllText(dialog.FileName,Encoding.UTF8));if(!ApplyProfile(profile))throw new IOException(Store.LastError);Say("个性化配置已载入。",5,true);}catch(Exception e){MessageBox.Show(pocket,e.Message,"配置没有导入");}}
    public void ExportProfile(){var dialog=new Microsoft.Win32.SaveFileDialog{Title="导出个性化配置",Filter="JSON 配置 (*.json)|*.json",FileName="my-pet-profile.json"};if(dialog.ShowDialog()!=true)return;try{File.WriteAllText(dialog.FileName,Profile.Json(),new UTF8Encoding(false));Say("配置已导出，可以分享给朋友。",5,true);}catch(Exception e){MessageBox.Show(pocket,e.Message,"无法导出");}}
    public void ImportSkin(){var dialog=new Microsoft.Win32.OpenFileDialog{Title="导入原生v2透明PNG图集",Filter="透明 PNG 图集 (*.png)|*.png",CheckFileExists=true};if(dialog.ShowDialog()!=true)return;try{SpriteAtlas.Import(dialog.FileName);ResetFormAfterSkinSelection();art.InvalidateVisual();RefreshPocket();Say("新图集已载入，动作和设置继续保留。",5,true);}catch(Exception e){MessageBox.Show(pocket,e.Message,"图集没有导入");}}
    public void Express(PetExpression expression,bool speak){
        CurrentExpression=expression;expressionStart=clock.Elapsed.TotalSeconds;expressionUntil=expressionStart+4;nextExpression=expressionUntil+random.Next(45,90);
        string state=expression==PetExpression.Cheer||expression==PetExpression.Startled?"jumping":expression==PetExpression.Grumpy?"failed":expression==PetExpression.Confused?"waiting":expression==PetExpression.Unimpressed||expression==PetExpression.Roll?"review":"waving";
        PlayAnimation(state,speak?ExpressionName(expression):null);
    }
    public void ToggleRoam() {
        Pref.Roaming=!Pref.Roaming;
        Profile.roaming=Companion.Roaming=Pref.Roaming;
        roamTime=0;
        if(Pref.Roaming){
            WakeForInteraction();
            if(pocket!=null && pocket.IsVisible && !Timer.Running && !Companion.Sleeping){
                pocket.FlushNote();
                if(!pocket.HasUnsavedNote)pocket.Hide();
            }
        }
        Save();RefreshPocket();
    }
    public void OpenPocket() {gestures.CancelPendingTap();if(pocket==null){pocket=new PocketWindow(this);pocket.Closed+=delegate{pocket=null;};}if(!IsVisible)Show();pocket.Show();pocket.WindowState=WindowState.Normal;pocket.PlaceNearPet();pocket.Activate();}
    public void RefreshPocket(){if(pocket!=null)pocket.RefreshLive();}
    public void Save(){if(!initialized)return;var size=FormWindowSize(art.HumanForm);Pref.X=Left+(Width-size.Width)/2;Pref.Y=PawBaseline-size.Height;Pref.Pets=Companion.Pets;Pref.Snacks=Companion.Snacks;Pref.Affection=Companion.Affection;Store.Write("settings.json",new JavaScriptSerializer().Serialize(Pref));Store.Write("profile.json",Profile.Json());}
    public void SetWater(int minutes,bool announce){Pref.WaterMinutes=minutes;Profile.waterMinutes=minutes;nextWater=minutes>0?DateTime.UtcNow.AddMinutes(minutes):DateTime.MaxValue;Save();if(announce)Say(minutes>0?"每 "+minutes+" 分钟，轻轻提醒你喝水。":"喝水提醒已关闭。",5,true);}
    public void ToggleVisible(){gestures.CancelPendingTap();if(IsVisible){ClearActionSound();art.CancelActing();StopGuestPlay();Hide();}else{Show();ClampToScreen();Say("我在这里。",3,false);}}
    public void Quit(){if(pocket!=null){pocket.FlushNote();if(pocket.HasUnsavedNote){OpenPocket();MessageBox.Show(pocket,"随手记还未保存成功，请先复制备份。\n"+Store.LastError,"随手记需要你留意一下");return;}}Save();Application.Current.Shutdown();}
    void Tick(object sender,EventArgs e){
        var now=DateTime.UtcNow;Companion.Tick(now);UpdateAudioSettings();
        if(initialized&&Pref.DockToTaskbar&&!pressing&&!dragging&&IsVisible)ClampToScreen();
        if(showSignal.WaitOne(0)){Show();ClampToScreen();OpenPocket();}
        if((actionSignal!=null&&actionSignal.WaitOne(0))||ActionCommand.HasPending)ProcessPendingCommands();
        if(now>bubbleUntil)bubble.Visibility=Visibility.Collapsed;
        if(Timer.Tick(now))FinishFocus();UpdateFocusBadge();
        if(now>=nextWater&&!Timer.Running){Notify("喝一口水吧","小黑陪你歇一小会儿。");nextWater=now.AddMinutes(Pref.WaterMinutes);}
        if(now>=nextAmbient){if(!Companion.Sleeping&&!Timer.Running&&!hovered)Say(new[]{"不用赶，我陪着你。","做自己的事就好，我在呢。","伸个懒腰，再继续吧。"}[random.Next(3)],5,false);nextAmbient=now.AddMinutes(random.Next(18,29));}
        if(!Companion.Sleeping&&!Timer.Running&&!pressing&&now-lastInteraction>TimeSpan.FromMinutes(6)){Companion.Sleeping=true;autoSleeping=true;}
        RefreshPocket();
    }
    public int ProcessPendingCommands(){return ActionCommand.DispatchPending(delegate(ActionCommand request){if(!IsVisible)Show();PlayAnimation(request.animation,request.message);});}
    void Notify(string title,string body){if(!IsVisible)Show();ClampToScreen();Say(title+"\n"+body,14,true);tray.BalloonTipTitle=title;tray.BalloonTipText=body;tray.ShowBalloonTip(6000);}
    void Animate(object sender,EventArgs e){
        double t=clock.Elapsed.TotalSeconds,dt=Math.Min(.12,t-lastFrame);lastFrame=t;
        HandleGesture(gestures.Tick(t));UpdateToolbar(t);
        if(!IsVisible&&(pocket==null||!pocket.IsVisible))return;
        UpdateAudioSettings();UpdateCarryMotion(t);art.Clock=t;art.Sleeping=Companion.Sleeping;art.Hovered=hovered;
        UpdateSoundCue();if(tallLayout&&!art.HumanForm&&!art.MasterActive&&!art.IsChangingForm)UpdateFormLayout(false);
        if(t>expressionUntil)CurrentExpression=PetExpression.Idle;
        if(t>nextExpression&&CompanionReactions.CanAutoReact(Pref.AutoExpressions,Pref.Quiet,Companion.Sleeping,Timer.Running,hovered,pressing,t<animationUntil||art.HeadNapActive||art.Performing,pocket!=null&&pocket.IsVisible)){StartAnimation(reactions.NextAmbient(random),null,true);}
        art.Expression=CurrentExpression;art.ExpressionAge=t-expressionStart;art.ShowHeyxiu=Pref.ShowHeyxiu;art.HeyxiuReaction=Math.Max(0,(heyxiuUntil-t)/4);
        art.Reaction=Math.Max(0,Math.Min(1,(reactUntil-t)/2.8));art.Eating=false;art.Playing=false;
        if(t>nextBlink){blinkStart=t;nextBlink=t+3+random.NextDouble()*5;}
        double b=t-blinkStart;art.Blink=b<.24?Math.Sin(b/.24*Math.PI):0;
        NativePoint pos;if(GetCursorPos(out pos)){
            Point local=PointFromScreen(new Point(pos.X,pos.Y));double gx=Math.Max(-1,Math.Min(1,(local.X/Width-.5)*1.5)),gy=Math.Max(-1,Math.Min(1,(local.Y/Height-.48)*1.5));
            art.GazeX+=(gx-art.GazeX)*.13;art.GazeY+=(gy-art.GazeY)*.13;
        }
        bool explicitMove=!pressing&&t<animationUntil&&(requestedAnimation=="running-right"||requestedAnimation=="running-left");
        bool canRoam=Pref.Roaming && t>=animationUntil && !art.HeadNapActive && !art.Performing && !Companion.Sleeping && !Timer.Running &&
            !hovered && !pressing && visitor==null && !art.TailActive && !art.IsChangingForm && (pocket==null || !pocket.IsVisible);
        bool walk=canRoam && roamTime<7;
        if(canRoam)roamTime=(roamTime+dt)%24;
        string next=t<animationUntil?requestedAnimation:Timer.Running&&!Companion.Sleeping?"running":"idle";
        if(explicitMove||walk){var area=WorkArea();if(walk&&!explicitMove){if(roamDestination<area.Left+10||roamDestination>area.Right-Width-10||Math.Abs(Left-roamDestination)<4)roamDestination=area.Left+12+random.NextDouble()*Math.Max(0,area.Width-Width-24);movementDirection=Math.Sign(roamDestination-Left);if(movementDirection==0)movementDirection=1;}
            if(Left<=area.Left+1&&movementDirection<0)movementDirection=1;if(Left>=area.Right-Width-1&&movementDirection>0)movementDirection=-1;
            Left=Math.Max(area.Left,Math.Min(area.Right-Width,Left+movementDirection*62*dt*Profile.animationSpeed));next=movementDirection<0?"running-left":"running-right";if(explicitMove)requestedAnimation=next;
        }
        if(dragging)next="waiting";
        if(next!=ActiveAnimation){ActiveAnimation=next;stateStarted=t;}
        art.Walking=explicitMove||walk||dragging;art.Animation=ActiveAnimation;art.AnimationElapsed=t-stateStarted;art.AnimationSpeed=Profile.animationSpeed*(Companion.Sleeping?.5:1);art.TrackGaze=ActiveAnimation=="idle"&&hovered&&!pressing&&!Companion.Sleeping;art.InvalidateVisual();
        if(pocket!=null)pocket.RefreshAnimation(art);
    }
    ContextMenu BuildContext(){
        var m=new ContextMenu{FontFamily=FontFamily,FontSize=14,Background=PetArt.B("#FFFDF7"),Foreground=PetArt.B("#344C3C")};
        Action<string,Action> add=(label,act)=>{var i=new MenuItem{Header=label,Padding=new Thickness(10,6,10,6)};i.Click+=delegate{act();};m.Items.Add(i);};
        add("找师父练习",VisitMaster);add("变身",ToggleForm);add("尾巴分身",SplitTail);add("皇受",VisitTabby);add("比丢",VisitBidiu);m.Items.Add(new Separator());
        var size=new MenuItem{Header="桌宠大小"};size.Items.Add(new MenuItem{Header=SizeChooser(),StaysOpenOnClick=true});m.Items.Add(size);
        var dock=new MenuItem{Header="贴住任务栏上沿",IsCheckable=true,IsChecked=Pref.DockToTaskbar};dock.Click+=delegate{SetDocked(dock.IsChecked);};m.Items.Add(dock);
        add("打开小黑的口袋",OpenPocket);add("专注与天气",OpenTools);m.Items.Add(new Separator());
        add("喂小鱼干",Feed);add("跑几步",QuickRun);add("嘿咻上头打盹",HeadNap);add("嘿咻捉迷藏",FindHeyxiu);
        add(Companion.Sleeping?"结束休息":"安静休息",Sleep);add(Pref.Roaming?"停下散步":"在桌面散步",ToggleRoam);m.Items.Add(new Separator());add("暂时藏起来（托盘找我）",ToggleVisible);add("退出小黑",Quit);
        m.PlacementTarget=art;return m;
    }
    public static string ExpressionName(PetExpression expression){switch(expression){case PetExpression.Confused:return "等你回应";case PetExpression.Unimpressed:return "认真检查";case PetExpression.Startled:return "开心小跳";case PetExpression.Grumpy:return "委屈一下";case PetExpression.Cheer:return "开心小跳";case PetExpression.Roll:return "认真检查";case PetExpression.Yawn:return "轻轻招手";default:return "招招手";}}
    void BuildTray(){
        tray=new Forms.NotifyIcon();tray.Text="小黑桌面伙伴 · 双击打开口袋";
        try{tray.Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);}catch{tray.Icon=System.Drawing.SystemIcons.Application;}
        trayMenu=new Forms.ContextMenuStrip();trayMenu.Items.Add("打开小口袋",null,delegate{Dispatcher.Invoke(new Action(OpenPocket));});trayMenu.Items.Add("显示 / 隐藏小黑",null,delegate{Dispatcher.Invoke(new Action(ToggleVisible));});trayMenu.Items.Add("回到屏幕右下角",null,delegate{Dispatcher.Invoke(new Action(delegate{Show();var a=SystemParameters.WorkArea;Left=a.Right-Width-32;Top=a.Bottom-Height;SetDocked(true);}));});trayMenu.Items.Add(new Forms.ToolStripSeparator());trayMenu.Items.Add("退出",null,delegate{Dispatcher.Invoke(new Action(Quit));});
        tray.ContextMenuStrip=trayMenu;tray.DoubleClick+=delegate{Dispatcher.Invoke(new Action(OpenPocket));};tray.BalloonTipClicked+=delegate{Dispatcher.Invoke(new Action(OpenPocket));};tray.Visible=true;
    }
}
public static class UI {
    public static readonly Brush Ink=PetArt.B("#263E32"),Muted=PetArt.B("#788577"),Green=PetArt.B("#486C4B"),Line=PetArt.B("#E2E8DB");
    public static TextBlock Text(string text,double size,Brush color){return new TextBlock {Text=text,FontSize=size,Foreground=color,TextWrapping=TextWrapping.Wrap};}
    public static Button Button(string label,Action action,bool primary){
        var b=new Button{Content=label,FontSize=13,Padding=new Thickness(14,10,14,10),Cursor=Cursors.Hand,Background=primary?Green:PetArt.B("#EFF3E8"),Foreground=primary?System.Windows.Media.Brushes.White:Ink,BorderThickness=new Thickness(0),HorizontalContentAlignment=HorizontalAlignment.Center};
        var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(11));border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));
        var cp=new FrameworkElementFactory(typeof(ContentPresenter));cp.SetValue(FrameworkElement.MarginProperty,new TemplateBindingExtension(Control.PaddingProperty));cp.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);cp.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(cp);
        var temp=new ControlTemplate(typeof(Button)){VisualTree=border};var hover=new Trigger{Property=UIElement.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(UIElement.OpacityProperty,.78));temp.Triggers.Add(hover);var disabled=new Trigger{Property=UIElement.IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(UIElement.OpacityProperty,.4));temp.Triggers.Add(disabled);b.Template=temp;
        b.Click+=delegate{action();};return b;
    }
    public static Border Card(UIElement content){return new Border{Background=System.Windows.Media.Brushes.White,BorderBrush=Line,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(17),Padding=new Thickness(18),Margin=new Thickness(0,0,0,12),Child=content};}
    public static StackPanel Stack(){return new StackPanel();}
    public static TextBlock Heading(string text){var t=Text(text,15,Ink);t.FontWeight=FontWeights.SemiBold;t.Margin=new Thickness(0,0,0,11);return t;}
    public static CheckBox Check(string text,bool value,Action<bool> changed){var c=new CheckBox{Content=text,IsChecked=value,FontSize=14,Foreground=Ink,Margin=new Thickness(0,6,0,8),VerticalContentAlignment=VerticalAlignment.Center};c.Checked+=delegate{changed(true);};c.Unchecked+=delegate{changed(false);};return c;}
}
public sealed partial class PocketWindow:Window {
    public bool HasUnsavedNote { get { return noteDirty; } }
    readonly PetWindow pet; readonly StackPanel page=new StackPanel();TextBlock status,stats,focusText;PetArt livePreview;
    CheckBox roamingControl;
    TextBlock noteStatus;TextBox note;bool noteDirty;DispatcherTimer noteSave;Button focusButton;string pendingNote;
    public PocketWindow(PetWindow owner){
        pet=owner;Title="小黑的口袋 · 动作与个性化";Width=500;Height=760;MinWidth=460;MinHeight=620;ResizeMode=ResizeMode.CanResize;WindowStyle=WindowStyle.SingleBorderWindow;Background=PetArt.B("#F7F8F1");FontFamily=new FontFamily("Microsoft YaHei UI");Foreground=UI.Ink;Topmost=pet.Pref.Topmost;ShowInTaskbar=true;
        var dock=new DockPanel{Margin=new Thickness(24,19,24,10)};Content=dock;
        var head=new StackPanel();DockPanel.SetDock(head,Dock.Top);dock.Children.Add(head);
        var over=UI.Text("小黑和嘿咻 · 一起陪着你",11,UI.Muted);over.Margin=new Thickness(0,0,0,4);head.Children.Add(over);
        var title=UI.Text("小黑的口袋",26,UI.Ink);title.FontWeight=FontWeights.SemiBold;head.Children.Add(title);
        var nav=new UniformGrid{Columns=4,Margin=new Thickness(0,18,0,19)};
        foreach(string tab in new[]{"陪伴","小工具","个性化","设置"}){string t=tab;var b=UI.Button(tab,delegate{ShowTab(t);},false);b.Margin=new Thickness(0,0,6,0);b.Padding=new Thickness(9,10,9,10);nav.Children.Add(b);}head.Children.Add(nav);
        var foot=UI.Text("摸一下是喜欢，待在一起也是。",11,UI.Muted);foot.Margin=new Thickness(0,8,0,2);DockPanel.SetDock(foot,Dock.Bottom);dock.Children.Add(foot);
        var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=page};dock.Children.Add(scroll);
        ShowTab("陪伴");Closing+=delegate(object s,System.ComponentModel.CancelEventArgs e){FlushNote();if(noteDirty){e.Cancel=true;MessageBox.Show(this,"随手记未能保存，请先复制备份，再重试。\n"+Store.LastError,"保留未保存的随手记");}};Closed+=delegate{if(noteSave!=null)noteSave.Stop();};
    }
    public void PlaceNearPet(){if(!IsVisible)return;var r=pet.WorkArea();double x=pet.Left-Width-10;if(x<r.Left+12)x=pet.Left+pet.Width+10;Left=Math.Max(r.Left+8,Math.Min(x,r.Right-Width-8));Top=Math.Max(r.Top+8,Math.Min(pet.Top+pet.Height-Height,r.Bottom-Height-8));}
    internal void ShowTab(string tab){FlushNote();if(noteDirty){if(note!=null)pendingNote=note.Text;return;}weatherEpoch++;page.Children.Clear();note=null;focusButton=null;status=null;stats=null;focusText=null;livePreview=null;roamingControl=null;if(tab=="陪伴")CompanionPage();else if(tab=="小工具")ToolsPage();else if(tab=="个性化")PersonalizePage();else SettingsPage();RefreshLive();}
    void CompanionPage(){
        status=UI.Text("",13,UI.Muted);stats=UI.Text("",12,UI.Muted);
        var hero=new Grid();hero.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(150)});hero.ColumnDefinitions.Add(new ColumnDefinition());
        livePreview=new PetArt{Width=144,Height=168,CatFrameHeight=104};livePreview.CopyStateFrom(pet.Art);hero.Children.Add(livePreview);
        var words=UI.Stack();words.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(words,1);hero.Children.Add(words);
        words.Children.Add(UI.Heading(pet.Profile.name));words.Children.Add(status);stats.Margin=new Thickness(0,10,0,0);words.Children.Add(stats);page.Children.Add(UI.Card(hero));
        var visits=new UniformGrid{Columns=2};
        AddAction(visits,"找师父练习","跳进师父手心",pet.VisitMaster);AddAction(visits,"切换形态","猫形 / 人形",pet.ToggleForm);
        AddAction(visits,"尾巴分身","放出或召回嘿咻",pet.SplitTail);
        AddAction(visits,"皇受来串门","和小皇一起玩",pet.VisitTabby);
        AddAction(visits,"比丢来串门","和比丢一起玩",pet.VisitBidiu);
        page.Children.Add(UI.Card(visits));
        var cinemaGrid=new UniformGrid{Columns=3,Margin=new Thickness(0,8,0,0)};
        for(int n=0;n<SpriteTimeline.States.Length;n++){string animation=SpriteTimeline.States[n];var button=UI.Button(SpriteTimeline.Labels[n],delegate{pet.PlayAnimation(animation,null);},false);button.Margin=new Thickness(0,0,6,7);button.Padding=new Thickness(5,10,5,10);cinemaGrid.Children.Add(button);}
        var cinema=new Expander{Header="全部动作",Content=cinemaGrid};
        var actions=UI.Stack();actions.Children.Add(UI.Heading("一起做点小事"));var grid=new UniformGrid{Columns=2};
        AddAction(grid,"摸摸头","用招手回应你的喜欢",pet.Stroke);AddAction(grid,"喂小鱼干","记下一份小小奖励",pet.Feed);AddAction(grid,"一起玩","开心地小跳一下",pet.Play);AddAction(grid,"休息 / 叫醒","放慢动作，安静待着",pet.Sleep);actions.Children.Add(grid);
        roamingControl=UI.Check("让小黑在附近散步",pet.Pref.Roaming,delegate(bool v){if(pet.Pref.Roaming!=v)pet.ToggleRoam();});
        actions.Children.Add(roamingControl);page.Children.Add(UI.Card(actions));
        if(pet.Profile.customActions.Length>0){var custom=UI.Stack();custom.Children.Add(UI.Heading("我的小动作"));var buttons=new UniformGrid{Columns=2};foreach(CustomAction entry in pet.Profile.customActions){CustomAction chosen=entry;var b=UI.Button(entry.label,delegate{pet.RunCustom(chosen);},false);b.Margin=new Thickness(0,0,6,7);buttons.Children.Add(b);}custom.Children.Add(buttons);page.Children.Add(UI.Card(custom));}
        page.Children.Add(UI.Card(cinema));
        var hint=UI.Text("靠近小黑就能变身、分身或叫伙伴。双击小跳，拖动搬家。",12,UI.Muted);page.Children.Add(hint);
    }
    public void RefreshAnimation(PetArt source){if(livePreview!=null)livePreview.CopyStateFrom(source);}
    void AddAction(Panel host,string title,string subtitle,Action action){var words=UI.Stack();var label=UI.Text(title,14,UI.Ink);label.FontWeight=FontWeights.SemiBold;words.Children.Add(label);var small=UI.Text(subtitle,10,UI.Muted);small.Margin=new Thickness(0,5,0,0);words.Children.Add(small);var b=UI.Button(title,action,false);b.Content=words;b.Margin=new Thickness(0,0,7,8);b.HorizontalContentAlignment=HorizontalAlignment.Left;System.Windows.Automation.AutomationProperties.SetName(b,title);host.Children.Add(b);}
    void ToolsPage(){
        page.Children.Add(MakeFocusCard());page.Children.Add(MakeWeatherCard());
        var water=UI.Stack();water.Children.Add(UI.Heading("喝水小提醒"));var intervals=new System.Collections.Generic.List<int>(new[]{0,30,45,60});if(!intervals.Contains(pet.Pref.WaterMinutes))intervals.Add(pet.Pref.WaterMinutes);var combo=new ComboBox{FontSize=13,Padding=new Thickness(8)};foreach(int minutes in intervals)combo.Items.Add(minutes==0?"关闭，想喝就喝":"每 "+minutes+" 分钟");combo.SelectedIndex=intervals.IndexOf(pet.Pref.WaterMinutes);combo.SelectionChanged+=delegate{if(combo.SelectedIndex>=0)pet.SetWater(intervals[combo.SelectedIndex],true);};water.Children.Add(combo);page.Children.Add(UI.Card(water));
        var notes=UI.Stack();notes.Children.Add(UI.Heading("随手记"));note=new TextBox{Text=pendingNote??Store.ReadNote(),AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=90,MaxHeight=180,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,FontSize=13,Padding=new Thickness(10),BorderBrush=UI.Line,Background=PetArt.B("#FCFDF9"),Foreground=UI.Ink};notes.Children.Add(note);
        noteStatus=UI.Text("自动保存在这台电脑",10,UI.Muted);noteStatus.Margin=new Thickness(0,7,0,0);notes.Children.Add(noteStatus);
        if(noteSave!=null)noteSave.Stop();noteSave=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(700)};noteSave.Tick+=delegate{noteSave.Stop();FlushNote();};
        note.TextChanged+=delegate{noteDirty=true;noteStatus.Text="正在记下来…";noteSave.Stop();noteSave.Start();};page.Children.Add(UI.Card(notes));
    }
    public void FlushNote(){if(!noteDirty||note==null)return;bool ok=Store.Write("note.txt",note.Text);if(ok){noteDirty=false;pendingNote=null;}else pendingNote=note.Text;if(noteStatus!=null){noteStatus.Text=ok?"已保存在本机 · "+DateTime.Now.ToString("HH:mm"):"保存失败，请先复制内容备份";noteStatus.Foreground=ok?UI.Muted:PetArt.B("#A55343");}}
    TextBox Field(Panel parent,string label,string text,bool multiline){parent.Children.Add(UI.Text(label,12,UI.Muted));var box=new TextBox{Text=text,FontSize=13,Padding=new Thickness(9),Margin=new Thickness(0,6,0,12),BorderBrush=UI.Line,Background=PetArt.B("#FCFDF9"),Foreground=UI.Ink,AcceptsReturn=multiline,TextWrapping=TextWrapping.Wrap,MinHeight=multiline?68:36};parent.Children.Add(box);return box;}
    void PersonalizePage(){
        var basics=UI.Stack();basics.Children.Add(UI.Heading("按你的方式陪伴"));var name=Field(basics,"名字",pet.Profile.name,false);name.MaxLength=24;var greeting=Field(basics,"启动时想说的话",pet.Profile.greeting,true);greeting.MaxLength=160;
        basics.Children.Add(UI.Text("动作速度 · 0.5 到 2 倍",12,UI.Muted));var speed=new Slider{Minimum=.5,Maximum=2,Value=pet.Profile.animationSpeed,TickFrequency=.1,IsSnapToTickEnabled=true,Margin=new Thickness(0,8,0,15)};basics.Children.Add(speed);
        var focus=Field(basics,"默认专注时长（1–180 分钟）",pet.Profile.focusMinutes.ToString(),false);var water=Field(basics,"喝水提醒间隔（0 关闭，5–240 分钟）",pet.Profile.waterMinutes.ToString(),false);
        var quiet=UI.Check("安静陪伴，减少气泡",pet.Profile.quiet,delegate{});var roam=UI.Check("在附近散步",pet.Profile.roaming,delegate{});var ambient=UI.Check("偶尔做个小动作",pet.Profile.autoExpressions,delegate{});basics.Children.Add(quiet);basics.Children.Add(roam);basics.Children.Add(ambient);page.Children.Add(UI.Card(basics));
        var actions=UI.Stack();actions.Children.Add(UI.Heading("自己的话，自己的小动作"));actions.Children.Add(UI.Text("给按钮取名，选一个现有动画，再写一句话。不会运行脚本或命令。",12,UI.Muted));
        var actionRows=new StackPanel{Margin=new Thickness(0,12,0,0)};actions.Children.Add(actionRows);var readers=new System.Collections.Generic.List<Func<CustomAction>>();
        Action<CustomAction> append=delegate(CustomAction entry){
            if(readers.Count>=12){MessageBox.Show(this,"最多添加12个小动作。","小动作");return;}
            var group=UI.Stack();group.Margin=new Thickness(0,0,0,16);var label=Field(group,"按钮名字",entry.label,false);label.MaxLength=20;
            var animation=new ComboBox{FontSize=13,Padding=new Thickness(7),Margin=new Thickness(0,0,0,10)};foreach(string title in SpriteTimeline.Labels)animation.Items.Add(title);animation.SelectedIndex=Math.Max(0,SpriteTimeline.Row(entry.animation));group.Children.Add(animation);
            var message=Field(group,"气泡文字（可以留空）",entry.message,true);message.MaxLength=160;
            Func<CustomAction> read=delegate{return new CustomAction{label=label.Text,animation=SpriteTimeline.States[Math.Max(0,animation.SelectedIndex)],message=message.Text};};readers.Add(read);
            var remove=UI.Button("删除这个小动作",delegate{readers.Remove(read);actionRows.Children.Remove(group);},false);group.Children.Add(remove);actionRows.Children.Add(group);
        };
        foreach(CustomAction entry in pet.Profile.customActions)append(entry);
        var add=UI.Button("＋ 添加小动作",delegate{append(new CustomAction{label="我的小动作",animation="waving",message="今天也一起加油。"});},false);actions.Children.Add(add);page.Children.Add(UI.Card(actions));
        var save=UI.Button("保存个性化",delegate{
            int focusMinutes,waterMinutes;if(!Int32.TryParse(focus.Text,out focusMinutes)||!Int32.TryParse(water.Text,out waterMinutes)){MessageBox.Show(this,"时长请填写整数分钟。","还没保存");return;}
            var profile=new CustomizationProfile{schemaVersion=1,name=name.Text,greeting=greeting.Text,animationSpeed=speed.Value,scale=pet.Pref.Scale,roaming=roam.IsChecked==true,quiet=quiet.IsChecked==true,autoExpressions=ambient.IsChecked==true,focusMinutes=focusMinutes,waterMinutes=waterMinutes,customActions=readers.Select(read=>read()).ToArray()};
            try{if(!pet.ApplyProfile(profile))throw new IOException(Store.LastError);pet.Say("按你的喜欢，记下来啦。",4,true);}catch(Exception error){MessageBox.Show(this,error.Message,"还没保存");}
        },true);save.Margin=new Thickness(0,0,0,12);page.Children.Add(save);
        var exchange=new UniformGrid{Columns=2};var import=UI.Button("导入配置 JSON",delegate{pet.ImportProfile();ShowTab("个性化");},false);var export=UI.Button("导出当前配置",pet.ExportProfile,false);import.Margin=new Thickness(0,0,6,0);exchange.Children.Add(import);exchange.Children.Add(export);page.Children.Add(exchange);
        var skin=UI.Stack();skin.Margin=new Thickness(0,15,0,0);skin.Children.Add(UI.Heading("换一套自己的形象"));skin.Children.Add(UI.Text("导入 1536 × 2288 的透明 PNG 图集。沿用同一套动作布局，设置和随手记继续保留。",12,UI.Muted));var skinButtons=new UniformGrid{Columns=2,Margin=new Thickness(0,12,0,0)};var load=UI.Button("导入 PNG 图集",pet.ImportSkin,false);load.Margin=new Thickness(0,0,6,0);skinButtons.Children.Add(load);skinButtons.Children.Add(UI.Button("恢复内置形象",delegate{try{SpriteAtlas.Reset();pet.ResetFormAfterSkinSelection();pet.Art.InvalidateVisual();pet.Say("换回小黑和嘿咻。",4,true);}catch(Exception error){MessageBox.Show(this,error.Message,"无法恢复");}},false));skin.Children.Add(skinButtons);page.Children.Add(UI.Card(skin));
    }
    void SettingsPage(){
        page.Children.Add(AudioSettings());
        var display=UI.Stack();display.Children.Add(UI.Heading("舒服地待着"));display.Children.Add(UI.Check("小黑始终显示在其他窗口上方",pet.Pref.Topmost,delegate(bool b){pet.Pref.Topmost=b;pet.Topmost=b;Topmost=b;pet.Save();}));
        display.Children.Add(UI.Check("安静陪伴，减少闲聊气泡",pet.Pref.Quiet,delegate(bool b){pet.Pref.Quiet=b;pet.Profile.quiet=b;pet.Companion.Quiet=b;pet.Save();}));
        display.Children.Add(UI.Text("嘿咻和小黑在同一张动画中，一起陪着你。",12,UI.Muted));display.Children.Add(UI.Check("偶尔自己做个小动作",pet.Pref.AutoExpressions,delegate(bool b){pet.Pref.AutoExpressions=b;pet.Profile.autoExpressions=b;pet.Save();}));
        display.Children.Add(UI.Check("贴住任务栏上沿",pet.Pref.DockToTaskbar,pet.SetDocked));display.Children.Add(pet.SizeChooser());page.Children.Add(UI.Card(display));
        var home=UI.Stack();home.Children.Add(UI.Heading("小黑的住处"));home.Children.Add(UI.Text("关闭口袋，小黑会继续陪着你。\n藏起来后，可以从右下角托盘找回来。\n小黑无需联网，随手记只保存在本机。",12,UI.Muted));var hide=UI.Button("暂时藏起小黑",delegate{pet.ToggleVisible();},false);hide.Margin=new Thickness(0,14,0,8);home.Children.Add(hide);var folder=UI.Button("打开本机数据文件夹",delegate{Directory.CreateDirectory(Store.Dir);Process.Start(new ProcessStartInfo{FileName=Store.Dir,UseShellExecute=true});},false);home.Children.Add(folder);page.Children.Add(UI.Card(home));
        var about=UI.Text("小黑桌面伙伴 3.9.4\n悬停快捷互动 · 原生动作图集 · 可编辑配置与形象\n非官方同人作品 · 不会自动设置开机启动。",11,UI.Muted);about.LineHeight=21;page.Children.Add(about);var quit=UI.Button("收工，退出小黑",pet.Quit,false);quit.Margin=new Thickness(0,17,0,0);page.Children.Add(quit);
    }
    public void RefreshLive(){
        if(roamingControl!=null && roamingControl.IsChecked!=pet.Pref.Roaming)roamingControl.IsChecked=pet.Pref.Roaming;
        if(status!=null)status.Text=pet.Companion.Sleeping?"安静休息，动作放慢。":pet.Timer.Running?"陪你专注，不打扰。":pet.Pref.Roaming?"在附近走走，也等你摸摸头。":"在这里，陪你做自己的事。";if(stats!=null)stats.Text="摸摸头 "+pet.Companion.Pets+" 次  ·  小鱼干 "+pet.Companion.Snacks+" 条";UpdateFocusControls();}
}
}
