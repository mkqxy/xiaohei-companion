using System;
using System.IO;
using System.Text;
using XiaoheiDesktop;

public static class AudioTests {
    static int assertions;
    static readonly string[] Cues={"pet","cat-answer","heyxiu","snack","focus-complete","tabby-paws","tabby-leaf","bidiu-hop","bidiu-bite","master-sleeve"};

    static void Check(bool value,string message) {
        assertions++;
        if(!value) throw new Exception(message);
    }
    static int Peak(byte[] wav) {
        int peak=0;
        for(int i=44;i<wav.Length;i+=2)
            peak=Math.Max(peak,Math.Abs((int)BitConverter.ToInt16(wav,i)));
        return peak;
    }
    static bool Same(byte[] a,byte[] b) {
        if(a.Length!=b.Length) return false;
        for(int i=0;i<a.Length;i++) if(a[i]!=b[i]) return false;
        return true;
    }

    public static void Main() {
        try {Run();} catch(Exception e) {Console.Error.WriteLine(e.GetType().Name+" "+e.Message);Console.Error.WriteLine(e.StackTrace);Environment.Exit(1);}
    }
    static void Run() {
        foreach(string cue in Cues) {
            byte[] wav=PetAudio.BuildWave(cue,1);
            using(var reader=new BinaryReader(new MemoryStream(wav),Encoding.ASCII)) {
                Check(Encoding.ASCII.GetString(reader.ReadBytes(4))=="RIFF",cue+" RIFF signature");
                Check(reader.ReadInt32()==wav.Length-8,cue+" RIFF byte count");
                Check(Encoding.ASCII.GetString(reader.ReadBytes(8))=="WAVEfmt ",cue+" PCM format chunk");
                Check(reader.ReadInt32()==16 && reader.ReadInt16()==1,cue+" PCM format");
                Check(reader.ReadInt16()==1 && reader.ReadInt32()==22050,cue+" mono sample rate");
                Check(reader.ReadInt32()==44100 && reader.ReadInt16()==2 && reader.ReadInt16()==16,cue+" sample layout");
                Check(Encoding.ASCII.GetString(reader.ReadBytes(4))=="data" && reader.ReadInt32()==wav.Length-44,cue+" audio data length");
            }
            double duration=(wav.Length-44)/44100.0;
            Check(duration>=.20 && duration<=1.5,cue+" remains a short cue");
            Check(Peak(wav)>10000 && Peak(wav)<21000,cue+" audible and keeps peak headroom");
            Check(Math.Abs((int)BitConverter.ToInt16(wav,44))<=1 && Math.Abs((int)BitConverter.ToInt16(wav,wav.Length-2))<=1,cue+" click-free boundaries within one quantization step");
            Check(Peak(PetAudio.BuildWave(cue,0))==0,cue+" zero volume is silent");
            Check(Math.Abs(Peak(PetAudio.BuildWave(cue,.3))/((double)Peak(wav))-.3)<.0001,cue+" volume affects PCM amplitude");
            Check(Same(wav,PetAudio.BuildWave(cue,1)),cue+" synthesis is repeatable");
        }
        Check(PetAudio.BuildWave(null,.3)==null && PetAudio.BuildWave("other",.3)==null,"Unknown cues are skipped");
        foreach(string silent in new[]{"jump","transform","tail","tabby","bidiu","walking"})
            Check(PetAudio.BuildWave(silent,.3)==null,silent+" no longer has a generic announcement");
        Check(typeof(PetAudio).Assembly.GetManifestResourceInfo("Xiaohei.CatAnswer.wav")!=null,"The group executable embeds the actual CC0 kitten call");
        using(var reader=new PetAudio()){
            foreach(string line in new[]{"human-hello","human-master"}) {
                Check(PetAudio.BuildWave(line,.3)==null,"No synthetic speech is bundled");
                Check(!reader.Play(line,false,true) && reader.MouthTrack(line).Length==0,"An absent voice cue stays silent");
            }
        }
        Check(Peak(PetAudio.BuildWave("pet",-1))==0,"Negative volume clamps to silence");
        Check(Same(PetAudio.BuildWave("pet",4),PetAudio.BuildWave("pet",1)),"Large volume clamps to 100%");
        Check(Same(PetAudio.BuildWave("pet",Double.NaN),PetAudio.BuildWave("pet",.3)),"NaN volume uses the default");
        Check(Same(PetAudio.BuildWave("pet",Double.PositiveInfinity),PetAudio.BuildWave("pet",.3)),"Infinite volume uses the default");

        using(var audio=new PetAudio()) {
            Check(audio.Enabled && audio.Volume==.3 && !audio.Suppressed,"Defaults are enabled, soft and unsuppressed");
            audio.Enabled=false;
            Check(!audio.Play("pet") && !audio.Play("pet",true),"Explicit mute applies to interactions and previews");
            audio.Enabled=true; audio.Volume=0;
            Check(!audio.Play("jump") && !audio.Play("jump",true),"Zero volume also blocks previews");
            audio.Volume=.3; audio.Suppressed=true;
            foreach(string cue in Cues) Check(!audio.Play(cue),cue+" is silent while suppressed");
            Check(!audio.Play("other",true),"Unknown preview is ignored");
            audio.Dispose();
            Check(!audio.Play("pet",true),"Disposed audio cannot play");
        }
        string root=Environment.GetEnvironmentVariable("XIAOHEI_DATA_DIR");
        string clip=Path.Combine(root,"voice.wav"), invalid=Path.Combine(root,"broken.wav");
        File.WriteAllBytes(clip,PetAudio.BuildWave("cat-answer",1));
        using(var audio=new PetAudio(Path.Combine(root,"sounds"))) {
            audio.ImportClip("human-master",clip);
            Check(audio.Play("human-master",false,true) && !audio.Play("human-master",false,true),"A personal voice plays once per interaction");
            Check(Array.Exists(audio.MouthTrack("human-master"),v=>v>.2),"The imported recording drives the mouth");
            Check(!audio.Play("tabby-leaf",false,true),"A nearby contact cannot interrupt a spoken line");
            audio.RestoreClip("human-master");
            Check(!audio.Play("human-master",true,true),"Removing the clip restores a silent greeting");
            audio.ImportClip("pet",clip);
            Check(audio.HasClip("pet")&&!audio.HasClip("snack"),"A replacement is limited to its selected action");
            string saved=Path.Combine(root,"sounds","pet.wav");
            Check(Same(File.ReadAllBytes(saved),File.ReadAllBytes(clip)),"Import preserves the original PCM samples");
            Check(Same(PetAudio.ReadClip(saved,.30),PetAudio.BuildWave("cat-answer",.30)),"The configured volume attenuates custom recordings equally");
            File.WriteAllBytes(invalid,new byte[]{1,2,3});
            bool rejected=false;
            try {audio.ImportClip("pet",invalid);}catch(InvalidDataException){rejected=true;}
            Check(rejected && Same(File.ReadAllBytes(saved),File.ReadAllBytes(clip)),"Invalid imports leave the previous custom sound intact");
            File.WriteAllBytes(invalid,PetAudio.BuildWave("snack",1));
            audio.ImportClip("pet",invalid);
            Check(Same(File.ReadAllBytes(saved),File.ReadAllBytes(invalid)),"An existing custom cue can be replaced");
            using(var reopened=new PetAudio(Path.Combine(root,"sounds")))Check(reopened.HasClip("pet"),"Clips persist across sessions");
            audio.RestoreClip("pet");audio.RestoreClip("pet");Check(!audio.HasClip("pet"),"Restore is repeatable and removes the custom cue only");
        }
        Console.WriteLine("PASS: "+assertions+" audio assertions (no audible playback).");
    }
}
