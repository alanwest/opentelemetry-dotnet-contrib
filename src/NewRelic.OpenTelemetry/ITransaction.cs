using System.Diagnostics;

namespace NewRelic.OpenTelemetry;

internal interface ITransaction
{
    Activity[] Spans { get; }

    bool OnEnd(Activity activity);
}
