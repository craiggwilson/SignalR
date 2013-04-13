using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Microsoft.AspNet.SignalR.MongoDB
{
    internal class MongoTailableCursor : IEnumerator<MessageBatch>
    {
        private readonly object _lock = new object();
        private readonly MongoCollection<MessageBatch> _collection;
        private readonly TraceSource _traceSource;

        private long _lastId;
        private MongoCursorEnumerator<MessageBatch> _wrapped;
        private volatile bool _disposed;

        public MongoTailableCursor(MongoCollection<MessageBatch> collection, long lastId, TraceSource traceSource)
        {
            _lastId = lastId;
            _collection = collection;
            _traceSource = traceSource;
        }

        public MessageBatch Current
        {
            get { return _wrapped.Current; } // could be null...
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_wrapped != null)
                {
                    _wrapped.Dispose();
                }
                _disposed = true;
            }
        }

        public bool MoveNext()
        {
            if (_disposed)
            {
                return false;
            }

            if (_wrapped == null)
            {
                _wrapped = CreateEnumerator();
            }

            while (!_disposed)
            {
                while (_wrapped.MoveNext())
                {
                    _lastId = _wrapped.Current.Id;

                    // if we are disposed, we exit immediately...
                    return !_disposed;
                }

                // MongoCursorEnumerator doesn't block while waiting for a message,
                // rather, it simply returns false.  After it receives a document,
                // it will return true again.

                if (_wrapped.IsDead)
                {
                    // The server may have killed off our tailable cursor. Dispose
                    // of the old one and create a new one picking back up where
                    // we left off. This happens when there is no data left to stream.
                    _wrapped.Dispose();
                    _wrapped = CreateEnumerator();
                }

                // let's pause for a minute to let a message show up
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }

            return false;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        private MongoCursorEnumerator<MessageBatch> CreateEnumerator()
        {
            var flags = QueryFlags.AwaitData | QueryFlags.NoCursorTimeout | QueryFlags.TailableCursor;
            var query = Query.EQ("_id", _lastId);
            var cursor = _collection.Find(query)
                .SetFlags(flags);

            return new MongoCursorEnumerator<MessageBatch>(cursor);
        }
    }
}