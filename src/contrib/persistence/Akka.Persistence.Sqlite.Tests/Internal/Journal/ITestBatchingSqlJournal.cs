//-----------------------------------------------------------------------
// <copyright file="ITestJournal.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

namespace Akka.Persistence.Sqlite.Tests.Internal.Journal
{
    /// <summary>
    ///     <see cref="TestBatchingSqliteJournal"/> proxy object interface. Used to simplify communication with <see cref="TestBatchingSqliteJournal"/> actor instance.
    /// </summary>
    public interface ITestBatchingSqlJournal
    {
        /// <summary>
        ///     List of interceptors to alter batching behavior of proxied journal.
        /// </summary>
        BatchingSqlJournalBehavior OnBatching { get; }
    }
}
