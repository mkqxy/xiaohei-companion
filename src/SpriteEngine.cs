using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XiaoheiDesktop {
// The native atlas is shared without redrawing or transforming the character art.
public static class SpriteTimeline {
    public static readonly string[] States={"idle","running-right","running-left","waving","jumping","failed","waiting","running","review"};
    public static readonly string[] Labels={"待机眨眼","向右跑","向左跑","招招手","开心小跳","委屈一下","等你回应","专注工作","认真检查"};
    // Native idle holds are six times the atlas base timings; all other rows use base timings.
    static readonly int[][] Durations={new[]{1680,660,660,840,840,1920},new[]{120,120,120,120,120,120,120,220},new[]{120,120,120,120,120,120,120,220},new[]{140,140,140,280},new[]{140,140,140,140,280},new[]{140,140,140,140,140,140,140,240},new[]{150,150,150,150,150,260},new[]{120,120,120,120,120,220},new[]{150,150,150,150,150,280}};
    public static string Normalize(string id){return id=="working"?"running":id;}
    public static int Row(string id){return Array.IndexOf(States,Normalize(id));}
    public static int Count(string id){int row=Row(id);return row<0?0:Durations[row].Length;}
    public static int DurationMs(string id){int r=Row(id),sum=0;if(r<0)return 0;foreach(int n in Durations[r])sum+=n;return sum;}
    public static int FrameAt(string id,double elapsedSeconds,double speed){
        int row=Row(id);if(row<0)throw new ArgumentException("Unknown animation.");
        if(Double.IsNaN(elapsedSeconds)||Double.IsInfinity(elapsedSeconds)||elapsedSeconds<0)elapsedSeconds=0;
        if(Double.IsNaN(speed)||Double.IsInfinity(speed))speed=1;speed=Math.Max(.5,Math.Min(2,speed));
        double ms=(elapsedSeconds*1000*speed)%DurationMs(id);int[] d=Durations[row];
        for(int i=0;i<d.Length;i++){if(ms<d[i])return i;ms-=d[i];}return d.Length-1;
    }
    // Clockwise screen coordinates: up=0, right=4, down=8, left=12.
    public static int LookIndex(double x,double y){
        if(Double.IsNaN(x)||Double.IsNaN(y)||Double.IsInfinity(x)||Double.IsInfinity(y)||Math.Sqrt(x*x+y*y)<.24)return -1;
        double angle=Math.Atan2(x,-y)*180/Math.PI;if(angle<0)angle+=360;
        return ((int)Math.Floor((angle+11.25)/22.5))%16;
    }
}
public sealed class SpriteAtlas {
    public const int Width=1536,Height=2288,CellWidth=192,CellHeight=208;
    readonly BitmapSource[] cells=new BitmapSource[88];
    static SpriteAtlas current,builtIn;static bool attempted;
    public bool IsCustomSkin {get;private set;}
    public static SpriteAtlas BuiltIn {get {
        if(builtIn!=null)return builtIn;
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Xiaohei.DefaultAtlas.png")){
            if(stream!=null)builtIn=Load(stream);
        }
        if(builtIn==null){
            string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","xiaohei","spritesheet.png");
            if(File.Exists(path))builtIn=LoadFile(path);
        }
        return builtIn;
    }}
    public static string LoadWarning="";
    public static SpriteAtlas Current {get{
        if(!attempted){attempted=true;try{
            string custom=Path.Combine(Store.Dir,"custom-spritesheet.png");
            if(File.Exists(custom)){try{current=LoadFile(custom);current.IsCustomSkin=true;}catch(Exception e){LoadWarning="自定义图集无法加载，已使用内置形象："+e.Message;}}
            if(current==null)current=BuiltIn;
        }catch(Exception e){LoadWarning="动画素材无法加载，暂用备用绘图："+e.Message;}}
        return current;
    }}
    public static SpriteAtlas LoadFile(string file){using(var stream=File.OpenRead(file)){return Load(stream);}}
    public static SpriteAtlas Load(Stream stream){
        if(stream.Length>20*1024*1024)throw new ArgumentException("PNG 图集不能超过 20 MiB。");
        byte[] signature=new byte[8];if(stream.Read(signature,0,8)!=8||signature[0]!=137||signature[1]!=80||signature[2]!=78||signature[3]!=71)throw new ArgumentException("请选择 PNG 图集。");stream.Position=0;
        var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();
        if(image.PixelWidth!=Width||image.PixelHeight!=Height)throw new ArgumentException("图集必须是 1536 × 2288 像素，8 列、11 行。");
        var rgba=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);rgba.Freeze();byte[] data=new byte[Width*Height*4];rgba.CopyPixels(data,Width*4,0);
        bool transparent=false;for(int p=3;p<data.Length;p+=4)if(data[p]==0){transparent=true;break;}
        if(!transparent||data[3]!=0||data[(Width-1)*4+3]!=0||data[((Height-1)*Width)*4+3]!=0||data[data.Length-1]!=0)throw new ArgumentException("图集需要真实透明背景，四角透明。");
        for(int row=0;row<11;row++)for(int col=0;col<8;col++){
            bool used=row>=9||col<SpriteTimeline.Count(SpriteTimeline.States[row]);bool occupied=false;
            for(int y=row*CellHeight;y<(row+1)*CellHeight&&!occupied;y++)for(int x=col*CellWidth;x<(col+1)*CellWidth;x++)if(data[(y*Width+x)*4+3]>16){occupied=true;break;}
            if(used&&!occupied)throw new ArgumentException("图集中有空白动作单元，请补齐九类动画和十六个视线方向。");
            if(!used&&!(row==0&&col==6)&&occupied)throw new ArgumentException("未使用的图格必须透明，请使用原生 v2 布局。");
        }
        var atlas=new SpriteAtlas();for(int row=0;row<11;row++)for(int col=0;col<8;col++){var cell=new CroppedBitmap(rgba,new Int32Rect(col*CellWidth,row*CellHeight,CellWidth,CellHeight));cell.Freeze();atlas.cells[row*8+col]=cell;}return atlas;
    }
    public BitmapSource Frame(int row,int column){if(row<0||row>10||column<0||column>7)throw new ArgumentOutOfRangeException();return cells[row*8+column];}
    public static void Import(string file){
        SpriteAtlas validated=LoadFile(file);Directory.CreateDirectory(Store.Dir);string target=Path.Combine(Store.Dir,"custom-spritesheet.png"),temp=target+".tmp";
        File.Copy(file,temp,true);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);validated.IsCustomSkin=true;current=validated;attempted=true;LoadWarning="";
    }
    public static void Reset(){string path=Path.Combine(Store.Dir,"custom-spritesheet.png");if(File.Exists(path))File.Delete(path);current=null;attempted=false;LoadWarning="";}
}
}
