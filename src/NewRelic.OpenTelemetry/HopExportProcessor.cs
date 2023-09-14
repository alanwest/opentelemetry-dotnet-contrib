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

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;

namespace NewRelic.OpenTelemetry;

/// <summary>
/// A span processor that batches span by hop.
/// </summary>
public class HopExportProcessor : BaseExportProcessor<Activity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HopExportProcessor"/> class.
    /// </summary>
    /// <param name="exporter">Exporter instance.</param>
    public HopExportProcessor(OtlpExporterOptions options)
        : base(new OpenTelemetry.Exporter.OtlpTraceExporter(options))
    {
        HopExportProcessorEventSource.Log.Stuff($"Initialized. Endpoint={options.Endpoint}");
    }

    /// <inheritdoc />
    public override void OnStart(Activity data)
    {
        if (data != null && data.IsTransactionStart())
        {
            Hop.StartHop();
        }
    }

    /// <inheritdoc />
    public override void OnEnd(Activity data)
    {
        this.OnExport(data);
    }

    /// <inheritdoc />
    protected override void OnExport(Activity data)
    {
        var hop = Hop.Current;

        HopExportProcessorEventSource.Log.Stuff($"Span ended: {data.DisplayName}");

        if (hop.OnEnd(data))
        {
            HopExportProcessorEventSource.Log.Stuff($"Exporting spans. Count={hop.Spans.Length}");
            using var batch = new Batch<Activity>(hop.Spans, hop.Spans.Length);
            var result = this.exporter.Export(batch);
        }
    }
}
