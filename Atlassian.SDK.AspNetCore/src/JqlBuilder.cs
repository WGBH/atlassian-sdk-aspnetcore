using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Atlassian.Jira.JqlBuilder
{
    public abstract class JqlOperator
    {
        public string Name { get; }
        public string Value { get; }

        private protected JqlOperator(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public sealed class Logical : JqlOperator
        {
            Logical(string name, string value)
                : base(name, value) { }

            public static Logical And
                = new Logical(nameof(And), "AND");
            public static Logical Or
                = new Logical(nameof(Or), "OR");
        }

        public sealed class Existence : JqlOperator
        {
            Existence(string name, string value)
                : base(name, value) { }

            public static Existence IsEmpty
                = new Existence(nameof(IsEmpty), "IS EMPTY");

            public static Existence IsNotEmpty
                = new Existence(nameof(IsNotEmpty), "IS NOT EMPTY");
        }

        public sealed class Binary : JqlOperator
        {
            Binary(string name, string value)
                : base(name, value) { }

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

        public sealed class Multi : JqlOperator
        {
            Multi(string name, string value)
                : base(name, value) { }

            public static readonly Multi In
                = new Multi(nameof(In), "IN");
            public static readonly Multi NotIn
                = new Multi(nameof(NotIn), "NOT IN");
        }

        public sealed class Direction : JqlOperator
        {
            Direction(string name, string value)
                : base(name, value) { }

            public static readonly Direction Ascending
                = new Direction(nameof(Ascending), "ASC");
            public static readonly Direction Descending
                = new Direction(nameof(Descending), "DESC");
        }

        // This assumes the operator values are distinct across all subclasses
        public override bool Equals(object? obj) =>
            obj is JqlOperator other && Value == other.Value;

        public override int GetHashCode() =>
            Value.GetHashCode();
    }

    public interface IJqlExpression
    {
        [return: NotNull]
        string ToString();
    }

    public abstract class JqlFilterExpression : IJqlExpression
    {
        public JqlOperator Operator { get; }

        private protected JqlFilterExpression(JqlOperator oper) =>
            Operator = oper;

        public abstract override string ToString();

        internal static string EscapeValue(object value) =>
            value switch
            {
                DateTime dateTime => (dateTime == dateTime.Date)
                    ? dateTime.ToString("\\'yyyy/MM/dd\\'")
                    : dateTime.ToString("\\'yyyy/MM/dd HH:mm\\'"),
                _ => "'" + value.ToString()!.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'") + "'"
            };

        public sealed class Logical : JqlFilterExpression
        {
            public new JqlOperator.Logical Operator => (JqlOperator.Logical) base.Operator;
            public IReadOnlySet<JqlFilterExpression> Expressions { get; }

            internal Logical(JqlOperator.Logical oper, IEnumerable<JqlFilterExpression> expressions) : base(oper)
            {
                if (expressions == null)
                    throw new ArgumentNullException(nameof(expressions));
                if (expressions.Any(e => e == null))
                    throw new ArgumentException("Collection must not contain the null value!", nameof(expressions));

                Expressions = expressions.ToImmutableHashSet();
            }

            public override string ToString() =>
                "(" + string.Join(" " + Operator.Value + " ", Expressions) + ")";

            public override bool Equals(object? obj) =>
                obj is Logical other && Operator.Equals(other.Operator) && Expressions.SequenceEqual(other.Expressions);

            public override int GetHashCode() =>
                HashCode.Combine(Operator, Expressions);
        }

        public sealed class Existence : JqlFilterExpression
        {
            public JqlField Field { get; }
            public new JqlOperator.Existence Operator => (JqlOperator.Existence) base.Operator;

            internal Existence(JqlField field, JqlOperator.Existence oper) : base(oper)
            {
                Field = field;
            }

            public override string ToString() =>
                EscapeValue(Field.Name) + ' ' + Operator.Value;

            public override bool Equals(object? obj) =>
                obj is Existence other && Field.Equals(other.Field) && Operator.Equals(other.Operator);

            public override int GetHashCode() =>
                HashCode.Combine(Field, Operator);
        }

        public sealed class Binary : JqlFilterExpression
        {
            public JqlField Field { get; }
            public new JqlOperator.Binary Operator => (JqlOperator.Binary) base.Operator;
            public object Value { get; }

            internal Binary(JqlField field, JqlOperator.Binary oper, object value) : base(oper)
            {
                if ((object) field == null)
                    throw new ArgumentNullException(nameof(field));
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                Field = field;
                Value = value;
            }

            public sealed override string ToString() =>
                EscapeValue(Field.Name) + ' ' + Operator.Value + ' ' + EscapeValue(Value);

            public override bool Equals(object? obj) =>
                obj is Binary other && Field.Equals(other.Field) && Operator.Equals(other.Operator)
                    && Object.Equals(Value, other.Value);

            public override int GetHashCode() =>
                HashCode.Combine(Field, Operator, Value);
        }

        public sealed class Multi : JqlFilterExpression
        {
            public JqlField Field { get; }
            public new JqlOperator.Multi Operator => (JqlOperator.Multi) base.Operator;
            public IReadOnlySet<object> Values { get; }

            internal Multi(JqlField field, JqlOperator.Multi oper, IEnumerable<object> values) : base(oper)
            {
                if (values == null)
                    throw new ArgumentNullException(nameof(values));
                if (values.Any(v => v == null))
                    throw new ArgumentException("Collection must not contain the null value!", nameof(values));

                Field = field;
                Values = values.ToImmutableHashSet();
            }

            public override string ToString() =>
                EscapeValue(Field.Name) + ' ' + Operator.Value +  " (" + String.Join(", ", Values.Select(v => EscapeValue(v))) + ")";

            public override bool Equals(object? obj) =>
                obj is Multi other && Field.Equals(other.Field) && Operator.Equals(other.Operator)
                    && Values.SequenceEqual(other.Values);

            public override int GetHashCode() =>
                HashCode.Combine(Field, Operator, Values);
        }

        public JqlOrderExpression OrderBy(JqlField field, JqlOperator.Direction direction) =>
            new JqlOrderExpression(this, new[] { new JqlOrderExpression.OrderField(field, direction) });

        public JqlOrderExpression OrderBy(JqlField field) =>
            OrderBy(field, JqlOperator.Direction.Ascending);

        public JqlOrderExpression OrderBy(string field, JqlOperator.Direction direction) =>
            OrderBy(new JqlField(field), direction);

        public JqlOrderExpression OrderBy(string field) =>
            OrderBy(field, JqlOperator.Direction.Ascending);

        public JqlOrderExpression OrderBy(IEnumerable<(string, JqlOperator.Direction)> fields) =>
            new JqlOrderExpression(this, fields.Select(f => new JqlOrderExpression.OrderField(new JqlField(f.Item1), f.Item2)));

        public JqlOrderExpression OrderBy(params (string, JqlOperator.Direction)[] fields) =>
            OrderBy((IEnumerable<(string, JqlOperator.Direction)>) fields);

        public JqlOrderExpression OrderBy(IEnumerable<(JqlField, JqlOperator.Direction)> fields) =>
            new JqlOrderExpression(this, fields.Select(f => new JqlOrderExpression.OrderField(f.Item1, f.Item2)));

        public JqlOrderExpression OrderBy(params (JqlField, JqlOperator.Direction)[] fields) =>
            OrderBy((IEnumerable<(JqlField, JqlOperator.Direction)>) fields);

        public static JqlFilterExpression.Logical operator &(JqlFilterExpression left, JqlFilterExpression right) =>
            new JqlFilterExpression.Logical(JqlOperator.Logical.And, new[] {left, right});

        public static JqlFilterExpression.Logical operator |(JqlFilterExpression left, JqlFilterExpression right) =>
            new JqlFilterExpression.Logical(JqlOperator.Logical.Or, new[] {left, right});
    }

    public sealed class JqlOrderExpression : IJqlExpression
    {
        public sealed class OrderField
        {
            public JqlField Field { get; }
            public JqlOperator.Direction Direction { get; }

            internal OrderField(JqlField field, JqlOperator.Direction direction)
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
                + String.Join(", ", Fields.Select(f => JqlFilterExpression.EscapeValue(f.Field.Name) + ' ' + f.Direction.Value));

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

        public JqlFilterExpression.Existence IsEmpty() =>
            new JqlFilterExpression.Existence(this, JqlOperator.Existence.IsEmpty);

        public JqlFilterExpression.Existence IsNotEmpty() =>
            new JqlFilterExpression.Existence(this, JqlOperator.Existence.IsNotEmpty);

        public JqlFilterExpression.Multi In(IEnumerable<object> values) =>
            new JqlFilterExpression.Multi(this, JqlOperator.Multi.In, values);

        public JqlFilterExpression.Multi In(params object[] values) =>
            In((IEnumerable<object>) values);

        public JqlFilterExpression.Multi NotIn(IEnumerable<object> values) =>
            new JqlFilterExpression.Multi(this, JqlOperator.Multi.NotIn, values);

        public JqlFilterExpression.Multi NotIn(params object[] values) =>
            NotIn((IEnumerable<object>) values);

        public JqlFilterExpression.Binary Like(object value) =>
            new JqlFilterExpression.Binary(this, JqlOperator.Binary.Like, value);

        public JqlFilterExpression.Binary NotLike(object value) =>
            new JqlFilterExpression.Binary(this, JqlOperator.Binary.NotLike, value);

        public static JqlFilterExpression.Binary operator ==(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlOperator.Binary.Equal, value);

        public static JqlFilterExpression.Binary operator !=(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlOperator.Binary.NotEqual, value);

        public static JqlFilterExpression.Binary operator >(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlOperator.Binary.GreaterThan, value);

        public static JqlFilterExpression.Binary operator >=(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlOperator.Binary.GreaterThanOrEqual, value);

       public static JqlFilterExpression.Binary operator <(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlOperator.Binary.LessThan, value);

        public static JqlFilterExpression.Binary operator <=(JqlField field, object value) =>
            new JqlFilterExpression.Binary(field, JqlOperator.Binary.LessThanOrEqual, value);
    }

    public static class Jql
    {
        public static JqlFilterExpression.Logical All(IEnumerable<JqlFilterExpression> expressions) =>
            new JqlFilterExpression.Logical(JqlOperator.Logical.And, expressions);

        public static JqlFilterExpression.Logical All(params JqlFilterExpression[] expressions) =>
            All((IEnumerable<JqlFilterExpression>)expressions);

        public static JqlFilterExpression.Logical Any(IEnumerable<JqlFilterExpression> expressions) =>
            new JqlFilterExpression.Logical(JqlOperator.Logical.Or, expressions);

        public static JqlFilterExpression.Logical Any(params JqlFilterExpression[] expressions) =>
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
        public static class Direction
        {
            public static readonly JqlOperator.Direction Ascending = JqlOperator.Direction.Ascending;
            public static readonly JqlOperator.Direction Descending = JqlOperator.Direction.Descending;
        }
    }
}