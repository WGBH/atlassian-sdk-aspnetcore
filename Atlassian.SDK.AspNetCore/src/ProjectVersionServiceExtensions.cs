// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using Atlassian.Jira.Async;

namespace Atlassian.Jira
{
    public static class ProjectVersionServiceExtensions
    {
        public static IJiraAsyncEnumerable<ProjectVersion> GetVersionsAsyncEnum(
            this IProjectVersionService versionsService, string projectKey, int startAt = 0, int maxResults = 50)
        {
            JiraAsyncEnumerable.Pager<ProjectVersion> getNextPage = (startPageAt, cancellationToken) =>
                versionsService.GetPagedVersionsAsync(projectKey, startPageAt, maxResults, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt, maxResults);
        }
    }
}