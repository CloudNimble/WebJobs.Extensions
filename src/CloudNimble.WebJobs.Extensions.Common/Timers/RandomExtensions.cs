// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using System;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{
    internal static class RandomExtensions
    {
        public static double Next(this Random random, double minValue, double maxValue)
        {
            Ensure.ArgumentNotNull(random, nameof(random));
            return (maxValue - minValue) * random.NextDouble() + minValue;
        }
    }
}
