using System.Diagnostics;

namespace NewRelic.OpenTelemetry;

internal sealed class Hop : ITransaction
{
    private static readonly ITransaction Noop = new NoopTransaction();
    private static readonly AsyncLocal<ITransaction> current = new AsyncLocal<ITransaction>();

    private List<Activity> spans = new List<Activity>();

    public Activity[] Spans => this.spans.ToArray();

    public static ITransaction Current
    {
        get => current.Value ?? Noop;
        set => current.Value = value != null ? value : Noop;
    }

    public static Hop StartHop()
    {
        var hop = new Hop();
        Current = hop;
        return hop;
    }

    public bool OnEnd(Activity activity)
    {
        this.spans.Add(activity);
        return activity.IsTransactionStart();
    }
}
