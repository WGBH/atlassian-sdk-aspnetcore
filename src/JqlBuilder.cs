using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        // This assumes the operator values are distinct across all subclasses
        public override bool Equals(object? obj) =>
            obj is JqlOperator other && Value == other.Value;

        public override int GetHashCode() =>
            Value.GetHashCode();
    }

    public abstract class JqlExpression
    {
        public JqlOperator Operator { get; }

        private protected JqlExpression(JqlOperator oper) =>
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

        public sealed class Logical : JqlExpression
        {
            public new JqlOperator.Logical Operator => (JqlOperator.Logical) base.Operator;
            public IReadOnlySet<JqlExpression> Expressions { get; }

            internal Logical(JqlOperator.Logical oper, IEnumerable<JqlExpression> expressions) : base(oper)
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

        public sealed class Binary : JqlExpression
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

        public sealed class Multi : JqlExpression
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

        public OrderedJqlExpression OrderBy(JqlField field, bool descending = false) =>
            new OrderedJqlExpression(this, new[] { new OrderedJqlExpression.OrderField(field, descending) });

        public OrderedJqlExpression OrderBy(string field, bool descending = false) =>
            OrderBy(new JqlField(field), descending);

        public OrderedJqlExpression OrderBy(IEnumerable<(string, bool)> fields) =>
            new OrderedJqlExpression(this, fields.Select(f => new OrderedJqlExpression.OrderField(new JqlField(f.Item1), f.Item2)));

        public OrderedJqlExpression OrderBy(params (string, bool)[] fields) =>
            OrderBy((IEnumerable<(string, bool)>) fields);

        public OrderedJqlExpression OrderBy(IEnumerable<(JqlField, bool)> fields) =>
            new OrderedJqlExpression(this, fields.Select(f => new OrderedJqlExpression.OrderField(f.Item1, f.Item2)));

        public OrderedJqlExpression OrderBy(params (JqlField, bool)[] fields) =>
            OrderBy((IEnumerable<(JqlField, bool)>) fields);
    }

    public sealed class OrderedJqlExpression
    {
        public sealed class OrderField
        {
            public JqlField Field { get; }
            public bool Descending { get; }

            internal OrderField(JqlField field, bool descending = false)
            {
                Field = field;
                Descending = descending;
            }

            public override bool Equals(object? obj) =>
                obj is OrderField other && Field.Equals(other.Field) && Descending == other.Descending;

            public override int GetHashCode() =>
                HashCode.Combine(Field, Descending);
        }

        public JqlExpression Expression { get; }
        public IReadOnlyList<OrderField> Fields { get; }

        internal OrderedJqlExpression(JqlExpression expression, IEnumerable<OrderField> fields)
        {
            Expression = expression;
            Fields = fields.ToImmutableList();
        }

        public override string ToString() =>
            Expression.ToString() + " ORDER BY "
                + String.Join(", ", Fields.Select(f => JqlExpression.EscapeValue(f.Field.Name) + (f.Descending? " DESC" : " ASC")));

        public override bool Equals(object? obj) =>
            obj is OrderedJqlExpression other && Expression.Equals(other.Expression)
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

        public JqlExpression.Multi In(IEnumerable<object> values) =>
            new JqlExpression.Multi(this, JqlOperator.Multi.In, values);

        public JqlExpression.Multi In(params object[] values) =>
            In((IEnumerable<object>) values);

        public JqlExpression.Multi NotIn(IEnumerable<object> values) =>
            new JqlExpression.Multi(this, JqlOperator.Multi.NotIn, values);

        public JqlExpression.Multi NotIn(params object[] values) =>
            NotIn((IEnumerable<object>) values);

        public JqlExpression.Binary Like(object value) =>
            new JqlExpression.Binary(this, JqlOperator.Binary.Like, value);

        public JqlExpression.Binary NotLike(object value) =>
            new JqlExpression.Binary(this, JqlOperator.Binary.NotLike, value);

        public static JqlExpression.Binary operator ==(JqlField field, object? value) =>
            new JqlExpression.Binary(field, JqlOperator.Binary.Equal, value);

        public static JqlExpression.Binary operator !=(JqlField field, object? value) =>
            new JqlExpression.Binary(field, JqlOperator.Binary.NotEqual, value);

        public static JqlExpression.Binary operator >(JqlField field, object value) =>
            new JqlExpression.Binary(field, JqlOperator.Binary.GreaterThan, value);

        public static JqlExpression.Binary operator >=(JqlField field, object value) =>
            new JqlExpression.Binary(field, JqlOperator.Binary.GreaterThanOrEqual, value);

       public static JqlExpression.Binary operator <(JqlField field, object value) =>
            new JqlExpression.Binary(field, JqlOperator.Binary.LessThan, value);

        public static JqlExpression.Binary operator <=(JqlField field, object value) =>
            new JqlExpression.Binary(field, JqlOperator.Binary.LessThanOrEqual, value);
    }

    public static class Jql
    {
        public static JqlExpression.Logical And(IEnumerable<JqlExpression> expressions) =>
            new JqlExpression.Logical(JqlOperator.Logical.And, expressions);

        public static JqlExpression.Logical And(params JqlExpression[] expressions) =>
            And((IEnumerable<JqlExpression>)expressions);

        public static JqlExpression.Logical Or(IEnumerable<JqlExpression> expressions) =>
            new JqlExpression.Logical(JqlOperator.Logical.Or, expressions);

        public static JqlExpression.Logical Or(params JqlExpression[] expressions) =>
            Or((IEnumerable<JqlExpression>)expressions);

        public static JqlField Field(string name) =>
            new JqlField(name);

        public static JqlField Field(int customFieldId) =>
            Field("cf[" + customFieldId + ']');
    }
}