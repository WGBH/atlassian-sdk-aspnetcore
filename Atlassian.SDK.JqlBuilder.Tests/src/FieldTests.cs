using System;
using Xunit;

using static Atlassian.Jira.JqlBuilder.Jql;

namespace Atlassian.Jira.JqlBuilder
{
    public class FieldTests
    {
        [Fact]
        public void ShouldGuardAgainstNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => Field(null!));
        }
    }
}