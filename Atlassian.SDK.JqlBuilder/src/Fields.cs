using System;
using System.Collections.Generic;

namespace Atlassian.Jira.JqlBuilder
{
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

    partial class Jql
    {
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
    }
}