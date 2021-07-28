using System;

namespace Atlassian.Jira.JqlBuilder
{
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
}