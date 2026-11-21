using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace XiaoheiDesktop {
public enum GuestKind { Heyxiu, Tabby, Bidiu }
public sealed partial class PetWindow {
    GuestPlayWindow visitor;

    public void VisitTabby() { BeginVisit(GuestKind.Tabby); }
    public void VisitBidiu() { BeginVisit(GuestKind.Bidiu); }
    public void FindHeyxiu() { BeginVisit(GuestKind.Heyxiu); }
    public void StopGuestPlay() {
        if (visitor != null) visitor.Close();
    }

    void BeginVisit(GuestKind kind) {
        bool hunt = kind==GuestKind.Heyxiu;
        if (Timer.Running) {
            Say("等这一段专注结束，再叫伙伴来玩吧。", 4, true);
            return;
        }
        art.CancelTail();
        if (visitor!=null && visitor.IsVisible && visitor.Kind==kind && !hunt) {
            visitor.Interact(); return;
        }
        StopGuestPlay();
        PlayAnimation(hunt ? "review" : "waving", null);
        Say(hunt ? "嘿咻躲起来啦！点到它三次就赢。" : kind==GuestKind.Bidiu ?
            "比丢来啦！点它，看看吃了什么颜色。" : "小皇来啦！点它扑叶子，再点蹭蹭。", 6, true);
        bool pocketOnRight = pocket != null && pocket.IsVisible && pocket.Left > Left;
        var guest = new GuestPlayWindow(this, kind, pocketOnRight);
        visitor = guest;
        art.GuestIsHeyxiu=hunt;
        guest.Closed += delegate { if (visitor == guest) {visitor = null; art.GuestIsHeyxiu=false;} };
        guest.Show();
    }
}

public sealed class GuestPlayWindow : Window {
    readonly PetWindow pet;
    public GuestKind Kind {get;private set;}
    bool hunt {get{return Kind==GuestKind.Heyxiu;}}
    readonly int side;
    readonly GuestArt art;
    readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);
    readonly Stopwatch clock = new Stopwatch();
    readonly int[] hidingPlaces = { 0, 1, 2 };
    int finds;
    double lastAction = -10, finishAt = double.PositiveInfinity;
    string contactCue;
    double contactTime;

    public GuestPlayWindow(PetWindow pet, bool hunt, bool pocketOnRight)
        : this(pet,hunt?GuestKind.Heyxiu:GuestKind.Tabby,pocketOnRight) {}
    public GuestPlayWindow(PetWindow pet, GuestKind kind, bool pocketOnRight) {
        this.pet = pet;
        Kind = kind;
        Title = "小黑的伙伴 · " + (hunt ? "嘿咻" : kind==GuestKind.Bidiu ? "比丢" : "皇受");
        SizeForPet();
        Owner = pet;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = null;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        Topmost = pet.Topmost;
        Rect area = pet.WorkArea();
        double leftSpace = pet.Left - area.Left;
        double rightSpace = area.Right - pet.Left - pet.Width;
        side = (pocketOnRight && leftSpace >= Width) ||
               (rightSpace < Width && leftSpace > rightSpace) ? -1 : 1;
        var random = new Random();
        for (int index = hidingPlaces.Length - 1; index > 0; index--) {
            int other = random.Next(index + 1);
            int old = hidingPlaces[index];
            hidingPlaces[index] = hidingPlaces[other];
            hidingPlaces[other] = old;
        }
        art = new GuestArt(kind) { Width=Double.NaN, Height=Double.NaN };
        var hit = new Button {
            Content = art, Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            Padding = new Thickness(0), Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            ToolTip = hunt ? "点到嘿咻" : kind==GuestKind.Bidiu ? "点比丢，喂它变色" : "点皇受，扑叶子或蹭小黑"
        };
        // The button keeps keyboard and UI Automation support without a rectangular
        // hover background obscuring the desktop.
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        hit.Template = new ControlTemplate(typeof(Button)) { VisualTree = presenter };
        AutomationProperties.SetName(hit, hunt ? "找到嘿咻" : kind==GuestKind.Bidiu ? "喂比丢变色" : "抛一片叶子");
        hit.MouseMove += delegate(object sender, MouseEventArgs e) {
            var p=e.GetPosition(hit);
            art.LookX=Math.Max(-1,Math.Min(1,(p.X-Width*.54)/(Width*.45)));
            art.LookY=Math.Max(-1,Math.Min(1,(p.Y-Height*.5)/(Height*.5)));
        };
        hit.MouseLeave += delegate {art.LookX=0;art.LookY=0;};
        hit.Click += delegate { Interact(); };
        hit.MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e) { e.Handled = true; Close(); };
        KeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); };
        Content = hit;
        Loaded += delegate {
            clock.Start(); Place(0);
            if (!hunt) {
                contactCue = kind == GuestKind.Tabby ? "tabby-paws" : "bidiu-hop";
                contactTime = kind == GuestKind.Tabby ? .75 : Math.PI * 3 / 9;
            }
            timer.Start();
        };
        Closed += delegate { timer.Stop(); clock.Stop(); };
        timer.Interval = TimeSpan.FromMilliseconds(50);
        timer.Tick += delegate { Update(); };
        Place(0);
    }

    public void Interact() {
        double now = clock.Elapsed.TotalSeconds;
        if (!double.IsInfinity(finishAt) || now - lastAction < (hunt ? .45 : 1.15)) return;
        lastAction = now;
        art.Interactions++;
        if (hunt) {
            pet.PlaySound("heyxiu");
            finds++;
            if (finds == 3) {
                pet.PlayAnimation("jumping", null);
                pet.Say("三次都找到啦！嘿咻，回家。", 5, true);
                finishAt = now + 1.4;
            } else {
                pet.PlayAnimation("review", null);
                pet.Say("找到了 " + finds + " / 3！看看另一边。", 4, true);
            }
        } else if(Kind==GuestKind.Bidiu) {
            contactCue="bidiu-bite";contactTime=now+.60;
            pet.PlayAnimation("review",null);
            pet.Say(art.Interactions%2==1 ? "绿球进肚，毛也变绿啦！" : "一口茄子……紫色的比丢！",3,true);
        } else {
            bool rub=art.Interactions%2==0;
            contactCue=rub?"pet":"tabby-leaf";contactTime=now+(rub?.37:.62);
            pet.PlayAnimation(rub ? "waiting" : "jumping", null);
            pet.Say(rub ? "小皇又挨过来了。" : "接住啦，小皇！", 3, true);
        }
        Update();
    }

    void Update() {
        double now = clock.Elapsed.TotalSeconds;
        if (!pet.IsVisible || pet.Timer.Running || pet.Companion.Sleeping ||
            now >= finishAt || now - Math.Max(0, lastAction) > (hunt ? 80 : 45)) {
            Close();
            return;
        }
        Place(now);
        art.Clock = now;
        art.ReactionAge = now - lastAction;
        if(contactCue!=null && now>=contactTime) {
            string cue=contactCue;contactCue=null;
            if(now-contactTime<.26)pet.PlaySound(cue);
        }
        art.Arriving = !hunt && now < 1.15;
        art.Celebrating = hunt && finds == 3;
        art.InvalidateVisual();
    }

    double GuestScale {get{return pet.Pref.Scale/.36;}}
    void SizeForPet() {
        double width=(hunt?56:118)*GuestScale, height=(hunt?52:96)*GuestScale;
        if (Width != width) Width=width;
        if (Height != height) Height=height;
    }
    void Place(double now) {
        SizeForPet();
        double cx = pet.Left + pet.Width / 2;
        double paw = pet.PawBaseline;
        double catWidth = pet.Art.SpriteBounds().Width;
        if (catWidth < 1) catWidth = 100;
        double x = cx + side * (catWidth * .48 + Width * .35) - Width / 2;
        double y = paw - Height;
        int facing = -side;
        if (hunt) {
            int spot = hidingPlaces[Math.Min(finds, 2)];
            if (spot == 0) x = cx - catWidth * .74 - Width / 2;
            if (spot == 1) x = cx + catWidth * .69 - Width / 2;
            if (spot == 2) {
                x = cx + catWidth * .24 - Width / 2;
                y = paw - pet.Art.SpriteBounds().Height - Height * .5;
            }
            // A hop and turn make a successful find apparent even at a screen edge.
            if (now - lastAction < .35) y -= 7 * GuestScale * Math.Sin(Math.PI * (now - lastAction) / .35);
        } else {
            double remaining = Math.Max(0, 1 - now / 1.15);
            x += side * 70 * GuestScale * remaining * remaining;
            if(Kind==GuestKind.Tabby && art.Interactions>0 && art.Interactions%2==0) {
                double t=now-lastAction;
                double approach=Math.Max(0,Math.Min(1,Math.Min(t/.42,(2.45-t)/.68)));
                x-=side*Math.Min(catWidth*.40,37*GuestScale)*approach;
            }
        }
        Rect area = pet.WorkArea();
        x = Math.Max(area.Left, Math.Min(x, area.Right - Width));
        y = Math.Max(area.Top, Math.Min(y, area.Bottom - Height));
        art.FaceRight = facing > 0;
        if (Math.Abs(Left - x) > .2 || double.IsNaN(Left)) Left = x;
        if (Math.Abs(Top - y) > .2 || double.IsNaN(Top)) Top = y;
    }
}
}
