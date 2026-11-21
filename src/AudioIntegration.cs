using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace XiaoheiDesktop {
public sealed partial class PetWindow {
    internal readonly PetAudio audio = new PetAudio(Path.Combine(Store.Dir,"sounds"));
    public event Action<string> SoundCueRequested;
    public bool CaptureSoundCues {get;set;}
    string pendingSound;
    double soundAt;

    void CueAfter(string cue,double delay) {
        pendingSound=cue;soundAt=art.Clock+delay;
    }
    void UpdateSoundCue() {
        if(pendingSound==null || art.Clock<soundAt)return;
        string cue=pendingSound;pendingSound=null;
        if(art.Clock-soundAt<.26)PlaySound(cue);
    }
    void ClearActionSound() {
        pendingSound=null;
        if(art.ClearMouth())audio.Stop();
    }

    void UpdateAudioSettings() {
        audio.Enabled = Pref.SoundEnabled;
        audio.Volume = Pref.SoundVolume;
        Pref.SoundVolume = audio.Volume;
        audio.Suppressed = Pref.Quiet || Companion.Sleeping || Timer.Running || !IsVisible;
        if(audio.Suppressed || (!CaptureSoundCues && (!audio.Enabled || audio.Volume==0)))art.ClearMouth();
    }

    public bool PlaySound(string cue) {
        UpdateAudioSettings();
        if(!audio.Play(cue,false,CaptureSoundCues))return false;
        if(cue=="cat-answer" || cue=="human-hello" || cue=="human-master") {
            art.BeginMouth(audio.MouthTrack(cue));
        }
        var notify=SoundCueRequested;if(notify!=null)notify(cue);
        return true;
    }
    void RespondAsCat(string cue) {
        if(!art.HumanForm && !art.UsingCustomSkin)PlaySound(cue);
    }

    public void SetSoundEnabled(bool enabled) {
        Pref.SoundEnabled = enabled;
        UpdateAudioSettings();
        Save();
    }

    public void SetSoundVolume(double volume) {
        Pref.SoundVolume = volume;
        UpdateAudioSettings();
        scaleSave.Stop();
        scaleSave.Start();
    }

    public bool PreviewSound(string cue="pet") {
        UpdateAudioSettings();
        return audio.Play(cue, true);
    }
}

public sealed partial class PocketWindow {
    FrameworkElement AudioSettings() {
        var sound = UI.Stack();
        sound.Children.Add(UI.Heading("小黑的声音"));
        sound.Children.Add(UI.Check("互动音效", pet.Pref.SoundEnabled, pet.SetSoundEnabled));
        var level = UI.Text("", 12, UI.Muted);
        var volume = new Slider {
            Minimum = 0, Maximum = 100, Value = pet.Pref.SoundVolume * 100,
            TickFrequency = 1, IsSnapToTickEnabled = true,
            Margin = new Thickness(0, 7, 0, 8)
        };
        AutomationProperties.SetName(volume, "音效音量");
        Action display = delegate {level.Text = "音量  " + Math.Round(volume.Value) + "%";};
        volume.ValueChanged += delegate {pet.SetSoundVolume(volume.Value / 100);display();};
        display();
        sound.Children.Add(level);
        sound.Children.Add(volume);
        sound.Children.Add(UI.Text("猫形应声使用实录。伙伴互动默认安静。", 12, UI.Muted));
        var cues=new[]{"cat-answer","human-hello","human-master","pet","heyxiu","snack","focus-complete","tabby-paws","tabby-leaf","bidiu-hop","bidiu-bite","master-sleeve"};
        var pick=new ComboBox{ItemsSource=new[]{"猫形应声","人形自我介绍","人形喊师父","长按呼噜","嘿咻回应","咬小鱼干","专注结束","皇受落爪","皇受扑叶","比丢落地","比丢吃果子","无限衣袖"},SelectedIndex=0,Margin=new Thickness(0,9,0,0)};
        AutomationProperties.SetName(pick,"选择互动声音");
        Func<string> selected=delegate{return cues[pick.SelectedIndex];};
        sound.Children.Add(pick);
        var preview = UI.Button("听听小黑的声音", delegate {pet.PreviewSound(selected());}, false);
        preview.Margin = new Thickness(0, 9, 0, 0);
        sound.Children.Add(preview);
        var status=UI.Text("",12,UI.Muted);
        Button restore=null;
        Action refresh=null;
        restore=UI.Button("恢复默认",delegate {
            try { pet.audio.RestoreClip(selected());refresh(); }
            catch(Exception e) {status.Text="恢复失败："+e.Message;}
        },false);
        refresh=delegate {
            bool custom=pet.audio.HasClip(selected()),speech=selected().StartsWith("human-");
            preview.IsEnabled=custom||PetAudio.HasBuiltIn(selected());
            restore.IsEnabled=custom;
            status.Text=custom?"正在使用你选的录音":speech?"未导入原声，互动时显示台词。":PetAudio.HasBuiltIn(selected())?"正在使用内置声音":"默认安静，可导入自己的录音。";
            preview.Content=!preview.IsEnabled?"尚未导入录音":"听听小黑的声音";
        };
        var import=UI.Button("换成本地 WAV",delegate {
            var dialog=new Microsoft.Win32.OpenFileDialog{Title="选择 5 秒以内的 16 位 PCM WAV",Filter="WAV 短音效 (*.wav)|*.wav"};
            if(dialog.ShowDialog(this)!=true)return;
            try {pet.audio.ImportClip(selected(),dialog.FileName);refresh();pet.PreviewSound(selected());}
            catch(Exception e) {status.Text="未能导入："+e.Message;}
        },false);
        pick.SelectionChanged+=delegate{refresh();};
        var buttons=new System.Windows.Controls.Primitives.UniformGrid{Columns=2,Margin=new Thickness(0,6,0,6)};
        buttons.Children.Add(import);buttons.Children.Add(restore);
        var more=UI.Stack();more.Children.Add(buttons);more.Children.Add(status);
        sound.Children.Add(new Expander{Header="自定义声音",Content=more,Margin=new Thickness(0,8,0,0)});
        refresh();
        return UI.Card(sound);
    }
}
}
