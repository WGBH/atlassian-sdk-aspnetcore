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

        internal sealed class Multi : JqlFilterOperator
        {
            Multi(string type, string value)
                : base(type, value) { }

            public static readonly Multi In
                = new Multi(nameof(In), "IN");
            public static readonly Multi NotIn
                = new Multi(nameof(NotIn), "NOT IN");
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
                JqlTextUtil.EscapeValue(_field.Name) + ' ' + Operator.Value;

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
                JqlTextUtil.EscapeValue(_field.Name) + ' ' + Operator.Value + ' ' + JqlTextUtil.EscapeValue(_value);

            public override bool Equals(object? obj) =>
                obj is Binary other && _field.Equals(other._field) && Operator.Equals(other.Operator)
                    && Object.Equals(_value, other._value);

            public override int GetHashCode() =>
                HashCode.Combine(_field, Operator, _value);
        }

        internal sealed class Multi : JqlFilterExpression
        {
            readonly JqlField _field;
            readonly IReadOnlySet<object> _values;

            internal Multi(JqlField field, JqlFilterOperator.Multi oper, IEnumerable<object> values)
                : base(nameof(Multi), oper)
            {
                if (values == null)
                    throw new ArgumentNullException(nameof(values));
                if (values.Any(v => v == null))
                    throw new ArgumentException("Collection must not contain the null value!", nameof(values));

                _field = field;
                _values = values.ToImmutableHashSet();
            }

            public override string ToString() =>
                JqlTextUtil.EscapeValue(_field.Name) + ' ' + Operator.Value
                    +  " (" + String.Join(", ", _values.Select(v => JqlTextUtil.EscapeValue(v))) + ")";

            public override bool Equals(object? obj) =>
                obj is Multi other && _field.Equals(other._field) && Operator.Equals(other.Operator)
                    && _values.SequenceEqual(other._values);

            public override int GetHashCode() =>
                HashCode.Combine(_field, Operator, _values);
        }

        public JqlOrderExpression OrderBy(JqlField field, JqlSortDirection direction) =>
            new JqlOrderExpression(this, new[] { new JqlOrderExpression.OrderField(field, direction) });

        public JqlOrderExpression OrderBy(JqlField field) =>
            OrderBy(field, JqlSortDirection.Ascending);

        public JqlOrderExpression OrderBy(string field, JqlSortDirection direction) =>
            OrderBy(new JqlField(field), direction);

        public JqlOrderExpression OrderBy(string field) =>
            OrderBy(field, JqlSortDirection.Ascending);

        public JqlOrderExpression OrderBy(IEnumerable<(string, JqlSortDirection)> fields) =>
            new JqlOrderExpression(this, fields.Select(f => new JqlOrderExpression.OrderField(new JqlField(f.Item1), f.Item2)));

        public JqlOrderExpression OrderBy(params (string, JqlSortDirection)[] fields) =>
            OrderBy((IEnumerable<(string, JqlSortDirection)>) fields);

        public JqlOrderExpression OrderBy(IEnumerable<(JqlField, JqlSortDirection)> fields) =>
            new JqlOrderExpression(this, fields.Select(f => new JqlOrderExpression.OrderField(f.Item1, f.Item2)));

        public JqlOrderExpression OrderBy(params (JqlField, JqlSortDirection)[] fields) =>
            OrderBy((IEnumerable<(JqlField, JqlSortDirection)>) fields);

        public static JqlFilterExpression operator &(JqlFilterExpression left, JqlFilterExpression right) =>
            new JqlFilterExpression.Logical(JqlFilterOperator.Logical.And, new[] {left, right});

        public static JqlFilterExpression operator |(JqlFilterExpression left, JqlFilterExpression right) =>
            new JqlFilterExpression.Logical(JqlFilterOperator.Logical.Or, new[] {left, right});
    }

    public sealed class JqlOrderExpression : IJqlExpression
    {
        public sealed class OrderField
        {
            public JqlField Field { get; }
            public JqlSortDirection Direction { get; }

            internal OrderField(JqlField field, JqlSortDirection direction)
            {
                if ((object) field == null)
                    throw new ArgumentNullException(nameof(field));
                if ((object) direction == null)
                    throw new ArgumentNullException(nameof(direction));

                Field = field;
                Direction = direction;
            }

            public override bool Equals(object? obj) =>
                obj is OrderField other && Field.Equals(other.Field) && Direction == other.Direction;

            public override int GetHashCode() =>
                HashCode.Combine(Field, Direction);
        }

        public JqlFilterExpression Expression { get; }
        public IReadOnlyList<OrderField> Fields { get; }

        internal JqlOrderExpression(JqlFilterExpression expression, IEnumerable<OrderField> fields)
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
                + String.Join(", ", Fields.Select(f => JqlTextUtil.EscapeValue(f.Field.Name) + ' ' + f.Direction.Value));

        public override bool Equals(object? obj) =>
            obj is JqlOrderExpression other && Expression.Equals(other.Expression)
                && Fields.SequenceEqual(other.Fields);

        public override int GetHashCode() =>
            HashCode.Combine(Expression, Fields);
    }

    public sealed class JqlField
    {
        public string Name { get; }

        internal JqlField(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        public override bool Equals(object? obj) =>
            obj is JqlField other && Name == other.Name;

        public override int GetHashCode() =>
            Name.GetHashCode();

        public JqlFilterExpression IsEmpty() =>
            new JqlFilterExpression.Existence(this, JqlFilterOperator.Existence.IsEmpty);

        public JqlFilterExpression IsNotEmpty() =>
            new JqlFilterExpression.Existence(this, JqlFilterOperator.Existence.IsNotEmpty);

        public JqlFilterExpression In(IEnumerable<object> values) =>
            new JqlFilterExpression.Multi(this, JqlFilterOperator.Multi.In, values);

        public JqlFilterExpression In(params object[] values) =>
            In((IEnumerable<object>) values);

        public JqlFilterExpression NotIn(IEnumerable<object> values) =>
            new JqlFilterExpression.Multi(this, JqlFilterOperator.Multi.NotIn, values);

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
            new JqlField(name);

        public static JqlField Field(int customFieldId) =>
            Field("cf[" + customFieldId + ']');

        public static class Fields
        {
            public static readonly JqlField AffectedVersion
                = new JqlField("affectedVersion");
            public static readonly JqlField Approvals
                = new JqlField("approvals");
            public static readonly JqlField Assignee
                = new JqlField("assignee");
            public static readonly JqlField Attachments
                = new JqlField("attachments");
            public static readonly JqlField Category
                = new JqlField("change-gating-type");
            public static readonly JqlField ChangeGatingType
                = new JqlField("category");
            public static readonly JqlField Comment
                = new JqlField("comment");
            public static readonly JqlField Component
                = new JqlField("component");
            public static readonly JqlField Created
                = new JqlField("created");
            public static readonly JqlField Creator
                = new JqlField("creator");
            public static readonly JqlField CustomerRequestType
                = new JqlField("Customer Request Type");
            public static readonly JqlField Description
                = new JqlField("description");
            public static readonly JqlField Due
                = new JqlField("due");
            public static readonly JqlField Environment
                = new JqlField("environment");
            public static readonly JqlField EpicLink
                = new JqlField("Epic Link");
            public static readonly JqlField EpicName
                = new JqlField("Epic Name");
            public static readonly JqlField EpicStatus
                = new JqlField("Epic Status");
            public static readonly JqlField Filter
                = new JqlField("filter");
            public static readonly JqlField FixVersion
                = new JqlField("fixVersion");
            public static readonly JqlField IssueKey
                = new JqlField("issueKey");
            public static readonly JqlField IssueLinkType
                = new JqlField("issueLinkType");
            public static readonly JqlField IssueType
                = new JqlField("issueType");
            public static readonly JqlField Labels
                = new JqlField("labels");
            public static readonly JqlField LastViewed
                = new JqlField("lastViewed");
            public static readonly JqlField Level
                = new JqlField("level");
            public static readonly JqlField Organization
                = new JqlField("organizations");
            public static readonly JqlField OriginalEstimate
                = new JqlField("originalEstimate");
            public static readonly JqlField Parent
                = new JqlField("parent");
            public static readonly JqlField Priority
                = new JqlField("priority");
            public static readonly JqlField Project
                = new JqlField("project");
            public static readonly JqlField ProjectType
                = new JqlField("projectType");
            public static readonly JqlField RemainingEstimate
                = new JqlField("remainingEstimate");
            public static readonly JqlField Reporter
                = new JqlField("reporter");
            public static readonly JqlField RequestChannelType
                = new JqlField("request-channel-type");
            public static readonly JqlField RequestLastActivityTime
                = new JqlField("request-last-activity-time");
            public static readonly JqlField Resolution
                = new JqlField("resolution");
            public static readonly JqlField Resolved
                = new JqlField("resolved");
            public static readonly JqlField Sprint
                = new JqlField("sprint");
            public static readonly JqlField Status
                = new JqlField("status");
            public static readonly JqlField StatusCategory
                = new JqlField("statusCategory");
            public static readonly JqlField Summary
                = new JqlField("summary");
            public static readonly JqlField Text
                = new JqlField("text");
            public static readonly JqlField TimeToFirstResponse
                = new JqlField("Time to first response");
            public static readonly JqlField TimeToResolution
                = new JqlField("Time to resolution");
            public static readonly JqlField TimeSpent
                = new JqlField("timeSpent");
            public static readonly JqlField Updated
                = new JqlField("updated");
            public static readonly JqlField Voter
                = new JqlField("voter");
            public static readonly JqlField Votes
                = new JqlField("votes");
            public static readonly JqlField Watcher
                = new JqlField("watcher");
            public static readonly JqlField Watchers
                = new JqlField("watchers");
            public static readonly JqlField WorklogComment
                = new JqlField("text");
            public static readonly JqlField WorklogDate
                = new JqlField("worklogDate");
            public static readonly JqlField WorkRatio
                = new JqlField("workRatio");
        }

        // copy references for convenience when used with 'import static ...Jql'
        public static class SortDirection
        {
            public static readonly JqlSortDirection Ascending = JqlSortDirection.Ascending;
            public static readonly JqlSortDirection Descending = JqlSortDirection.Descending;
        }
    }
}