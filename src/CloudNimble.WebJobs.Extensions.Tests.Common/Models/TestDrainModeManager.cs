// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Timers;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{

    /// <summary>
    /// Test implementation of IDrainModeManager for testing purposes.
    /// </summary>
    public class TestDrainModeManager : IDrainModeManager
    {
        public bool IsDrainModeEnabled { get; set; }

        public void RegisterListener(IListener listener)
        {
            // No-op for testing
        }

        public Task EnableDrainModeAsync(CancellationToken cancellationToken)
        {
            IsDrainModeEnabled = true;
            return Task.CompletedTask;
        }
    }

}