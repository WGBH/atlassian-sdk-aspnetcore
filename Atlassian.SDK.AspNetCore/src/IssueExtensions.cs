// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using Atlassian.Jira.Async;

namespace Atlassian.Jira
{
    public static class IssueExtensions
    {
        public static IJiraAsyncEnumerable<Issue> GetSubTasksAsyncEnum(
            this Issue issue, int? maxIssues = null, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
                issue.GetSubTasksAsync(maxIssues, startPageAt, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt, maxIssues);
        }

        public static IJiraAsyncEnumerable<Comment> GetCommentsAsyncEnum(
            this Issue issue, int? maxComments = null, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Comment> getNextPage = (startPageAt, cancellationToken) =>
                issue.GetPagedCommentsAsync(maxComments, startAt, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt, maxComments);
        }
    }
}