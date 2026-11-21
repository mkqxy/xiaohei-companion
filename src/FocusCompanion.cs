using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace XiaoheiDesktop {
public sealed partial class PetWindow {
    private Button focusBadge;
    public bool FocusPaused {get{return !Timer.Running && (Pref.FocusPausedTicks>0);}}
    private void BuildFocusBadge() {
        focusBadge=UI.Button("",OpenTools,false);
        focusBadge.HorizontalAlignment=HorizontalAlignment.Center;
        focusBadge.VerticalAlignment=VerticalAlignment.Top;
        focusBadge.Margin=new Thickness(0,20,0,0);
        focusBadge.Padding=new Thickness(12,5,12,5);
        focusBadge.ToolTip="打开专注计时";focusBadge.Visibility=Visibility.Collapsed;
        root.Children.Add(focusBadge);
    }
    private void UpdateFocusBadge() {
        if(focusBadge==null)return;
        bool shown=(Timer.Running||FocusPaused)&&bubble.Visibility!=Visibility.Visible;
        focusBadge.Visibility=shown?Visibility.Visible:Visibility.Collapsed;
        if(shown)focusBadge.Content=(Timer.Running?"专注中  ":"已暂停  ")+Timer.Display;
    }
    private void RestoreFocus(DateTime now) {
        if(Pref.FocusDuration<1||Pref.FocusDuration>180)return;
        var maximum=TimeSpan.FromMinutes(Pref.FocusDuration);
        if(Pref.FocusDeadlineTicks>0 && Pref.FocusDeadlineTicks<=DateTime.MaxValue.Ticks){
            DateTime deadline=new DateTime(Pref.FocusDeadlineTicks,DateTimeKind.Utc);
            // An expired offline session completes once on the next UI tick.
            if(deadline-now<=maximum){Timer.Reset(Pref.FocusDuration);Timer.Deadline=deadline;Timer.Remaining=deadline>now?deadline-now:TimeSpan.Zero;return;}
        }
        if(Pref.FocusPausedTicks>0 && Pref.FocusPausedTicks<=maximum.Ticks){
            Timer.Reset(Pref.FocusDuration);Timer.Remaining=TimeSpan.FromTicks(Pref.FocusPausedTicks);
        }else Pref.FocusPausedTicks=0;
        Pref.FocusDeadlineTicks=0;
    }
    private void CommitFocus(bool paused) {
        Pref.FocusDuration=Timer.SelectedMinutes;
        Pref.FocusDeadlineTicks=Timer.Deadline.HasValue?Timer.Deadline.Value.Ticks:0;
        Pref.FocusPausedTicks=paused?Timer.Remaining.Ticks:0;
        UpdateFocusBadge();Save();RefreshPocket();
    }
    public void StartFocus(int minutes) {
        if(minutes<1||minutes>180)throw new ArgumentOutOfRangeException("minutes");
        Timer.Reset(minutes);Profile.focusMinutes=minutes;
        ResumeFocus();
    }
    public void ResumeFocus() {
        if(Timer.Running)return;
        ClearActionSound();art.CancelActing();StopGuestPlay();art.CancelTail();WakeForInteraction();Companion.Sleeping=false;autoSleeping=false;
        art.ClearSnack();animationUntil=0;
        bubble.Visibility=Visibility.Collapsed;bubbleUntil=DateTime.MinValue;
        Timer.Start(DateTime.UtcNow);UpdateAudioSettings();CommitFocus(false);
        if (pocket != null && pocket.IsVisible) {
            pocket.FlushNote();
            if (!pocket.HasUnsavedNote) pocket.Hide();
            else Say("专注已开始。随手记还没保存好，口袋先留在这里。", 8, true);
        }
    }
    public void PauseFocus() {
        if(!Timer.Running)return;
        var now=DateTime.UtcNow;
        if(Timer.Tick(now)){FinishFocus();return;}
        Timer.Pause(now);CommitFocus(true);
    }
    public void EndFocus() {
        Timer.Reset(Timer.SelectedMinutes);CommitFocus(false);
    }
    private void FinishFocus() {
        CommitFocus(false);lastInteraction=DateTime.UtcNow;
        PlaySound("focus-complete");
        if(Pref.WaterMinutes>0 && nextWater<=lastInteraction)
            nextWater=lastInteraction.AddMinutes(Pref.WaterMinutes);
        if(!Companion.Sleeping)PlayAnimation("waving",null);
        if(Pref.Quiet)Say("专注完成，起来伸个懒腰吧。",10,true);
        else Notify("已陪你专注 "+Timer.SelectedMinutes+" 分钟","看看远处，活动一下肩膀吧。");
    }
    public void OpenTools() {OpenPocket();pocket.ShowTab("小工具");}
}
public sealed partial class PocketWindow {
    private TextBox focusMinutesBox;
    private TextBlock focusHint;
    private UniformGrid focusPresets;
    private Button endButton;
    private FrameworkElement MakeFocusCard() {
        var card=UI.Stack();card.Children.Add(UI.Heading("专注陪伴"));
        focusText=UI.Text(pet.Timer.Display,36,UI.Ink);focusText.HorizontalAlignment=HorizontalAlignment.Center;
        focusText.FontFamily=new FontFamily("Segoe UI");card.Children.Add(focusText);
        focusHint=UI.Text("",12,UI.Muted);focusHint.Margin=new Thickness(0,3,0,9);focusHint.HorizontalAlignment=HorizontalAlignment.Center;
        card.Children.Add(focusHint);
        var duration=new DockPanel();
        focusMinutesBox=new TextBox{Text=pet.Timer.SelectedMinutes.ToString(),MaxLength=3,Width=49,Padding=new Thickness(5,4,5,4),
            VerticalContentAlignment=VerticalAlignment.Center,HorizontalContentAlignment=HorizontalAlignment.Center,ToolTip="自定义时长，1–180 分钟"};
        System.Windows.Automation.AutomationProperties.SetName(focusMinutesBox,"自定义专注分钟");
        var minutes=UI.Text(" 分钟",13,UI.Muted);minutes.VerticalAlignment=VerticalAlignment.Center;minutes.Margin=new Thickness(3,0,7,0);
        DockPanel.SetDock(focusMinutesBox,Dock.Left);duration.Children.Add(focusMinutesBox);
        DockPanel.SetDock(minutes,Dock.Left);duration.Children.Add(minutes);
        focusPresets=new UniformGrid{Columns=3};
        foreach(int n in new[]{5,25,45}){
            var choice=n;var preset=UI.Button(n+" 分钟",delegate{
                pet.Timer.Reset(choice);focusMinutesBox.Text=choice.ToString();RefreshLive();
            },false);
            preset.Padding=new Thickness(0,5,0,5);preset.Margin=new Thickness(3,0,0,0);focusPresets.Children.Add(preset);
        }
        duration.Children.Add(focusPresets);card.Children.Add(duration);
        var row=new UniformGrid{Columns=2,Margin=new Thickness(0,9,0,0)};
        focusButton=UI.Button("开始陪伴",delegate{
            if(pet.Timer.Running)pet.PauseFocus();
            else if(pet.FocusPaused)pet.ResumeFocus();
            else{
                int chosen;
                if(!Int32.TryParse(focusMinutesBox.Text,out chosen)||chosen<1||chosen>180){focusHint.Text="请输入 1–180 分钟。";focusMinutesBox.Focus();focusMinutesBox.SelectAll();return;}
                pet.StartFocus(chosen);
            }
            RefreshLive();
        },true);
        focusButton.Margin=new Thickness(0,0,5,0);row.Children.Add(focusButton);
        endButton=UI.Button("重置",delegate{pet.EndFocus();focusMinutesBox.Text=pet.Timer.SelectedMinutes.ToString();RefreshLive();},false);
        row.Children.Add(endButton);card.Children.Add(row);return UI.Card(card);
    }
    private void UpdateFocusControls(){
        if(focusButton==null)return;
        bool active=pet.Timer.Running||pet.FocusPaused;
        focusMinutesBox.IsEnabled=!active;focusPresets.IsEnabled=!active;
        focusText.Text=pet.Timer.Display;
        focusButton.Content=pet.Timer.Running?"暂停":pet.FocusPaused?"继续陪伴":"开始陪伴";
        endButton.Content=active?"结束本轮":"重置";
        if(active||!focusMinutesBox.IsKeyboardFocused)
            focusHint.Text=pet.Timer.Running?"剩余时间也会显示在小黑头顶。":pet.FocusPaused?"已暂停，准备好后再继续。":pet.Timer.Remaining==TimeSpan.Zero?"这一轮完成了，休息一下吧。":"选好分钟数，小黑会收起口袋陪你专注。";
    }
}
}
