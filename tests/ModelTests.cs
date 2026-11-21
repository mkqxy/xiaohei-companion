using System;
using XiaoheiDesktop;

public static class ModelTests
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    public static void Main()
    {
        DateTime now = new DateTime(2026, 11, 20, 12, 0, 0);
        CompanionModel model = new CompanionModel(delegate { return now; });
        Check(model.StartedAt == now, "StartedAt should use the supplied clock.");

        model.Feed();
        int affection = model.Affection;
        model.Feed();
        Check(model.Snacks == 1 && model.Affection == affection,
            "Rapid feeding must not add snacks or affection.");
        now = now.AddSeconds(24.9);
        model.Feed();
        Check(model.Snacks == 1, "Feed cooldown must remain active before 25s.");
        now = now.AddSeconds(0.1);
        model.Feed();
        Check(model.Snacks == 2, "Feed cooldown must end at 25s.");

        model.Play();
        double energy = model.Energy;
        model.Play();
        Check(model.Energy == energy, "Rapid play must not consume energy twice.");
        now = now.AddSeconds(8);
        model.Play();
        Check(model.Energy == energy - 6, "Play should become available at 8s.");

        model.Stroke();
        model.Stroke();
        Check(model.Pets == 1, "A rapid duplicate stroke should not inflate counters.");
        now = now.AddSeconds(0.6);
        model.Stroke();
        Check(model.Pets == 2, "Stroke should become available at 0.6s.");

        model.ToggleSleep();
        Check(model.Sleeping, "ToggleSleep should enter sleep.");
        now = now.AddSeconds(1);
        model.Stroke();
        Check(model.Sleeping, "Petting a sleeping companion should not wake it.");
        model.Tick(now);
        energy = model.Energy;
        now = now.AddSeconds(10);
        model.Tick(now);
        Check(Math.Abs(model.Energy - Math.Min(100, energy + 2.5)) < 0.00001,
            "Sleep should gently restore energy based on elapsed time.");

        now = now.AddSeconds(8);
        model.Play();
        Check(!model.Sleeping, "An explicit play action should wake the companion.");
        model.Affection = 999;
        model.Energy = -20;
        model.Snacks = -1;
        model.Pets = -1;
        Check(model.Affection == 100 && model.Energy == 0 && model.Snacks == 0 && model.Pets == 0,
            "Restored public values must be clamped to their valid ranges.");

        model.Energy = Double.NaN;
        Check(!Double.IsNaN(model.Energy), "Invalid saved energy must not contaminate state.");
        model.Affection = 80;
        now = now.AddDays(1);
        model.Tick(now);
        Check(model.Affection == 80 && model.Energy <= 100,
            "Absence must never reduce affection and energy must remain bounded.");
        energy = model.Energy;
        model.Tick(now.AddHours(-1));
        Check(model.Energy == energy, "A backward clock adjustment must not drain energy.");
        Console.WriteLine("PASS: " + assertions + " companion-state assertions.");
    }
}
