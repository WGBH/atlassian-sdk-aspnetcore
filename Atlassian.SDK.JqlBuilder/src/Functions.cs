// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlassian.Jira.JqlBuilder
{
    // Note: This class is special cased in JqlTextUtil.EscapeValue() and JqlFilterExpression.MultiValue.ToString()
    public class JqlFunction
    {
        public string Name { get; }
        public IReadOnlyList<string> Arguments { get; }

        internal JqlFunction(string name, IEnumerable<string> arguments)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));
            if (arguments.Any(a => a == null))
                throw new ArgumentException("Collection must not contain the null value!", nameof(arguments));

            Name = name;
            Arguments = arguments.ToList().AsReadOnly();
        }

        internal JqlFunction(string name, params string[] arguments)
            : this(name, (IEnumerable<string>) arguments) { }

        public override string ToString() =>
            Name + '(' + String.Join(", ", Arguments.Select(a => JqlTextUtil.EscapeValue(a))) + ')';

        public override bool Equals(object? obj) =>
            obj is JqlFunction other && Name == other.Name && Arguments.SequenceEqual(other.Arguments);

        public override int GetHashCode() =>
            HashCode.Combine(Name, Arguments);
    }

    partial class Jql
    {
        public static class Functions
        {
            public static JqlFunction Approved() =>
                new JqlFunction("approved");

            public static JqlFunction Approver(string username1, string username2) =>
                new JqlFunction("approver", username1, username2);

            public static JqlFunction Breached() =>
                new JqlFunction("breached");

            public static JqlFunction CascadeOption(string parentOption) =>
                new JqlFunction("cascadeOption", parentOption);

            public static JqlFunction CascadeOption(string parentOption, string childOption) =>
                new JqlFunction("cascadeOption", parentOption, childOption);

            public static JqlFunction ClosedSprints() =>
                new JqlFunction("closedSprints");

            public static JqlFunction Completed() =>
                new JqlFunction("completed");

            public static JqlFunction ComponentsLeadByUser() =>
                new JqlFunction("componentsLeadByUser");

            public static JqlFunction ComponentsLeadByUser(string username) =>
                new JqlFunction("componentsLeadByUser", username);

            public static JqlFunction CurrentLogin() =>
                new JqlFunction("currentLogin");

            public static JqlFunction CurrentUser() =>
                new JqlFunction("currentUser");

            public static JqlFunction EarliestUnreleasedVersion(string project) =>
                new JqlFunction("earliestUnreleasedVersion", project);

            public static JqlFunction Elapsed() =>
                new JqlFunction("elapsed");

            public static JqlFunction EndOfDay() =>
                new JqlFunction("endOfDay");

            public static JqlFunction EndOfDay(string increment) =>
                new JqlFunction("endOfDay", increment);

            public static JqlFunction EndOfMonth() =>
                new JqlFunction("endOfMonth");

            public static JqlFunction EndOfMonth(string increment) =>
                new JqlFunction("endOfMonth", increment);

            public static JqlFunction EndOfWeek() =>
                new JqlFunction("endOfWeek");

            public static JqlFunction EndOfWeek(string increment) =>
                new JqlFunction("endOfWeek", increment);

            public static JqlFunction EndOfYear() =>
                new JqlFunction("endOfYear");

            public static JqlFunction EndOfYear(string increment) =>
                new JqlFunction("endOfYear", increment);

            public static JqlFunction IssueHistory() =>
                new JqlFunction("issueHistory");

            public static JqlFunction IssuesWithRemoteLinksByGlobalId() =>
                new JqlFunction("issuesWithRemoteLinksByGlobalId");

            public static JqlFunction LastLogin() =>
                new JqlFunction("lastLogin");

            public static JqlFunction LatestReleasedVersion(string project) =>
                new JqlFunction("latestReleasedVersion", project);

            public static JqlFunction LinkedIssues(string issueKey) =>
                new JqlFunction("linkedIssues", issueKey);

            public static JqlFunction LinkedIssues(string issueKey, string linkType) =>
                new JqlFunction("linkedIssues", issueKey, linkType);

            public static JqlFunction MembersOf(string group) =>
                new JqlFunction("membersOf", group);

            public static JqlFunction MyApproval() =>
                new JqlFunction("myApproval");

            public static JqlFunction MyPending() =>
                new JqlFunction("myPending");

            public static JqlFunction Now() =>
                new JqlFunction("now");

            public static JqlFunction OpenSprints() =>
                new JqlFunction("openSprints");

            public static JqlFunction Paused() =>
                new JqlFunction("paused");

            public static JqlFunction Pending() =>
                new JqlFunction("pending");

            public static JqlFunction PendingBy(string username1, string username2) =>
                new JqlFunction("pendingBy", username1, username2);

            public static JqlFunction ProjectsLeadByUser() =>
                new JqlFunction("projectsLeadByUser");

            public static JqlFunction ProjectsLeadByUser(string username) =>
                new JqlFunction("projectsLeadByUser", username);

            public static JqlFunction ProjectsWhereUserHasPermission(string permission) =>
                new JqlFunction("projectsWhereUserHasPermission", permission);

            public static JqlFunction ProjectsWhereUserHasRole(string role) =>
                new JqlFunction("projectsWhereUserHasRole", role);

            public static JqlFunction ReleasedVersions() =>
                new JqlFunction("releasedVersions");

            public static JqlFunction ReleasedVersions(string project) =>
                new JqlFunction("releasedVersions", project);

            public static JqlFunction Remaining() =>
                new JqlFunction("remaining");

            public static JqlFunction Running() =>
                new JqlFunction("running");

            public static JqlFunction StandardIssueTypes() =>
                new JqlFunction("standardIssueTypes");

            public static JqlFunction StartOfDay() =>
                new JqlFunction("startOfDay");

            public static JqlFunction StartOfDay(string increment) =>
                new JqlFunction("startOfDay", increment);

            public static JqlFunction StartOfMonth() =>
                new JqlFunction("startOfMonth");

            public static JqlFunction StartOfMonth(string increment) =>
                new JqlFunction("startOfMonth", increment);

            public static JqlFunction StartOfWeek() =>
                new JqlFunction("startOfWeek");

            public static JqlFunction StartOfWeek(string increment) =>
                new JqlFunction("startOfWeek", increment);

            public static JqlFunction StartOfYear() =>
                new JqlFunction("startOfYear");

            public static JqlFunction StartOfYear(string increment) =>
                new JqlFunction("startOfYear", increment);

            public static JqlFunction SubtaskIssueTypes() =>
                new JqlFunction("subtaskIssueTypes");

            public static JqlFunction UnreleasedVersions() =>
                new JqlFunction("unreleasedVersions");

            public static JqlFunction UnreleasedVersions(string project) =>
                new JqlFunction("unreleasedVersions", project);

            public static JqlFunction UpdatedBy(string username) =>
                new JqlFunction("updatedBy", username);

            public static JqlFunction UpdatedBy(string username, DateTime dateFrom) =>
                new JqlFunction("updatedBy", username, JqlTextUtil.FormatDateTime(dateFrom));

            public static JqlFunction UpdatedBy(string username, DateTime dateFrom, DateTime dateTo) =>
                new JqlFunction("updatedBy", username, JqlTextUtil.FormatDateTime(dateFrom), JqlTextUtil.FormatDateTime(dateTo));

            public static JqlFunction VotedIssues() =>
                new JqlFunction("votedIssues");

            public static JqlFunction WatchedIssues() =>
                new JqlFunction("watchedIssues");

            public static JqlFunction WithinCalendarHours() =>
                new JqlFunction("withinCalendarHours");
        }

        // copy references for convenience when used with 'import static ...Jql'
        public static class SortDirection
        {
            public static readonly JqlSortDirection Ascending = JqlSortDirection.Ascending;
            public static readonly JqlSortDirection Descending = JqlSortDirection.Descending;
        }
    }
}