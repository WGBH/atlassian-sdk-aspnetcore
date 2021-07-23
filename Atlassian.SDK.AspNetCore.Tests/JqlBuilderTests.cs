using System;
using System.Text.RegularExpressions;
using Xunit;

using static Atlassian.Jira.JqlBuilder.Jql;
using static Atlassian.Jira.JqlBuilder.JqlOperator.Direction;

namespace Atlassian.Jira.AspNetCore.Tests
{
    public class JqlBuilderTests
    {
        [Fact]
        public void ShouldBuildProperJqlLogical()
        {
            var jql1 = And
            (
                Field("project") == "PROJ",
                Field("assignee") == "rambi_suzuki",
                Field("project") == "PROJ"
            ).ToString();
            Assert.Single(Regex.Matches(jql1, "PROJ"));
            Assert.Single(Regex.Matches(jql1, "rambi_suzuki"));
            Assert.Matches("\\(('project' = 'PROJ'|'assignee' = 'rambi_suzuki') AND ('project' = 'PROJ'|'assignee' = 'rambi_suzuki')\\)", jql1);

            var jql2 = Or
            (
                Field("project") == "PROJ",
                Field("assignee") == "rambi_suzuki",
                Field("project") == "PROJ"
            ).ToString();
            Assert.Single(Regex.Matches(jql2, "PROJ"));
            Assert.Single(Regex.Matches(jql2, "rambi_suzuki"));
            Assert.Matches("\\(('project' = 'PROJ'|'assignee' = 'rambi_suzuki') OR ('project' = 'PROJ'|'assignee' = 'rambi_suzuki')\\)", jql2);
        }

        [Fact]
        public void ShouldBuildProperJqlBinary()
        {
            var jql1 = Field("project") == "PROJ";
            Assert.Equal("'project' = 'PROJ'", jql1.ToString());

            var jql2 = Field("project") != "PROJ";
            Assert.Equal("'project' != 'PROJ'", jql2.ToString());

            var jql3 = Field("project") > "PROJ";
            Assert.Equal("'project' > 'PROJ'", jql3.ToString());

            var jql4 = Field("project") >= "PROJ";
            Assert.Equal("'project' >= 'PROJ'", jql4.ToString());

            var jql5 = Field("project") < "PROJ";
            Assert.Equal("'project' < 'PROJ'", jql5.ToString());

            var jql6 = Field("project") <= "PROJ";
            Assert.Equal("'project' <= 'PROJ'", jql6.ToString());

            var jql7 = Field("summary").Like("test");
            Assert.Equal("'summary' ~ 'test'", jql7.ToString());

            var jql8 = Field("summary").NotLike("test");
            Assert.Equal("'summary' !~ 'test'", jql8.ToString());
        }

        [Fact]
        public void ShouldBuildProperJqlMulti()
        {
            var jql1 = Field("project").In("PROJ", "JORP", "PROJ").ToString();
            Assert.Single(Regex.Matches(jql1, "PROJ"));
            Assert.Single(Regex.Matches(jql1, "JORP"));
            Assert.Matches("'project' IN \\('(PROJ|JORP)', '(PROJ|JORP)'\\)", jql1);

            var jql2 = Field("project").NotIn("PROJ", "JORP", "JORP").ToString();
            Assert.Single(Regex.Matches(jql2, "PROJ"));
            Assert.Single(Regex.Matches(jql2, "JORP"));
            Assert.Matches("'project' NOT IN \\('(PROJ|JORP)', '(PROJ|JORP)'\\)", jql2);
        }

        [Fact]
        public void ShouldEscapeJqlValuesProperly()
        {
            var jql1 = Field("assignee") == "Bobby O'Shea";
            Assert.Equal("'assignee' = 'Bobby O\\'Shea'", jql1.ToString());

            var jql2 = Field("assignee") == null;
            Assert.Equal("'assignee' = null", jql2.ToString());

            var jql3 = Field("created") == new DateTime(1984, 6, 3, 8, 20, 34);
            Assert.Equal("'created' = '1984/06/03 08:20'", jql3.ToString());

            var jql4 = Field("created") == new DateTime(1984, 6, 3);
            Assert.Equal("'created' = '1984/06/03'", jql4.ToString());
        }

        [Fact]
        public void ShouldBuildProperJqlOrderBy()
        {
            var jql1 = (Field("project") == "PROJ").OrderBy("created");
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' ASC", jql1.ToString());

            var jql2 = (Field("project") == "PROJ").OrderBy("created", Descending);
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' DESC", jql2.ToString());

            var jql3 = (Field("project") == "PROJ").OrderBy(("created", Ascending), ("assigned", Descending));
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' ASC, 'assigned' DESC", jql3.ToString());
        }
    }
}