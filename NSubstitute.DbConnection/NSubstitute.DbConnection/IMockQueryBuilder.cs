namespace NSubstitute.DbConnection
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    public interface IMockQueryBuilder
    {
        /// <summary>
        /// Specifies that the query will only match if no parameters are passed (by default the query will match regardless of any parameters)
        /// </summary>
        /// <returns>The query builder</returns>
        IMockQueryBuilder WithNoParameters();

        /// <summary>
        /// Specifies that the query will only match if the given parameters are passed
        /// </summary>
        /// <param name="parameters">The parameters to match on</param>
        /// <returns>The query builder</returns>
        IMockQueryBuilder WithParameters(IReadOnlyDictionary<string, object> parameters);

        /// <summary>
        /// Specifies the first result set that the query will return.
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="results">The result set</param>
        /// <returns>The result builder</returns>
        IMockQueryResultBuilder Returns<T>(IReadOnlyList<T> results);

        /// <summary>
        /// Specifies the first result set that the query will return.
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="results">The result set</param>
        /// <returns>The result builder</returns>
        IMockQueryResultBuilder Returns<T>(params T[] results);
    }
}