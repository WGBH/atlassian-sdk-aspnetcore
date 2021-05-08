using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Atlassian.Jira.AspNetCore
{
    public interface IJiraAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        ValueTask<int> CountAsync(CancellationToken cancellationToken = default);

        ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default);
    }

    static class JiraAsyncEnumerable
    {
        public delegate Task<IPagedQueryResult<T>> Pager<T>(int startPageAt, CancellationToken cancellationToken);

        public static JiraAsyncEnumerable<T> Create<T>(Pager<T> getNextPage, int startAt) =>
            new JiraAsyncEnumerable<T>(getNextPage, startAt);
    }

    // This class repeats some code to prevent awaiting tasks that its own methods creates.
    // For example, this is why AnyAsync does not simple call CountAsync
    class JiraAsyncEnumerable<T> : IJiraAsyncEnumerable<T>
    {
        readonly JiraAsyncEnumerable.Pager<T> _getNextPage;
        readonly int _startAt;

        IPagedQueryResult<T>? _currentPage;

        public JiraAsyncEnumerable(JiraAsyncEnumerable.Pager<T> getNextPage, int startAt)
        {
            _getNextPage = getNextPage;
            _startAt = startAt;
        }

        public async ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        {
            if (_currentPage == null)
                _currentPage = await _getNextPage(_startAt, cancellationToken).ConfigureAwait(false);

            return _currentPage.TotalItems;
        }

        public async ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            if (_currentPage == null)
                _currentPage = await _getNextPage(_startAt, cancellationToken).ConfigureAwait(false);

            return _currentPage.TotalItems > 0;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (_currentPage == null)
                _currentPage = await _getNextPage(_startAt, cancellationToken).ConfigureAwait(false);

            if (_currentPage.ItemsPerPage == 0)
                yield break;

            var i = _startAt;

            while (i < _currentPage.TotalItems)
            {
                foreach (var issue in _currentPage)
                {
                    i++;
                    yield return issue;
                }

                if (i < _currentPage.TotalItems)
                    _currentPage = await _getNextPage(i, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}