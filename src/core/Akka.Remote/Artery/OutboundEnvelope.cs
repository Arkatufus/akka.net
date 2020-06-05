﻿using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.Util;

namespace Akka.Remote.Artery
{
    internal static class OutboundEnvelope
    {
        public static IOutboundEnvelope Create(Option<RemoteActorRef> recipient, object message, Option<IActorRef> sender)
            => new ReusableOutboundEnvelope().Init(recipient, message, sender);
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal interface IOutboundEnvelope : INoSerializationVerificationNeeded
    {
        Option<RemoteActorRef> Recipient { get; }
        object Message { get; }
        Option<IActorRef> Sender { get; }

        IOutboundEnvelope WithMessage(object Message);
        IOutboundEnvelope Copy();
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    internal class ReusableOutboundEnvelope : IOutboundEnvelope
    {
        // ARTERY: ObjectPool isn't implemented yet
        // public static ObjectPool<ReusableOutboundEnvelope> CreateObjectPool(int capacity)
        //     => new ObjectPool<ReusableOutboundEnvelope>(capacity, () => new ReusableOutboundEnvelope(), env => env.Clear());

        public Option<RemoteActorRef> Recipient { get; private set; } = Option<RemoteActorRef>.None;
        public object Message { get; private set; } = null;
        public Option<IActorRef> Sender { get; private set; } = Option<IActorRef>.None;

        public IOutboundEnvelope WithMessage(object message)
        {
            Message = message;
            return this;
        }

        public IOutboundEnvelope Copy()
        {
            return new ReusableOutboundEnvelope().Init(Recipient, Message, Sender);
        }

        internal void Clear()
        {
            Recipient = Option<RemoteActorRef>.None;
            Message = null;
            Sender = Option<IActorRef>.None;
        }

        internal ReusableOutboundEnvelope Init(Option<RemoteActorRef> recipient, object message, Option<IActorRef> sender)
        {
            Recipient = recipient;
            Message = message;
            Sender = sender;
            return this;
        }

        public override string ToString()
            => $"OutboundEnvelope({Recipient}, {Message}, {Sender})";
    }
}