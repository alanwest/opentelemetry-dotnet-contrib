// <copyright file="ActivityExtensionsTests.cs" company="OpenTelemetry Authors">
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
using Xunit;

namespace NewRelic.OpenTelemetry.Tests;

public class ActivityExtensionsTests
{
    private static ActivitySource activitySource = new ActivitySource($"ActivitySource.{nameof(ActivityExtensionsTests)}");

    public ActivityExtensionsTests()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);
    }

    [Theory]
    [InlineData(ActivityKind.Server, true, true)]
    [InlineData(ActivityKind.Consumer, true, true)]
    [InlineData(ActivityKind.Internal, false, true)]
    [InlineData(ActivityKind.Internal, true, false)]
    public void IsTransactionStartTest(ActivityKind kind, bool hasParent, bool expected)
    {
        var parentContext = hasParent
            ? new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded)
            : default;
        using var activity = activitySource.StartActivity($"{nameof(this.IsTransactionStartTest)}", kind, parentContext);
        Assert.NotNull(activity);
        Assert.Equal(expected, activity.IsTransactionStart());
    }
}
