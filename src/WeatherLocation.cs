using System;
using System.Device.Location;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace XiaoheiDesktop {
// The weather API only needs a nearby forecast cell. The OS position does
// not leave this class; only tenths of a degree are kept in memory.
public sealed class WeatherArea {
    public readonly double Latitude, Longitude;
    public WeatherArea(double latitude, double longitude) {
        if (Double.IsNaN(latitude) || latitude < -90 || latitude > 90 ||
            Double.IsNaN(longitude) || longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException("latitude", "位置数据不可用。");
        Latitude = Math.Round(latitude, 1, MidpointRounding.AwayFromZero);
        Longitude = Math.Round(longitude, 1, MidpointRounding.AwayFromZero);
    }
    public bool Matches(WeatherArea other) {
        return other != null && Latitude == other.Latitude && Longitude == other.Longitude;
    }
}

public sealed class LocationUnavailable : Exception {
    public LocationUnavailable(string message) : base(message) {}
}

public static class WindowsWeatherLocation {
    static readonly object gate = new object();
    static Task<WeatherArea> inProgress;
    public static Task<WeatherArea> ReadAsync() {
        lock (gate) {
            if (inProgress == null || inProgress.IsCompleted) inProgress = Task.Run((Func<WeatherArea>)Read);
            return inProgress;
        }
    }
    static WeatherArea Read() {
        var timeout = TimeSpan.FromSeconds(7);
        var elapsed = Stopwatch.StartNew();
        using (var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default)) {
            watcher.TryStart(true, TimeSpan.FromSeconds(3));
            do {
                if (watcher.Permission == GeoPositionPermission.Denied)
                    throw new LocationUnavailable("Windows 未允许定位，可输入城市。");
                if (watcher.Status == GeoPositionStatus.Disabled)
                    throw new LocationUnavailable("设备定位未开启，可输入城市。");
                var position = watcher.Position;
                if (watcher.Permission == GeoPositionPermission.Granted && position != null && !position.Location.IsUnknown &&
                    position.Timestamp > DateTimeOffset.Now.AddMinutes(-30)) {
                    return new WeatherArea(position.Location.Latitude, position.Location.Longitude);
                }
                Thread.Sleep(140);
            } while (elapsed.Elapsed < timeout);
            throw new LocationUnavailable("暂时没有可用位置，可输入城市。");
        }
    }
}
}
