using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
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

        new public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach(var issue in ((JqlResultsAsyncEnumerable) this).WithCancellation(cancellationToken))
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

        new public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var i = 0;

            await foreach (var issue in ((JqlResultsAsyncEnumerable) this).WithCancellation(cancellationToken))
            {
                yield return _selector(issue, i);
                i++;
            }
        }
    }

    public interface IAsyncJiraQueryable<T> : IOrderedQueryable<T>, IQueryable<T> { }

    class AsyncJiraQueryable<T> : IAsyncJiraQueryable<T>
    {
        class NoCanDoQueryProvider : IQueryProvider
        {
            readonly IIssueService _issueService;

            public NoCanDoQueryProvider(IIssueService issueService) =>
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

        class JqlResultsAsyncEnumerablePreparer : ExpressionVisitor
        {
            object? _selector;

            public static IJqlResultsAsyncEnumerable<T> Prepare(IIssueService issueService, Expression expression) =>
                new JqlResultsAsyncEnumerablePreparer().PrepareInternal(issueService, expression);

            IJqlResultsAsyncEnumerable<T> PrepareInternal(IIssueService issueService, Expression expression)
            {
                var jql = new JqlExpressionVisitor().Process(Visit(expression));

                JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
                    issueService.GetIssuesFromJqlAsync(jql.Expression, jql.NumberOfResults, startPageAt, cancellationToken);

                // Determine the type of the selector (if any),
                // and use it to return the appropriate IJqlResultsAsyncEnumerable.
                return _selector switch
                {
                    null => (IJqlResultsAsyncEnumerable<T>)
                        new JqlResultsAsyncEnumerable(getNextPage, jql.SkipResults ?? 0,
                            jql.Expression, jql.NumberOfResults),

                    Func<Issue, T> oneArgSelector =>
                        new OneArgSelectorJqlResultsAsyncEnumerable<T>(getNextPage, jql.SkipResults ?? 0,
                            jql.Expression, jql.NumberOfResults, oneArgSelector),

                    Func<Issue, int, T> twoArgSelector =>
                        new TwoArgSelectorJqlResultsAsyncEnumerable<T>(getNextPage, jql.SkipResults ?? 0,
                            jql.Expression, jql.NumberOfResults, twoArgSelector),

                    _ =>
                        throw new NotSupportedException("The selector expression could not be compiled.")
                };
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                // Examine the outermost method call. If this is Select,
                // peel it off; it might be used to transform the issue.
                if (node.Method.Name == nameof(Queryable.Select))
                {
                    Visit(node.Arguments[1]);

                    return node.Arguments[0];
                }

                return node;
            }

            protected override Expression VisitLambda<TDelegate>(Expression<TDelegate> node)
            {
                if (typeof(T) != typeof(Issue))
                    _selector = node.Compile()!;

                return node;
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

        public IQueryProvider Provider => new NoCanDoQueryProvider(_issueService);

        public IJqlResultsAsyncEnumerable<T> Prepare() =>
            JqlResultsAsyncEnumerablePreparer.Prepare(_issueService, Expression);
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