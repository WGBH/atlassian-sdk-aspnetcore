// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using Xunit;

using static Atlassian.Jira.JqlBuilder.Jql;

namespace Atlassian.Jira.JqlBuilder
{
    public class FunctionTests
    {
        [Fact]
        public void ShouldBuildProperJqlFunction()
        {
            var jql1 = Fields.Reporter == Functions.CurrentUser();
            Assert.Equal("'reporter' = currentUser()", jql1.ToString());

            var jql2 = Fields.FixVersion.In(Function("unreleasedVersions", "JORP"));
            Assert.Equal("'fixVersion' IN unreleasedVersions('JORP')", jql2.ToString());

            var jql3 = Fields.IssueKey.In(
                Functions.UpdatedBy("mira_kajira", new DateTime(2020, 02, 01), new DateTime(2020, 02, 29)));
            Assert.Equal("'issueKey' IN updatedBy('mira_kajira', '2020/02/01', '2020/02/29')", jql3.ToString());
        }

        [Fact]
        public void ShouldGuardAgainstNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => Fields.Status == Function(null!, "bar"));

            Assert.Throws<ArgumentNullException>(() => Fields.Status == Function("dummy", default(IEnumerable<String>)!));

            Assert.Throws<ArgumentException>(() => Fields.Created > Functions.StartOfWeek(null!));
        }
    }
}