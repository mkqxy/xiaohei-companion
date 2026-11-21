using System;
using System.Windows.Media;

namespace XiaoheiDesktop {
public sealed partial class PetWindow {
    readonly RotateTransform carryTurn = new RotateTransform();
    readonly ScaleTransform landingSquash = new ScaleTransform();
    readonly TranslateTransform carryLift = new TranslateTransform();
    bool wasCarried;
    double landedAt = Double.NegativeInfinity;

    void BuildCarryMotion() {
        var motion = new TransformGroup();
        motion.Children.Add(landingSquash);
        motion.Children.Add(carryTurn);
        motion.Children.Add(carryLift);
        art.RenderTransform = motion;
    }

    void UpdateCarryMotion(double now) {
        if (wasCarried && !dragging) landedAt = now;
        wasCarried = dragging;
        carryTurn.CenterX = art.ActualWidth / 2;
        carryTurn.CenterY = art.ActualHeight * .32;
        landingSquash.CenterX = art.ActualWidth / 2;
        landingSquash.CenterY = art.ActualHeight;
        carryTurn.Angle = dragging ? Math.Sin(now * 8) * 3 : 0;
        carryLift.Y = dragging ? -3 * Pref.Scale / .36 : 0;
        double elapsed = now - landedAt;
        double bounce = elapsed >= 0 && elapsed < .42 ? Math.Sin(Math.PI * elapsed / .42) : 0;
        landingSquash.ScaleX = 1 + bounce * .055;
        landingSquash.ScaleY = 1 - bounce * .085;
    }
}
}
