/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CassetteMotionPro.Workspace
{
    // Reads a typed measurement such as "742 mm", "-35", "+5", "12,5 mm" or "1,025 mm".
    // Fitters type decimal commas and thousands separators, and phone keyboards produce a
    // Unicode minus. Misreading those (for example "12,5" as 12) would put a wrong number in
    // a client report, so anything ambiguous is rejected and shown as "could not be read".
    public static class MeasurementNumber
    {
        private static readonly Regex Pattern = new Regex(
            @"^([+-]?)\s*((?:\d+(?:[.,]\d+)*)|(?:[.,]\d+))\s*(.*)$",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        public static bool TryParse(string value, out double number)
        {
            string unit;
            return TryParse(value, out number, out unit);
        }

        public static bool TryParse(string value, out double number, out string unit)
        {
            number = 0;
            unit = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string text = value.Trim().Replace('\u2212', '-').Replace('\u00A0', ' ').Replace('\u2009', ' ');
            Match match = Pattern.Match(text);
            if (!match.Success)
                return false;

            string normalized;
            if (!TryNormalize(match.Groups[2].Value, out normalized))
                return false;

            double parsed;
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ||
                double.IsNaN(parsed) || double.IsInfinity(parsed))
                return false;

            number = match.Groups[1].Value == "-" ? -parsed : parsed;
            unit = match.Groups[3].Value.Trim();
            return true;
        }

        private static bool TryNormalize(string token, out string normalized)
        {
            normalized = string.Empty;
            int dots = Count(token, '.');
            int commas = Count(token, ',');

            if (token[0] == '.' || token[0] == ',')
            {
                if (dots + commas != 1)
                    return false;
                normalized = "0." + token.Substring(1);
                return true;
            }

            if (dots + commas == 0)
            {
                normalized = token;
                return true;
            }

            if (dots > 0 && commas > 0)
            {
                // Both present: the last one is the decimal mark and the other groups thousands.
                char decimalMark = token.LastIndexOf('.') > token.LastIndexOf(',') ? '.' : ',';
                char groupMark = decimalMark == '.' ? ',' : '.';
                if (Count(token, decimalMark) != 1)
                    return false;
                string[] parts = token.Split(decimalMark);
                if (parts[1].IndexOf(groupMark) >= 0 || !IsGrouped(parts[0], groupMark))
                    return false;
                normalized = parts[0].Replace(groupMark.ToString(), string.Empty) + "." + parts[1];
                return true;
            }

            char separator = dots > 0 ? '.' : ',';
            if (dots + commas == 1)
            {
                string[] pair = token.Split(separator);
                // "1,025" is a thousands separator; "12,5", "1,25" and "0,125" are decimals.
                // A single "." is always a decimal point.
                bool thousands = separator == ',' && pair[1].Length == 3 && pair[0].Length <= 3 && pair[0][0] != '0';
                normalized = thousands ? pair[0] + pair[1] : pair[0] + "." + pair[1];
                return true;
            }

            // Several of the same mark can only be thousands groups ("1,234,567").
            if (!IsGrouped(token, separator))
                return false;
            normalized = token.Replace(separator.ToString(), string.Empty);
            return true;
        }

        private static bool IsGrouped(string integerPart, char groupMark)
        {
            if (integerPart.IndexOf(groupMark) < 0)
                return true;
            string[] groups = integerPart.Split(groupMark);
            if (groups[0].Length < 1 || groups[0].Length > 3 || groups[0][0] == '0')
                return false;
            for (int i = 1; i < groups.Length; i++)
            {
                if (groups[i].Length != 3)
                    return false;
            }
            return true;
        }

        private static int Count(string text, char mark)
        {
            int count = 0;
            foreach (char character in text)
            {
                if (character == mark)
                    count++;
            }
            return count;
        }
    }
}
