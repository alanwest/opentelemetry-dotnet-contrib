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
    private readonly object mutex = new();

    private ConcurrentDictionary<string, KeyValuePair<string, IHop>> hops = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HopExportProcessor"/> class.
    /// </summary>
    /// <param name="exporter">Exporter instance.</param>
    public HopExportProcessor(BaseExporter<Activity> exporter)
        : base(exporter)
    {
    }

    /// <inheritdoc />
    public override void OnStart(Activity data)
    {
        this.SetExporterParentProvider();

        if (data.IsHopStart(out var reason))
        {
            var hop = new Hop();
            this.hops.TryAdd(data.Id, new(data.Id, hop));
            HopExportProcessorEventSource.Log.Stuff($"Start hop {hop.HopId}: {reason} {data.DisplayName}");
        }
        else if (this.hops.TryGetValue(data.ParentId, out var hopEntry))
        {
            var hop = hopEntry.Value;
            this.hops.TryAdd(data.Id, hopEntry);
            HopExportProcessorEventSource.Log.Stuff($"Span started {hop.HopId}: {data.DisplayName}");
        }
        else
        {
            HopExportProcessorEventSource.Log.Stuff($"OnStart no hop {data.DisplayName}");
        }
    }

    /// <inheritdoc />
    protected override void OnExport(Activity data)
    {
        var hopFound = this.hops.TryGetValue(data.Id, out var hopEntry);
        if (hopFound)
        {
            var hop = hopEntry.Value;
            if (hop.SpanEnd(data))
            {
                var spans = hop.Spans;
                using var batch = new Batch<Activity>(spans, spans.Length);
                lock (this.mutex)
                {
                    var result = this.exporter.Export(batch);
                }

                foreach (var span in spans)
                {
                    this.hops.TryRemove(span.Id, out var _);
                }

                HopExportProcessorEventSource.Log.Stuff($"End hop {hop.HopId}: Count={spans.Length}");
            }
            else
            {
                HopExportProcessorEventSource.Log.Stuff($"Span ended {hop.HopId}: {data.DisplayName}");
            }
        }
        else
        {
            HopExportProcessorEventSource.Log.Stuff($"OnEnd no hop {data.DisplayName}");
        }
    }

    private void SetExporterParentProvider()
    {
        this.exporter.GetType().GetProperty("ParentProvider").GetSetMethod(true).Invoke(this.exporter, new[] { this.ParentProvider });
    }
}
