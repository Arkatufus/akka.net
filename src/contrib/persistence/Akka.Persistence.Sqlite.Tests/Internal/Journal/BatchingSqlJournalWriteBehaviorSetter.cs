//-----------------------------------------------------------------------
// <copyright file="JournalWriteBehaviorSetter.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Persistence.Sqlite.Tests.Internal.Journal
{
    /// <summary>
    ///     Setter strategy for <see cref="TestBatchingSqliteJournal"/> which will set write interceptor.
    /// </summary>
    internal class BatchingSqlJournalWriteBehaviorSetter : IBatchingSqlJournalBehaviorSetter
    {
        internal BatchingSqlJournalWriteBehaviorSetter(IActorRef journal)
        {
            this._journal = journal;
        }

        private readonly IActorRef _journal;

        public Task SetInterceptorAsync(IBatchingSqlJournalInterceptor interceptor)
            => _journal.Ask<TestBatchingSqliteJournal.Ack>(
                new TestBatchingSqliteJournal.UseInterceptor(interceptor),
                TimeSpan.FromSeconds(3)
            );
    }
}
