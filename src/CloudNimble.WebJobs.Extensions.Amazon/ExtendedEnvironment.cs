// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace CloudNimble.WebJobs.Extensions.Amazon
{

    /// <summary>
    /// 
    /// </summary>
    public static class ExtendedEnvironment
    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int GetProcessorCount()
        {
            // RWM: Check if running on Beanstalk
            int processorCount = Environment.ProcessorCount;  //1;
                                                              //var skuValue = Environment.GetEnvironmentVariable(Constants.AzureWebsiteSku);
                                                              //if (!string.Equals(skuValue, Constants.DynamicSku, StringComparison.OrdinalIgnoreCase))
                                                              //{
                                                              //processorCount = Environment.ProcessorCount;
                                                              //}
            return processorCount;
        }

    }

}
