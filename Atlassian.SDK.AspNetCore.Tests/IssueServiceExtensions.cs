using System.Linq;
using System.Reflection;
using Xunit;
using JiraRoot = Atlassian.Jira.Jira;

namespace Atlassian.Jira.AspNetCore.Tests
{
    public class IssueServiceExtensions
    {
        [Fact]
        public void ShouldReflectMemberOut()
        {
            var jiraRootIn = JiraRoot.CreateRestClient("https://example.com");
            var issueService = jiraRootIn.Issues;

            var jiraRootOut = (JiraRoot?) issueService.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(f => f.FieldType == typeof(JiraRoot))
                .GetValue(issueService);

            Assert.NotNull(jiraRootOut);
        }
    }
}
