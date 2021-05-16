using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Jira.Linq;

namespace Atlassian.Jira.AspNetCore
{
    public interface IJqlResultsAsyncEnumerable : IJiraAsyncEnumerable<Issue>
    {
        string JqlString { get; }
    }

    class JqlResultsAsyncEnumerable : JiraAsyncEnumerable<Issue>, IJqlResultsAsyncEnumerable
    {
        public string JqlString { get; }

        public JqlResultsAsyncEnumerable(JiraAsyncEnumerable.Pager<Issue> getNextPage,
            int startAt, string jqlString) : base(getNextPage, startAt)
        {
            JqlString = jqlString;
        }
    }

    public static class IssueServiceExtensions
    {
        // Hack: using reflection to access private fields.
        static FieldInfo IssuesField = typeof(JiraQueryProvider)
            .GetField("_issues", BindingFlags.Instance | BindingFlags.NonPublic)!;
        static FieldInfo TranslatorField = typeof(JiraQueryProvider)
            .GetField("_translator", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public static IJqlResultsAsyncEnumerable AsAsync(
            this IQueryable<Issue> queryable, bool countOnly = false)
        {
            var jiraQueryable = queryable as JiraQueryable<Issue>;
            if (jiraQueryable == null)
                throw new InvalidOperationException("Queryable object must be a JiraQueryable!");

            var provider = jiraQueryable.Provider;
            var expression = jiraQueryable.Expression;

            var issueService = (IIssueService) IssuesField.GetValue(provider)!;
            var expressionVisitor = (IJqlExpressionVisitor) TranslatorField.GetValue(provider)!;

            var jql = expressionVisitor.Process(expression);

            if (countOnly)
                return issueService.QueryIssuesAsyncEnum(jql.Expression, 0);
            else
                return issueService.QueryIssuesAsyncEnum(jql.Expression, jql.NumberOfResults, jql.SkipResults ?? 0);
        }

        public static ValueTask<int> QueryCountAsync(this IIssueService issueService, string jqlString,
            CancellationToken cancellationToken = default)
        {
            return issueService
                .QueryIssuesAsyncEnum(jqlString, 0)
                .CountAsync(cancellationToken);
        }

        public static IJqlResultsAsyncEnumerable QueryIssuesAsyncEnum(this IIssueService issueService,
            string jqlString, int? maxIssues = null, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
                issueService.GetIssuesFromJqlAsync(jqlString, maxIssues, startPageAt, cancellationToken);

            return new JqlResultsAsyncEnumerable(getNextPage, startAt, jqlString);
        }

        public static IJqlResultsAsyncEnumerable QueryIssuesAsyncEnum(this IIssueService issueService, IssueSearchOptions options)
        {
            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
            {
                options.StartAt = startPageAt;
                return issueService.GetIssuesFromJqlAsync(options, cancellationToken);
            };

            return new JqlResultsAsyncEnumerable(getNextPage, options.StartAt, options.Jql);
        }

        public static IJiraAsyncEnumerable<Comment> GetCommentsAsyncEnum(
            this IIssueService issueService, string issueKey, int? maxComments, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Comment> getNextPage = (startPageAt, cancellationToken) =>
                issueService.GetPagedCommentsAsync(issueKey, maxComments, startPageAt, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt);
        }

        public static IJiraAsyncEnumerable<Issue> GetSubTasksAsyncEnum(
            this IIssueService issueService, string issueKey, int? maxIssues, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
                issueService.GetSubTasksAsync(issueKey, maxIssues, startAt, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt);
        }
    }
}