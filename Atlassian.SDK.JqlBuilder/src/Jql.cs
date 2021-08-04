// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System.Collections.Generic;

namespace Atlassian.Jira.JqlBuilder
{
    public static partial class Jql
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
    }
}