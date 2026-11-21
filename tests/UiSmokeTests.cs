using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using XiaoheiDesktop;
public static class UiSmokeTests {
 static int count;
 static void Check(bool b,string m){count++;if(!b)throw new Exception(m);}
 static void Pump(){var f=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate{f.Continue=false;}));Dispatcher.PushFrame(f);}
 static object Get(PetWindow pet,string name){return typeof(PetWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pet);}
 static void Set(PetWindow pet,string name,object value){typeof(PetWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,value);}
 static PocketWindow LocalTestPocket(PetWindow pet) {
  var result = new PocketWindow(pet);
  Func<System.Threading.Tasks.Task<WeatherArea>> fake = delegate { throw new LocationUnavailable("测试设备未开启定位"); };
  typeof(PocketWindow).GetField("readWeatherArea",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(result,fake);
  return result;
 }
 static void Animate(PetWindow pet){typeof(PetWindow).GetMethod("Animate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,new object[]{null,EventArgs.Empty});}
 static Rect PaintedBounds(PetWindow pet){
  var content=(Grid)pet.Content;
  var overlays=content.Children.Cast<UIElement>().Where(c=>c!=pet.Art).ToDictionary(c=>c,c=>c.Visibility);
  try{
   foreach(var item in overlays)item.Key.Visibility=Visibility.Collapsed;
   pet.Art.InvalidateVisual();pet.UpdateLayout();
   int w=(int)Math.Ceiling(content.ActualWidth),h=(int)Math.Ceiling(content.ActualHeight);
   var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(w,h,96,96,PixelFormats.Pbgra32);
   bitmap.Render(content);byte[] pixels=new byte[w*h*4];bitmap.CopyPixels(pixels,w*4,0);
   int left=w,right=-1,top=h,bottom=-1;
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    if(pixels[4*(y*w+x)+3]<96)continue;
    left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
   }
   if(right<left)throw new Exception("No visible pet pixels in the desktop window");
   return new Rect(left,top,right+1-left,bottom+1-top);
  }finally{foreach(var item in overlays)item.Key.Visibility=item.Value;}
 }
 static void CheckFormScale(PetWindow pet){
  var art=pet.Art;
  foreach(double scale in new[]{.28,.36,1.0}){
   pet.SetScale(scale);pet.UpdateLayout();
   art.Animation="idle";art.AnimationElapsed=0;art.TrackGaze=false;
   var cat=PaintedBounds(pet);
   double baseline=pet.PawBaseline,center=pet.Left+pet.Width/2,catHeight=pet.Height;
   double catBottom=pet.Top+cat.Bottom;
   pet.ToggleForm();Thread.Sleep(720);Animate(pet);
   art.Animation="idle";art.AnimationElapsed=0;
   var human=PaintedBounds(pet);
   Check(human.Height/cat.Height>1.48&&human.Height/cat.Height<1.61,"Visible human is one and a half cats tall at scale "+scale);
   Check(Math.Abs(pet.Top+human.Bottom-catBottom)<=2&&Math.Abs(pet.PawBaseline-baseline)<2,"Visible feet align across forms at scale "+scale);
   Check(Math.Abs(pet.Left+pet.Width/2-center)<2&&Math.Abs(Store.Load().Scale-scale)<.001,"Changing form keeps the center and chosen size at "+scale);
   Point face=new Point(human.X+human.Width*.5,human.Y+human.Height*.18);
   Check(pet.InputHitTest(face) is PetArt,"The enlarged boy's face receives desktop mouse input");
   art.Animation="jumping";art.AnimationElapsed=.39;var jump=PaintedBounds(pet);
   Check(jump.Top>=80&&jump.Bottom<pet.ActualHeight,"A full jump clears the toolbar and fits the actual window");
   art.BeginTail();art.Clock+=2.7;art.Animation="idle";var split=PaintedBounds(pet);
   Check(split.Left>0&&split.Right<pet.ActualWidth&&split.Bottom<pet.ActualHeight,"Human tail companions stay inside the desktop at scale "+scale);
   art.CancelTail();Animate(pet);pet.ToggleForm();
   double savedTop=Store.Load().Y;
   Thread.Sleep(720);Animate(pet);pet.UpdateLayout();
   Check(Math.Abs(pet.Height-catHeight)<1&&Math.Abs(pet.Top-savedTop)<2&&Math.Abs(pet.PawBaseline-baseline)<2,"Returning to cat restores and saves the compact window at scale "+scale);
  }
 }
 static void Gesture(PetWindow pet,PetGestureKind kind){typeof(PetWindow).GetMethod("HandleGesture",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,new object[]{new PetGesture(kind,false)});}
 static void Click(PocketWindow p,string name){var b=Find<Button>(p).First(x=>x.Content is string&&(string)x.Content==name);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));p.UpdateLayout();Pump();}
 static System.Collections.Generic.IEnumerable<T> Find<T>(DependencyObject p) where T:DependencyObject {for(int i=0;i<VisualTreeHelper.GetChildrenCount(p);i++){var c=VisualTreeHelper.GetChild(p,i);if(c is T)yield return (T)c;foreach(var item in Find<T>(c))yield return item;}}
 [STAThread] public static void Main(string[] a){try{Run(a);}catch(Exception error){Console.Error.WriteLine("FAIL: "+error.Message);Console.Error.WriteLine(error.StackTrace);Environment.Exit(1);}}
 static void Run(string[] a){
  Environment.SetEnvironmentVariable("XIAOHEI_DATA_DIR",Path.GetFullPath(a[0]));
  Check(Path.GetFullPath(Store.Dir)==Path.GetFullPath(a[0]),"Tests use isolated data directory");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};bool mutexCreated;var ipcMutex=new Mutex(true,"Local\\XiaoheiCompanionV3-"+ActionCommand.InstanceKey,out mutexCreated);Check(mutexCreated,"IPC test owns an isolated instance namespace");var signal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\XiaoheiCompanionShowV3-"+ActionCommand.InstanceKey);var actionSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\XiaoheiCompanionActionV3-"+ActionCommand.InstanceKey);
  var pet=new PetWindow(signal,actionSignal);pet.Show();Pump();pet.UpdateLayout();
  Check(pet.Pref.SoundEnabled&&Math.Abs(pet.Pref.SoundVolume-.30)<.001,"An existing settings file without sound fields gets gentle default audio");pet.SetSoundEnabled(false);
  Check(pet.IsVisible,"Pet visible");Check(!Double.IsNaN(pet.Left),"Finite location");
  var character=Find<PetArt>(pet).Single();var bounds=character.SpriteBounds();var face=character.TransformToAncestor(pet).Transform(new Point(bounds.Left+bounds.Width*.5,bounds.Top+bounds.Height*.4));var hit=pet.InputHitTest(face);Check(hit is PetArt,"Cat face must receive mouse input at small size");
  pet.SetScale(.36);pet.UpdateLayout();Check(pet.Width==190&&pet.Height<230&&Math.Abs(character.SpriteBounds().Height-320*.36)<1,"Room for quick controls retains the original small sprite height within WPF pixel rounding");
  var toolbar=(StackPanel)Get(pet,"toolbar");var visibility=(QuickToolbarVisibility)Get(pet,"toolbarVisibility");visibility.Enter();Animate(pet);pet.UpdateLayout();var quickButtons=toolbar.Children.Cast<Button>().ToArray();
  Check(quickButtons.Length==5&&quickButtons.All(x=>x.ActualWidth>=28&&x.ActualHeight>=28&&x.FontSize>=12),"Five readable quick actions have at least 28px click targets at default scale");
  Check(quickButtons.Select(x=>(string)x.Tag).SequenceEqual(new[]{"变身","分身","皇受","比丢","工具"}),"The core stories are each a single click from the floating pet");
  foreach(double scale in new[]{.28,.36,1.0}){pet.SetScale(scale);pet.UpdateLayout();bool fits=true;foreach(Button b in quickButtons){Point center=b.TransformToAncestor(pet).Transform(new Point(b.ActualWidth/2,b.ActualHeight/2));if(center.X<0||center.X>pet.ActualWidth||center.Y<0||center.Y>pet.ActualHeight||pet.InputHitTest(center)==null)fits=false;}Check(fits&&toolbar.ActualWidth<=pet.ActualWidth,"All quick buttons remain on screen and hit-testable at scale "+scale);}
  Check(quickButtons.All(x=>x.Content is Canvas)&&toolbar.VerticalAlignment==VerticalAlignment.Top,"The hover strip contains icons above the pet, with no foot labels");
  pet.SetDocked(true);var area=pet.WorkArea();Check(Math.Abs(pet.PawBaseline-area.Bottom)<=2,"Feet dock immediately above the work area edge");
  double anchor=pet.Left+pet.Width/2;pet.SetScale(.58);Check(Math.Abs(pet.PawBaseline-area.Bottom)<=2&&Math.Abs(pet.Left+pet.Width/2-anchor)<2,"Resize holds feet on taskbar and preserves center");
  pet.Top-=65;pet.FinishPetDrag();Check(!pet.Pref.DockToTaskbar,"Dragging away releases the pet");
  pet.Top=area.Bottom-pet.Height-8;pet.FinishPetDrag();Check(pet.Pref.DockToTaskbar,"Dragging near the edge snaps the pet back");
  var ctx=(ContextMenu)typeof(PetWindow).GetMethod("BuildContext",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,null);
  var sizeItem=ctx.Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"桌宠大小"));var box=(StackPanel)((MenuItem)sizeItem.Items[0]).Header;
  var scaleSlider=box.Children.OfType<Slider>().Single();scaleSlider.Value=.4;Check(Math.Abs(pet.Pref.Scale-.4)<.001,"Context slider resizes the actual pet");
  CheckFormScale(pet);
  pet.SetScale(.36);int initialPets=pet.Companion.Pets;Gesture(pet,PetGestureKind.SingleTap);string firstTap=pet.ActiveAnimation;Gesture(pet,PetGestureKind.SingleTap);Check(firstTap!=pet.ActiveAnimation&&pet.Companion.Pets==initialPets+1,"Direct single taps vary their response while preserving existing affection and counter cooldowns");
  Gesture(pet,PetGestureKind.DoubleTap);Check(pet.ActiveAnimation=="jumping","Double click starts the genuine jump without opening a pocket");Gesture(pet,PetGestureKind.Hold);Check(pet.ActiveAnimation=="waiting","A stationary hold gives a gentle waiting response");
  var controller=(PetGestureController)Get(pet,"gestures");controller.Down(0,false,false);controller.Up(.01);pet.PlayAnimation("review",null);Check(controller.Tick(100).Kind==PetGestureKind.None,"Explicit animation calls cancel delayed single taps before they can overwrite the requested state");
  quickButtons[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(pet.Pref.HumanForm&&Store.Load().HumanForm,"One click switches form and stores the choice");
  quickButtons[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(!pet.Pref.HumanForm,"A second click restores the cat");
  quickButtons[3].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Check(pet.OwnedWindows.OfType<GuestPlayWindow>().Any(w=>w.Title.Contains("比丢")),"Bidiu arrives from the floating pet");pet.StopGuestPlay();
  pet.QuickRun();Check(pet.ActiveAnimation=="running-left"||pet.ActiveAnimation=="running-right","Run starts the actual directional animation");
  visibility.Leave(-2);Set(pet,"hovered",false);Set(pet,"animationUntil",-1.0);Set(pet,"nextExpression",-1.0);DateTime lastHuman=DateTime.UtcNow.AddMinutes(-1);Set(pet,"lastInteraction",lastHuman);pet.Pref.AutoExpressions=true;pet.Pref.Quiet=false;Animate(pet);string ambient=pet.ActiveAnimation;
  Check(ambient!="idle"&&(DateTime)Get(pet,"lastInteraction")==lastHuman,"Idle variation starts by itself without falsely resetting the human inactivity clock");
  Check(Math.Abs((double)Get(pet,"animationUntil")-(double)Get(pet,"stateStarted")-SpriteTimeline.DurationMs(ambient)/1000.0/pet.Profile.animationSpeed)<.01,"An automatic response lasts exactly one native cycle before returning to idle");
  Set(pet,"animationUntil",-1.0);Set(pet,"nextExpression",-1.0);Animate(pet);Check(pet.ActiveAnimation!=ambient,"Consecutive automatic responses choose different friendly animations");
  pet.Pref.Quiet=true;Set(pet,"animationUntil",-1.0);Set(pet,"nextExpression",-1.0);Animate(pet);Check(pet.ActiveAnimation=="idle","Quiet mode suppresses automatic reactions");pet.Pref.Quiet=false;
  pet.Timer.Start(DateTime.UtcNow);Animate(pet);Check(pet.ActiveAnimation=="running","A running focus timer automatically uses the genuine working animation");Set(pet,"lastInteraction",DateTime.UtcNow.AddMinutes(-10));typeof(PetWindow).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,new object[]{null,EventArgs.Empty});Check(!pet.Companion.Sleeping,"A focus session cannot fall asleep because of the inactivity timer");pet.Timer.Reset(25);
  pet.Sleep();Set(pet,"nextExpression",-1.0);Animate(pet);Check(pet.Companion.Sleeping&&pet.ActiveAnimation=="idle","Rest overrides ambient actions");
  pet.Sleep();Check(!pet.Companion.Sleeping,"The rest action can wake the pet");
  pet.Companion.Sleeping=true;Set(pet,"autoSleeping",true);Animate(pet);pet.Sleep();Check(!pet.Companion.Sleeping&&!(bool)Get(pet,"autoSleeping"),"The wake action wakes an automatic idle nap on its first click");
  Check(Find<TextBlock>(pet).First().FontSize>=12,"Small pet retains readable bubble text");
  var preview=new PetArt{Width=400,Height=320,Clock=1.5};preview.Measure(new Size(400,320));preview.Arrange(new Rect(0,0,400,320));preview.UpdateLayout();
  var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(400,320,96,96,PixelFormats.Pbgra32);bitmap.Render(preview);byte[] px=new byte[400*320*4];bitmap.CopyPixels(px,400*4,0);
  Check(SpriteAtlas.Current!=null,"Embedded genuine AI sprite atlas loads without external asset files");
  int opaque=0;for(int pixel=3;pixel<px.Length;pixel+=4)if(px[pixel]>250)opaque++;Check(opaque>18000,"Native sprite renders with a substantial intact silhouette");
  var spriteBounds=preview.SpriteBounds();Check(preview.IsHeyxiu(new Point(spriteBounds.Right-15,spriteBounds.Bottom-15))&&!preview.IsHeyxiu(new Point(200,130)),"Sprite buddy retains its own interaction target");
  typeof(PetWindow).GetMethod("HandleGesture",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,new object[]{new PetGesture(PetGestureKind.SingleTap,true)});
  Check(pet.Art.HeadNapActive&&pet.OwnedWindows.OfType<GuestPlayWindow>().Count()==0,"A direct tap on the foot buddy starts the head nap");
  double napStart=pet.Art.Clock;pet.Art.Clock=napStart+2;
  var napBounds=pet.Art.SpriteBounds();
  Check(pet.Art.IsHeyxiu(new Point(napBounds.X+napBounds.Width*.52,napBounds.Y+napBounds.Height*.08))&&!pet.Art.IsHeyxiu(new Point(napBounds.Right-8,napBounds.Bottom-8)),"A perched buddy has moved its tap target away from the foot");
  var napPaint=PaintedBounds(pet);Check(napPaint.Top>79 && napPaint.Bottom<=pet.Height,"The head-riding buddy fits below the toolbar");
  preview.CopyStateFrom(pet.Art);Check(preview.HeadNapActive,"The pocket preview copies the nap phase");
  pet.HeadNap();pet.Art.Clock+=1.5;Check(!pet.Art.HeadNapActive,"A second tap on the perched buddy wakes it and shortens the nap");
  pet.HeadNap();pet.StartFocus(1);Check(!pet.Art.HeadNapActive,"Starting focus cancels a nap before work begins");pet.EndFocus();pet.Timer.Reset(25);
  pet.HeadNap();pet.SplitTail();Check(pet.Art.TailActive&&!pet.Art.HeadNapActive,"Tail splitting replaces the head nap cleanly");pet.Art.CancelTail();
  preview.Clock+=8;Check(!preview.HeadNapActive,"The buddy returns without a second click");
  Animate(pet);
  foreach(string state in SpriteTimeline.States){preview.Animation=state;preview.AnimationElapsed=.4;preview.InvalidateVisual();preview.UpdateLayout();bitmap.Render(preview);}Check(true,"All nine genuine animation states render safely");
  pet.Pref.Roaming=true;typeof(PetWindow).GetField("hovered",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,false);pet.PlayAnimation("waving",null);typeof(PetWindow).GetMethod("Animate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,new object[]{null,EventArgs.Empty});Check(pet.Art.Animation=="waving","Explicit actions take priority over automatic roaming while the pocket is closed");pet.Pref.Roaming=false;
  pet.OpenPocket();
  var roamPocket=(PocketWindow)Get(pet,"pocket");
  Set(pet,"roamTime",15.0);Set(pet,"animationUntil",-1.0);
  pet.ToggleRoam();
  Check(!roamPocket.IsVisible && (double)Get(pet,"roamTime")==0,"Enabling walks closes the saved pocket and starts a fresh walk");
  Set(pet,"hovered",true);Animate(pet);
  Check(!pet.Art.Walking && (double)Get(pet,"roamTime")==0,"Time spent hovering does not consume the first walk");
  Set(pet,"hovered",false);Animate(pet);
  Check(pet.Art.Walking,"Moving the pointer away starts the first walk immediately");
  pet.ToggleRoam();Animate(pet);Check(!pet.Art.Walking,"Turning walks off stops the pet in place");
  pet.OpenPocket();
  Check(Find<CheckBox>(roamPocket).Single(c=>(string)c.Content=="让小黑在附近散步").IsChecked==false,"Stopping from the context menu is reflected in the re-opened pocket");
  roamPocket.Close();
  typeof(PetWindow).GetField("dragging",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,true);typeof(PetWindow).GetField("pressing",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,true);pet.Art.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount){RoutedEvent=System.Windows.Input.Mouse.LostMouseCaptureEvent});Check(!(bool)typeof(PetWindow).GetField("dragging",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pet),"Losing mouse capture clears drag animation instead of leaving the pet running forever");
  var p=LocalTestPocket(pet);p.Show();p.UpdateLayout();Pump();
  var allMoves=Find<Expander>(p).Single();Check(!allMoves.IsExpanded,"Extra animations start folded away");allMoves.IsExpanded=true;p.UpdateLayout();Pump();Click(p,"向右跑");Check(pet.ActiveAnimation=="running-right","Expanded running button selects the real directional atlas state");
  var pocketArt=Find<PetArt>(p).Single();p.RefreshAnimation(pet.Art);Check(pocketArt.Animation==pet.Art.Animation,"Workbench previews the live animation state");
  ActionCommand.FromArguments(new[]{"--action","jumping","--message","外部调用"}).Enqueue();Check(pet.ProcessPendingCommands()==1&&pet.ActiveAnimation=="jumping","A queued external action reaches the running WPF pet");
  var invocation=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=typeof(PetWindow).Assembly.Location,Arguments="--action waving --message IPC-test",UseShellExecute=false,CreateNoWindow=true});Check(invocation.WaitForExit(3000)&&invocation.ExitCode==0,"A second executable invocation attaches to the isolated running instance and exits");invocation.Dispose();Check(actionSignal.WaitOne(500),"The second invocation signals the live named IPC event");Check(pet.ProcessPendingCommands()==1&&pet.ActiveAnimation=="waving","Cross-process CLI invocation changes the existing pet animation");
  for(int n=0;n<33;n++)ActionCommand.FromArguments(new[]{"--action",n==32?"waiting":"waving"}).Enqueue();actionSignal.Set();var timerTick=typeof(PetWindow).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic);timerTick.Invoke(pet,new object[]{null,EventArgs.Empty});Check(ActionCommand.HasPending,"A burst retains the request beyond the bounded 32-command batch");timerTick.Invoke(pet,new object[]{null,EventArgs.Empty});Check(!ActionCommand.HasPending&&pet.ActiveAnimation=="waiting","A subsequent tick drains the rest even when IPC signals coalesce");
  pet.SetWater(12,false);Click(p,"小工具");Check(Find<TextBlock>(p).Any(x=>x.Text=="25:00"),"Tools displayed");Check(Find<ComboBox>(p).Any(x=>x.SelectedItem is string&&(string)x.SelectedItem=="每 12 分钟"),"Imported non-preset water intervals display truthfully rather than showing off");
  Click(p,"45 分钟");Check(pet.Timer.Display=="45:00","Preset actually sets timer");
  var minutesBox=Find<TextBox>(p).Single(x=>System.Windows.Automation.AutomationProperties.GetName(x)=="自定义专注分钟");
  minutesBox.Text="181";Click(p,"开始陪伴");Check(!pet.Timer.Running,"Custom minutes outside the range cannot start focus");
  minutesBox.Text="17";Click(p,"开始陪伴");var focusBadge=(Button)Get(pet,"focusBadge");
  Check(pet.Timer.Running&&pet.Timer.SelectedMinutes==17&&focusBadge.Visibility==Visibility.Visible&&focusBadge.Content.ToString().Contains("17:00"),"A custom session starts immediately and renders on the desktop pet");
  Check(Store.Load().FocusDeadlineTicks==pet.Timer.Deadline.Value.Ticks,"Session deadline is durable at start");
  Click(p,"暂停");Check(pet.FocusPaused&&Store.Load().FocusPausedTicks>0&&focusBadge.Content.ToString().Contains("暂停"),"Pause persists exact remaining time and updates the badge");
  Click(p,"继续陪伴");Check(pet.Timer.Running&&!pet.FocusPaused,"Resume starts the remaining duration");Click(p,"结束本轮");Check(!pet.Timer.Running&&!pet.FocusPaused&&focusBadge.Visibility==Visibility.Collapsed&&Store.Load().FocusDeadlineTicks==0,"Ending clears the floating countdown and saved deadline");
  Click(p,"45 分钟");
  var wx=WeatherLookup.Fetch("北京",delegate(string url){return url.Contains("geocoding-api")?"{\"results\":[{\"name\":\"北京\",\"latitude\":39.9,\"longitude\":116.4,\"country\":\"中国\"}]}":"{\"current\":{\"time\":\"2026-11-20T12:30\",\"temperature_2m\":7.3,\"weather_code\":61}}";},DateTime.UtcNow);
  WeatherLookup.Remember(wx);Check(WeatherLookup.Last().Summary.Contains("小雨")&&wx.Fresh(DateTime.UtcNow)&&!wx.Fresh(DateTime.UtcNow.AddMinutes(21)),"Weather readings decode current fields, persist and expire");
  bool missing=false;try{WeatherLookup.Fetch("不存在",url=>"{}",DateTime.UtcNow);}catch(ArgumentException){missing=true;}Check(missing,"Missing geocoding results never display invented weather");
  var edit=Find<TextBox>(p).Single(x=>x.AcceptsReturn);edit.Text="小黑的自动保存测试\r\n中文与 newline √";p.FlushNote();Check(!p.HasUnsavedNote,"Note save reports success");Check(File.ReadAllText(Path.Combine(Store.Dir,"note.txt")).Contains("中文"),"UTF8 saved");
  Click(p,"设置");
  var soundSlider=Find<Slider>(p).Single(x=>System.Windows.Automation.AutomationProperties.GetName(x)=="音效音量");
  soundSlider.Value=0;pet.Save();Check(pet.Pref.SoundVolume==0&&Store.Load().SoundVolume==0,"Audio volume slider updates and persists");
  var soundCheck=Find<CheckBox>(p).Single(x=>Equals(x.Content,"互动音效"));
  soundCheck.IsChecked=true;Check(pet.Pref.SoundEnabled&&Store.Load().SoundEnabled,"Audio checkbox updates and persists through the normal settings UI");
  pet.Pref.Quiet=true;Animate(pet);Check(((PetAudio)Get(pet,"audio")).Suppressed,"Quiet mode immediately suppresses audio");pet.Pref.Quiet=false;
  pet.StartFocus(1);Check(((PetAudio)Get(pet,"audio")).Suppressed,"Starting focus immediately stops interaction sounds");pet.EndFocus();
  pet.SetSoundEnabled(false);pet.SetSoundVolume(.30);pet.Timer.Reset(45);
  Set(pet,"dragging",true);Animate(pet);Check(pet.ActiveAnimation=="waiting"&&((System.Windows.Media.TransformGroup)pet.Art.RenderTransform).Children.OfType<System.Windows.Media.TranslateTransform>().Single().Y<0,"Dragging has a carried pose with a gentle lift");
  Set(pet,"dragging",false);Animate(pet);Thread.Sleep(85);Animate(pet);Check(((System.Windows.Media.TransformGroup)pet.Art.RenderTransform).Children.OfType<System.Windows.Media.ScaleTransform>().Single().ScaleY<1,"Putting down the pet has a short anchored landing response");
  Thread.Sleep(450);Animate(pet);Click(p,"陪伴");Click(p,"小工具");Check(Find<TextBox>(p).Single(x=>x.AcceptsReturn).Text.Contains("中文"),"Note survives tabs");Click(p,"重置");Check(pet.Timer.Display=="45:00","Reset retains duration across tabs");
  for(int i=0;i<3;i++){Click(p,"设置");Click(p,"陪伴");Click(p,"小工具");}
  Check(p.IsVisible,"Repeated tab rebuilds safe");p.Close();
  pet.PlayAnimation("jumping",null);Check(pet.ActiveAnimation=="jumping","Jump action starts genuine generated jump row");pet.Heyxiu();Check(pet.ActiveAnimation=="waving","Buddy interaction uses a genuine animation");
  p=LocalTestPocket(pet);p.Show();Pump();Click(p,"小工具");Click(p,"重置");Check(pet.Timer.Display=="45:00","Reset retains duration after reopen");p.Close();
  pet.VisitTabby();Pump();
  var visitor=pet.OwnedWindows.OfType<GuestPlayWindow>().Single();
  var visitorButton=Find<Button>(visitor).Single();visitor.UpdateLayout();
  Check(visitor.IsVisible&&visitorButton.ActualWidth>=100&&visitorButton.ActualHeight>=80,"Tabby arrives as a separately clickable character on the desktop");
  visitorButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  Check(pet.ActiveAnimation=="jumping","A leaf toss from the tabby button elicits Xiaohei's jump");
  pet.FindHeyxiu();Pump();
  var hiddenBuddy=pet.OwnedWindows.OfType<GuestPlayWindow>().Single();
  Check(!visitor.IsVisible&&hiddenBuddy.Title.Contains("嘿咻"),"A new game replaces the previous visitor");
  var firstSpot=new Point(hiddenBuddy.Left,hiddenBuddy.Top);
  hiddenBuddy.Interact();
  Check(new Point(hiddenBuddy.Left,hiddenBuddy.Top)!=firstSpot,"Finding the buddy changes its hiding place");
  hiddenBuddy.Interact();
  Check(pet.ActiveAnimation=="review","A rapid repeated click cannot collect two finds");
  Thread.Sleep(510);hiddenBuddy.Interact();Thread.Sleep(510);hiddenBuddy.Interact();
  Check(pet.ActiveAnimation=="jumping","Three distinct finds celebrate the completed hunt");
  pet.StopGuestPlay();Check(!hiddenBuddy.IsVisible,"Closing the game clears its owned window");
  pet.SetDocked(false);pet.SetScale(.8);pet.Left=80;pet.Top=90;pet.Save();pet.Close();Pump();
  pet=new PetWindow(signal);pet.Show();Pump();Check(Math.Abs(pet.Pref.Scale-.8)<.001,"Scale persists");Check(Math.Abs(pet.Left-80)<1&&Math.Abs(pet.Top-90)<1,"Position persists");
  pet.SetScale(.36);pet.Left=100;pet.Top=150;double legacyBaseline=pet.PawBaseline;pet.Save();pet.Close();Pump();
  var legacy=Store.Load();legacy.HumanForm=true;legacy.LayoutVersion=3;
  Store.Write("settings.json",new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(legacy));
  pet=new PetWindow(signal);pet.Show();Pump();pet.UpdateLayout();
  Check(pet.Art.HumanForm&&Math.Abs(pet.PawBaseline-legacyBaseline)<2&&pet.Art.SpriteBounds().Height>166,"An existing free-floating human reopens taller on the saved standing line");
  pet.Close();signal.Dispose();actionSignal.Dispose();ipcMutex.ReleaseMutex();ipcMutex.Dispose();app.Shutdown();Console.WriteLine("PASS: "+count+" WPF integration assertions.");
 }
}
