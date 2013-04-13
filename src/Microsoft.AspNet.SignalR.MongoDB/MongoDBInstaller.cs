// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Microsoft.AspNet.SignalR.MongoDB
{
    internal class MongoDBInstaller
    {
        private const string IdCollectionName = "ids";

        private readonly MongoDatabase _database;
        private readonly TraceSource _traceSource;

        public MongoDBInstaller(MongoDatabase database, TraceSource traceSource)
        {
            _database = database;
            _traceSource = traceSource;
        }

        public void Install()
        {
            CreateMessageIdCollection();
            // CreateCappedCollection
        }

        private void CreateMessageIdCollection()
        {
            var collection = _database.GetCollection(IdCollectionName);
            collection.EnsureIndex(IndexKeys.Ascending("_id").Ascending("next_id")); //collection name, next_id
        }
    }
}