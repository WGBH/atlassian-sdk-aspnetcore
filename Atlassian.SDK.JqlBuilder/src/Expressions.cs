using System;
using System.Collections.Generic;
#if NETSTANDARD2_1
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;

namespace Atlassian.Jira.JqlBuilder
{
    public interface IJqlExpression
    {
#if NETSTANDARD2_1
        [return: NotNull]
#endif
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
            readonly HashSet<JqlFilterExpression> _expressions;

            internal Logical(JqlFilterOperator.Logical oper, IEnumerable<JqlFilterExpression> expressions)
                : base(nameof(Logical), oper)
            {
                if (expressions == null)
                    throw new ArgumentNullException(nameof(expressions));
                if (expressions.Any(e => e == null))
                    throw new ArgumentException("Collection must not contain the null value!", nameof(expressions));

                _expressions = new HashSet<JqlFilterExpression>(expressions);
            }

            public override string ToString() =>
                "(" + string.Join(" " + Operator.Value + " ", _expressions) + ")";

            public override bool Equals(object? obj) =>
                obj is Logical other && Operator.Equals(other.Operator) && _expressions.SetEquals(other._expressions);

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
            readonly HashSet<object> _values;

            internal MultiValue(JqlField field, JqlFilterOperator.MultiValue oper, IEnumerable<object> values)
                : base(nameof(MultiValue), oper)
            {
                if (values == null)
                    throw new ArgumentNullException(nameof(values));
                if (values.Any(v => v == null))
                    throw new ArgumentException("Collection must not contain the null value!", nameof(values));

                _field = field;
                _values = new HashSet<object>(values);
            }

            public override string ToString() =>
                _field.ToString() + ' ' + Operator.Value
                    +  " (" + String.Join(", ", _values.Select(v => JqlTextUtil.EscapeValue(v))) + ")";

            public override bool Equals(object? obj) =>
                obj is MultiValue other && _field.Equals(other._field) && Operator.Equals(other.Operator)
                    && _values.SetEquals(other._values);

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
            Fields = fields.ToList().AsReadOnly();
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
}