// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.MongoDB;
using MongoDB.Driver;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use MongoDB as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseMongoDB(this IDependencyResolver resolver, string connectionString)
        {
            var configuration = new MongoDBScaleoutConfiguration(connectionString);

            return UseMongoDB(resolver, configuration);
        }

        /// <summary>
        /// Use MongoDB as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver</param>
        /// <param name="configuration">The Redis scale-out configuration options.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseMongoDB(this IDependencyResolver resolver, MongoDBScaleoutConfiguration configuration)
        {
            var bus = new Lazy<MongoDBMessageBus>(() => new MongoDBMessageBus(resolver, configuration));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}