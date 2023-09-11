using System.Diagnostics;

namespace NewRelic.OpenTelemetry;

internal static class ActivityExtensions
{
    public static bool IsTransactionStart(this Activity activity)
    {
        return activity.Kind == ActivityKind.Server
            || activity.Kind == ActivityKind.Consumer
            || activity.ParentSpanId == default;
    }
}
