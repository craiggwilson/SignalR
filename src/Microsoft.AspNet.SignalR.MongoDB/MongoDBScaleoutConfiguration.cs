// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Messaging;
using MongoDB.Driver;

namespace Microsoft.AspNet.SignalR.MongoDB
{
    /// <summary>
    /// Settings for the MongoDB scale-out message bus implementation.
    /// </summary>
    public class MongoDBScaleoutConfiguration : ScaleoutConfiguration
    {
        public MongoDBScaleoutConfiguration(string connectionString)
            : this(new MongoClient(connectionString))
        { }

        public MongoDBScaleoutConfiguration(MongoClientSettings settings)
            : this(new MongoClient(settings))
        { }

        private MongoDBScaleoutConfiguration(MongoClient client)
        {
            Client = client;
            CollectionCount = 1;
            DatabaseName = "signalR";
            MaxCollectionSizeInMB = 1024 * 5; // 5GB

            float averageDocSizeInKB = 20; // random guess
            float averageDocSizeInMB = averageDocSizeInKB / 1024;
            long maxAverageDocsInCollection = (long)(MaxCollectionSizeInMB / averageDocSizeInMB);
            MaxNumberOfDocumentsInCollection = MaxCollectionSizeInMB * 1024;
        }

        internal MongoClient Client { get; private set; }

        /// <summary>
        /// The number of collections to use for messages.
        /// </summary>
        public int CollectionCount { get; set; }

        /// <summary>
        /// The database to use.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// The maximum size of each message collection.
        /// </summary>
        public long MaxCollectionSizeInMB { get; set; }

        /// <summary>
        /// The maximum number of documents in each message collection.
        /// </summary>
        public long MaxNumberOfDocumentsInCollection { get; set; }
    }
}
