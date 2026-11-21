using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;

namespace XiaoheiDesktop {
// Movement cues are quiet until a recording is imported. A personal PCM clip can
// replace any single cue without changing the shared application package.
public sealed class PetAudio : IDisposable {
    const int SampleRate = 22050;
    const double CooldownSeconds = 1.2;
    readonly object gate = new object();
    readonly Stopwatch clock = Stopwatch.StartNew();
    readonly Dictionary<string, byte[]> waves = new Dictionary<string, byte[]>();
    readonly string soundDirectory;
    string lastCue;
    double speechUntil;
    SoundPlayer player;
    MemoryStream stream;
    bool enabled = true, suppressed, disposed;
    double volume = .30, lastPlayedAt = Double.NegativeInfinity;

    public PetAudio(string directory=null) { soundDirectory=directory; }

    string ClipPath(string cue) {
        if(!KnownCue(cue)) throw new ArgumentException("未知的声音。");
        return Path.Combine(soundDirectory,cue+".wav");
    }
    public bool HasClip(string cue) {
        return soundDirectory!=null && File.Exists(ClipPath(cue));
    }
    public void ImportClip(string cue,string source) {
        lock(gate) {
            string target=ClipPath(cue);
            byte[] pcm=ReadClip(source,1);
            Directory.CreateDirectory(soundDirectory);
            string tmp=Path.Combine(soundDirectory,Guid.NewGuid().ToString("N")+".tmp");
            try {
                File.WriteAllBytes(tmp,pcm);
                if(File.Exists(target)) File.Replace(tmp,target,null);
                else File.Move(tmp,target);
            } finally { if(File.Exists(tmp)) File.Delete(tmp); }
            StopCore(); waves.Remove(cue);
        }
    }
    public void RestoreClip(string cue) {
        lock(gate) {
            string path=ClipPath(cue);
            if(File.Exists(path)) File.Delete(path);
            StopCore(); waves.Remove(cue);
        }
    }

    // RIFF files commonly contain metadata before or after data. Keep only
    // validated PCM frames and rebuild a canonical header for SoundPlayer.
    public static byte[] ReadClip(string path,double volume) {
        using(var file=File.OpenRead(path)) return ScalePcm(file,volume);
    }
    static byte[] ScalePcm(Stream file,double volume) {
        using(var r=new BinaryReader(file)) {
            if(file.Length<44 || file.Length>4*1024*1024 ||
               r.ReadUInt32()!=0x46464952 || r.ReadUInt32()!=file.Length-8 ||
               r.ReadUInt32()!=0x45564157) throw new InvalidDataException("请选择 16 位 PCM WAV 文件。");
            int channels=0,rate=0,bytes=0; long start=0;
            while(file.Position+8<=file.Length) {
                uint id=r.ReadUInt32(),length=r.ReadUInt32();
                long end=file.Position+length;
                if(end+(length%2)>file.Length)throw new InvalidDataException("WAV 数据不完整。");
                if(id==0x20746D66) {
                    if(length<16 || r.ReadUInt16()!=1)throw new InvalidDataException("需要未压缩的 PCM WAV。");
                    channels=r.ReadUInt16();rate=r.ReadInt32();
                    if(channels<1 || channels>2 || rate<8000 || rate>96000 ||
                       r.ReadInt32()!=rate*channels*2 || r.ReadUInt16()!=channels*2 || r.ReadUInt16()!=16)
                        throw new InvalidDataException("请选择单声道或立体声、16 位 PCM WAV。");
                } else if(id==0x61746164) {start=file.Position;bytes=(int)length;}
                file.Position=end+(length%2);
            }
            if(rate==0 || bytes==0 || bytes%(channels*2)!=0 || bytes>rate*channels*2*5)
                throw new InvalidDataException("请选用 5 秒以内的 WAV 短音效。");
            file.Position=start;
            using(var output=new MemoryStream(44+bytes)) using(var w=new BinaryWriter(output)) {
                WriteHeader(w,channels,rate,bytes);
                volume=NormalizeVolume(volume);
                for(int i=0;i<bytes;i+=2)w.Write((short)Math.Round(r.ReadInt16()*volume));
                return output.ToArray();
            }
        }
    }

    public bool Enabled {
        get { lock(gate) return enabled; }
        set { lock(gate) { enabled=value; if(!enabled) StopCore(); } }
    }
    public bool Suppressed {
        get { lock(gate) return suppressed; }
        set {
            lock(gate) {
                if(value && !suppressed) StopCore();
                suppressed=value;
            }
        }
    }
    public double Volume {
        get { lock(gate) return volume; }
        set {
            lock(gate) {
                double next=NormalizeVolume(value);
                if(volume==next) return;
                volume=next;
                waves.Clear();
                StopCore();
            }
        }
    }

