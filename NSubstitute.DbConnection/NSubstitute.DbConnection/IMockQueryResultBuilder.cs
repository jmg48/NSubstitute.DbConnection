namespace NSubstitute.DbConnection
{
    using System.Collections.Generic;

    public interface IMockQueryResultBuilder
    {
        IMockQueryResultBuilder ThenReturns<T>(IReadOnlyList<T> results);

        IMockQueryResultBuilder ThenReturns<T>(params T[] results);
    }
}