using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;

namespace Akka.Cluster.Sharding
{
    /// <summary>
    /// Entirely customizable typed message extractor. Prefer <see cref="HashCodeMessageExtractor{M}"/> or
    /// <see cref="HashCodeNoEnvelopeMessageExtractor{M}"/> if possible.
    /// </summary>
    /// <typeparam name="E">Possibly an Envelope around the messages accepted by the entity actor, is the same as `M` if there is no envelope.</typeparam>
    /// <typeparam name="M">The type of message accepted by the entity actor</typeparam>
#nullable enable
    public abstract class ShardingMessageExtractor<E, M>
    {
        /// <summary>
        /// Create the default message extractor, using envelopes to identify what entity a message is for
        /// and the hashcode of the entityId to decide which shard an entity belongs to.
        ///
        /// This is recommended since it does not force details about sharding into the entity protocol
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="numberOfShards"></param>
        /// <returns></returns>
        public static ShardingMessageExtractor<ShardingEnvelope<T>, T> Apply<T>(int numberOfShards) 
            where T : class? 
            => new HashCodeMessageExtractor<T>(numberOfShards);

        public static ShardingMessageExtractor<T, T> NoEnvelope<T>(
            int numberOfShards,
            Func<T, string> extractEntityId)
            where T : class?
            => new HashCodeNoEnvelopeMessageExtractor<T>(numberOfShards, extractEntityId);

        public abstract string EntityId(E message);
        public abstract string ShardId(string entityId);
        public abstract M UnwrapMessage(E message);
    }

    public sealed class HashCodeMessageExtractor<M> 
        : ShardingMessageExtractor<ShardingEnvelope<M>, M>
        where M : class?
    {
        public int NumberOfShards { get; }

        public HashCodeMessageExtractor(int numberOfShards)
        {
            NumberOfShards = numberOfShards;
        }

        public override string EntityId(ShardingEnvelope<M> envelope)
            => envelope.EntityId;

        public override string ShardId(string entityId)
            => HashCodeMessageExtractor.ShardId(entityId, NumberOfShards);

        public override M UnwrapMessage(ShardingEnvelope<M> envelope)
            => envelope.Message;
    }

    public class HashCodeNoEnvelopeMessageExtractor<M> : ShardingMessageExtractor<M, M>
    {
        private readonly Func<M, string> _extractEntityId;
        public int NumberOfShards { get; }

        public HashCodeNoEnvelopeMessageExtractor(int numberOfShards, Func<M, string> extractEntityId)
        {
            NumberOfShards = numberOfShards;
            _extractEntityId = extractEntityId;
        }

        public override string EntityId(M message)
            => _extractEntityId(message);

        public override string ShardId(string entityId)
            => HashCodeMessageExtractor.ShardId(entityId, NumberOfShards);

        public override M UnwrapMessage(M message)
            => message;

        public override string ToString()
            => $"HashCodeNoEnvelopeMessageExtractor({NumberOfShards})";
    }
}
