// <copyright file="HopExportProcessorEventSource.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics.Tracing;

namespace NewRelic.OpenTelemetry;

[EventSource(Name = "OpenTelemetry-NewRelic-HopExportProcessor")]
internal sealed class HopExportProcessorEventSource : EventSource
{
    public static readonly HopExportProcessorEventSource Log = new HopExportProcessorEventSource();

    [Event(1, Message = "Exporter failed to send trace data. Exception: {0}", Level = EventLevel.Error)]
    public void Stuff(string stuff)
    {
        this.WriteEvent(1, stuff);
    }
}
