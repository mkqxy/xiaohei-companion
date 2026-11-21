using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
namespace XiaoheiDesktop {
// Local invocation protocol: animation/message data only, never a process or script.
public sealed class ActionCommand {
    public string animation="idle",message="",issuedUtc="";
    public static string InstanceKey {get{using(var hash=SHA256.Create()){return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(Store.Dir).ToUpperInvariant()))).Replace("-","").Substring(0,16);}}}
    public static ActionCommand FromArguments(string[] args){
        string animation=null,message="";bool requested=false;
        for(int n=0;n<args.Length;n++){
            if(args[n]=="--action"){if(requested||n+1==args.Length)throw new ArgumentException("--action needs one animation ID.");animation=args[++n];requested=true;}
            else if(args[n]=="--message"){if(n+1==args.Length)throw new ArgumentException("--message needs text.");message=args[++n];}
            else if(args[n]!="--open-pocket"&&args[n]!="--inspect-pet")throw new ArgumentException("Unknown argument: "+args[n]);
        }
        if(!requested){if(message.Length>0)throw new ArgumentException("--message requires --action.");return null;}
        animation=SpriteTimeline.Normalize(animation);if(SpriteTimeline.Row(animation)<0)throw new ArgumentException("Unknown animation ID.");if(message.Length>160)throw new ArgumentException("Message cannot exceed 160 characters.");
        return new ActionCommand{animation=animation,message=message,issuedUtc=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture)};
    }
    static string QueueDirectory {get{return Path.Combine(Store.Dir,"commands");}}
    public static bool HasPending {get{try{return Directory.Exists(QueueDirectory)&&Directory.EnumerateFiles(QueueDirectory,"*.json").Any();}catch{return false;}}}
    public void Enqueue(){
        if(SpriteTimeline.Row(animation)<0||message==null||message.Length>160)throw new ArgumentException("Invalid action request.");
        Directory.CreateDirectory(QueueDirectory);
        // Windows may report the same UTC tick for several successive commands. The
        // system-wide performance counter preserves their order within that tick.
        string stamp=DateTime.UtcNow.Ticks.ToString("D19",CultureInfo.InvariantCulture);
        string sequence=Stopwatch.GetTimestamp().ToString("D19",CultureInfo.InvariantCulture);
        string temp=Path.Combine(QueueDirectory,stamp+"-"+sequence+"-"+Guid.NewGuid().ToString("N")+".tmp");
        File.WriteAllText(temp,new JavaScriptSerializer().Serialize(this),new UTF8Encoding(false));
        File.Move(temp,Path.ChangeExtension(temp,"json"));
    }
    public static int DispatchPending(Action<ActionCommand> dispatch){
        if(!Directory.Exists(QueueDirectory))return 0;string[] files=Directory.GetFiles(QueueDirectory,"*.json");Array.Sort(files,StringComparer.Ordinal);int dispatched=0;
        foreach(string file in files){
            // Imported skin/config data is never searched for commands.
            ActionCommand request=null;try{
                if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)continue;
                if(new FileInfo(file).Length>16384)continue;string json=File.ReadAllText(file,Encoding.UTF8);var js=new JavaScriptSerializer();var map=js.DeserializeObject(json) as Dictionary<string,object>;if(map==null)continue;bool valid=true;foreach(string key in map.Keys)if(key!="animation"&&key!="message"&&key!="issuedUtc")valid=false;if(!valid)continue;
                request=js.Deserialize<ActionCommand>(json);DateTime issued;if(request==null||SpriteTimeline.Row(request.animation)<0||request.message==null||request.message.Length>160||!DateTime.TryParse(request.issuedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out issued)||DateTime.UtcNow-issued.ToUniversalTime()>TimeSpan.FromMinutes(2)||issued.ToUniversalTime()-DateTime.UtcNow>TimeSpan.FromMinutes(2))continue;
                request.animation=SpriteTimeline.Normalize(request.animation);
            }catch{request=null;}finally{try{File.Delete(file);}catch{}}
            if(request!=null){dispatch(request);dispatched++;}if(dispatched>=32)break;
        }
        return dispatched;
    }
}
}
