using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace XiaoheiDesktop {
public enum PetExpression { Idle, Happy, Confused, Unimpressed, Startled, Grumpy, Roll, Cheer, Yawn }
public sealed partial class PetArt : FrameworkElement {
 public string Animation="idle";
 public double AnimationElapsed,AnimationSpeed=1;
 public bool UseSprite=true,TrackGaze=false;
 public double Clock,GazeX,GazeY,Reaction,Blink,ExpressionAge,HeyxiuReaction;
 public bool Sleeping,Playing,Eating,Hovered,Walking,ShowHeyxiu=true;
 public PetExpression Expression=PetExpression.Idle;
 static readonly Dictionary<string,Geometry> Cache=new Dictionary<string,Geometry>();
 static readonly Dictionary<string,SolidColorBrush> Brushes=new Dictionary<string,SolidColorBrush>();
 const string Fur="#080C0D",Outline="#3B302D",Cream="#FCF7D1",Ear="#ACD58D";
 public static SolidColorBrush B(string c){SolidColorBrush v;if(!Brushes.TryGetValue(c,out v)){v=new SolidColorBrush((Color)ColorConverter.ConvertFromString(c));v.Freeze();Brushes[c]=v;}return v;}
 static Geometry Geo(string s){Geometry g;if(!Cache.TryGetValue(s,out g)){g=Geometry.Parse(s);g.Freeze();Cache[s]=g;}return g;}
 static Pen P(string c,double w){return new Pen(B(c),w){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};}
 static void Path(DrawingContext d,string s,string f,string c,double w){d.DrawGeometry(f==null?null:B(f),c==null?null:P(c,w),Geo(s));}
 static void E(DrawingContext d,double x,double y,double rx,double ry,string f,string c,double w){d.DrawEllipse(f==null?null:B(f),c==null?null:P(c,w),new Point(x,y),rx,ry);}
 static void Text(DrawingContext d,string s,double x,double y,int size,string c){d.DrawText(new FormattedText(s,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),size,B(c),1),new Point(x,y));}
 double ArtScale{get{return Math.Min(ActualWidth/400,ActualHeight/320);}}
 public bool IsHeyxiu(Point p){if(HumanForm||TailActive||GuestIsHeyxiu||UsingCustomSkin)return false;if(UseSprite&&SpriteAtlas.Current!=null){Rect r=SpriteBounds();return p.X>r.Left+r.Width*.70&&p.X<r.Right&&p.Y>r.Top+r.Height*.78&&p.Y<r.Bottom;}double s=ArtScale;double cx=Sleeping?310:262;return ShowHeyxiu&&s>0&&Math.Abs(p.X/s-cx)<27&&p.Y/s>255;}
 public const double HumanHeightFactor=1.50;
 // A fixed viewport (for example an atlas exporter) can still fit one complete pose.
 // The desktop and the pocket supply a common cat height for both forms.
 public double CatFrameHeight {get;set;}
 public Rect SpriteBounds(){
    bool human=PaintedHuman;
    double h=Math.Min(ActualHeight,ActualWidth*208.0/192);
    if(CatFrameHeight>0)h=Math.Min(h,CatFrameHeight*(human?HumanHeightFactor:1));
    double w=h*192/208;
    // In the source cell the paws end at 200 and the soles at 198.
    double footOffset=human&&CatFrameHeight>0?h*(10-8/HumanHeightFactor)/208:0;
    return new Rect((ActualWidth-w)/2,ActualHeight-h+footOffset,w,h);
 }
 private double snackSince = Double.NegativeInfinity;
 public void ServeSnack() { snackSince = Clock; InvalidateVisual(); }
 public void ClearSnack() { snackSince = Double.NegativeInfinity; }
 public void CopyStateFrom(PetArt source){Animation=source.Animation;AnimationElapsed=source.AnimationElapsed;AnimationSpeed=source.AnimationSpeed;GazeX=source.GazeX;GazeY=source.GazeY;TrackGaze=source.TrackGaze;UseSprite=source.UseSprite;Clock=source.Clock;Blink=source.Blink;Expression=source.Expression;ExpressionAge=source.ExpressionAge;Sleeping=source.Sleeping;snackSince=source.snackSince;CopyFormFrom(source);InvalidateVisual();}
 void DrawSprite(DrawingContext d, SpriteAtlas atlas) {
    int row = SpriteTimeline.Row(Animation);
    if (row < 0) row = 0;
    int col = SpriteTimeline.FrameAt(SpriteTimeline.States[row], AnimationElapsed, AnimationSpeed);
    int look = TrackGaze && row == 0 ? SpriteTimeline.LookIndex(GazeX, GazeY) : -1;
    if (look >= 0) { row = 9 + look / 8; col = look % 8; }
    Rect r = SpriteBounds();
    if (r.IsEmpty || r.Width <= 0 || r.Height <= 0) return;
    double snackAge = Clock - snackSince;
    bool hasSnack = !Sleeping && snackAge >= 0 && snackAge < 4;
    if (!Sleeping && !hasSnack) { d.DrawImage(atlas.Frame(row,col),r); return; }
    d.PushTransform(new TranslateTransform(r.X,r.Y));
    d.PushTransform(new ScaleTransform(r.Width/192,r.Height/208));
    if (Sleeping) {
        E(d,96,190,89,15,"#34655C","#35463C",2);
        E(d,96,185,88,13,"#BBD8AF","#436650",2);
        // Use the closed-eye frame and let the whole pose breathe on the cushion.
        double breath = 1.2 * Math.Sin(Clock * 2.2);
        d.DrawImage(atlas.Frame(0,3),new Rect(18,26+breath,156,166-breath));
        d.PushOpacity(.60+.25*Math.Sin(Clock*1.6));
        Text(d,"z",156,39+breath,19,"#ECF5E3");
        Text(d,"z",169,25+breath,13,"#F4FAE9");
        d.Pop();
    } else {
        double nod = Math.Sin(Math.PI * Math.Min(1,snackAge/3.2)) * Math.Pow(Math.Sin(snackAge*4.5),2);
        d.DrawImage(atlas.Frame(row,col),new Rect(0,9*nod,192,195-9*nod));
        d.PushOpacity(Math.Min(1,Math.Min(snackAge/.18,(4-snackAge)/.35)));
        E(d,62,191,33,6,"#4E372925",null,0);
        Path(d,"M28,181 Q32,200 63,200 Q90,200 96,181 Z","#92BFB0","#405951",2);
        E(d,62,181,34,7,"#FFEDC8","#405951",2);
        if (snackAge < 2.9) {
            double nibble = 1 - .23 * Math.Floor(Math.Min(2,snackAge/.95));
            d.PushTransform(new ScaleTransform(nibble,nibble,62,179));
            Path(d,"M43,173 L53,177 Q64,166 80,177 Q67,188 54,181 L43,185 Z","#E6A45D","#6C5038",2);
            E(d,73,177,1.5,1.5,"#514636",null,0);
            d.Pop();
        }
        d.Pop();
    }
    d.Pop(); d.Pop();
 }
 protected override void OnRender(DrawingContext d){
  SpriteAtlas atlas=UseSprite?SpriteAtlas.Current:null;
  if(DrawFormEffects(d,atlas))return;
  if(atlas!=null){DrawSprite(d,atlas);return;}
  double s=ArtScale;if(s<=0)return;d.PushTransform(new ScaleTransform(s,s));double t=Clock,a=ExpressionAge;
  E(d,163,305,83,5,"#18000000",null,0);
  if(Sleeping)DrawSleeping(d,t);else{
   double jump=Expression==PetExpression.Cheer?-Math.Abs(Math.Sin(a*7))*22:Expression==PetExpression.Startled?-Math.Max(0,1-a/.65)*20:Walking?-Math.Abs(Math.Sin(t*9))*3:0;
   d.PushTransform(new TranslateTransform(0,Math.Sin(t*2)*1.2+jump));bool roll=Expression==PetExpression.Roll;
   if(roll){d.PushTransform(new TranslateTransform(0,35));d.PushTransform(new ScaleTransform(.67,.67,163,165));d.PushTransform(new RotateTransform(Math.Sin(a*3.6)*100,163,165));}
   DrawBody(d,t);DrawHead(d,t);if(Playing||Eating)DrawToys(d,t);
   if(roll){d.Pop();d.Pop();d.Pop();}d.Pop();DrawSymbols(d);
  }
  if(ShowHeyxiu)DrawHeyxiu(d,t);d.Pop();
 }
 void DrawBody(DrawingContext d,double t){
  d.PushTransform(new RotateTransform(Math.Sin(t*(Playing?7:2.1))*(Playing?18:7),120,264));
  Path(d,"M 124,260 C 93,275 74,256 67,237 C 61,221 48,225 49,243 C 51,278 78,294 120,284 Z",Fur,Outline,4);d.Pop();
  // The black abdomen continues behind both front legs; contours separate the paws.
  Path(d,"M 136,199 C 126,218 114,245 112,267 C 109,285 113,297 127,296 Q 137,301 144,296 Q 154,303 164,297 Q 176,304 186,297 Q 197,302 208,296 C 222,294 224,281 220,266 C 216,241 202,214 190,199 Z",Fur,Outline,4);
  // Rear paws are subtle side contours rather than two detached round feet.
  Path(d,"M 119,257 C 114,276 118,289 129,294 M 213,257 C 220,276 215,289 204,294",null,Outline,3.6);
  double step=Walking?Math.Sin(t*10)*4:0;d.PushTransform(new TranslateTransform(-1.3,step));
  Path(d,"M 137,239 C 131,258 132,280 142,292 Q 151,302 162,295 C 167,284 162,258 158,244",Fur,Outline,3.6);d.Pop();
  d.PushTransform(new TranslateTransform(1.3,-step));Path(d,"M 190,239 C 195,258 193,281 184,293 Q 175,303 165,295 C 160,284 167,258 172,244",Fur,Outline,3.6);d.Pop();
  if(Playing||Expression==PetExpression.Cheer||Expression==PetExpression.Startled){
   d.PushTransform(new RotateTransform(Math.Sin(t*8)*10,207,235));Path(d,"M 201,248 C 220,243 237,222 234,207 C 231,198 220,200 217,211 L 198,231 Z",Fur,Outline,3.8);d.Pop();
   if(Expression==PetExpression.Cheer||Expression==PetExpression.Startled)Path(d,"M 129,247 C 106,241 95,221 98,207 C 101,198 112,201 115,211 L 134,231 Z",Fur,Outline,3.8);
  }
 }
 void DrawHead(DrawingContext d,double t){
  double tilt=Expression==PetExpression.Confused?16:Expression==PetExpression.Unimpressed?-6:Playing?Math.Sin(t*5)*7:Expression==PetExpression.Grumpy?Math.Sin(t*18)*1.2:Reaction>0?Math.Sin(t*4)*3:GazeX*2;
  d.PushTransform(new ScaleTransform(1.09,1,163,156));
  d.PushTransform(new RotateTransform(tilt,162,156));d.PushTransform(new RotateTransform(Math.Sin(t*2.3)*1.2,90,90));
  Path(d,"M 67,103 C 59,76 60,42 70,23 C 89,22 114,35 128,49 Z",Fur,Outline,4.2);Path(d,"M 71,84 Q 65,53 75,35 Q 95,44 107,59 Z",Ear,null,0);d.Pop();
  d.PushTransform(new RotateTransform(Math.Sin(t*2.3+1)*1.2,237,90));Path(d,"M 199,48 C 216,34 240,22 256,24 C 266,46 270,80 258,107 Z",Fur,Outline,4.2);Path(d,"M 222,59 Q 239,41 251,35 Q 260,57 254,88 Z",Ear,null,0);d.Pop();
  if(Expression==PetExpression.Startled)Path(d,"M 68,132 L 52,142 L 64,148 L 52,163 L 68,168 M 259,132 L 275,142 L 262,151 L 274,165 L 258,170",Fur,Outline,3);
  Path(d,"M 64,120 C 64,72 104,40 162,40 C 222,40 263,72 263,124 C 267,180 237,211 163,212 C 95,213 60,183 64,120 Z",Fur,Outline,4.3);
  bool smile=Expression==PetExpression.Happy||Expression==PetExpression.Cheer||(Reaction>0&&!Eating&&!Playing&&Expression==PetExpression.Idle);
  if(Expression==PetExpression.Grumpy){
   Path(d,"M 73,112 L 149,137 Q 146,180 112,184 Q 76,179 73,112 Z",Cream,Outline,3.4);Path(d,"M 252,112 L 176,137 Q 178,180 213,184 Q 248,179 252,112 Z",Cream,Outline,3.4);E(d,117,155,15,23,Fur,null,0);E(d,208,155,15,23,Fur,null,0);
  }else if(smile||Expression==PetExpression.Yawn||Blink>.72){
   Path(d,"M 78,140 Q 108,117 140,139 M 185,139 Q 214,117 246,139",null,Cream,6);if(smile){E(d,87,160,11,4,"#789773",null,0);E(d,239,160,11,4,"#789773",null,0);}
  }else{Eye(d,112,126);Eye(d,214,126);}
  Path(d,"M 151,174 Q 163,171 175,174 L 164,190 Q 163,192 161,189 Z","#65A9AE",Outline,3);
  if(Expression==PetExpression.Yawn)E(d,164,196,9,12,"#5A9196",Outline,2);else if(Eating)Path(d,"M 153,197 Q 164,207 175,196",null,"#91B780",3);else if(smile)Path(d,"M 153,198 Q 164,207 175,198",null,Outline,2.5);else if(Expression==PetExpression.Startled)E(d,164,198,5,7,"#65A9AE",null,0);d.Pop();d.Pop();
 }
 void Eye(DrawingContext d,double x,double y){
  bool half=Expression==PetExpression.Unimpressed;if(half)d.PushClip(new RectangleGeometry(new Rect(x-49,y+4,98,64)));
  double sy=1-Math.Min(.7,Blink)*.8;d.PushTransform(new ScaleTransform(1,sy,x,y));E(d,x,y,46,61,Cream,"#574237",3.5);
  double px=Expression==PetExpression.Startled?14:27,py=Expression==PetExpression.Startled?23:39;E(d,x+GazeX*8,y+GazeY*7,px,py,Fur,Outline,2.6);d.Pop();
  if(half){d.Pop();d.DrawLine(P(Outline,3),new Point(x-44,y+5),new Point(x+44,y+5));}
 }
 void DrawSymbols(DrawingContext d){
  if(Expression==PetExpression.Confused){Text(d,"?",284,37,37,"#8DB88F");Text(d,"?",309,65,21,"#BDD2A9");}
  if(Expression==PetExpression.Unimpressed)Text(d,"···",282,70,33,"#8D9B89");
  if(Expression==PetExpression.Startled){Text(d,"!!",273,40,29,"#C6A05C");Path(d,"M 42,101 L 24,94 M 42,117 L 23,119 M 278,103 L 296,94",null,"#C6A05C",4);}
  if(Expression==PetExpression.Grumpy)Path(d,"M 278,64 L 278,74 L 268,74 M 285,64 L 285,74 L 295,74 M 268,82 L 278,82 L 278,92 M 285,92 L 285,82 L 295,82",null,"#C9826A",4);
  if(Reaction>0||Expression==PetExpression.Happy||Expression==PetExpression.Cheer){d.PushOpacity(Expression==PetExpression.Idle?Math.Min(1,Reaction*3):1);Heart(d,279,75+Math.Sin(Clock*3)*5,.75,"#A7C993");Heart(d,38,80+Math.Sin(Clock*3+1)*7,.5,"#CFDDB7");d.Pop();}
 }
 static void Heart(DrawingContext d,double x,double y,double scale,string color){d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new ScaleTransform(scale,scale));Path(d,"M 0,0 C -15,-18 -32,4 0,23 C 32,4 15,-18 0,0 Z",color,null,0);d.Pop();d.Pop();}
 void DrawToys(DrawingContext d,double t){
  if(Eating){d.PushTransform(new TranslateTransform(0,Math.Sin(t*5)*3));Path(d,"M 232,211 Q 247,195 264,207 L 278,198 L 276,220 L 264,214 Q 247,226 232,211 Z","#9DC9C6","#487A79",2.4);E(d,242,210,2,2,"#224746",null,0);d.Pop();}
  if(Playing){double xx=55+Math.Sin(t*4)*26;d.DrawLine(P("#9A9078",3),new Point(22,132),new Point(xx,212));E(d,xx,224,9,13,"#DBB680","#A78C65",2);}
 }
 void DrawHeyxiu(DrawingContext d,double t){
  double baseX=Sleeping?310:262;d.PushTransform(new ScaleTransform(.56,.56,baseX,305));
  double hop=HeyxiuReaction>0?Math.Abs(Math.Sin(t*10))*17:t%17<1.5?Math.Abs(Math.Sin(t*6))*5:0;
  double x=baseX-(HeyxiuReaction>0?Math.Sin(Math.Min(1,HeyxiuReaction)*Math.PI)*22:0),y=282-hop;E(d,x,306,23,3,"#16000000",null,0);
  d.PushTransform(new TranslateTransform(x,y));d.PushTransform(new RotateTransform(Math.Sin(t*2)*4));
  Path(d,"M -25,-3 Q -31,-18 -24,-29 Q -13,-29 -6,-22 Q 6,-25 15,-21 Q 24,-29 29,-27 Q 33,-11 26,1 Q 29,22 0,23 Q -27,22 -25,-3 Z",Fur,Outline,2.3);
  Path(d,"M -23,-13 L -23,-24 L -12,-19 Z",Ear,null,0);Path(d,"M 17,-18 L 25,-23 L 26,-12 Z",Ear,null,0);
  if(Sleeping)Path(d,"M -21,2 Q -13,8 -5,1 M 5,1 Q 13,8 21,2",null,Cream,2.6);else{E(d,-13,0,11,15,Cream,Outline,1.2);E(d,13,0,11,15,Cream,Outline,1.2);E(d,-13+GazeX*2,1+GazeY,6,10,Fur,null,0);E(d,13+GazeX*2,1+GazeY,6,10,Fur,null,0);}d.Pop();d.Pop();d.Pop();
 }
 void DrawSleeping(DrawingContext d,double t){
  d.PushTransform(new TranslateTransform(0,Math.Sin(t*1.6)*1.2));d.PushTransform(new ScaleTransform(1,1+Math.Sin(t*1.6)*.007,160,300));
  Path(d,"M 60,270 C 49,219 81,188 125,196 C 158,197 189,225 195,262 C 207,289 181,301 128,299 C 89,302 65,294 60,270 Z",Fur,Outline,4);
  Path(d,"M 92,249 C 64,249 57,275 81,290 C 102,305 146,301 158,287 C 166,276 157,267 145,271 C 127,278 99,280 92,265",null,Outline,4);
  Path(d,"M 148,230 C 134,210 133,188 138,174 C 157,174 172,184 182,198 Z",Fur,Outline,3.8);Path(d,"M 143,209 L 142,185 Q 158,190 166,202 Z",Ear,null,0);
  Path(d,"M 224,201 Q 241,177 260,178 Q 267,201 256,225 Z",Fur,Outline,3.8);Path(d,"M 240,206 L 256,190 L 255,214 Z",Ear,null,0);
  Path(d,"M 132,248 C 126,212 153,197 196,197 C 241,194 271,211 272,249 C 274,281 250,295 203,294 C 157,297 132,283 132,248 Z",Fur,Outline,4);
  Path(d,"M 148,250 Q 169,264 188,248 M 216,248 Q 236,264 256,248",null,Cream,5.5);Path(d,"M 194,270 L 207,270 L 201,280 Z","#63A8AE",Outline,2);
  E(d,179,293,17,7,Fur,Outline,3);E(d,231,293,17,7,Fur,Outline,3);d.Pop();d.Pop();Text(d,"z",261,162+Math.Sin(t)*3,19,"#90AA84");Text(d,"z",279,140+Math.Sin(t+1)*4,13,"#A4B69B");
 }
}
}
