using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNet.SignalR.Messaging;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.AspNet.SignalR.MongoDB
{
    [BsonIgnoreExtraElements]
    internal class MessageBatch
    {
        public const string CollectionNamePrefix = "messages_";

        public long Id { get; set; }

        public IList<Message> Messages { get; set; }
    }
}
