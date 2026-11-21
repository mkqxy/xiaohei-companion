using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XiaoheiDesktop;
public static class SpriteTests {
 static int count;
 static void Check(bool condition,string message){count++;if(!condition)throw new Exception(message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,message);}
 static byte[] Snapshot(PetArt art) {
    art.InvalidateVisual();art.Measure(new Size(192,208));art.Arrange(new Rect(0,0,192,208));art.UpdateLayout();
    var b=new RenderTargetBitmap(192,208,96,96,PixelFormats.Pbgra32);b.Render(art);
    byte[] pixels=new byte[192*208*4];b.CopyPixels(pixels,192*4,0);return pixels;
 }
 static bool Same(byte[] a,byte[] b){return System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(a,b);}
 static void TestShapeChangesWithCustomSkin() {
    // A magenta square is deliberately unrelated to the tail and feet mask
    // in the default animation. Any reuse of the mask changes rendered pixels.
    const int width=SpriteAtlas.Width,height=SpriteAtlas.Height;
    byte[] data=new byte[width*height*4];
    for(int row=0;row<11;row++)for(int col=0;col<(row<9?SpriteTimeline.Count(SpriteTimeline.States[row]):8);col++){
        for(int y=13;y<201;y++)for(int x=4;x<186;x++){int n=((row*208+y)*width+col*192+x)*4;data[n]=161;data[n+1]=51;data[n+2]=229;data[n+3]=255;}
    }
    string file=Path.Combine(Store.Dir,"sample-pink.png");
    var e=new PngBitmapEncoder();e.Frames.Add(BitmapFrame.Create(BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,data,width*4)));
    using(var f=File.Create(file))e.Save(f);
    byte[] originalFile=File.ReadAllBytes(file);SpriteAtlas.Import(file);
    var art=new PetArt{Width=192,Height=208,Clock=1};byte[] originalPicture=Snapshot(art);
    Check(art.UsingCustomSkin&&!SpriteAtlas.BuiltIn.IsCustomSkin,"Imported and embedded atlases remain independent");
    art.GuestIsHeyxiu=true;Check(Same(originalPicture,Snapshot(art)),"Hide and seek leaves the imported sprite intact");art.GuestIsHeyxiu=false;
    art.SetHumanForm(true,true);art.Clock=2;Check(!Same(originalPicture,Snapshot(art)),"Changing to the boy form has its own live rendering");
    art.SetHumanForm(false,true);art.Clock=3;Check(Same(originalPicture,Snapshot(art)),"Changing back restores every imported sprite pixel");
    art.BeginTail();art.Clock=4.6;Check(art.TailActive&&!Same(originalPicture,Snapshot(art)),"The classic tail scene temporarily borrows Xiaohei");
    art.RecallTail();art.Clock=5.6;Check(!art.TailActive&&Same(originalPicture,Snapshot(art)),"Manual recall restores imported skin");
    art.BeginTail();art.Clock+=7;Check(!art.TailActive&&Same(originalPicture,Snapshot(art)),"Automatic recall restores imported skin");
    Check(Same(originalFile,File.ReadAllBytes(Path.Combine(Store.Dir,"custom-spritesheet.png"))),"Shape changes never modify the user's installed atlas");
    SpriteAtlas.Reset();
 }
 [STAThread] public static void Main(string[] args){
  Check(SpriteTimeline.FrameAt("jumping",.139,1)==0&&SpriteTimeline.FrameAt("jumping",.140,1)==1,"Duration boundary advances exactly to the next native frame");
  Check(SpriteTimeline.FrameAt("jumping",.84,1)==0,"One complete jump returns to its first frame");
  Check(SpriteTimeline.FrameAt("idle",1.0,1)==0&&SpriteTimeline.FrameAt("idle",1.68,1)==1,"Idle retains native calm sixfold timing");
  Check(SpriteTimeline.FrameAt("jumping",.07,2)==1,"Configured speed scales elapsed time without manufacturing frames");
  Check(SpriteTimeline.DurationMs("running-right")==1060&&SpriteTimeline.DurationMs("running-left")==1060,"Both directional loops retain shared native cadence");
  Check(SpriteTimeline.FrameAt("idle",Double.NaN,1)==0&&SpriteTimeline.FrameAt("idle",-3,1)==0,"Invalid clocks start safely at frame zero");
  foreach(string state in SpriteTimeline.States){bool inBounds=true;for(int n=0;n<200;n++)if(SpriteTimeline.FrameAt(state,n*.173,1.7)>=SpriteTimeline.Count(state))inBounds=false;Check(inBounds,"Animation never enters a reserved or unused cell: "+state);}
  Check(SpriteTimeline.LookIndex(0,-1)==0&&SpriteTimeline.LookIndex(1,0)==4&&SpriteTimeline.LookIndex(0,1)==8&&SpriteTimeline.LookIndex(-1,0)==12,"Gaze uses screen-coordinate cardinal order");
  Check(SpriteTimeline.LookIndex(1,-1)==2&&SpriteTimeline.LookIndex(.01,.01)==-1,"Diagonal and neutral gaze use the correct cells");
  var profile=CustomizationProfile.Parse("{\"schemaVersion\":1,\"name\":\"我的小黑\",\"animationSpeed\":3,\"scale\":0.1,\"focusMinutes\":999,\"waterMinutes\":1,\"customActions\":[{\"label\":\"加油\",\"animation\":\"working\",\"message\":\"一起写作\"}]}");
  Check(profile.name=="我的小黑"&&profile.animationSpeed==2&&profile.scale==.28&&profile.focusMinutes==180&&profile.waterMinutes==5,"Imported profile validates and bounds numeric settings");
  Check(profile.customActions[0].animation=="running"&&profile.customActions[0].message=="一起写作","Friendly working alias and custom dialogue survive import");
  var legacy=new CustomizationProfile{greeting=CustomizationProfile.LegacyDefaultGreeting,name="原来的名字",scale=.4,customActions=new[]{new CustomAction{label="保留",animation="waiting",message="自己的话"}}};var migrated=CustomizationProfile.Parse(legacy.Json());
  Check(migrated.greeting==CustomizationProfile.DefaultGreeting&&migrated.name==legacy.name&&migrated.scale==.4&&migrated.customActions[0].message=="自己的话","Only the exact outdated default greeting migrates while the schema and custom settings survive");
  legacy.greeting="我的自写问候：右键或双击。";Check(CustomizationProfile.Parse(legacy.Json()).greeting==legacy.greeting,"A user-written greeting is never replaced by the interaction migration");
  var gestures=new PetGestureController(.5);gestures.Down(0,true,false);gestures.Up(.1);
  Check(gestures.Tick(.599).Kind==PetGestureKind.None,"Single tap waits for the complete system double-click interval");
  var single=gestures.Tick(.6);Check(single.Kind==PetGestureKind.SingleTap&&single.Buddy&&gestures.Tick(1).Kind==PetGestureKind.None,"A released single tap fires exactly once and retains its original buddy target");
  gestures.Down(2,false,false);gestures.Up(2.08);var doubleTap=gestures.Down(2.2,false,true);gestures.Up(2.3);
  Check(doubleTap.Kind==PetGestureKind.DoubleTap&&gestures.Tick(3).Kind==PetGestureKind.None,"Double click fires only jump and cancels the first delayed single tap");
  gestures.Down(4,false,false);Check(!gestures.Move(6)&&gestures.Move(6.01),"Drag threshold is six device-independent pixels");gestures.Up(4.3);
  Check(gestures.Tick(5).Kind==PetGestureKind.None,"Dragging suppresses both hold and delayed click feedback");
  gestures.Down(6,false,false);Check(gestures.Tick(6.649).Kind==PetGestureKind.None&&gestures.Tick(6.65).Kind==PetGestureKind.Hold,"A stationary hold fires at 650ms, independently of double-click timing");
  Check(gestures.Tick(6.9).Kind==PetGestureKind.None&&gestures.Move(12),"A hold fires once and may still become a drag without losing capture");gestures.Up(7);Check(gestures.Tick(8).Kind==PetGestureKind.None,"Releasing a hold cannot also generate a single tap");
  gestures.Down(9,false,false);gestures.Up(9.1);gestures.CancelPendingTap();Check(gestures.Tick(10).Kind==PetGestureKind.None,"An explicit toolbar or CLI action supersedes a pending click");
  gestures.Down(11,false,false);gestures.Cancel();Check(!gestures.Pressed&&gestures.Tick(12).Kind==PetGestureKind.None,"Unexpected capture loss clears a held contact and its delayed effects");
  var visibility=new QuickToolbarVisibility();Check(!visibility.Visible(0,false,false),"Quick toolbar starts hidden");visibility.Enter();Check(visibility.Visible(0,false,false),"Hover reveals the quick toolbar immediately");visibility.Leave(1);
  Check(visibility.Visible(1.799,false,false)&&!visibility.Visible(1.8,false,false),"Toolbar remains clickable through the entire 800ms transparent gap crossing");
  Check(visibility.Visible(3,true,false)&&visibility.Visible(3,false,true),"Toolbar hover and a held press prevent it disappearing before a click");visibility.Enter();Check(visibility.Visible(20,false,false),"Re-entering cancels an old hide deadline");
  var reactions=new CompanionReactions();var tapStates=new System.Collections.Generic.HashSet<string>();string previous="",line;
  for(int n=0;n<12;n++){string choice=reactions.NextTap(out line);Check(choice!=previous&&SpriteTimeline.Row(choice)>=0&&line.Length>0,"Successive taps have varied genuine animation feedback");previous=choice;if(n<4)tapStates.Add(choice);}Check(tapStates.Count==4,"The first four taps expose four distinct friendly responses");
  var variationRandom=new Random(123);bool varied=true;for(int n=0;n<100;n++){string choice=reactions.NextAmbient(variationRandom);if(choice==previous||SpriteTimeline.Row(choice)<0)varied=false;previous=choice;}Check(varied,"Ambient reactions do not repeat the previous tap or ambient animation");
  Check(CompanionReactions.CanAutoReact(true,false,false,false,false,false,false,false),"Ambient reactions are allowed only while freely idle");
  Check(!CompanionReactions.CanAutoReact(false,false,false,false,false,false,false,false)&&!CompanionReactions.CanAutoReact(true,true,false,false,false,false,false,false)&&!CompanionReactions.CanAutoReact(true,false,true,false,false,false,false,false)&&!CompanionReactions.CanAutoReact(true,false,false,true,false,false,false,false),"Ambient reactions respect disabled, quiet, resting and focus preferences");
  Check(!CompanionReactions.CanAutoReact(true,false,false,false,true,false,false,false)&&!CompanionReactions.CanAutoReact(true,false,false,false,false,true,false,false)&&!CompanionReactions.CanAutoReact(true,false,false,false,false,false,true,false)&&!CompanionReactions.CanAutoReact(true,false,false,false,false,false,false,true),"Ambient reactions yield to pointer hover, a press or drag, an explicit action and an open pocket");
  Reject(delegate{CustomizationProfile.Parse("{\"schemaVersion\":2}");},"Unknown schema cannot silently change behavior");
  Reject(delegate{CustomizationProfile.Parse("{\"command\":\"calc.exe\"}");},"Profile cannot smuggle executable commands");
  Reject(delegate{CustomizationProfile.Parse("{\"customActions\":[{\"label\":\"x\",\"animation\":\"waving\",\"script\":\"bad\"}]}");},"Custom actions cannot run scripts");
  Reject(delegate{CustomizationProfile.Parse("{\"customActions\":[{\"animation\":\"not-a-real-row\"}]}");},"Invalid actions cannot reference unused atlas rows");
  var command=ActionCommand.FromArguments(new[]{"--action","waving","--message","写完啦，小黑招手！"});Check(command.animation=="waving"&&command.message.Contains("招手"),"CLI parses exact animation and literal bounded dialogue");
  Reject(delegate{ActionCommand.FromArguments(new[]{"--action","cmd.exe"});},"CLI cannot invoke an executable");
  Reject(delegate{ActionCommand.FromArguments(new[]{"--message","hello"});},"Message-only invocation cannot change state");
  command.Enqueue();int handled=0;Check(ActionCommand.DispatchPending(delegate(ActionCommand received){handled++;Check(received.message==command.message,"Queued UTF8 text is preserved literally");})==1&&handled==1,"Local queue dispatches a valid command once");Check(ActionCommand.DispatchPending(delegate{})==0,"Dispatched commands do not replay");
  Check(SpriteAtlas.Current!=null,"The embedded atlas loads offline");var original=SpriteAtlas.Current;Check(original.Frame(10,7).PixelWidth==192&&original.Frame(10,7).PixelHeight==208,"Look frames use exact native cell dimensions");
  var pixels=new byte[16*16*4];var bitmap=BitmapSource.Create(16,16,96,96,PixelFormats.Bgra32,null,pixels,64);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
  string temp=Path.Combine(Store.Dir,"invalid-test.png");Directory.CreateDirectory(Store.Dir);using(var output=File.Create(temp))encoder.Save(output);
  Reject(delegate{SpriteAtlas.Import(temp);},"Wrong-sized custom artwork is rejected before replacing the current atlas");Check(Object.ReferenceEquals(original,SpriteAtlas.Current),"Failed imports preserve the live original");File.Delete(temp);
  if(args.Length>1){var browserProfile=CustomizationProfile.Parse(File.ReadAllText(args[1]));Check(browserProfile.name=="小黑·测试搭子"&&browserProfile.customActions.Length==3&&browserProfile.customActions[2].animation=="running","The actual browser-exported JSON profile imports into the C# runtime unchanged");}
  TestShapeChangesWithCustomSkin();
  Console.WriteLine("PASS: "+count+" sprite/config assertions.");
 }
}
