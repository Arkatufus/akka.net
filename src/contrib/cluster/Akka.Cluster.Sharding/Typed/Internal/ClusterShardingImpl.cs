using System;
using System.Collections.Generic;
using System.Text;
using Akka.Annotations;

namespace Akka.Cluster.Sharding.Typed.Internal
{

    [InternalApi]
    internal class ExtractorAdapter<E, M> : ShardingMessageExtractor<object, M>
    {
        public ExtractorAdapter(int numberOfShards) : base(numberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            throw new NotImplementedException();
        }

        public override string ShardId(string entityId)
        {
            throw new NotImplementedException();
        }

        public override M UnwrapMessage(object message)
        {
            throw new NotImplementedException();
        }
    }

    class ClusterShardingImpl
    {
    }
}
