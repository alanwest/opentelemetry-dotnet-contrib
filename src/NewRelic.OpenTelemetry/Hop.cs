// <copyright file="Hop.cs" company="OpenTelemetry Authors">
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

namespace NewRelic.OpenTelemetry;

internal sealed class Hop : ITransaction
{
    private static readonly ITransaction Noop = new NoopTransaction();
    private static readonly AsyncLocal<ITransaction> CurrentPrivate = new AsyncLocal<ITransaction>();

    private List<Activity> spans = new List<Activity>();

    public static ITransaction Current
    {
        get => CurrentPrivate.Value ?? Noop;
        set => CurrentPrivate.Value = value != null ? value : Noop;
    }

    public Activity[] Spans => this.spans.ToArray();

    public static Hop StartHop()
    {
        var hop = new Hop();
        Current = hop;
        return hop;
    }

    public bool OnEnd(Activity activity)
    {
        this.spans.Add(activity);
        return activity.ParentSpanId == default || activity.HasRemoteParent;
    }
}
