// -----------------------------------------------------------------------
//  <copyright file="SqliteLegacyJournalSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Sqlite.Tests
{
    public class SqliteLegacyJournalSpec: Akka.TestKit.Xunit2.TestKit
    {
        private Dictionary<string, IActorRef> _actors = new Dictionary<string, IActorRef>();
        private readonly TestProbe _probe;

        public SqliteLegacyJournalSpec(ITestOutputHelper output)
            : base(CreateSpecConfig("Filename=file:EventJournal-v1.3.0.db"), nameof(SqliteLegacyJournalSpec), output)
        {
            SqlitePersistence.Get(Sys);
            _probe = CreateTestProbe();
        }
        
        private static Config CreateSpecConfig(string connectionString)
        {
            return ConfigurationFactory.ParseString($@"
akka.persistence {{
    publish-plugin-commands = on
    journal {{
        plugin = akka.persistence.journal.sqlite
        sqlite {{
            auto-initialize = on
            connection-string = ""{connectionString}""
        }}
    }}
    snapshot-store {{
        plugin = akka.persistence.snapshot-store.sqlite
        sqlite {{
            auto-initialize = on
            connection-string = ""{connectionString}""
        }}
    }}
}}").WithFallback(SqlitePersistence.DefaultConfiguration());
        }

        [Fact]
        public void Generator()
        {
            Generate();
        }

        private void Generate()
        {
            _actors["A"] = Sys.ActorOf(Props.Create(() => new PersistedActor("A", _probe)));
            _actors["B"] = Sys.ActorOf(Props.Create(() => new PersistedActor("B", _probe)));
            _actors["C"] = Sys.ActorOf(Props.Create(() => new PersistedActor("C", _probe)));
            
            foreach (var i in Enumerable.Range(1, 5))
            {
                _actors["A"].Tell(new PersistedActor.Persisted(i));
                _probe.ExpectMsg<PersistedActor.PersistAck>();
                _actors["B"].Tell(new PersistedActor.Persisted(i));
                _probe.ExpectMsg<PersistedActor.PersistAck>();
                _actors["C"].Tell(new PersistedActor.Persisted(i));
                _probe.ExpectMsg<PersistedActor.PersistAck>();
            }
            
            var a = _probe.ExpectMsg<PersistedActor.SaveSnapshotAck>();
            var b = _probe.ExpectMsg<PersistedActor.SaveSnapshotAck>();
            var c = _probe.ExpectMsg<PersistedActor.SaveSnapshotAck>();
            new [] { a.State.Payload, b.State.Payload, c.State.Payload }.Should().BeEquivalentTo(5, 5, 5);
            a.Events.Count.Should().Be(0);
            b.Events.Count.Should().Be(0);
            c.Events.Count.Should().Be(0);
            
            foreach (var i in Enumerable.Range(6, 5))
            {
                _actors["A"].Tell(new PersistedActor.Persisted(i));
                _probe.ExpectMsg<PersistedActor.PersistAck>();
                _actors["B"].Tell(new PersistedActor.Persisted(i));
                _probe.ExpectMsg<PersistedActor.PersistAck>();
                _actors["C"].Tell(new PersistedActor.Persisted(i));
                _probe.ExpectMsg<PersistedActor.PersistAck>();
            }
        }

    }
}