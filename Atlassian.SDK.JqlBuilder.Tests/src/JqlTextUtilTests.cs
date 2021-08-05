using System;
using Xunit;

using static Atlassian.Jira.JqlBuilder.Jql;

namespace Atlassian.Jira.AspNetCore.Tests
{
    public class JqlBuilderTests
    {
        [Fact]
        public void ShouldEscapeValuesProperly()
        {
            var jql1 = Field("assignee") == "Bobby O'Shea";
            Assert.Equal("'assignee' = 'Bobby O\\'Shea'", jql1.ToString());

            var jql2 = Fields.Created == new DateTime(1984, 6, 3, 8, 20, 34);
            Assert.Equal("'created' = '1984/06/03 08:20'", jql2.ToString());

            var jql3 = Field("created") == new DateTime(1984, 6, 3);
            Assert.Equal("'created' = '1984/06/03'", jql3.ToString());

            var jql4 = Fields.CustomerRequestType == "new compy";
            Assert.Equal("'Customer Request Type' = 'new compy'", jql4.ToString());

            var jql5 = Fields.OriginalEstimate >= 240;
            Assert.Equal("'originalEstimate' >= 240", jql5.ToString());
        }
    }
}