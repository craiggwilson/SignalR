using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using MongoDB.Driver;

namespace Microsoft.AspNet.SignalR.MongoDB
{
    public class MongoStream : IDisposable
    {
        private readonly MongoIdProvider _idProvider;
        private readonly MongoCollection<MessageBatch> _messages;
        private readonly MongoMessageReceiver _receiver;
        private readonly Action<ulong, IList<Message>> _onReceived;
        private readonly Action<Exception> _onError;

        public MongoStream(int streamIndex, string collectionName, MongoDatabase database, Action<ulong, IList<Message>> onReceived, Action<Exception> onError, TraceSource traceSource)
        {
            _idProvider = new MongoIdProvider(streamIndex, database, collectionName, traceSource);
            _messages = database.GetCollection<MessageBatch>(collectionName);

            _receiver = new MongoMessageReceiver(streamIndex, _messages, onReceived, onError, traceSource);
            _onReceived = onReceived;
            _onError = onError;
        }

        public Task Send(IList<Message> messages)
        {
            return _idProvider.GetNextId()
                .Then(id =>
                {
                    var batch = new MessageBatch
                    {
                        Id = id,
                        Messages = messages
                    };

                    return TaskAsyncHelper.FromMethod(() => _messages.Insert(batch));
                });
        }

        public void Start()
        {
            _receiver.StartReceiving();
        }

        public void Dispose()
        {
            _receiver.Dispose();
        }
    }
}