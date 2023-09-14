// <copyright file="ActivityExtensions.cs" company="OpenTelemetry Authors">
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

internal enum HopStartReason
{
    None,
    RootSpan,
    HasRemoteParent,
    SpanKindServer,
    SpanKindConsumer,
    SpanName,
}

internal static class ActivityExtensions
{
    private static string[] spanNames =
    {
        "Microsoft.AspNetCore.Hosting.HttpRequestIn",
    };

    public static bool IsHopStart(this Activity activity, out HopStartReason reason)
    {
        if (activity.ParentId == default)
        {
            reason = HopStartReason.RootSpan;
        }
        else if (activity.HasRemoteParent)
        {
            reason = HopStartReason.HasRemoteParent;
        }
        else if (activity.Kind == ActivityKind.Server)
        {
            reason = HopStartReason.SpanKindServer;
        }
        else if (activity.Kind == ActivityKind.Consumer)
        {
            reason = HopStartReason.SpanKindConsumer;
        }
        else if (spanNames.Contains(activity.DisplayName))
        {
            reason = HopStartReason.SpanName;
        }
        else
        {
            reason = HopStartReason.None;
        }

        return reason != HopStartReason.None;
    }
}
