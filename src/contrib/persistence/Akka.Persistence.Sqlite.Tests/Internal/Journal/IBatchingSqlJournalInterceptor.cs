//-----------------------------------------------------------------------
// <copyright file="IJournalInterceptor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Threading.Tasks;

namespace Akka.Persistence.Sqlite.Tests.Internal.Journal
{
    /// <summary>
    ///     Interface to object which will intercept written and recovered messages in <see cref="TestBatchingSqliteJournal"/>.
    /// </summary>
    public interface IBatchingSqlJournalInterceptor
    {
        /// <summary>
        ///     Method will be called for each individual message before it is written or recovered.
        /// </summary>
        /// <param name="message">Written or recovered message.</param>
        Task InterceptAsync(IJournalRequest message);
    }
}
