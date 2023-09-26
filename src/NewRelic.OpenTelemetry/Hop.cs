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

internal sealed class Hop
{
    private object mutex = new object();
    private bool ended;
    private int spanCount = 1;
    private DateTime deadline;
    private List<Activity> spans = new List<Activity>();

    public Hop(Activity rootActivity)
    {
        this.spans.Add(rootActivity);
    }

    public void SpanStart(Activity activity)
    {
        lock (this.mutex)
        {
            if (!this.ended)
            {
                ++this.spanCount;
                this.spans.Add(activity);
            }
        }
    }

    public void SpanEnd(Activity activity)
    {
        lock (this.mutex)
        {
            --this.spanCount;
            if (this.spanCount == 0)
            {
                this.deadline = DateTime.UtcNow.AddSeconds(5);
            }
        }
    }

    public bool TryFinish(out Activity[] spans)
    {
        lock (this.mutex)
        {
            if (this.spanCount == 0 && this.deadline < DateTime.UtcNow)
            {
                this.ended = true;
                spans = this.spans.ToArray();
                return true;
            }

            spans = Array.Empty<Activity>();
            return false;
        }
    }
}
