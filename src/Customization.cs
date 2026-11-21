using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
namespace XiaoheiDesktop {
public sealed class CustomAction {
    public string label="",animation="waving",message="";
}
public sealed class CustomizationProfile {
    public const string DefaultGreeting="我来啦。靠近我，点「变身」试试。";
    public const string LegacyDefaultGreeting="小黑来啦。右键或双击，打开小口袋。";
    public int schemaVersion=1;
    public string name="小黑和嘿咻",greeting=DefaultGreeting;
    public double animationSpeed=1,scale=.36;
    public bool roaming=false,quiet=false,autoExpressions=true;
    public int focusMinutes=25,waterMinutes=0;
    public CustomAction[] customActions={new CustomAction{label="今天也加油",animation="waving",message="我在这里，陪你慢慢来。"}};
    static string Text(string value,int length,string fallback){value=(value??"").Trim();return value.Length==0?fallback:value.Substring(0,Math.Min(length,value.Length));}
    public static CustomizationProfile Parse(string json){
        if(json==null||json.Length>65536)throw new ArgumentException("配置文件不能超过 64 KiB。");
        var js=new JavaScriptSerializer();object raw=js.DeserializeObject(json);var map=raw as Dictionary<string,object>;
        if(map==null)throw new ArgumentException("配置需要是一个 JSON 对象。");
        var allowed=new HashSet<string>(new[]{"schemaVersion","name","greeting","animationSpeed","scale","roaming","quiet","focusMinutes","waterMinutes","autoExpressions","customActions"});
        foreach(string key in map.Keys)if(!allowed.Contains(key))throw new ArgumentException("不支持的配置项："+key+"。配置不允许脚本、命令或外部文件路径。");
        var p=js.Deserialize<CustomizationProfile>(json);if(p==null||p.schemaVersion!=1)throw new ArgumentException("仅支持 schemaVersion 1 的配置。");
        if(p.greeting==LegacyDefaultGreeting || p.greeting=="小黑来啦。点一下打招呼，右键或「更多」打开口袋。")p.greeting=DefaultGreeting;
        p.name=Text(p.name,24,"小黑和嘿咻");p.greeting=Text(p.greeting,160,"我在这里。");
        if(Double.IsNaN(p.animationSpeed)||Double.IsInfinity(p.animationSpeed)||Double.IsNaN(p.scale)||Double.IsInfinity(p.scale))throw new ArgumentException("速度和大小必须是有限数值。");
        p.animationSpeed=Math.Max(.5,Math.Min(2,p.animationSpeed));p.scale=Math.Max(.28,Math.Min(1,p.scale));p.focusMinutes=Math.Max(1,Math.Min(180,p.focusMinutes));p.waterMinutes=Math.Max(0,Math.Min(240,p.waterMinutes));
        if(p.waterMinutes>0)p.waterMinutes=Math.Max(5,p.waterMinutes);
        var actions=new List<CustomAction>();object actionsRaw;
        if(map.TryGetValue("customActions",out actionsRaw)&&actionsRaw!=null){var array=actionsRaw as object[];if(array==null)throw new ArgumentException("customActions 必须是数组。");foreach(object item in array){var actionMap=item as Dictionary<string,object>;if(actionMap==null)throw new ArgumentException("自定义动作必须是对象。");foreach(string key in actionMap.Keys)if(key!="label"&&key!="animation"&&key!="message")throw new ArgumentException("自定义动作仅支持 label、animation、message。");}}
        if(p.customActions!=null){if(p.customActions.Length>12)throw new ArgumentException("最多添加 12 个自定义动作。");foreach(var a in p.customActions){if(a==null||SpriteTimeline.Row(a.animation)<0)throw new ArgumentException("自定义动作的动画名称不正确。");a.label=Text(a.label,20,"小动作");a.message=Text(a.message,160,"");a.animation=SpriteTimeline.Normalize(a.animation);actions.Add(a);}}
        p.customActions=actions.ToArray();return p;
    }
    public string Json(){return new JavaScriptSerializer().Serialize(this);}
    public static CustomizationProfile Load(){
        string saved=Path.Combine(Store.Dir,"profile.json"),portable=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"pet.config.json");
        try{string file=File.Exists(saved)?saved:portable;if(File.Exists(file))return Parse(File.ReadAllText(file,Encoding.UTF8));}catch(Exception e){LoadWarning="个性化配置未加载，已使用默认值："+e.Message;}
        return new CustomizationProfile();
    }
    public static string LoadWarning="";
}
}
