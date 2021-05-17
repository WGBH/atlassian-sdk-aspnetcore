using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Atlassian.Jira.Linq;

namespace Atlassian.Jira.AspNetCore
{
    class OneArgSelectorJqlResultsAsyncEnumerable<T> : JqlResultsAsyncEnumerable, IJqlResultsAsyncEnumerable<T>
    {
        readonly Func<Issue, T> _selector;

        public OneArgSelectorJqlResultsAsyncEnumerable(JiraAsyncEnumerable.Pager<Issue> getNextPage,
            int startAt, string jqlString, int? maxIssues, Func<Issue, T> selector)
            : base(getNextPage, startAt, jqlString, maxIssues)
        {
            _selector = selector;
        }

        new public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            await foreach(var issue in (JqlResultsAsyncEnumerable) this)
                yield return _selector(issue);
        }
    }

    class TwoArgSelectorJqlResultsAsyncEnumerable<T> : JqlResultsAsyncEnumerable, IJqlResultsAsyncEnumerable<T>
    {
        readonly Func<Issue, int, T> _selector;

        public TwoArgSelectorJqlResultsAsyncEnumerable(JiraAsyncEnumerable.Pager<Issue> getNextPage,
            int startAt, string jqlString, int? maxIssues, Func<Issue, int, T> selector)
            : base(getNextPage, startAt, jqlString, maxIssues)
        {
            _selector = selector;
        }

        new public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var i = 0;

            await foreach (var issue in (JqlResultsAsyncEnumerable) this)
            {
                yield return _selector(issue, i);
                i++;
            }
        }
    }

    public interface IAsyncJiraQueryable<T> : IOrderedQueryable<T>, IQueryable<T> { }

    class AsyncJiraQueryable<T> : IAsyncJiraQueryable<T>
    {
        class NoopQueryProvider : IQueryProvider
        {
            readonly IIssueService _issueService;

            public NoopQueryProvider(IIssueService issueService) =>
                _issueService = issueService;

            public IQueryable CreateQuery(Expression expression) =>
                throw new NotSupportedException();

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
                new AsyncJiraQueryable<TElement>(_issueService, expression);

            public object? Execute(Expression expression) =>
                throw CannotExecute(expression);

            public TResult Execute<TResult>(Expression expression) =>
                throw CannotExecute(expression);

            NotSupportedException CannotExecute(Expression expression)
            {
                var methodCall = expression as MethodCallExpression;
                var methodName = methodCall == null ? "a method" : methodCall.Method.Name;

                string message = $"An attempt was made to call {methodName} which caused the system "
                    + "to evaluate the query synchronously. Consider calling Prepare first.";

                return new NotSupportedException(message);
            }
        }

        readonly IIssueService _issueService;

        public Expression Expression { get; }

        public AsyncJiraQueryable(IIssueService issueService, Expression? expression = null)
        {
            _issueService = issueService;
            Expression = expression ?? Expression.Constant(this);
        }

        public IEnumerator<T> GetEnumerator() =>
            throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() =>
            throw new NotSupportedException();

        public Type ElementType => typeof(T);

        public IQueryProvider Provider => new NoopQueryProvider(_issueService);

        public IJqlResultsAsyncEnumerable<T> Prepare()
        {
            var jql = new JqlExpressionVisitor().Process(Expression);

            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
                _issueService.GetIssuesFromJqlAsync(jql.Expression, jql.NumberOfResults, startPageAt, cancellationToken);

            if(Expression is MethodCallExpression selectExpression
                && selectExpression.Method.Name == nameof(Queryable.Select) && typeof(T) != typeof(Issue))
            {
                var selectorExpression = ((UnaryExpression) selectExpression.Arguments.Last()).Operand;
                if (selectorExpression is Expression<Func<Issue, T>> oneArgSelectorExpression)
                {
                    var selector = oneArgSelectorExpression.Compile();

                    return new OneArgSelectorJqlResultsAsyncEnumerable<T>(
                        getNextPage, jql.SkipResults ?? 0, jql.Expression, jql.NumberOfResults, selector);
                }
                else
                {
                    var selector = ((Expression<Func<Issue, int, T>>) selectorExpression).Compile();

                    return new TwoArgSelectorJqlResultsAsyncEnumerable<T>(
                        getNextPage, jql.SkipResults ?? 0, jql.Expression, jql.NumberOfResults, selector);
                }
            }

            Debug.Assert(typeof(T) == typeof(Issue));

            return (IJqlResultsAsyncEnumerable<T>)
                new JqlResultsAsyncEnumerable(getNextPage, jql.SkipResults ?? 0, jql.Expression, jql.NumberOfResults);
        }
    }

    public static class AsyncJiraQueryableExtensions
    {
        public static IJqlResultsAsyncEnumerable<T> Prepare<T>(this IQueryable<T> queryable)
        {
            var asyncQueryable = queryable as AsyncJiraQueryable<T>;
            if (asyncQueryable == null)
            {
                throw new InvalidOperationException(
                    $"Queryable object must be an instance of {nameof(AsyncJiraQueryable<T>)}!");
            }

            return asyncQueryable.Prepare();
        }
    }
}