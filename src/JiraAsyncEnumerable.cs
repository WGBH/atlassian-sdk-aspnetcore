using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Atlassian.Jira.AspNetCore
{
    public interface IJiraAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        ValueTask<int> TotalItemsAsync(CancellationToken cancellationToken = default);

        ValueTask<bool> AnyItemsAsync(CancellationToken cancellationToken = default);

        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);

        ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default);
    }

    static class JiraAsyncEnumerable
    {
        public delegate Task<IPagedQueryResult<T>> Pager<T>(int startPageAt, CancellationToken cancellationToken);

        public static JiraAsyncEnumerable<T> Create<T>(Pager<T> getNextPage, int startAt, int? maxResults) =>
            new JiraAsyncEnumerable<T>(getNextPage, startAt, maxResults);
    }

    class JiraAsyncEnumerable<T> : IJiraAsyncEnumerable<T>
    {
        readonly JiraAsyncEnumerable.Pager<T> _getNextPage;
        readonly int _startAt;
        readonly int? _maxResults;

        IPagedQueryResult<T>? _currentPage;

        public JiraAsyncEnumerable(JiraAsyncEnumerable.Pager<T> getNextPage, int startAt, int? maxResults)
        {
            _getNextPage = getNextPage;
            _startAt = startAt;
            _maxResults = maxResults;
        }

        async Task EnsureCurrentPage(CancellationToken cancellationToken)
        {
            if (_currentPage == null)
                _currentPage = await _getNextPage(_startAt, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<int> TotalItemsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureCurrentPage(cancellationToken);

            return _currentPage!.TotalItems;
        }

        public async ValueTask<bool> AnyItemsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureCurrentPage(cancellationToken);

            return _currentPage!.TotalItems > 0;
        }

        public async ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        {
            await EnsureCurrentPage(cancellationToken);

            var absoluteMax = _currentPage!.TotalItems - _startAt;

            return _maxResults == null ? absoluteMax : Math.Min(absoluteMax, (int) _maxResults);
        }

        public async ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            await EnsureCurrentPage(cancellationToken);

            return _currentPage!.TotalItems - _startAt > 0
                && (_maxResults == null || _maxResults > 0);
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var i = 0;

            while (i < await CountAsync(cancellationToken))
            {
                foreach (var item in _currentPage!)
                {
                    i++;
                    yield return item;
                }

                if (i < await CountAsync(cancellationToken))
                    _currentPage = await _getNextPage(i, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public interface IJqlResultsAsyncEnumerable<T> : IJiraAsyncEnumerable<T>
    {
        string JqlString { get; }
    }

    class JqlResultsAsyncEnumerable : JiraAsyncEnumerable<Issue>, IJqlResultsAsyncEnumerable<Issue>
    {
        public string JqlString { get; }

        public JqlResultsAsyncEnumerable(JiraAsyncEnumerable.Pager<Issue> getNextPage,
            int startAt, string jqlString, int? maxIssues) : base(getNextPage, startAt, maxIssues)
        {
            JqlString = jqlString;
        }
    }
}