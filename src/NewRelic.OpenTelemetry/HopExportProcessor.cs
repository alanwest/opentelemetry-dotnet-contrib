// <copyright file="HopExportProcessor.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;

namespace NewRelic.OpenTelemetry;

/// <summary>
/// A span processor that batches span by hop.
/// </summary>
public class HopExportProcessor : BaseExportProcessor<Activity>
{
    internal const int DefaultScheduledDelayMilliseconds = 5000;
    internal const int DefaultExporterTimeoutMilliseconds = 30000;

    private readonly ConcurrentDictionary<string, KeyValuePair<string, Hop>> hops = new();
    private readonly Thread exporterThread;
    private readonly int scheduledDelayMilliseconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="HopExportProcessor"/> class.
    /// </summary>
    /// <param name="exporter">Exporter instance.</param>
    public HopExportProcessor(BaseExporter<Activity> exporter)
        : base(exporter)
    {
        this.scheduledDelayMilliseconds = DefaultScheduledDelayMilliseconds;
        this.exporterThread = new Thread(this.ExporterProc)
        {
            IsBackground = true,
            Name = $"OpenTelemetry-{nameof(HopExportProcessor)}-{exporter.GetType().Name}",
        };
        this.exporterThread.Start();
    }

    /// <inheritdoc />
    public override void OnStart(Activity data)
    {
        this.SetExporterParentProvider();

        if (data.IsHopStart(out var reason))
        {
            this.hops.TryAdd(data.Id, new(data.Id, new Hop(data)));
            HopExportProcessorEventSource.Log.Stuff($"Root span started {data.Id}: {reason} {data.DisplayName}");
        }
        else if (this.hops.TryGetValue(data.ParentId, out var hopEntry))
        {
            hopEntry.Value.SpanStart(data);
            this.hops.TryAdd(data.Id, hopEntry);
            HopExportProcessorEventSource.Log.Stuff($"Span started {data.Id}: {data.DisplayName}");
        }
        else
        {
            HopExportProcessorEventSource.Log.Stuff($"OnStart no hop ParentId={data.ParentId}, ParentSpanId={data.ParentSpanId}, Id={data.Id}, SpanId={data.SpanId}, DisplayName={data.DisplayName}");
        }
    }

    /// <inheritdoc />
    public override void OnEnd(Activity data)
    {
        var hopFound = this.hops.TryGetValue(data.Id, out var hopEntry);
        if (hopFound)
        {
            var hop = hopEntry.Value;
            hop.SpanEnd(data);
            HopExportProcessorEventSource.Log.Stuff($"Span ended {data.Id}: {data.DisplayName}");
        }
        else
        {
            HopExportProcessorEventSource.Log.Stuff($"OnEnd no hop ParentId={data.ParentId}, ParentSpanId={data.ParentSpanId}, Id={data.Id}, SpanId={data.SpanId}, DisplayName={data.DisplayName}");
        }
    }

    /// <inheritdoc />
    protected override void OnExport(Activity data)
    {
    }

    private void ExporterProc()
    {
        while (true)
        {
            Thread.Sleep(this.scheduledDelayMilliseconds);
            foreach (var hopEntry in this.hops.ToArray())
            {
                var hop = hopEntry.Value.Value;
                if (hop.TryFinish(out var spans))
                {
                    foreach (var span in spans)
                    {
                        this.hops.TryRemove(span.Id, out var _);
                    }

                    using var batch = new Batch<Activity>(spans, spans.Length);
                    var result = this.exporter.Export(batch);

                    HopExportProcessorEventSource.Log.Stuff($"Export batch {spans[0].Id}: {spans.Length} span(s)");
                }
            }
        }
    }

    private void SetExporterParentProvider()
    {
        this.exporter.GetType().GetProperty("ParentProvider").GetSetMethod(true).Invoke(this.exporter, new[] { this.ParentProvider });
    }
}
