namespace NSubstitute.DbConnection
{
    using System;
    using System.Collections.Generic;

    public interface IMockQueryBuilder
    {
        /// <summary>
        /// Specifies that the query will only match if no parameters are passed (by default the query will match regardless of any parameters)
        /// </summary>
        /// <returns>The query builder</returns>
        IMockQueryBuilder WithNoParameters();

        /// <summary>
        /// Adds a parameter for the query to match on
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="value">The parameter value</param>
        /// <returns>The query builder</returns>
        IMockQueryBuilder WithParameter(string name, object value);

        /// <summary>
        /// Specifies that the query will only match if the given parameters are passed
        /// </summary>
        /// <param name="parameters">The parameters to match on</param>
        /// <returns>The query builder</returns>
        IMockQueryBuilder WithParameters(IReadOnlyDictionary<string, object> parameters);

        /// <summary>
        /// Specifies that the query will only match if the given parameters are passed
        /// </summary>
        /// <param name="parameters">The parameters to match on</param>
        /// <returns>The query builder</returns>
        IMockQueryBuilder WithParameters(params (string Key, object Value)[] parameters);

        /// <summary>
        /// Specifies that the query will only match if the given output parameters are passed.
        /// The provided value will be set on the matching IDbCommand parameter.
        /// </summary>
        /// <param name="parameters">The parameters to match on</param>
        /// <returns>The query builder</returns>
        IMockQueryBuilder WithOutputParameters(IReadOnlyDictionary<string, object> parameters);

        /// <summary>
        /// Specifies that the query will only match if the given output parameters are passed.
        /// The provided value will be set on the matching IDbCommand parameter.
        /// </summary>
        /// <param name="parameters">The parameters to match on</param>
        /// <returns>The query builder</returns>
        IMockQueryBuilder WithOutputParameters(params (string Key, object OutputValue)[] parameters);

        /// <summary>
        /// Specifies the first result set that the query will return.
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="results">The result set</param>
        /// <returns>The result builder</returns>
        IMockQueryResultBuilder Returns<T>(IEnumerable<T> results);

        /// <summary>
        /// Specifies the first result set that the query will return.
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="results">The result set</param>
        /// <returns>The result builder</returns>
        IMockQueryResultBuilder Returns<T>(params T[] results);

        /// <summary>
        /// Specifies the first result set that the query will return.
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="resultSelector">Returns the result set</param>
        /// <returns>The result selector</returns>
        IMockQueryResultBuilder Returns<T>(Func<QueryInfo, IEnumerable<T>> resultSelector);

        /// <summary>
        /// Specifies the number of rows affected by the query
        /// </summary>
        /// <param name="rowCount">The row count</param>
        void Affects(int rowCount);

        /// <summary>
        /// Specifies the number of rows affected by the query
        /// </summary>
        /// <param name="rowCountSelector">Returns the row count</param>
        void Affects(Func<QueryInfo, int> rowCountSelector);

        /// <summary>
        /// Specifies the exception that the query will throw
        /// </summary>
        /// <typeparam name="T">The type of the exception. Should be a derived type of <see cref="Exception"/></typeparam>
        /// <typeparam name="exception">The exception</typeparam>
        /// <returns>The result builder</returns>
        void Throws<T>(T exception)
            where T : Exception;
    }
}