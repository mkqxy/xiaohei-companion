using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using XiaoheiDesktop;

public static class LocationFocusTests {
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    static int count;
    static void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
    static void Set(object obj,string name,object value){obj.GetType().GetField(name,Hidden).SetValue(obj,value);}
    static void Pump() {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(delegate {frame.Continue=false;}));
        Dispatcher.PushFrame(frame);
    }
    static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is T) yield return (T)child;
            foreach(var descendant in Find<T>(child))yield return descendant;
        }
    }
    [STAThread] static void Main(string[] args) {
        Check(Path.GetFullPath(args[0])==Path.GetFullPath(Store.Dir),"Expected isolated data");
        string requested=null;
        var area=new WeatherArea(23.13259,113.26655);
        var local=WeatherLookup.FetchNear(area, delegate(string url) {
            requested=url;
            return "{\"current\":{\"time\":\"2026-11-21T12:00\",\"temperature_2m\":15.5,\"weather_code\":0}}";
        },DateTime.UtcNow);
        Check(requested.Contains("latitude=23.1&longitude=113.3&") && local.query==null,"Nearby forecast uses only coarsened position");
        WeatherLookup.Remember(local);
        Check(!File.Exists(Path.Combine(Store.Dir,"weather.json")),"OS position and local weather never persist");
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset)){
            var pet=new PetWindow(signal){ShowActivated=false,Topmost=false,ShowInTaskbar=false};
            pet.Show();pet.Hide();
            var pocket=new PocketWindow(pet){ShowActivated=false,Topmost=false,ShowInTaskbar=false,Left=-12000,Top=-12000};
            Set(pet,"pocket",pocket);
            var pending=new TaskCompletionSource<WeatherArea>();
            int locateCalls=0,localCalls=0,manualCalls=0;
            Set(pocket,"readWeatherArea",new Func<Task<WeatherArea>>(delegate {locateCalls++;return pending.Task;}));
            Set(pocket,"readLocalWeather",new Func<WeatherArea,Task<WeatherReading>>(delegate {localCalls++;return Task.FromResult(local);}));
            Set(pocket,"readCityWeather",new Func<string,Task<WeatherReading>>(delegate(string city){
                manualCalls++;return Task.FromResult(new WeatherReading{query=city,place=city,observed="12:00",fetchedTicks=DateTime.UtcNow.Ticks});
            }));
            pocket.Show();
            typeof(PocketWindow).GetMethod("ShowTab",Hidden).Invoke(pocket,new object[]{"小工具"});
            pocket.UpdateLayout();Pump();
            Check(locateCalls==1,"Visible tools request Windows location automatically once");
            var cityBox=Find<TextBox>(pocket).Single(b=>System.Windows.Automation.AutomationProperties.GetName(b)=="查询城市");
            cityBox.Text="测试城";
            Find<Button>(pocket).Single(b=>Equals(b.Content,"查城市")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            pending.SetResult(area);Pump();
            Check(manualCalls==1 && localCalls==0 && Find<TextBlock>(pocket).Any(b=>b.Text.Contains("手选城市")),"Manual choice supersedes a pending location fix");
            var note=Find<TextBox>(pocket).Single(b=>b.AcceptsReturn);
            note.Text="临时笔记在专注收起前应保存";
            pet.StartFocus(3);Pump();
            Check(!pocket.IsVisible && pet.Timer.Running && File.ReadAllText(Path.Combine(Store.Dir,"note.txt"))==note.Text,
                "Starting focus saves notes then hides the pocket");
            pocket.Show();pocket.UpdateLayout();Pump();
            Check(locateCalls==2 && localCalls==1 && Find<TextBlock>(pocket).Any(b=>b.Text.Contains("系统定位")),"Reopened tools show the current-area forecast");
            pet.PauseFocus();pet.ResumeFocus();Pump();
            Check(pet.Timer.Running && !pocket.IsVisible,"Resuming focus also collapses the pocket");
            pocket.Show();pocket.UpdateLayout();Pump();
            Check(localCalls==1 && locateCalls==3,"Fresh weather is reused after checking the current area");
            Set(pocket,"readWeatherArea",new Func<Task<WeatherArea>>(delegate{throw new LocationUnavailable("Windows 未允许定位，可输入城市。");}));
            Find<Button>(pocket).Single(b=>Equals(b.Content,"定位")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();
            Check(Find<TextBlock>(pocket).Any(b=>b.Text.Contains("Windows 未允许定位")),"Denied OS permission exposes manual fallback");
            pet.EndFocus();
            string blockedTemp = Path.Combine(Store.Dir,"note.txt.tmp");
            Directory.CreateDirectory(blockedTemp);
            try {
                note.Text = "保存失败时保留在口袋中的笔记";
                pet.StartFocus(1);Pump();
                Check(pet.Timer.Running && pocket.IsVisible && pocket.HasUnsavedNote,
                    "A failed note save leaves the visible pocket and unsaved text intact");
            } finally {
                Directory.Delete(blockedTemp);
                pocket.FlushNote();
            }
            pet.EndFocus();pocket.Close();pet.Close();
        }
        app.Shutdown();Console.WriteLine("PASS: "+count+" focus and weather workflow assertions.");
    }
}
