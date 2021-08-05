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
                _ when value is sbyte || value is byte || value is short || value is ushort
                        || value is int || value is uint || value is long || value is ulong
                        || value is float || value is double || value is decimal
                        || value is IntPtr /* aka nint in C# 9 */ || value is UIntPtr /* aka nuint in C# 9 */ =>
                    value.ToString(),
                _ => "'" + value.ToString()!.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'") + "'"
            };

        public static string FormatDateTime(DateTime dateTime) =>
            (dateTime == dateTime.Date)
                ? dateTime.ToString("yyyy/MM/dd")
                : dateTime.ToString("yyyy/MM/dd HH:mm");
    }
}