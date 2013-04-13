using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Microsoft.AspNet.SignalR.MongoDB
{
    public class MongoIdProvider
    {
        private const string IdsCollectionName = "ids";

        private readonly string _collectionName;
        private readonly MongoCollection _idsCollection;
        private readonly int _streamIndex;
        private readonly TraceSource _traceSource;

        public MongoIdProvider(int streamIndex, MongoDatabase database, string collectionName, TraceSource traceSource)
        {
            _collectionName = collectionName;
            _idsCollection = database.GetCollection(IdsCollectionName);
            _streamIndex = streamIndex;
            _traceSource = traceSource;
        }

        public Task<long> GetNextId()
        {
            _traceSource.TraceVerbose("Stream {0}: Getting next id from MongoDB for collection {1}.", _streamIndex, _collectionName);
            var query = Query.EQ("_id", _collectionName);
            var update = Update.Inc("next_id", 1);

            var idDocument = _idsCollection.FindAndModify(
                query: query,
                sortBy: null,
                update: update,
                returnNew: true,
                upsert: true);

            return TaskAsyncHelper.FromResult<long>(idDocument.ModifiedDocument["next_id"].AsInt64);
        }
    }
}