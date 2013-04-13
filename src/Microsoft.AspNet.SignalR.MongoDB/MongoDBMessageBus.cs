// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Microsoft.AspNet.SignalR.MongoDB
{
    /// <summary>
    /// Uses MongoDB scale-out SignalR applications in web farms.
    /// </summary>
    public class MongoDBMessageBus : ScaleoutMessageBus
    {
        private const string MessagesCollectionNamePrefix = "messages_";

        private readonly MongoClient _client;
        private readonly MongoDBScaleoutConfiguration _configuration;
        private readonly MongoDatabase _database;
        private readonly List<MongoStream> _streams;
        private readonly TraceSource _traceSource;

        public MongoDBMessageBus(IDependencyResolver resolver, MongoDBScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _configuration = configuration;
            _client = configuration.Client;
            _database = _client.GetServer().GetDatabase(configuration.DatabaseName);
            _streams = new List<MongoStream>();

            var traceManager = resolver.Resolve<ITraceManager>();
            _traceSource = traceManager["SignalR." + typeof(MongoDBMessageBus).Name];

            ThreadPool.QueueUserWorkItem(Initialize);
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _streams[streamIndex].Send(messages);
        }

        private void Initialize(object data)
        {
            while (true)
            {
                try
                {
                    var collectionNames = new HashSet<string>(_database.GetCollectionNames());

                    var options = new CollectionOptionsBuilder()
                        .SetCapped(true)
                        .SetMaxDocuments(_configuration.MaxNumberOfDocumentsInCollection)
                        .SetMaxSize(_configuration.MaxCollectionSizeInMB);

                    for (var i = 0; i < _configuration.CollectionCount; i++)
                    {
                        var collectionName = GetCollectionName(i);

                        if (!collectionNames.Contains(collectionName))
                        {
                            _database.CreateCollection(collectionName, options);
                        }                        
                    }
                    break;
                }
                catch (Exception ex)
                {
                    // Exception while installing
                    for (var i = 0; i < _configuration.CollectionCount; i++)
                    {
                        OnError(i, ex);
                    }

                    // Try again in a little bit
                    Thread.Sleep(2000);
                }
            }

            for (int i = 0; i < _configuration.CollectionCount; i++)
            {
                var stream = new MongoStream(
                    streamIndex: i, 
                    collectionName: GetCollectionName(i),
                    database: _database,
                    onReceived: (id, messages) => OnReceived(i, id, messages),
                    onError: ex => OnError(i, ex),
                    traceSource: _traceSource);

                _streams.Add(stream);
                stream.Start();
            }
        }

        private static string GetCollectionName(int streamIndex)
        {
            return "messages_" + streamIndex;
        }
    }
}