    // Explicit preview can be heard while the pet is quiet or focusing. Muting
    // and a zero volume still apply. A single active sound prevents layering.
    public bool Play(string cue, bool preview=false,bool capture=false) {
        lock(gate) {
            if(disposed || (!capture && (!enabled || volume<=0)) || (suppressed && !preview)) return false;
            if(!KnownCue(cue)) return false;
            double now=clock.Elapsed.TotalSeconds;
            if(!preview && now<speechUntil) return false;
            if(!preview && cue==lastCue && cue!="focus-complete" && now-lastPlayedAt<CooldownSeconds) return false;

            try {
                byte[] wave;
                if(!waves.TryGetValue(cue,out wave)) {
                    wave=LoadWave(cue,volume);
                    if(wave==null)return false;
                    waves.Add(cue,wave);
                }
                double voiceEnd=cue.StartsWith("human-",StringComparison.Ordinal)
                    ? now+(double)(wave.Length-44)/BitConverter.ToInt32(wave,28) : 0;
                if(capture){speechUntil=voiceEnd;lastPlayedAt=now;lastCue=cue;return true;}
                StopCore();
                stream=new MemoryStream(wave,false);
                player=new SoundPlayer(stream);
                // Only a small in-memory PCM file is loaded here. Playback uses
                // WinMM's asynchronous path and never waits for the sound to end.
                player.Load();
                player.Play();
                speechUntil=voiceEnd;
                lastPlayedAt=now;lastCue=cue;
                return true;
            } catch(Exception error) {
                if(error is OutOfMemoryException) throw;
                StopCore();
                return false;
            }
        }
    }

    public void Stop() { lock(gate) StopCore(); }

    byte[] LoadWave(string cue,double gain) {
        if(HasClip(cue)) {
            try { return ReadClip(ClipPath(cue),gain); }
            catch(IOException) {} catch(InvalidDataException) {} catch(UnauthorizedAccessException) {}
        }
        return BuildWave(cue,gain);
    }
    // 25 amplitude samples per second match the desktop render timer. Compute
    // them from the selected recording so replacement speech also lip-syncs.
    public double[] MouthTrack(string cue) {
        byte[] wave=LoadWave(cue,1);
        if(wave==null)return new double[0];
        int rate=BitConverter.ToInt32(wave,24), channels=BitConverter.ToInt16(wave,22);
        int stride=Math.Max(1,rate/25)*channels*2;
        var levels=new double[(wave.Length-44+stride-1)/stride];
        for(int i=0;i<levels.Length;i++) {
            int start=44+i*stride,end=Math.Min(wave.Length,start+stride);
            double energy=0;
            for(int j=start;j<end;j+=2){double value=BitConverter.ToInt16(wave,j)/32768.0;energy+=value*value;}
            levels[i]=Math.Max(0,Math.Min(1,(Math.Sqrt(energy*2/(end-start))-.012)*11));
        }
        return levels;
    }

    void StopCore() {
        speechUntil=0;
        if(player!=null) {
            try { player.Stop(); } catch(InvalidOperationException) { }
            player.Dispose();
            player=null;
        }
        if(stream!=null) { stream.Dispose(); stream=null; }
    }

    public void Dispose() {
        lock(gate) {
            if(disposed) return;
            disposed=true;
            StopCore();
            waves.Clear();
            clock.Stop();
        }
    }

    static double NormalizeVolume(double value) {
        if(Double.IsNaN(value) || Double.IsInfinity(value)) return .30;
        return Math.Max(0,Math.Min(1,value));
    }

    static bool KnownCue(string cue) {
        switch(cue) {
            case "cat-answer": case "focus-complete": case "human-hello": case "human-master":
            case "pet": case "heyxiu": case "snack": case "tabby-paws": case "tabby-leaf":
            case "bidiu-hop": case "bidiu-bite": case "master-sleeve": return true;
            default: return false;
        }
    }

    internal static bool HasBuiltIn(string cue) {
        return cue=="cat-answer" || cue=="focus-complete";
    }

    public static byte[] BuildWave(string cue,double volume) {
        if(!HasBuiltIn(cue)) return null;
        volume=NormalizeVolume(volume);
        if(cue=="cat-answer") {
            using(var recording=typeof(PetAudio).Assembly.GetManifestResourceStream("Xiaohei.CatAnswer.wav"))
                return ScalePcm(recording,volume);
        }
        const double duration=.70;
        int count=(int)Math.Round(duration*SampleRate);
        var samples=new double[count];
        double peak=0;
        for(int i=0;i<count;i++) {
            double t=(double)i/SampleRate;
            double edge=Math.Max(0,Math.Min(1,Math.Min(t/.009,(duration-t)/.045)));
            samples[i]=(Bell(t,659,.57)*.80+Bell(t-.19,880,.51)*.46)*Math.Sin(edge*Math.PI/2);
            peak=Math.Max(peak,Math.Abs(samples[i]));
        }
        using(var output=new MemoryStream(44+count*2))
        using(var writer=new BinaryWriter(output,Encoding.ASCII)) {
            WriteHeader(writer,1,SampleRate,count*2);
            double gain=.54*volume/peak;
            for(int i=0;i<count;i++) writer.Write((short)(i==0 || i==count-1?0:Math.Round(samples[i]*gain*Int16.MaxValue)));
            return output.ToArray();
        }
    }

    static void WriteHeader(BinaryWriter w,int channels,int rate,int bytes) {
        w.Write(0x46464952); w.Write(36+bytes); w.Write(0x45564157);
        w.Write(0x20746D66); w.Write(16); w.Write((short)1); w.Write((short)channels);
        w.Write(rate); w.Write(rate*channels*2); w.Write((short)(channels*2)); w.Write((short)16);
        w.Write(0x61746164); w.Write(bytes);
    }

    static double Bell(double t,double frequency,double duration) {
        if(t<0 || t>=duration) return 0;
        double phase=2*Math.PI*frequency*t;
        double attack=Math.Min(1,t/.006);
        return (Math.Sin(phase)+.19*Math.Sin(phase*2.01)+.045*Math.Sin(phase*4.2))*
               Math.Exp(-7*t/duration)*attack;
    }
}
}
