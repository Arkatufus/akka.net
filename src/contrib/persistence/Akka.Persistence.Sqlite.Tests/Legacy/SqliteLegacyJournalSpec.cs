// -----------------------------------------------------------------------
//  <copyright file="SqliteLegacyJournalSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Sqlite.Tests
{
    public class SqliteLegacyJournalSpec: Akka.TestKit.Xunit2.TestKit
    {
        private readonly PersistenceExtension _extension;
        private readonly string _writerGuid;
        private const int ActorInstanceId = 1;
        private IActorRef Journal => _extension.JournalFor(null);
        private IActorRef Snapshot => _extension.SnapshotStoreFor(null);
        
        public SqliteLegacyJournalSpec(ITestOutputHelper output)
            : base(CreateSpecConfig("Filename=file:EventJournal-v1.3.0.db"), nameof(SqliteLegacyJournalSpec), output)
        {
            _extension = Persistence.Instance.Apply(Sys as ExtendedActorSystem);
            _writerGuid = Guid.NewGuid().ToString();
            SqlitePersistence.Get(Sys);

            Initialize();
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
        public void EmptyTest()
        {
        }

        private void Initialize()
        {
            var probe = CreateTestProbe();
            WriteMessages(1, 5, "A", probe, _writerGuid);
            WriteMessages(1, 5, "B", probe, _writerGuid);
            WriteMessages(1, 5, "C", probe, _writerGuid);
            
            Snapshot.Tell(new SaveSnapshot(new SnapshotMetadata("A", 5, DateTime.Now), "A-5"), probe);
            probe.ExpectMsg<SaveSnapshotSuccess>(msg => msg.Metadata.PersistenceId == "A" && msg.Metadata.SequenceNr == 5);
            
            Snapshot.Tell(new SaveSnapshot(new SnapshotMetadata("B", 5, DateTime.Now), "B-5"), probe);
            probe.ExpectMsg<SaveSnapshotSuccess>(msg => msg.Metadata.PersistenceId == "B" && msg.Metadata.SequenceNr == 5);
            
            Snapshot.Tell(new SaveSnapshot(new SnapshotMetadata("C", 5, DateTime.Now), "C-5"), probe);
            probe.ExpectMsg<SaveSnapshotSuccess>(msg => msg.Metadata.PersistenceId == "C" && msg.Metadata.SequenceNr == 5);
            
            Journal.Tell(new DeleteMessagesTo("A", 5, probe), probe);
            probe.ExpectMsg<DeleteMessagesSuccess>(msg => msg.ToSequenceNr == 5);
            
            Journal.Tell(new DeleteMessagesTo("B", 5, probe), probe);
            probe.ExpectMsg<DeleteMessagesSuccess>(msg => msg.ToSequenceNr == 5);
            
            Journal.Tell(new DeleteMessagesTo("C", 5, probe), probe);
            probe.ExpectMsg<DeleteMessagesSuccess>(msg => msg.ToSequenceNr == 5);
            
            WriteMessages(6, 5, "A", probe, _writerGuid);
            WriteMessages(6, 5, "B", probe, _writerGuid);
            WriteMessages(6, 5, "C", probe, _writerGuid);
        }
        
        private void WriteMessages(int from, int count, string pid, IActorRef sender, string writerGuid)
        {
            Persistent Persistent(long i) => new Persistent($"{pid}-{i}", i, pid, string.Empty, false, sender, writerGuid);
            
            var messages = Enumerable.Range(from, count).Select(i => new AtomicWrite(Persistent(i))).ToArray();
            var probe = CreateTestProbe();

            Journal.Tell(new WriteMessages(messages, probe.Ref, ActorInstanceId));

            probe.ExpectMsg<WriteMessagesSuccessful>();
            for (var i = from; i < from + count; i++)
            {
                var n = i;
                probe.ExpectMsg<WriteMessageSuccess>(m =>
                    m.Persistent.Payload.ToString() == $"{pid}-{n}" && m.Persistent.SequenceNr == n &&
                    m.Persistent.PersistenceId == pid);
            }
        }
        
    }
}