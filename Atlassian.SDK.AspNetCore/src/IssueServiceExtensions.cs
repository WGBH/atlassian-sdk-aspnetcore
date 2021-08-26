// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Jira.Async;
using Atlassian.Jira.JqlBuilder;
using JiraRoot = Atlassian.Jira.Jira;

namespace Atlassian.Jira
{
    public static class IssueServiceExtensions
    {
        public static ValueTask<int> QueryCountAsync(this IIssueService issueService, string jqlString,
            CancellationToken cancellationToken = default)
        {
            return issueService
                .QueryIssuesAsyncEnum(jqlString, 0)
                .CountAsync(cancellationToken);
        }

        public static ValueTask<int> QueryCountAsync(this IIssueService issueService, IJqlExpression jqlExpression,
            CancellationToken cancellationToken = default)
        {
            return QueryCountAsync(issueService, jqlExpression.ToString(), cancellationToken);
        }

        public static Task<IPagedQueryResult<Issue>> GetIssuesFromJqlAsync(this IIssueService issueService,
            IJqlExpression jqlExpression, int? maxIssues = null, int startAt = 0,
            CancellationToken cancellationToken = default)
        {
            return issueService.GetIssuesFromJqlAsync(jqlExpression.ToString(), maxIssues, startAt, cancellationToken);
        }

        public static IJqlResultsAsyncEnumerable<Issue> QueryIssuesAsyncEnum(this IIssueService issueService,
            string jqlString, int? maxIssues = null, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
                issueService.GetIssuesFromJqlAsync(jqlString, maxIssues, startPageAt, cancellationToken);

            return new JqlResultsAsyncEnumerable(getNextPage, startAt, jqlString, maxIssues);
        }

        public static IJqlResultsAsyncEnumerable<Issue> QueryIssuesAsyncEnum(this IIssueService issueService,
            IJqlExpression jqlExpression, int? maxIssues = null, int startAt = 0)
        {
            return QueryIssuesAsyncEnum(issueService, jqlExpression.ToString(), maxIssues, startAt);
        }

        public static IJqlResultsAsyncEnumerable<Issue> QueryIssuesAsyncEnum(this IIssueService issueService,
            IssueSearchOptions options)
        {
            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
            {
                options.StartAt = startPageAt;
                return issueService.GetIssuesFromJqlAsync(options, cancellationToken);
            };

            return new JqlResultsAsyncEnumerable(getNextPage, options.StartAt, options.Jql, null);
        }

        public static IJiraAsyncEnumerable<Comment> GetCommentsAsyncEnum(
            this IIssueService issueService, string issueKey, int? maxComments, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Comment> getNextPage = (startPageAt, cancellationToken) =>
                issueService.GetPagedCommentsAsync(issueKey, maxComments, startPageAt, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt, maxComments);
        }

        public static IJiraAsyncEnumerable<Issue> GetSubTasksAsyncEnum(
            this IIssueService issueService, string issueKey, int? maxIssues, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
                issueService.GetSubTasksAsync(issueKey, maxIssues, startAt, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt, maxIssues);
        }

        public static IAsyncJiraQueryable<Issue> GetAsyncQueryable(this IIssueService issueService) =>
            new AsyncJiraQueryable<Issue>(issueService);

        // The way to create a new issue in a Jira instance is using the Issue class’s constructor,
        // but this requires the root Jira object. I am purposefully avoiding having the
        // root Jira object available via dependency injection; hence this extension method.
        // I might have designed the SDK so that the Issue class does not depend on the
        // root Jira class and all issue operations use the services, but ¯\_(ツ)_/¯
        public static Issue NewIssue(this IIssueService issueService, string projectKey, string? parentIssueKey = null)
        {
            // HACK Pull the root Jira object out via reflection
            var jiraRoot = (JiraRoot?) issueService.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(f => f.FieldType == typeof(JiraRoot))
                .GetValue(issueService);

            return new Issue(jiraRoot, projectKey, parentIssueKey);
        }

        public static Issue NewIssue(this IIssueService issueService, Project project, Issue? parent = null) =>
            issueService.NewIssue(project.Key, parent?.Key.Value);
    }
}