using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace XiaoheiDesktop {
// Original fan artwork. Character designs are excluded from the code licence;
// see ASSET_NOTICE.md. All geometry is drawn in DIP and scales with display DPI.
internal sealed partial class GuestArt : FrameworkElement {
    static readonly Dictionary<string, Geometry> Shapes = new Dictionary<string, Geometry>();
    static readonly Pen Outline = Stroke("#433C41", 2.05);
    static readonly Pen Fine = Stroke("#50484A", 1.2);
    readonly GuestKind kind;
    bool hunt {get{return kind==GuestKind.Heyxiu;}}
    public int Interactions;
    public double LookX, LookY;
    public double Clock, ReactionAge = 10;
    public bool Arriving, Celebrating, FaceRight;
    public GuestArt(bool hunt) : this(hunt ? GuestKind.Heyxiu : GuestKind.Tabby) {}
    public GuestArt(GuestKind kind) {
        this.kind = kind;
        Width = hunt ? 56 : 118;
        Height = hunt ? 52 : 96;
    }

    static Pen Stroke(string colour, double width) {
        var pen = new Pen(PetArt.B(colour), width) { StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        pen.Freeze();
        return pen;
    }

    static void Shape(DrawingContext dc, string path, string fill, Pen edge) {
        Geometry shape;
        if (!Shapes.TryGetValue(path, out shape)) {
            shape = Geometry.Parse(path);
            shape.Freeze();
            Shapes[path] = shape;
        }
        dc.DrawGeometry(fill == null ? null : PetArt.B(fill), edge, shape);
    }
    static void Oval(DrawingContext dc, double x, double y, double rx, double ry, string colour, Pen edge) {
        dc.DrawEllipse(PetArt.B(colour), edge, new Point(x,y), rx, ry);
    }

    protected override void OnRender(DrawingContext dc) {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        double designWidth = hunt ? 56 : 118;
        double designHeight = hunt ? 52 : 96;
        dc.PushTransform(new ScaleTransform(ActualWidth/designWidth, ActualHeight/designHeight));
        dc.DrawRoundedRectangle(Brushes.Transparent, null, new Rect(3,3,designWidth-6,designHeight-6),15,15);
        dc.PushTransform(new TranslateTransform(0,hunt ? 7 : 7.5));
        if (hunt) DrawHeyxiu(dc);
        else {
            if (!FaceRight) dc.PushTransform(new ScaleTransform(-1,1,59,48));
            if (kind==GuestKind.Bidiu) DrawBidiu(dc); else DrawTabby(dc);
            if (!FaceRight) dc.Pop();
        }
        dc.Pop(); dc.Pop();
    }

    void DrawHeyxiu(DrawingContext dc) {
        double lift = Celebrating ? Math.Abs(Math.Sin(Clock*9))*6 : Math.Sin(Clock*3.2)*1.3;
        Oval(dc, 28, 43, 12, 2, "#20404C46", null);
        dc.PushTransform(new TranslateTransform(0,-lift));
        dc.PushTransform(new RotateTransform(Math.Sin(Clock*2.1)*5,28,34));
        Shape(dc,"M 9,28 Q 6,20 10,10 Q 15,10 19,16 Q 29,12 36,16 Q 40,10 46,10 Q 49,17 46,28 C 46,40 37,43 28,43 C 16,43 9,38 9,28 Z","#101115",Outline);
        Shape(dc,"M 10,13 Q 14,14 16,18 L 10,23 Z M 44,13 L 45,22 L 39,17 Z","#AECA91",null);
        double eyeHeight = Clock % 3.5 < .16 ? 1.15 : 8.7;
        Oval(dc,19,28,7,eyeHeight,"#FFF5CE",null);
        Oval(dc,36,28,7,eyeHeight,"#FFF5CE",null);
        if (eyeHeight>2) {
            Oval(dc,20,28,4.15,5.65,"#100F13",null);
            Oval(dc,35,28,4.15,5.65,"#100F13",null);
        }
        Shape(dc,"M 25.5,34 L 30,34 L 28,37 Z","#7EB3B5",null);
        dc.Pop(); dc.Pop();
    }

}
}
