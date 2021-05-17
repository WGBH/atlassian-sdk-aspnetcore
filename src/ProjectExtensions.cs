namespace Atlassian.Jira.AspNetCore
{
    public static class ProjectExtensions
    {
        public static IJiraAsyncEnumerable<ProjectVersion> GetVersionsAsyncEnum(
            this Project project, int startAt = 0, int maxResults = 50)
        {
            JiraAsyncEnumerable.Pager<ProjectVersion> getNextPage = (startPageAt, cancellationToken) =>
                project.GetPagedVersionsAsync(startPageAt, maxResults, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt, maxResults);
        }
    }
}