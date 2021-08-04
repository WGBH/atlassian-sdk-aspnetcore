using Atlassian.Jira.Async;

namespace Atlassian.Jira
{
    public static class JiraGroupServiceExtensions
    {
        public static IJiraAsyncEnumerable<JiraUser> GetUsersAsyncEnum(this IJiraGroupService groupService,
            string groupName, bool includeInactiveUsers = false, int maxResults = 50, int startAt = 0)
        {
            JiraAsyncEnumerable.Pager<JiraUser> getNextPage = (startPageAt, cancellationToken) =>
                groupService.GetUsersAsync(groupName, includeInactiveUsers, maxResults, startPageAt, cancellationToken);

            return JiraAsyncEnumerable.Create(getNextPage, startAt, maxResults);
        }
    }
}