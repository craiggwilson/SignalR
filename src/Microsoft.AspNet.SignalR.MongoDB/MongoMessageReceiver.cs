using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Microsoft.AspNet.SignalR.MongoDB
{
    internal class MongoMessageReceiver : IDisposable
    {
        private readonly MongoCollection<MessageBatch> _messages;
        private readonly Action<ulong, IList<Message>> _onReceive;
        private readonly Action<Exception> _onError;
        private readonly int _streamIndex;
        private readonly TraceSource _traceSource;

        private long? _lastBatchId;
        private MongoTailableCursor _cursor;

        public MongoMessageReceiver(int streamIndex, MongoCollection<MessageBatch> messages, Action<ulong, IList<Message>> onReceive, Action<Exception> onError, TraceSource traceSource)
        {
            _messages = messages;
            _onReceive = onReceive;
            _onError = onError;
            _streamIndex = streamIndex;
            _traceSource = traceSource;
        }

        public Task StartReceiving()
        {
            var tcs = new TaskCompletionSource<object>();

            ThreadPool.QueueUserWorkItem(Receive, tcs);

            return tcs.Task;
        }

        public void Dispose()
        {
            _cursor.Dispose();
        }

        private void Receive(object data)
        {
            var tcs = (TaskCompletionSource<object>)data;

            if (!_lastBatchId.HasValue)
            {
                try
                {
                    _lastBatchId = GetLastId();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    return;
                }
            }

            _cursor = new MongoTailableCursor(_messages, _lastBatchId.Value, _traceSource);
            while (_cursor.MoveNext())
            {
                _onReceive((ulong)_cursor.Current.Id, _cursor.Current.Messages);
            }
        }

        private long GetLastId()
        {
            var lastId = _messages.FindAllAs<BsonDocument>()
                .SetSortOrder(SortBy.Descending("_id"))
                .SetLimit(-1)
                .SetFields(Fields.Include("_id"))
                .SingleOrDefault();

            return lastId == null
                ? 0
                : lastId["_id"].AsInt64;
        }
    }
}