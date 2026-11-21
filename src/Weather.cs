using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XiaoheiDesktop {
public sealed class WeatherReading {
    public string query, place, observed;
    public double temperature;
    public int code;
    public long fetchedTicks;
    public bool Fresh(DateTime now) {
        return fetchedTicks > 0 && now.Ticks >= fetchedTicks && now.Ticks - fetchedTicks < TimeSpan.FromMinutes(20).Ticks;
    }
    public string Summary {
        get { return place+"　"+temperature.ToString("0.#",CultureInfo.InvariantCulture)+" °C　"+WeatherLookup.Condition(code); }
    }
}
public static class WeatherLookup {
    public sealed class Place { public string name, admin1, country; public double? latitude, longitude; }
    public sealed class Places { public Place[] results; }
    public sealed class Current { public string time; public double? temperature_2m; public int? weather_code; }
    public sealed class Forecast { public Current current; }
    public static string Condition(int code) {
        switch(code) {
            case 0:return "晴"; case 1:return "大部晴朗"; case 2:return "局部多云"; case 3:return "阴";
            case 45:case 48:return "有雾"; case 51:case 53:case 55:return "毛毛雨";
            case 56:case 57:case 66:case 67:return "冻雨";
            case 61:return "小雨";case 63:return "中雨";case 65:return "大雨";
            case 71:return "小雪";case 73:return "中雪";case 75:return "大雪";case 77:return "米雪";
            case 80:case 81:case 82:return "阵雨";case 85:case 86:return "阵雪";
            case 95:return "雷雨";case 96:case 99:return "雷雨伴冰雹";default:return "天气代码 "+code;
        }
    }
    static string Get(string url) {
        // Framework applications compiled with the system csc can inherit TLS 1.0
        // on otherwise current Windows machines. Preserve OS defaults when enabled.
        if((int)ServicePointManager.SecurityProtocol!=0 &&
           (ServicePointManager.SecurityProtocol & SecurityProtocolType.Tls12)==0)
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
        var request=(HttpWebRequest)WebRequest.Create(url);
        request.Timeout=8000; request.ReadWriteTimeout=8000;
        request.UserAgent="XiaoheiCompanion/3.3 Weather";
        using(var response=request.GetResponse())
        using(var reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8))return reader.ReadToEnd();
    }
    public static WeatherReading Fetch(string query, Func<string,string> get, DateTime now) {
        var json=new JavaScriptSerializer();
        var locations=json.Deserialize<Places>(get("https://geocoding-api.open-meteo.com/v1/search?count=1&language=zh&name="+Uri.EscapeDataString(query)));
        if(locations==null||locations.results==null||locations.results.Length==0)throw new ArgumentException("没找到这个城市。可以试试城市全名、拼音或英文名。");
        var place=locations.results[0];
        if(!place.latitude.HasValue||!place.longitude.HasValue||String.IsNullOrWhiteSpace(place.name))throw new InvalidDataException();
        string area=place.admin1;
        string location=place.name;
        if(!String.IsNullOrWhiteSpace(area)&&!area.StartsWith(place.name,StringComparison.Ordinal))location+=" · "+area;
        if(!String.IsNullOrWhiteSpace(place.country))location+=" · "+place.country;
        return ReadForecast(query,location,place.latitude.Value,place.longitude.Value,get,now);
    }
    static WeatherReading ReadForecast(string query, string place, double latitude, double longitude, Func<string,string> get, DateTime now) {
        string url = String.Format(CultureInfo.InvariantCulture,
            "https://api.open-meteo.com/v1/forecast?latitude={0:0.####}&longitude={1:0.####}&current=temperature_2m,weather_code&timezone=auto", latitude, longitude);
        var result = new JavaScriptSerializer().Deserialize<Forecast>(get(url));
        var cur = result == null ? null : result.current;
        if (cur == null || !cur.temperature_2m.HasValue || !cur.weather_code.HasValue || String.IsNullOrWhiteSpace(cur.time))
            throw new InvalidDataException("天气数据不完整。");
        return new WeatherReading {query=query, place=place, observed=cur.time.Replace('T',' '), temperature=cur.temperature_2m.Value,
            code=cur.weather_code.Value, fetchedTicks=now.Ticks};
    }
    public static WeatherReading FetchNear(WeatherArea area, Func<string,string> get, DateTime now) {
        return ReadForecast(null, "所在区域", area.Latitude, area.Longitude, get, now);
    }
    public static Task<WeatherReading> NearAsync(WeatherArea area) {
        return Task.Run(delegate { return FetchNear(area, Get, DateTime.UtcNow); });
    }
    public static Task<WeatherReading> FetchAsync(string city) {
        return Task.Run(delegate {return Fetch(city,Get,DateTime.UtcNow);});
    }
    public static WeatherReading Last() {
        try {
            var file=new FileInfo(Path.Combine(Store.Dir,"weather.json"));
            if(!file.Exists||file.Length>16384)return null;
            var data=new JavaScriptSerializer().Deserialize<WeatherReading>(File.ReadAllText(file.FullName,Encoding.UTF8));
            return data!=null&&!String.IsNullOrWhiteSpace(data.place)&&!String.IsNullOrWhiteSpace(data.query)?data:null;
        } catch {return null;}
    }
    public static void Remember(WeatherReading weather) {
        if (weather == null || String.IsNullOrWhiteSpace(weather.query)) return;
        Store.Write("weather.json",new JavaScriptSerializer().Serialize(weather));
    }
}
public sealed partial class PocketWindow {
    private int weatherEpoch;
    private Func<Task<WeatherArea>> readWeatherArea = WindowsWeatherLocation.ReadAsync;
    private Func<WeatherArea,Task<WeatherReading>> readLocalWeather = WeatherLookup.NearAsync;
    private Func<string,Task<WeatherReading>> readCityWeather = WeatherLookup.FetchAsync;
    private WeatherArea lastWeatherArea;
    private WeatherReading lastAreaForecast;

