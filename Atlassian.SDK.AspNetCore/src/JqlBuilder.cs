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

            public static Logical And = new Logical(nameof(And), "AND");
            public static Logical Or = new Logical(nameof(Or), "OR");
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

        internal static string EscapeValue(object? value) =>
            value switch
            {
                DateTime dateTime => (dateTime == dateTime.Date)
                    ? dateTime.ToString("\\'yyyy/MM/dd\\'")
                    : dateTime.ToString("\\'yyyy/MM/dd HH:mm\\'"),
                null => "null",
                _ => "'" + value.ToString()!.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'") + "'"
            };

        public sealed class Logical : JqlFilterExpression
        {
            public new JqlOperator.Logical Operator => (JqlOperator.Logical) base.Operator;
            public IReadOnlySet<JqlFilterExpression> Expressions { get; }

            internal Logical(JqlOperator.Logical oper, IEnumerable<JqlFilterExpression> expressions) : base(oper)
            {
                Expressions = expressions.ToImmutableHashSet();
            }

            public override string ToString() =>
                "(" + string.Join(" " + Operator.Value + " ", Expressions) + ")";

            public override bool Equals(object? obj) =>
                obj is Logical other && Operator.Equals(other.Operator) && Expressions.SequenceEqual(other.Expressions);

            public override int GetHashCode() =>
                HashCode.Combine(Operator, Expressions);
        }

        public sealed class Binary : JqlFilterExpression
        {
            public JqlField Field { get; }
            public new JqlOperator.Binary Operator => (JqlOperator.Binary) base.Operator;
            public object? Value { get; }

            internal Binary(JqlField field, JqlOperator.Binary oper, object? value) : base(oper)
            {
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
    }

    public sealed class JqlOrderExpression : IJqlExpression
    {
        public sealed class OrderField
        {
            public JqlField Field { get; }
            public JqlOperator.Direction Direction { get; }

            internal OrderField(JqlField field, JqlOperator.Direction direction)
            {
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

        internal JqlField(string name) =>
            Name = name;

        public override bool Equals(object? obj) =>
            obj is JqlField other && Name == other.Name;

        public override int GetHashCode() =>
            Name.GetHashCode();

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

        public static JqlFilterExpression.Binary operator ==(JqlField field, object? value) =>
            new JqlFilterExpression.Binary(field, JqlOperator.Binary.Equal, value);

        public static JqlFilterExpression.Binary operator !=(JqlField field, object? value) =>
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
        public static JqlFilterExpression.Logical And(IEnumerable<JqlFilterExpression> expressions) =>
            new JqlFilterExpression.Logical(JqlOperator.Logical.And, expressions);

        public static JqlFilterExpression.Logical And(params JqlFilterExpression[] expressions) =>
            And((IEnumerable<JqlFilterExpression>)expressions);

        public static JqlFilterExpression.Logical Or(IEnumerable<JqlFilterExpression> expressions) =>
            new JqlFilterExpression.Logical(JqlOperator.Logical.Or, expressions);

        public static JqlFilterExpression.Logical Or(params JqlFilterExpression[] expressions) =>
            Or((IEnumerable<JqlFilterExpression>)expressions);

        public static JqlField Field(string name) =>
            new JqlField(name);

        public static JqlField Field(int customFieldId) =>
            Field("cf[" + customFieldId + ']');
    }
}