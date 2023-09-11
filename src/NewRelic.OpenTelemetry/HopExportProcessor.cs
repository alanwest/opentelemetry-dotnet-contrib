using System.Diagnostics;
using OpenTelemetry;

namespace NewRelic.OpenTelemetry;

public class HopExportProcessor : BaseExportProcessor<Activity>
{
    public HopExportProcessor(BaseExporter<Activity> exporter)
        : base(exporter)
    {
    }

    public override void OnStart(Activity activity)
    {
        if (activity.IsTransactionStart())
        {
            Hop.StartHop();
        }
    }

    public override void OnEnd(Activity activity)
    {
        this.OnExport(activity);
    }

    protected override void OnExport(Activity activity)
    {
        var hop = Hop.Current;
        if (hop.OnEnd(activity))
        {
            using (var batch = new Batch<Activity>(hop.Spans, hop.Spans.Length))
            {
                this.exporter.Export(batch);
            }
        }
    }
}