    private FrameworkElement MakeWeatherCard() {
        int epoch = ++weatherEpoch;
        int requestNumber = 0;
        var manual = WeatherLookup.Last();
        var panel = UI.Stack();
        panel.Children.Add(UI.Heading("小黑看天气"));
        var summary = UI.Text("正在准备本地天气…", 15, UI.Ink);
        var attribution = UI.Text("Windows 定位 · Open-Meteo", 11, UI.Muted);
        attribution.Margin = new Thickness(0, 3, 0, 10);
        panel.Children.Add(summary);
        panel.Children.Add(attribution);

        var row = new DockPanel();
        var city = new TextBox {
            Text = manual == null ? "" : manual.query, MaxLength = 60, Padding = new Thickness(8, 5, 8, 5),
            VerticalContentAlignment = VerticalAlignment.Center, ToolTip = "输入城市名，例如「北京」"
        };
        System.Windows.Automation.AutomationProperties.SetName(city, "查询城市");
        panel.Children.Add(row);
        var card = UI.Card(panel);
        Func<int, bool> current = delegate(int number) {
            return epoch == weatherEpoch && number == requestNumber && card.IsVisible;
        };
        Action<WeatherReading,string> display = delegate(WeatherReading info, string source) {
            summary.Text = info.Summary;
            attribution.Text = source + " · " + info.observed + " 当地 · Open-Meteo";
        };
        Func<Task> locate = async delegate {
            int number = ++requestNumber;
            summary.Text = "正在获取电脑所在区域…";
            attribution.Text = "Windows 定位 · Open-Meteo";
            WeatherArea area;
            try {
                area = await readWeatherArea();
            } catch (Exception error) {
                if (!current(number)) return;
                summary.Text = error is LocationUnavailable ? error.Message : "无法获取系统位置，可输入城市。";
                attribution.Text = manual == null ? "城市名支持中文和拼音" : "上次手选 · " + manual.Summary + " · " + manual.observed;
                return;
            }
            if (!current(number)) return;
            if (area.Matches(lastWeatherArea) && lastAreaForecast != null && lastAreaForecast.Fresh(DateTime.UtcNow)) {
                display(lastAreaForecast, "系统定位");
                return;
            }
            summary.Text = "已定位，正在查询附近天气…";
            try {
                var info = await readLocalWeather(area);
                if (!current(number)) return;
                lastWeatherArea = area;
                lastAreaForecast = info;
                display(info, "系统定位");
            } catch {
                if (!current(number)) return;
                if (area.Matches(lastWeatherArea) && lastAreaForecast != null) display(lastAreaForecast, "联网失败 · 上次定位天气");
                else { summary.Text = "已定位，暂时连不上天气服务。"; attribution.Text = "稍后点「定位」重试 · Open-Meteo"; }
            }
        };
        Func<Task> searchCity = async delegate {
            string query = city.Text.Trim();
            int number = ++requestNumber;
            if (query.Length < 1) {
                summary.Text = "先输入城市名。";
                attribution.Text = "例如 北京 或 Shanghai";
                city.Focus();
                return;
            }
            bool same = manual != null && String.Equals(manual.query, query, StringComparison.OrdinalIgnoreCase);
            if (same && manual.Fresh(DateTime.UtcNow)) { display(manual, "手选城市"); return; }
            summary.Text = "正在查询 " + query + "…";
            attribution.Text = "Open-Meteo";
            try {
                var info = await readCityWeather(query);
                if (!current(number)) return;
                manual = info;
                WeatherLookup.Remember(info);
                display(info, "手选城市");
            } catch (Exception error) {
                if (!current(number)) return;
                if (same) display(manual, "查询失败 · 上次手选天气");
                else {
                    summary.Text = error is ArgumentException ? error.Message : "暂时连不上天气服务。";
                    attribution.Text = "可稍后重试 · Open-Meteo";
                }
            }
        };
        var localButton = UI.Button("定位", async delegate { await locate(); }, false);
        localButton.ToolTip = "重新获取电脑所在区域的天气";
        var cityButton = UI.Button("查城市", async delegate { await searchCity(); }, false);
        foreach (var button in new[] {localButton, cityButton}) {
            button.Padding = new Thickness(10,5,10,5);
            button.Margin = new Thickness(6,0,0,0);
            DockPanel.SetDock(button,Dock.Right);
            row.Children.Add(button);
        }
        city.KeyDown += async delegate(object sender, KeyEventArgs args) {
            if (args.Key == Key.Return) { args.Handled = true; await searchCity(); }
        };
        row.Children.Add(city);
        card.IsVisibleChanged += async delegate {
            if (card.IsVisible && IsVisible && epoch == weatherEpoch) await locate();
            else requestNumber++;
        };
        return card;
    }
}
}
