using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Atlassian.Jira.JqlBuilder;
using Xunit;

using static Atlassian.Jira.JqlBuilder.Jql;

namespace Atlassian.Jira.AspNetCore.Tests
{
    public class JqlBuilderTests
    {
        [Fact]
        public void ShouldBuildProperJqlLogical()
        {
            var jql1 = And
            (
                Fields.Project == "PROJ",
                Fields.Assignee == "rambi_suzuki",
                Field("project") == "PROJ"
            ).ToString();
            Assert.Single(Regex.Matches(jql1, "PROJ"));
            Assert.Single(Regex.Matches(jql1, "rambi_suzuki"));
            Assert.Matches("\\(('project' = 'PROJ'|'assignee' = 'rambi_suzuki') AND ('project' = 'PROJ'|'assignee' = 'rambi_suzuki')\\)", jql1);

            var jql2 = Or
            (
                Field("project") == "PROJ",
                Fields.Assignee == "rambi_suzuki",
                Fields.Project == "PROJ"
            ).ToString();
            Assert.Single(Regex.Matches(jql2, "PROJ"));
            Assert.Single(Regex.Matches(jql2, "rambi_suzuki"));
            Assert.Matches("\\(('project' = 'PROJ'|'assignee' = 'rambi_suzuki') OR ('project' = 'PROJ'|'assignee' = 'rambi_suzuki')\\)", jql2);
        }

        [Fact]
        public void ShouldBuildProperJqlExistence()
        {
            var jql1 = Fields.Assignee.IsEmpty();
            Assert.Equal("'assignee' IS EMPTY", jql1.ToString());

            var jql2 = Field("assignee").IsNotEmpty();
            Assert.Equal("'assignee' IS NOT EMPTY", jql2.ToString());
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

            var jql4 = Fields.Project >= "PROJ";
            Assert.Equal("'project' >= 'PROJ'", jql4.ToString());

            var jql5 = Fields.Project < "PROJ";
            Assert.Equal("'project' < 'PROJ'", jql5.ToString());

            var jql6 = Fields.Project <= "PROJ";
            Assert.Equal("'project' <= 'PROJ'", jql6.ToString());

            var jql7 = Fields.Summary.Like("test");
            Assert.Equal("'summary' ~ 'test'", jql7.ToString());

            var jql8 = Field("summary").NotLike("test");
            Assert.Equal("'summary' !~ 'test'", jql8.ToString());
        }

        [Fact]
        public void ShouldBuildProperJqlMulti()
        {
            var jql1 = Fields.Project.In("PROJ", "JORP", "PROJ").ToString();
            Assert.Single(Regex.Matches(jql1, "PROJ"));
            Assert.Single(Regex.Matches(jql1, "JORP"));
            Assert.Matches("'project' IN \\('(PROJ|JORP)', '(PROJ|JORP)'\\)", jql1);

            var jql2 = Field("project").NotIn("PROJ", "JORP", "JORP").ToString();
            Assert.Single(Regex.Matches(jql2, "PROJ"));
            Assert.Single(Regex.Matches(jql2, "JORP"));
            Assert.Matches("'project' NOT IN \\('(PROJ|JORP)', '(PROJ|JORP)'\\)", jql2);
        }

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
        }

        [Fact]
        public void ShouldBuildProperJqlOrderBy()
        {
            var jql1 = (Field("project") == "PROJ").OrderBy("created");
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' ASC", jql1.ToString());

            var jql2 = (Fields.Project== "PROJ").OrderBy("created", Direction.Descending);
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' DESC", jql2.ToString());

            var jql3 = (Field("project") == "PROJ")
                .OrderBy(("created", Direction.Ascending), ("assigned", Direction.Descending));
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' ASC, 'assigned' DESC", jql3.ToString());
        }

        [Fact]
        public void ShouldGuardAgainstNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => Field(null!));

            Assert.Throws<ArgumentNullException>(() => And(null!));

            Assert.Throws<ArgumentException>(() => Or(Field("foo") !=  "bar", null!));

            Assert.Throws<ArgumentNullException>(() => ((JqlField)null!) == "bar");

            Assert.Throws<ArgumentNullException>(() => Field("assignee") == null!);

            Assert.Throws<ArgumentNullException>(() => Fields.Assignee.In(null!));

            Assert.Throws<ArgumentException>(() => Fields.Assignee.NotIn("jorpo_demerrich", null!));

            Assert.Throws<ArgumentNullException>(() => Field("foo").IsEmpty()
                .OrderBy((JqlField) null!, Direction.Descending));

            Assert.Throws<ArgumentNullException>(() => Field("foo").IsEmpty().OrderBy(Field("foo"), null!));

            Assert.Throws<ArgumentNullException>(() => Field("foo").IsEmpty()
                .OrderBy((IEnumerable<(JqlField, JqlOperator.Direction)>) null!));

            Assert.Throws<ArgumentNullException>(() => Field("foo").IsEmpty()
                .OrderBy((Field("foo"), Direction.Ascending), default((JqlField, JqlOperator.Direction))));
        }
    }
}