using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Atlassian.Jira.JqlBuilder
{
    public interface IJqlOperator
    {
        string Type { get; }
        string Value { get; }
    }

    public abstract class JqlFilterOperator : IJqlOperator
    {
        public string Type { get; }
        public string Value { get; }

        private protected JqlFilterOperator(string type, string value)
        {
            Type = type;
            Value = value;
        }

        internal sealed class Logical : JqlFilterOperator
        {
            Logical(string type, string value)
                : base(type, value) { }

            public static Logical And
                = new Logical(nameof(And), "AND");
            public static Logical Or
                = new Logical(nameof(Or), "OR");
        }

        internal sealed class Existence : JqlFilterOperator
        {
            Existence(string type, string value)
                : base(type, value) { }

            public static Existence IsEmpty
                = new Existence(nameof(IsEmpty), "IS EMPTY");

            public static Existence IsNotEmpty
                = new Existence(nameof(IsNotEmpty), "IS NOT EMPTY");
        }

        internal sealed class Binary : JqlFilterOperator
        {
            Binary(string type, string value)
                : base(type, value) { }

            public static readonly Binary Equal
                = new Binary(nameof(Equal), "=");
            public static readonly Binary NotEqual
                = new Binary(nameof(NotEqual), "!=");
            public static readonly Binary Like
                = new Binary(nameof(Like), "~");
            public static readonly Binary NotLike
                = new Binary(nameof(NotLike), "!~");
            public static readonly Binary GreaterThan
                = new Binary(nameof(GreaterThan), ">");
            public static readonly Binary GreaterThanOrEqual
                = new Binary(nameof(GreaterThanOrEqual), ">=");
            public static readonly Binary LessThan
                = new Binary(nameof(LessThan), "<");
            public static readonly Binary LessThanOrEqual
                = new Binary(nameof(LessThanOrEqual), "<=");
        }

        internal sealed class MultiValue : JqlFilterOperator
        {
            MultiValue(string type, string value)
                : base(type, value) { }

            public static readonly MultiValue In
                = new MultiValue(nameof(In), "IN");
            public static readonly MultiValue NotIn
                = new MultiValue(nameof(NotIn), "NOT IN");
        }

        // This assumes the operator values are distinct across all subclasses
        public override bool Equals(object? obj) =>
            obj is JqlFilterOperator other && Value == other.Value;

        public override int GetHashCode() =>
            Value.GetHashCode();
    }

    public sealed class JqlSortDirection : IJqlOperator
    {
        public string Type { get; }
        public string Value { get; }

        private JqlSortDirection(string type, string value)
        {
            Type = type;
            Value = value;
        }

        public static readonly JqlSortDirection Ascending
            = new JqlSortDirection(nameof(Ascending), "ASC");
        public static readonly JqlSortDirection Descending
            = new JqlSortDirection(nameof(Descending), "DESC");

        public override bool Equals(object? obj) =>
            obj is JqlSortDirection other && Value == other.Value;

        public override int GetHashCode() =>
            Value.GetHashCode();
    }

    static class JqlTextUtil
    {
        public static string EscapeValue(object value) =>
            value switch
            {
                DateTime dt => '\'' + FormatDateTime(dt) + '\'',
                JqlFunction function => function.ToString(),
                _ => "'" + value.ToString()!.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'") + "'"
            };

        public static string FormatDateTime(DateTime dateTime) =>
            (dateTime == dateTime.Date)
                ? dateTime.ToString("yyyy/MM/dd")
                : dateTime.ToString("yyyy/MM/dd HH:mm");
    }

    public interface IJqlExpression
    {
        [return: NotNull]
        string ToString();
    }

    public abstract class JqlFilterExpression : IJqlExpression
    {
        public string Type { get; }
        public JqlFilterOperator Operator { get; }

        private protected JqlFilterExpression(string type, JqlFilterOperator oper)
        {
            Type = type;
            Operator = oper;
        }

        public abstract override string ToString();
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();

        internal sealed class Logical : JqlFilterExpression
        {
            readonly IReadOnlySet<JqlFilterExpression> _expressions;

            internal Logical(JqlFilterOperator.Logical oper, IEnumerable<JqlFilterExpression> expressions)
                : base(nameof(Logical), oper)
            {
                if (expressions == null)
                    throw new ArgumentNullException(nameof(expressions));
                if (expressions.Any(e => e == null))
                    throw new ArgumentException("Collection must not contain the null value!", nameof(expressions));

                _expressions = expressions.ToImmutableHashSet();
            }

            public override string ToString() =>
                "(" + string.Join(" " + Operator.Value + " ", _expressions) + ")";

            public override bool Equals(object? obj) =>
                obj is Logical other && Operator.Equals(other.Operator) && _expressions.SequenceEqual(other._expressions);

            public override int GetHashCode() =>
                HashCode.Combine(Operator, _expressions);
        }

        internal sealed class Existence : JqlFilterExpression
        {
            readonly JqlField _field;

            internal Existence(JqlField field, JqlFilterOperator.Existence oper) : base(nameof(Existence), oper) =>
                _field = field;

            public override string ToString() =>
                _field.ToString() + ' ' + Operator.Value;

            public override bool Equals(object? obj) =>
                obj is Existence other && _field.Equals(other._field) && Operator.Equals(other.Operator);

            public override int GetHashCode() =>
                HashCode.Combine(_field, Operator);
        }

        internal sealed class Binary : JqlFilterExpression
        {
            readonly JqlField _field;
            readonly object _value;

            internal Binary(JqlField field, JqlFilterOperator.Binary oper, object value) : base(nameof(Binary), oper)
            {
                if ((object) field == null)
                    throw new ArgumentNullException(nameof(field));
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _field = field;
                _value = value;
            }

            public sealed override string ToString() =>
                _field.ToString() + ' ' + Operator.Value + ' ' + JqlTextUtil.EscapeValue(_value);

            public override bool Equals(object? obj) =>
                obj is Binary other && _field.Equals(other._field) && Operator.Equals(other.Operator)
                    && Object.Equals(_value, other._value);

            public override int GetHashCode() =>
                HashCode.Combine(_field, Operator, _value);
        }

        internal sealed class MultiValue : JqlFilterExpression
        {
            readonly JqlField _field;
            readonly IReadOnlySet<object> _values;

            internal MultiValue(JqlField field, JqlFilterOperator.MultiValue oper, IEnumerable<object> values)
                : base(nameof(MultiValue), oper)
            {
                if (values == null)
                    throw new ArgumentNullException(nameof(values));
                if (values.Any(v => v == null))
                    throw new ArgumentException("Collection must not contain the null value!", nameof(values));

                _field = field;
                _values = values.ToImmutableHashSet();
            }

            public override string ToString() =>
                _field.ToString() + ' ' + Operator.Value
                    +  " (" + String.Join(", ", _values.Select(v => JqlTextUtil.EscapeValue(v))) + ")";

            public override bool Equals(object? obj) =>
                obj is MultiValue other && _field.Equals(other._field) && Operator.Equals(other.Operator)
                    && _values.SequenceEqual(other._values);

            public override int GetHashCode() =>
                HashCode.Combine(_field, Operator, _values);
        }

        public JqlSortExpression OrderBy(JqlField field, JqlSortDirection direction) =>
            new JqlSortExpression(this, new[] { new JqlSortField(field, direction) });

        public JqlSortExpression OrderBy(JqlField field) =>
            OrderBy(field, JqlSortDirection.Ascending);

        public JqlSortExpression OrderBy(string field, JqlSortDirection direction) =>
            OrderBy(new JqlField.Simple(field), direction);

        public JqlSortExpression OrderBy(string field) =>
            OrderBy(field, JqlSortDirection.Ascending);

        public JqlSortExpression OrderBy(IEnumerable<(string, JqlSortDirection)> fields) =>
            new JqlSortExpression(this, fields.Select(f => new JqlSortField(new JqlField.Simple(f.Item1), f.Item2)));

        public JqlSortExpression OrderBy(params (string, JqlSortDirection)[] fields) =>
            OrderBy((IEnumerable<(string, JqlSortDirection)>) fields);

        public JqlSortExpression OrderBy(IEnumerable<(JqlField, JqlSortDirection)> fields) =>
            new JqlSortExpression(this, fields.Select(f => new JqlSortField(f.Item1, f.Item2)));

        public JqlSortExpression OrderBy(params (JqlField, JqlSortDirection)[] fields) =>
            OrderBy((IEnumerable<(JqlField, JqlSortDirection)>) fields);

        public static JqlFilterExpression operator &(JqlFilterExpression left, JqlFilterExpression right) =>
            new JqlFilterExpression.Logical(JqlFilterOperator.Logical.And, new[] {left, right});

        public static JqlFilterExpression operator |(JqlFilterExpression left, JqlFilterExpression right) =>
            new JqlFilterExpression.Logical(JqlFilterOperator.Logical.Or, new[] {left, right});
    }

    public sealed class JqlSortExpression : IJqlExpression
    {
        public JqlFilterExpression Expression { get; }
        public IReadOnlyList<JqlSortField> Fields { get; }

        internal JqlSortExpression(JqlFilterExpression expression, IEnumerable<JqlSortField> fields)
        {
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));
            if (fields.Any(f => f == null))
                throw new ArgumentException("Collection must not contain the null value!", nameof(fields));

            Expression = expression;
            Fields = fields.ToImmutableList();
        }

        public override string ToString() =>
            Expression.ToString() + " ORDER BY "
                + String.Join(", ", Fields.Select(f => f.Field.ToString() + ' ' + f.Direction.Value));

        public override bool Equals(object? obj) =>
            obj is JqlSortExpression other && Expression.Equals(other.Expression)
                && Fields.SequenceEqual(other.Fields);

        public override int GetHashCode() =>
            HashCode.Combine(Expression, Fields);
    }

    public abstract class JqlField
    {
        public string Type { get; }
        public abstract string Name { get; }

        private protected JqlField(string type) =>
            Type = type;

        public abstract override string ToString();
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();

        internal sealed class Simple : JqlField
        {
            public override String Name { get; }

            internal Simple(string name) : base(nameof(Simple))
            {
                if (name == null)
                    throw new ArgumentNullException(nameof(name));

                Name = name;
            }

            public override string ToString() =>
                JqlTextUtil.EscapeValue(Name);

            public override bool Equals(object? obj) =>
                obj is JqlField other && Name == other.Name;

            public override int GetHashCode() =>
                Name.GetHashCode();
        }

        internal sealed class Custom : JqlField
        {
            readonly int _id;

            public override string Name =>
                ToString();

            internal Custom(int id) : base(nameof(Custom)) =>
                _id = id;

            public override string ToString() =>
                "cf[" + _id + ']';

            public override bool Equals(object? obj) =>
                obj is Custom other && _id == other._id;

            public override int GetHashCode() =>
                _id;
        }

        internal sealed class Development : JqlField
        {
            readonly string _subscript;
            readonly string _property;

            public override string Name =>
                ToString();

            internal Development(string subscript, string property) : base(nameof(Development))
            {
                _subscript = subscript;
                _property = property;
            }

            public override string ToString() =>
                "Development[" + _subscript + "]." + _property;

            public override bool Equals(object? obj) =>
                obj is Development other && _subscript == other._subscript && _property == other._property;

            public override int GetHashCode() =>
                HashCode.Combine(_subscript, _property);
        }

        public JqlFilterExpression IsEmpty() =>
            new JqlFilterExpression.Existence(this, JqlFilterOperator.Existence.IsEmpty);

        public JqlFilterExpression IsNotEmpty() =>
            new JqlFilterExpression.Existence(this, JqlFilterOperator.Existence.IsNotEmpty);

        public JqlFilterExpression In(IEnumerable<object> values) =>
            new JqlFilterExpression.MultiValue(this, JqlFilterOperator.MultiValue.In, values);

        public JqlFilterExpression In(params object[] values) =>
            In((IEnumerable<object>) values);

        public JqlFilterExpression NotIn(IEnumerable<object> values) =>
            new JqlFilterExpression.MultiValue(this, JqlFilterOperator.MultiValue.NotIn, values);

        public JqlFilterExpression NotIn(params object[] values) =>
            NotIn((IEnumerable<object>) values);

        public JqlFilterExpression Like(object value) =>
            new JqlFilterExpression.Binary(this, JqlFilterOperator.Binary.Like, value);

        public JqlFilterExpression NotLike(object value) =>
            new JqlFilterExpression.Binary(this, JqlFilterOperator.Binary.NotLike, value);

        public static JqlFilterExpression operator ==(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlFilterOperator.Binary.Equal, value);

        public static JqlFilterExpression operator !=(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlFilterOperator.Binary.NotEqual, value);

        public static JqlFilterExpression operator >(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlFilterOperator.Binary.GreaterThan, value);

        public static JqlFilterExpression operator >=(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlFilterOperator.Binary.GreaterThanOrEqual, value);

       public static JqlFilterExpression operator <(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlFilterOperator.Binary.LessThan, value);

        public static JqlFilterExpression operator <=(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlFilterOperator.Binary.LessThanOrEqual, value);
    }

    public sealed class JqlSortField
    {
        public JqlField Field { get; }
        public JqlSortDirection Direction { get; }

        internal JqlSortField(JqlField field, JqlSortDirection direction)
        {
            if ((object) field == null)
                throw new ArgumentNullException(nameof(field));
            if ((object) direction == null)
                throw new ArgumentNullException(nameof(direction));

            Field = field;
            Direction = direction;
        }

        public override bool Equals(object? obj) =>
            obj is JqlSortField other && Field.Equals(other.Field) && Direction == other.Direction;

        public override int GetHashCode() =>
            HashCode.Combine(Field, Direction);
    }

    // Note: This class has a special case in JqlTextUtil.EscapeValue
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
            Arguments = arguments.ToImmutableList();
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

    public static class Jql
    {
        public static JqlFilterExpression All(IEnumerable<JqlFilterExpression> expressions) =>
            new JqlFilterExpression.Logical(JqlFilterOperator.Logical.And, expressions);

        public static JqlFilterExpression All(params JqlFilterExpression[] expressions) =>
            All((IEnumerable<JqlFilterExpression>)expressions);

        public static JqlFilterExpression Any(IEnumerable<JqlFilterExpression> expressions) =>
            new JqlFilterExpression.Logical(JqlFilterOperator.Logical.Or, expressions);

        public static JqlFilterExpression Any(params JqlFilterExpression[] expressions) =>
            Any((IEnumerable<JqlFilterExpression>)expressions);

        public static JqlField Field(string name) =>
            new JqlField.Simple(name);

        public static JqlField Field(int customFieldId) =>
            new JqlField.Custom(customFieldId);

        public static JqlFunction Function(string name, IEnumerable<string> arguments) =>
            new JqlFunction(name, arguments);

        public static JqlFunction Function(string name, params string[] arguments) =>
            new JqlFunction(name, arguments);

        public static class Fields
        {
            public static readonly JqlField AffectedVersion
                = new JqlField.Simple("affectedVersion");
            public static readonly JqlField Approvals
                = new JqlField.Simple("approvals");
            public static readonly JqlField Assignee
                = new JqlField.Simple("assignee");
            public static readonly JqlField Attachments
                = new JqlField.Simple("attachments");
            public static readonly JqlField Category
                = new JqlField.Simple("change-gating-type");
            public static readonly JqlField ChangeGatingType
                = new JqlField.Simple("category");
            public static readonly JqlField Comment
                = new JqlField.Simple("comment");
            public static readonly JqlField Component
                = new JqlField.Simple("component");
            public static readonly JqlField Created
                = new JqlField.Simple("created");
            public static readonly JqlField Creator
                = new JqlField.Simple("creator");
            public static readonly JqlField CustomerRequestType
                = new JqlField.Simple("Customer Request Type");
            public static readonly JqlField Description
                = new JqlField.Simple("description");
            public static readonly JqlField Due
                = new JqlField.Simple("due");
            public static readonly JqlField Environment
                = new JqlField.Simple("environment");
            public static readonly JqlField EpicLink
                = new JqlField.Simple("Epic Link");
            public static readonly JqlField EpicName
                = new JqlField.Simple("Epic Name");
            public static readonly JqlField EpicStatus
                = new JqlField.Simple("Epic Status");
            public static readonly JqlField Filter
                = new JqlField.Simple("filter");
            public static readonly JqlField FixVersion
                = new JqlField.Simple("fixVersion");
            public static readonly JqlField IssueKey
                = new JqlField.Simple("issueKey");
            public static readonly JqlField IssueLinkType
                = new JqlField.Simple("issueLinkType");
            public static readonly JqlField IssueType
                = new JqlField.Simple("issueType");
            public static readonly JqlField Labels
                = new JqlField.Simple("labels");
            public static readonly JqlField LastViewed
                = new JqlField.Simple("lastViewed");
            public static readonly JqlField Level
                = new JqlField.Simple("level");
            public static readonly JqlField Organization
                = new JqlField.Simple("organizations");
            public static readonly JqlField OriginalEstimate
                = new JqlField.Simple("originalEstimate");
            public static readonly JqlField Parent
                = new JqlField.Simple("parent");
            public static readonly JqlField Priority
                = new JqlField.Simple("priority");
            public static readonly JqlField Project
                = new JqlField.Simple("project");
            public static readonly JqlField ProjectType
                = new JqlField.Simple("projectType");
            public static readonly JqlField RemainingEstimate
                = new JqlField.Simple("remainingEstimate");
            public static readonly JqlField Reporter
                = new JqlField.Simple("reporter");
            public static readonly JqlField RequestChannelType
                = new JqlField.Simple("request-channel-type");
            public static readonly JqlField RequestLastActivityTime
                = new JqlField.Simple("request-last-activity-time");
            public static readonly JqlField Resolution
                = new JqlField.Simple("resolution");
            public static readonly JqlField Resolved
                = new JqlField.Simple("resolved");
            public static readonly JqlField Sprint
                = new JqlField.Simple("sprint");
            public static readonly JqlField Status
                = new JqlField.Simple("status");
            public static readonly JqlField StatusCategory
                = new JqlField.Simple("statusCategory");
            public static readonly JqlField Summary
                = new JqlField.Simple("summary");
            public static readonly JqlField Text
                = new JqlField.Simple("text");
            public static readonly JqlField TimeToFirstResponse
                = new JqlField.Simple("Time to first response");
            public static readonly JqlField TimeToResolution
                = new JqlField.Simple("Time to resolution");
            public static readonly JqlField TimeSpent
                = new JqlField.Simple("timeSpent");
            public static readonly JqlField Updated
                = new JqlField.Simple("updated");
            public static readonly JqlField Voter
                = new JqlField.Simple("voter");
            public static readonly JqlField Votes
                = new JqlField.Simple("votes");
            public static readonly JqlField Watcher
                = new JqlField.Simple("watcher");
            public static readonly JqlField Watchers
                = new JqlField.Simple("watchers");
            public static readonly JqlField WorklogComment
                = new JqlField.Simple("text");
            public static readonly JqlField WorklogDate
                = new JqlField.Simple("worklogDate");
            public static readonly JqlField WorkRatio
                = new JqlField.Simple("workRatio");

            public static class Development
            {
                public static class Branches
                {
                    public static readonly JqlField All =
                        new JqlField.Development("branches", "all");
                }

                public static class Builds
                {
                    const string Subscript = "builds";

                    public static readonly JqlField All =
                        new JqlField.Development(Subscript, "all");
                    public static readonly JqlField Failing =
                        new JqlField.Development(Subscript, "failing");
                    public static readonly JqlField Passed =
                        new JqlField.Development(Subscript, "passed");
                    public static readonly JqlField Status =
                        new JqlField.Development(Subscript, "status");
                }

                public static class Commits
                {
                    public static readonly JqlField All =
                        new JqlField.Development("commits", "all");
                }

                public static class Deployments
                {
                    const string Subscript = "deployments";

                    public static readonly JqlField All =
                        new JqlField.Development(Subscript, "all");
                    public static readonly JqlField Deployed =
                        new JqlField.Development(Subscript, "deployed");
                    public static readonly JqlField NotDeployed =
                        new JqlField.Development(Subscript, "notDeployed");
                    public static readonly JqlField Environment =
                        new JqlField.Development(Subscript, "environment");
                }

                public static class PullRequests
                {
                    const string Subscript = "pullrequests";

                    public static readonly JqlField All =
                        new JqlField.Development(Subscript, "all");
                    public static readonly JqlField Open =
                        new JqlField.Development(Subscript, "open");
                    public static readonly JqlField Declined =
                        new JqlField.Development(Subscript, "declined");
                    public static readonly JqlField Merged =
                        new JqlField.Development(Subscript, "merged");
                    public static readonly JqlField Status =
                        new JqlField.Development(Subscript, "status");
                }

                public static class Reviews
                {
                    const string Subscript = "reviews";

                    public static readonly JqlField All =
                        new JqlField.Development(Subscript, "all");
                    public static readonly JqlField Open =
                        new JqlField.Development(Subscript, "open");
                }
            }
        }

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