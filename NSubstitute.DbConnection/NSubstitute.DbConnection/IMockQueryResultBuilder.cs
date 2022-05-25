namespace NSubstitute.Community.DbConnection
{
    using System;
    using System.Collections.Generic;

    public interface IMockQueryResultBuilder
    {
        /// <summary>
        /// Specifies the next result set that the query will return.
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="results">The result set</param>
        /// <returns>The result builder</returns>
        IMockQueryResultBuilder ThenReturns<T>(IEnumerable<T> results);

        /// <summary>
        /// Specifies the next result set that the query will return.
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="results">The result set</param>
        /// <returns>The result builder</returns>
        IMockQueryResultBuilder ThenReturns<T>(params T[] results);

        /// <summary>
        /// Specifies the next result set that the query will return.
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="resultSelector">Returns the result set</param>
        /// <returns>The result builder</returns>
        IMockQueryResultBuilder ThenReturns<T>(Func<QueryInfo, IEnumerable<T>> resultSelector);
    }
}