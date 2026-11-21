using System;
using XiaoheiDesktop;

public static class ClockTests
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    public static void Main()
    {
        DateTime start = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        FocusClock clock = new FocusClock();
        Check(!clock.Running && clock.Remaining == TimeSpan.FromMinutes(25),
            "A new timer should be stopped at 25 minutes.");

        clock.Start(start);
        Check(clock.Running && clock.Deadline == start.AddMinutes(25),
            "Starting should establish a deadline.");
        Check(!clock.Tick(start.AddSeconds(90)) && clock.Remaining == TimeSpan.FromSeconds(1410),
            "A delayed tick must account for all 90 elapsed seconds.");
        Check(clock.Display == "23:30", "Display should reflect the actual deadline remainder.");

        clock.Pause(start.AddSeconds(100));
        TimeSpan paused = clock.Remaining;
        Check(!clock.Running && paused == TimeSpan.FromSeconds(1400),
            "Pause should capture the exact remainder, including time since the last tick.");
        Check(!clock.Tick(start.AddHours(12)) && clock.Remaining == paused,
            "Paused time must remain frozen across future ticks.");
        clock.Start(start.AddHours(12));
        Check(clock.Deadline == start.AddHours(12).Add(paused),
            "Resuming should schedule the frozen remainder from the new start time.");

        clock.Reset(45);
        Check(!clock.Running && clock.Remaining == TimeSpan.FromMinutes(45) && clock.Display == "45:00",
            "Reset should stop the timer and select the requested duration.");
        var selectedField = typeof(FocusClock).GetField("SelectedMinutes");
        if (selectedField != null)
            Check((int)selectedField.GetValue(clock) == 45,
                "Reset should preserve the selected duration when the API supports it.");

        clock.Reset(5);
        clock.Start(start);
        Check(!clock.Tick(start.AddMinutes(5).AddTicks(-1)) && clock.Running,
            "The timer must remain active immediately before its deadline.");
        Check(clock.Tick(start.AddMinutes(5)) && !clock.Running && clock.Remaining == TimeSpan.Zero,
            "The timer must finish exactly at its deadline.");
        Check(clock.Display == "00:00", "A completed timer should display zero without negative time.");
        Check(!clock.Tick(start.AddMinutes(6)) && clock.Remaining == TimeSpan.Zero,
            "Completion should be reported once, not on every later tick.");

        clock.Reset(5);
        clock.Start(start);
        Check(clock.Tick(start.AddHours(1)) && !clock.Running && clock.Remaining == TimeSpan.Zero,
            "A late tick after a long suspension must complete immediately and clamp at zero.");
        clock.Reset(45);clock.Start(start);clock.Tick(start.AddMinutes(45));clock.Start(start.AddHours(1));
        Check(clock.Deadline==start.AddHours(1).AddMinutes(45),"Restarting a completed timer retains its selected duration.");
        Console.WriteLine("PASS: " + assertions + " countdown assertions.");
    }
}
