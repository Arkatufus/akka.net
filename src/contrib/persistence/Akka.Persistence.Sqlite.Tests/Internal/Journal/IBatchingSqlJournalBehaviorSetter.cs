//-----------------------------------------------------------------------
// <copyright file="IJournalBehaviorSetter.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Threading.Tasks;

namespace Akka.Persistence.Sqlite.Tests.Internal.Journal
{
    public interface IBatchingSqlJournalBehaviorSetter
    {
        Task SetInterceptorAsync(IBatchingSqlJournalInterceptor interceptor);
    }
}
