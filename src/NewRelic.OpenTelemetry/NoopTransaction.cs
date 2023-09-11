using System.Diagnostics;

namespace NewRelic.OpenTelemetry;

internal sealed class NoopTransaction : ITransaction
{
    public Activity[] Spans => Array.Empty<Activity>();

    public bool OnEnd(Activity activity)
    {
        return false;
    }
}
