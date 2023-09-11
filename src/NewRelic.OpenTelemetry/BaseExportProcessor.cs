// <copyright file="BaseExportProcessor.cs" company="OpenTelemetry Authors">
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

#pragma warning disable CA1051
#pragma warning disable CA1708
#pragma warning disable CS1591
#nullable enable

using OpenTelemetry;

namespace NewRelic.OpenTelemetry;

/// <summary>
/// Implements processor that exports telemetry objects.
/// </summary>
/// <typeparam name="T">The type of telemetry object to be exported.</typeparam>
public abstract class BaseExportProcessor<T> : BaseProcessor<T>
    where T : class
{
    protected readonly BaseExporter<T> exporter;
    private readonly string friendlyTypeName;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseExportProcessor{T}"/> class.
    /// </summary>
    /// <param name="exporter">Exporter instance.</param>
    protected BaseExportProcessor(BaseExporter<T> exporter)
    {
        // Guard.ThrowIfNull(exporter);

        this.friendlyTypeName = $"{this.GetType().Name}{{{this.exporter!.GetType().Name}}}";
        this.exporter = exporter;
    }

    internal BaseExporter<T> Exporter => this.exporter;

    /// <inheritdoc />
    public override string ToString()
        => this.friendlyTypeName;

    /// <inheritdoc />
    public override void OnStart(T data)
    {
    }

    /// <inheritdoc />
    public override void OnEnd(T data)
    {
        this.OnExport(data);
    }

    // internal override void SetParentProvider(BaseProvider parentProvider)
    // {
    //     base.SetParentProvider(parentProvider);
    //     this.exporter.ParentProvider = parentProvider;
    // }

    /// <summary>
    /// Invoked on export.
    /// </summary>
    /// <param name="data">The data to export.</param>
    protected abstract void OnExport(T data);

    /// <inheritdoc />
    protected override bool OnForceFlush(int timeoutMilliseconds)
    {
        return this.exporter.ForceFlush(timeoutMilliseconds);
    }

    /// <inheritdoc />
    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        return this.exporter.Shutdown(timeoutMilliseconds);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                try
                {
                    this.exporter.Dispose();
                }
                catch (Exception)
                {
                    // OpenTelemetrySdkEventSource.Log.SpanProcessorException(nameof(this.Dispose), ex);
                }
            }

            this.disposed = true;
        }

        base.Dispose(disposing);
    }
}
#pragma warning restore CS1591
#pragma warning restore CA1708
#pragma warning restore CA1051
