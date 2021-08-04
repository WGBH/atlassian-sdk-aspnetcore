// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using Atlassian.Jira.Async;

namespace Atlassian.Jira
{
    public static class IssueFilterServiceExtensions
    {
        public static IJiraAsyncEnumerable<Issue> GetIssuesAsyncEnum(
            this IIssueFilterService filterService, string filterName, int? maxIssues = null, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<Issue> getNextPage = (startPageAt, cancellationToken) =>
                filterService.GetIssuesFromFavoriteAsync(filterName, maxIssues, startAt, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt, maxIssues);
        }
    }
}