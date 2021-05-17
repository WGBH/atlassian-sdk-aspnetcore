namespace Atlassian.Jira.AspNetCore
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