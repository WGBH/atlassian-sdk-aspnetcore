// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

using static Atlassian.Jira.JqlBuilder.Jql;

namespace Atlassian.Jira.JqlBuilder
{
    public class ExpressionTests
    {
        [Fact]
        public void ShouldBuildProperJqlAnyAll()
        {
            var jql1 = All
            (
                Fields.Project == "PROJ",
                Fields.Assignee == "rambi_suzuki",
                Field("project") == "PROJ"
            ).ToString();
            Assert.Single(Regex.Matches(jql1, "PROJ"));
            Assert.Single(Regex.Matches(jql1, "rambi_suzuki"));
            Assert.Matches("^\\(('project' = 'PROJ'|'assignee' = 'rambi_suzuki') AND ('project' = 'PROJ'|'assignee' = 'rambi_suzuki')\\)$", jql1);

            var jql2 = Any
            (
                Field("project") == "PROJ",
                Fields.Assignee == "rambi_suzuki",
                Fields.Project == "PROJ"
            ).ToString();
            Assert.Single(Regex.Matches(jql2, "PROJ"));
            Assert.Single(Regex.Matches(jql2, "rambi_suzuki"));
            Assert.Matches("^\\(('project' = 'PROJ'|'assignee' = 'rambi_suzuki') OR ('project' = 'PROJ'|'assignee' = 'rambi_suzuki')\\)$", jql2);
        }

        [Fact]
        public void ShouldBuildProperJqlLogical()
        {
            // following operator precedence, we expect the ANDs to be evaluated before the OR
            var jql =
            (
                Fields.Comment.IsNotEmpty() & Fields.Component.IsEmpty()
                    | Fields.Attachments.IsEmpty() & Fields.Assignee.IsNotEmpty()
            ).ToString();

            var part1a = "'comment' IS NOT EMPTY";
            var part1b = "'component' IS EMPTY";
            var part2a = "'attachments' IS EMPTY";
            var part2b = "'assignee' IS NOT EMPTY";

            Assert.Single(Regex.Matches(jql, part1a));
            Assert.Single(Regex.Matches(jql, part1b));
            Assert.Single(Regex.Matches(jql, part2a));
            Assert.Single(Regex.Matches(jql, part2b));

            var part1 = $"\\(({part1a}|{part1b}) AND ({part1a}|{part1b})\\)";
            var part2 = $"\\(({part2a}|{part2b}) AND ({part2a}|{part2b})\\)";

            Assert.Single(Regex.Matches(jql, part1));
            Assert.Single(Regex.Matches(jql, part2));

            // expected output is similar to
            // (("'comment' IS NOT EMPTY" AND "'component' IS EMPTY") OR ("'attachments' IS EMPTY" AND "'assignee' IS NOT EMPTY"))
            var full = $"^\\(({part1}|{part2}) OR ({part1}|{part2})\\)$";

            Assert.Matches(full, jql);
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
        public void ShouldBuildProperJqlMultiValue()
        {
            var jql1 = Fields.Project.In("PROJ", "JORP", "PROJ").ToString();
            Assert.Single(Regex.Matches(jql1, "PROJ"));
            Assert.Single(Regex.Matches(jql1, "JORP"));
            Assert.Matches("^'project' IN \\('(PROJ|JORP)', '(PROJ|JORP)'\\)$", jql1);

            var jql2 = Field("project").NotIn("PROJ", "JORP", "JORP").ToString();
            Assert.Single(Regex.Matches(jql2, "PROJ"));
            Assert.Single(Regex.Matches(jql2, "JORP"));
            Assert.Matches("^'project' NOT IN \\('(PROJ|JORP)', '(PROJ|JORP)'\\)$", jql2);
        }

        [Fact]
        public void ShouldGuardAgainstNullValuesInFilter()
        {
            Assert.Throws<ArgumentNullException>(() => All(null!));

            Assert.Throws<ArgumentException>(() => Any(Field("foo") !=  "bar", null!));

            Assert.Throws<ArgumentException>(() => Fields.Approvals.IsEmpty() & null!);

            Assert.Throws<ArgumentException>(() => null! | Fields.Approvals.IsNotEmpty());

            Assert.Throws<ArgumentNullException>(() => default(JqlField)! == "bar");

            Assert.Throws<ArgumentNullException>(() => Field("assignee") == null!);

            Assert.Throws<ArgumentNullException>(() => Fields.Assignee.In(null!));

            Assert.Throws<ArgumentException>(() => Fields.Assignee.NotIn("jorpo_demerrich", null!));
        }

        [Fact]
        public void ShouldBuildProperJqlOrderBy()
        {
            var jql1 = (Field("project") == "PROJ").OrderBy("created");
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' ASC", jql1.ToString());

            var jql2 = (Fields.Project== "PROJ").OrderBy("created", SortDirection.Descending);
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' DESC", jql2.ToString());

            var jql3 = (Field("project") == "PROJ")
                .OrderBy(("created", SortDirection.Ascending), ("assigned", SortDirection.Descending));
            Assert.Equal("'project' = 'PROJ' ORDER BY 'created' ASC, 'assigned' DESC", jql3.ToString());
        }

        [Fact]
        public void ShouldGuardAgainstNullValuesInOrderBy()
        {
            Assert.Throws<ArgumentNullException>(() => Field("foo").IsEmpty()
                .OrderBy(default(JqlField)!, SortDirection.Descending));

            Assert.Throws<ArgumentNullException>(() => Field("foo").IsEmpty().OrderBy(Field("foo"), null!));

            Assert.Throws<ArgumentNullException>(() => Field("foo").IsEmpty()
                .OrderBy(default(IEnumerable<(JqlField, JqlSortDirection)>)!));

            Assert.Throws<ArgumentNullException>(() => Field("foo").IsEmpty()
                .OrderBy((Field("foo"), SortDirection.Ascending), default((JqlField, JqlSortDirection))));
        }
    }
}