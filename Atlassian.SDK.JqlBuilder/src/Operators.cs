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
}