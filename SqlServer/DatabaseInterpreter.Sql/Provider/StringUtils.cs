using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DatabaseInterpreter.Core
{
    /// <summary>
    /// String utility class that provides a host of string related operations
    /// </summary>
    public static class StringUtils
    {
        private static Regex tokenizeRegex = new Regex("{{.*?}}");
        private static Random random = new Random((int)DateTime.Now.Ticks);
        private static char[] base36CharArray = "0123456789abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static string base36Chars = "0123456789abcdefghijklmnopqrstuvwxyz";

        /// <summary>Trims a sub string from a string.</summary>
        /// <param name="text"></param>
        /// <param name="textToTrim"></param>
        /// <returns></returns>
        public static string TrimStart(string text, string textToTrim, bool caseInsensitive)
        {
            while (true)
            {
                string str = text.Substring(0, textToTrim.Length);
                if (str == textToTrim || caseInsensitive && str.ToLower() == textToTrim.ToLower())
                    text = text.Length > str.Length ? text.Substring(textToTrim.Length) : "";
                else
                    break;
            }
            return text;
        }

        /// <summary>Trims a string to a specific number of max characters</summary>
        /// <param name="value"></param>
        /// <param name="charCount"></param>
        /// <returns></returns>
        [Obsolete("Please use the StringUtils.Truncate() method instead.")]
        public static string TrimTo(string value, int charCount)
        {
            return value == null || value.Length <= charCount ? value : value.Substring(0, charCount);
        }

        /// <summary>Replicates an input string n number of times</summary>
        /// <param name="input"></param>
        /// <param name="charCount"></param>
        /// <returns></returns>
        public static string Replicate(string input, int charCount)
        {
            return new StringBuilder().Insert(0, input, charCount).ToString();
        }

        /// <summary>
        /// Replicates a character n number of times and returns a string
        /// </summary>
        /// <param name="charCount"></param>
        /// <param name="character"></param>
        /// <returns></returns>
        public static string Replicate(char character, int charCount)
        {
            return new StringBuilder().Insert(0, character.ToString(), charCount).ToString();
        }

        /// <summary>Finds the nth index of string in a string</summary>
        /// <param name="source"></param>
        /// <param name="matchString"></param>
        /// <param name="stringInstance"></param>
        /// <returns></returns>
        public static int IndexOfNth(
            this string source,
            string matchString,
            int stringInstance,
            StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            if (string.IsNullOrEmpty(source))
                return -1;
            int startIndex = 0;
            int num1 = 0;
            while (num1 < stringInstance)
            {
                int count = source.Length - startIndex;
                int num2 = source.IndexOf(matchString, startIndex, count, stringComparison);
                if (num2 != -1)
                {
                    ++num1;
                    if (num1 == stringInstance)
                        return num2;
                    startIndex = num2 + matchString.Length;
                }
                else
                    break;
            }
            return -1;
        }

        /// <summary>Returns the nth Index of a character in a string</summary>
        /// <param name="source"></param>
        /// <param name="matchChar"></param>
        /// <param name="charInstance"></param>
        /// <returns></returns>
        public static int IndexOfNth(this string source, char matchChar, int charInstance)
        {
            if (string.IsNullOrEmpty(source) || charInstance < 1)
                return -1;
            int num = 0;
            for (int index = 0; index < source.Length; ++index)
            {
                if ((int)source[index] == (int)matchChar)
                {
                    ++num;
                    if (num == charInstance)
                        return index;
                }
            }
            return -1;
        }

        /// <summary>Finds the nth index of strting in a string</summary>
        /// <param name="source"></param>
        /// <param name="matchString"></param>
        /// <param name="charInstance"></param>
        /// <returns></returns>
        public static int LastIndexOfNth(
            this string source,
            string matchString,
            int charInstance,
            StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            if (string.IsNullOrEmpty(source))
                return -1;
            int num1 = source.Length;
            int num2 = 0;
            while (num2 < charInstance)
            {
                num1 = source.LastIndexOf(matchString, num1, num1, stringComparison);
                if (num1 != -1)
                {
                    ++num2;
                    if (num2 == charInstance)
                        return num1;
                }
                else
                    break;
            }
            return -1;
        }

        /// <summary>Finds the nth index of in a string from the end.</summary>
        /// <param name="source"></param>
        /// <param name="matchChar"></param>
        /// <param name="charInstance"></param>
        /// <returns></returns>
        public static int LastIndexOfNth(this string source, char matchChar, int charInstance)
        {
            if (string.IsNullOrEmpty(source))
                return -1;
            int num = 0;
            for (int index = source.Length - 1; index > -1; --index)
            {
                if ((int)source[index] == (int)matchChar)
                {
                    ++num;
                    if (num == charInstance)
                        return index;
                }
            }
            return -1;
        }

        /// <summary>Return a string in proper Case format</summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static string ProperCase(string Input)
        {
            return Input == null ? (string)null : Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Input);
        }

        /// <summary>
        /// Takes a phrase and turns it into CamelCase text.
        /// White Space, punctuation and separators are stripped
        /// </summary>
        /// <param name="phrase">Text to convert to CamelCase</param>
        public static string ToCamelCase(string phrase)
        {
            if (phrase == null)
                return string.Empty;
            StringBuilder stringBuilder = new StringBuilder(phrase.Length);
            bool flag = true;
            foreach (char c in phrase)
            {
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSeparator(c) || c > ' ' && c < '0')
                    flag = true;
                else if (char.IsDigit(c))
                {
                    stringBuilder.Append(c);
                    flag = true;
                }
                else
                {
                    if (flag)
                        stringBuilder.Append(char.ToUpper(c));
                    else
                        stringBuilder.Append(char.ToLower(c));
                    flag = false;
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Tries to create a phrase string from CamelCase text.
        /// Will place spaces before capitalized letters.
        ///
        /// Note that this method may not work for round tripping
        /// ToCamelCase calls, since ToCamelCase strips more characters
        /// than just spaces.
        /// </summary>
        /// <param name="camelCase"></param>
        /// <returns></returns>
        public static string FromCamelCase(string camelCase)
        {
            if (string.IsNullOrEmpty(camelCase))
                return camelCase;
            StringBuilder stringBuilder = new StringBuilder(camelCase.Length + 10);
            bool flag = true;
            char c1 = char.MinValue;
            foreach (char c2 in camelCase)
            {
                if (!flag && (char.IsUpper(c2) || char.IsDigit(c2) && !char.IsDigit(c1)))
                    stringBuilder.Append(' ');
                stringBuilder.Append(c2);
                flag = false;
                c1 = c2;
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Extracts a string from between a pair of delimiters. Only the first
        /// instance is found.
        /// </summary>
        /// <param name="source">Input String to work on</param>
        /// <param name="beginDelim">Beginning delimiter</param>
        /// <param name="endDelim">ending delimiter</param>
        /// <param name="caseSensitive">Determines whether the search for delimiters is case sensitive</param>
        /// <param name="allowMissingEndDelimiter"></param>
        /// <param name="returnDelimiters"></param>
        /// <returns>Extracted string or string.Empty on no match</returns>
        public static string ExtractString(
            this string source,
            string beginDelim,
            string endDelim,
            bool caseSensitive = false,
            bool allowMissingEndDelimiter = false,
            bool returnDelimiters = false)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            int startIndex;
            int num;
            if (caseSensitive)
            {
                startIndex = source.IndexOf(beginDelim, StringComparison.CurrentCulture);
                if (startIndex == -1)
                    return string.Empty;
                num = source.IndexOf(endDelim, startIndex + beginDelim.Length, StringComparison.CurrentCulture);
            }
            else
            {
                startIndex = source.IndexOf(beginDelim, 0, source.Length, StringComparison.OrdinalIgnoreCase);
                if (startIndex == -1)
                    return string.Empty;
                num = source.IndexOf(endDelim, startIndex + beginDelim.Length, StringComparison.OrdinalIgnoreCase);
            }
            if (allowMissingEndDelimiter && num < 0)
                return !returnDelimiters ? source.Substring(startIndex + beginDelim.Length) : source.Substring(startIndex);
            if (startIndex <= -1 || num <= 1)
                return string.Empty;
            return !returnDelimiters ? source.Substring(startIndex + beginDelim.Length, num - startIndex - beginDelim.Length) : source.Substring(startIndex, num - startIndex + endDelim.Length);
        }

        /// <summary>
        /// String replace function that supports replacing a specific instance with
        /// case insensitivity
        /// </summary>
        /// <param name="origString">Original input string</param>
        /// <param name="findString">The string that is to be replaced</param>
        /// <param name="replaceWith">The replacement string</param>
        /// <param name="instance">Instance of the FindString that is to be found. 1 based. If Instance = -1 all are replaced</param>
        /// <param name="caseInsensitive">Case insensitivity flag</param>
        /// <returns>updated string or original string if no matches</returns>
        public static string ReplaceStringInstance(
            string origString,
            string findString,
            string replaceWith,
            int instance,
            bool caseInsensitive)
        {
            if (instance == -1)
                return StringUtils.ReplaceString(origString, findString, replaceWith, caseInsensitive);
            int num = 0;
            for (int index = 0; index < instance; ++index)
            {
                num = !caseInsensitive ? origString.IndexOf(findString, num) : origString.IndexOf(findString, num, origString.Length - num, StringComparison.OrdinalIgnoreCase);
                if (num == -1)
                    return origString;
                if (index < instance - 1)
                    num += findString.Length;
            }
            return origString.Substring(0, num) + replaceWith + origString.Substring(num + findString.Length);
        }

        /// <summary>
        /// Replaces a substring within a string with another substring with optional case sensitivity turned off.
        /// </summary>
        /// <param name="origString">String to do replacements on</param>
        /// <param name="findString">The string to find</param>
        /// <param name="replaceString">The string to replace found string wiht</param>
        /// <param name="caseInsensitive">If true case insensitive search is performed</param>
        /// <returns>updated string or original string if no matches</returns>
        public static string ReplaceString(
            string origString,
            string findString,
            string replaceString,
            bool caseInsensitive)
        {
            int startIndex = 0;
            while (true)
            {
                int length = !caseInsensitive ? origString.IndexOf(findString, startIndex) : origString.IndexOf(findString, startIndex, origString.Length - startIndex, StringComparison.OrdinalIgnoreCase);
                if (length != -1)
                {
                    origString = origString.Substring(0, length) + replaceString + origString.Substring(length + findString.Length);
                    startIndex = length + replaceString.Length;
                }
                else
                    break;
            }
            return origString;
        }

        /// <summary>Truncate a string to maximum length.</summary>
        /// <param name="text">Text to truncate</param>
        /// <param name="maxLength">Maximum length</param>
        /// <returns>Trimmed string</returns>
        public static string Truncate(this string text, int maxLength)
        {
            return string.IsNullOrEmpty(text) || text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }

        /// <summary>
        /// Returns an abstract of the provided text by returning up to Length characters
        /// of a text string. If the text is truncated a ... is appended.
        /// </summary>
        /// <param name="text">Text to abstract</param>
        /// <param name="length">Number of characters to abstract to</param>
        /// <returns>string</returns>
        public static string TextAbstract(string text, int length)
        {
            if (text == null)
                return string.Empty;
            if (text.Length <= length)
                return text;
            text = text.Substring(0, length);
            text = text.Substring(0, text.LastIndexOf(" "));
            return text + "...";
        }

        /// <summary>
        /// Terminates a string with the given end string/character, but only if the
        /// text specified doesn't already exist and the string is not empty.
        /// </summary>
        /// <param name="value">String to terminate</param>
        /// <param name="terminator">String to terminate the text string with</param>
        /// <returns></returns>
        public static string TerminateString(string value, string terminator)
        {
            if (string.IsNullOrEmpty(value))
                return terminator;
            return value.EndsWith(terminator) ? value : value + terminator;
        }

        /// <summary>Returns the number or right characters specified</summary>
        /// <param name="full">full string to work with</param>
        /// <param name="rightCharCount">number of right characters to return</param>
        /// <returns></returns>
        public static string Right(string full, int rightCharCount)
        {
            return string.IsNullOrEmpty(full) || full.Length < rightCharCount || full.Length - rightCharCount < 0 ? full : full.Substring(full.Length - rightCharCount);
        }

        /// <summary>
        /// Determines if a string is contained in a list of other strings
        /// </summary>
        /// <param name="s"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool Inlist(string s, params string[] list)
        {
            return ((IEnumerable<string>)list).Contains<string>(s);
        }

        /// <summary>
        /// String.Contains() extension method that allows to specify case
        /// </summary>
        /// <param name="text">Input text</param>
        /// <param name="searchFor">text to search for</param>
        /// <param name="stringComparison">Case sensitivity options</param>
        /// <returns></returns>
        public static bool Contains(
            this string text,
            string searchFor,
            StringComparison stringComparison)
        {
            return text.IndexOf(searchFor, stringComparison) > -1;
        }

        /// <summary>
        /// Parses a string into an array of lines broken
        /// by \r\n or \n
        /// </summary>
        /// <param name="s">String to check for lines</param>
        /// <param name="maxLines">Optional - max number of lines to return</param>
        /// <returns>array of strings, or null if the string passed was a null</returns>
        public static string[] GetLines(this string s, int maxLines = 0)
        {
            if (s == null)
                return (string[])null;
            s = s.Replace("\r\n", "\n");
            return maxLines < 1 ? s.Split('\n') : ((IEnumerable<string>)s.Split('\n')).Take<string>(maxLines).ToArray<string>();
        }

        /// <summary>Returns a line count for a string</summary>
        /// <param name="s">string to count lines for</param>
        /// <returns></returns>
        public static int CountLines(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;
            return s.Split('\n').Length;
        }

        /// <summary>
        /// Strips all non digit values from a string and only
        /// returns the numeric string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string StripNonNumber(string input)
        {
            char[] charArray = input.ToCharArray();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in charArray)
            {
                if (char.IsNumber(c) || char.IsSeparator(c))
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Tokenizes a string based on a start and end string. Replaces the values with a token
        /// text (#@#1#@# for example).
        ///
        /// You can use Detokenize to get the original values back
        /// </summary>
        /// <param name="text"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="replaceDelimiter"></param>
        /// <returns></returns>
        public static List<string> TokenizeString(
            ref string text,
            string start,
            string end,
            string replaceDelimiter = "#@#")
        {
            List<string> stringList = new List<string>();
            MatchCollection matchCollection = StringUtils.tokenizeRegex.Matches(text);
            int num = 0;
            foreach (Match match in matchCollection)
            {
                StringUtils.tokenizeRegex = new Regex(Regex.Escape(match.Value));
                text = StringUtils.tokenizeRegex.Replace(text, string.Format("{0}{1}{2}", (object)replaceDelimiter, (object)num, (object)replaceDelimiter), 1);
                stringList.Add(match.Value);
                ++num;
            }
            return stringList;
        }

        /// <summary>
        /// Detokenizes a string tokenized with TokenizeString. Requires the collection created
        /// by detokenization
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tokens"></param>
        /// <param name="replaceDelimiter"></param>
        /// <returns></returns>
        public static string DetokenizeString(
            string text,
            List<string> tokens,
            string replaceDelimiter = "#@#")
        {
            int num = 0;
            foreach (string token in tokens)
            {
                text = text.Replace(string.Format("{0}{1}{2}", (object)replaceDelimiter, (object)num, (object)replaceDelimiter), token);
                ++num;
            }
            return text;
        }

        /// <summary>
        /// Parses an string into an integer. If the text can't be parsed
        /// a default text is returned instead
        /// </summary>
        /// <param name="input">Input numeric string to be parsed</param>
        /// <param name="defaultValue">Optional default text if parsing fails</param>
        /// <param name="formatProvider">Optional NumberFormat provider. Defaults to current culture's number format</param>
        /// <returns></returns>
        public static int ParseInt(string input, int defaultValue = 0, IFormatProvider numberFormat = null)
        {
            if (numberFormat == null)
                numberFormat = (IFormatProvider)CultureInfo.CurrentCulture.NumberFormat;
            int result = defaultValue;
            return !int.TryParse(input, NumberStyles.Any, numberFormat, out result) ? defaultValue : result;
        }

        /// <summary>
        /// Parses an string into an decimal. If the text can't be parsed
        /// a default text is returned instead
        /// </summary>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static Decimal ParseDecimal(
            string input,
            Decimal defaultValue = 0M,
            IFormatProvider numberFormat = null)
        {
            numberFormat = numberFormat ?? (IFormatProvider)CultureInfo.CurrentCulture.NumberFormat;
            Decimal result = defaultValue;
            return !Decimal.TryParse(input, NumberStyles.Any, numberFormat, out result) ? defaultValue : result;
        }

        /// <summary>
        /// Creates short string id based on a GUID hashcode.
        /// Not guaranteed to be unique across machines, but unlikely
        /// to duplicate in medium volume situations.
        /// </summary>
        /// <returns></returns>
        public static string NewStringId()
        {
            return Guid.NewGuid().ToString().GetHashCode().ToString("x");
        }

        /// <summary>
        /// Creates a new random string of upper, lower case letters and digits.
        /// Very useful for generating random data for storage in test data.
        /// </summary>
        /// <param name="size">The number of characters of the string to generate</param>
        /// <returns>randomized string</returns>
        public static string RandomString(int size, bool includeNumbers = false)
        {
            StringBuilder stringBuilder = new StringBuilder(size);
            for (int index = 0; index < size; ++index)
            {
                int num = !includeNumbers ? Convert.ToInt32(Math.Floor(52.0 * StringUtils.random.NextDouble())) : Convert.ToInt32(Math.Floor(62.0 * StringUtils.random.NextDouble()));
                char ch = num >= 26 ? (num <= 25 || num >= 52 ? Convert.ToChar(num - 52 + 48) : Convert.ToChar(num - 26 + 97)) : Convert.ToChar(num + 65);
                stringBuilder.Append(ch);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// UrlEncodes a string without the requirement for System.Web
        /// </summary>
        /// <param name="String"></param>
        /// <returns></returns>
        public static string UrlEncode(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : Uri.EscapeDataString(text);
        }

        /// <summary>
        /// Encodes a few additional characters for use in paths
        /// Encodes: . #
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string UrlEncodePathSafe(string text)
        {
            return StringUtils.UrlEncode(text).Replace(".", "%2E").Replace("#", "%23");
        }

        /// <summary>UrlDecodes a string without requiring System.Web</summary>
        /// <param name="text">String to decode.</param>
        /// <returns>decoded string</returns>
        public static string UrlDecode(string text)
        {
            text = text.Replace("+", " ");
            return Uri.UnescapeDataString(text);
        }

        /// <summary>Retrieves a text by key from a UrlEncoded string.</summary>
        /// <param name="urlEncoded">UrlEncoded String</param>
        /// <param name="key">Key to retrieve text for</param>
        /// <returns>returns the text or "" if the key is not found or the text is blank</returns>
        public static string GetUrlEncodedKey(string urlEncoded, string key)
        {
            urlEncoded = "&" + urlEncoded + "&";
            int num1 = urlEncoded.IndexOf("&" + key + "=", StringComparison.OrdinalIgnoreCase);
            if (num1 < 0)
                return string.Empty;
            int startIndex = num1 + 2 + key.Length;
            int num2 = urlEncoded.IndexOf("&", startIndex);
            return num2 < 0 ? string.Empty : StringUtils.UrlDecode(urlEncoded.Substring(startIndex, num2 - startIndex));
        }

        /// <summary>
        /// Allows setting of a text in a UrlEncoded string. If the key doesn't exist
        /// a new one is set, if it exists it's replaced with the new text.
        /// </summary>
        /// <param name="urlEncoded">A UrlEncoded string of key text pairs</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SetUrlEncodedKey(string urlEncoded, string key, string value)
        {
            if (!urlEncoded.EndsWith("?") && !urlEncoded.EndsWith("&"))
                urlEncoded += "&";
            Match match = Regex.Match(urlEncoded, "[?|&]" + key + "=.*?&");
            if (match == null || string.IsNullOrEmpty(match.Value))
                urlEncoded = urlEncoded + key + "=" + StringUtils.UrlEncode(value) + "&";
            else
                urlEncoded = urlEncoded.Replace(match.Value, match.Value.Substring(0, 1) + key + "=" + StringUtils.UrlEncode(value) + "&");
            return urlEncoded.TrimEnd('&');
        }

        /// <summary>
        /// Turns a BinHex string that contains raw byte values
        /// into a byte array
        /// </summary>
        /// <param name="hex">BinHex string (just two byte hex digits strung together)</param>
        /// <returns></returns>
        public static byte[] BinHexToBinary(string hex)
        {
            int index1 = hex.StartsWith("0x") ? 2 : 0;
            if (hex.Length % 2 != 0)
                throw new ArgumentException();
            byte[] numArray = new byte[(hex.Length - index1) / 2];
            for (int index2 = 0; index2 < numArray.Length; ++index2)
            {
                numArray[index2] = (byte)(StringUtils.ParseHexChar(hex[index1]) << 4 | StringUtils.ParseHexChar(hex[index1 + 1]));
                index1 += 2;
            }
            return numArray;
        }

        /// <summary>
        /// Converts a byte array into a BinHex string.
        /// BinHex is two digit hex byte values squished together
        /// into a string.
        /// </summary>
        /// <param name="data">Raw data to send</param>
        /// <returns>BinHex string or null if input is null</returns>
        public static string BinaryToBinHex(byte[] data)
        {
            if (data == null)
                return (string)null;
            StringBuilder stringBuilder = new StringBuilder(data.Length * 2);
            foreach (byte num in data)
                stringBuilder.AppendFormat("{0:x2}", (object)num);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts a string into bytes for storage in any byte[] types
        /// buffer or stream format (like MemoryStream).
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding">The character encoding to use. Defaults to Unicode</param>
        /// <returns></returns>
        public static byte[] StringToBytes(string text, Encoding encoding = null)
        {
            if (text == null)
                return (byte[])null;
            if (encoding == null)
                encoding = Encoding.Unicode;
            return encoding.GetBytes(text);
        }

        /// <summary>Converts a byte array to a stringUtils</summary>
        /// <param name="buffer">raw string byte data</param>
        /// <param name="encoding">Character encoding to use. Defaults to Unicode</param>
        /// <returns></returns>
        public static string BytesToString(byte[] buffer, Encoding encoding = null)
        {
            if (buffer == null)
                return (string)null;
            if (encoding == null)
                encoding = Encoding.Unicode;
            return encoding.GetString(buffer);
        }

        private static int ParseHexChar(char c)
        {
            if (c >= '0' && c <= '9')
                return (int)c - 48;
            if (c >= 'A' && c <= 'F')
                return (int)c - 65 + 10;
            if (c >= 'a' && c <= 'f')
                return (int)c - 97 + 10;
            throw new ArgumentException();
        }

        /// <summary>
        /// Encodes an integer into a string by mapping to alpha and digits (36 chars)
        /// chars are embedded as lower case
        ///
        /// Example: 4zx12ss
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Base36Encode(long value)
        {
            string str = "";
            bool flag = value < 0L;
            if (flag)
                value *= -1L;
            do
            {
                str = StringUtils.base36CharArray[value % (long)StringUtils.base36CharArray.Length].ToString() + str;
                value /= 36L;
            }
            while (value != 0L);
            return !flag ? str : str + "-";
        }

        /// <summary>Decodes a base36 encoded string to an integer</summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static long Base36Decode(string input)
        {
            bool flag = false;
            if (input.EndsWith("-"))
            {
                flag = true;
                input = input.Substring(0, input.Length - 1);
            }
            char[] charArray = input.ToCharArray();
            Array.Reverse((Array)charArray);
            long num1 = 0;
            for (long index = 0; index < (long)charArray.Length; ++index)
            {
                long num2 = (long)StringUtils.base36Chars.IndexOf(charArray[index]);
                num1 += Convert.ToInt64((double)num2 * Math.Pow(36.0, (double)index));
            }
            return !flag ? num1 : num1 * -1L;
        }

        /// <summary>Normalizes linefeeds to the appropriate</summary>
        /// <param name="text">The text to fix up</param>
        /// <param name="type">Type of linefeed to fix up to</param>
        /// <returns></returns>
        public static string NormalizeLineFeeds(string text, LineFeedTypes type = LineFeedTypes.Auto)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (type == LineFeedTypes.Auto)
                type = !Environment.NewLine.Contains<char>('\r') ? LineFeedTypes.Lf : LineFeedTypes.CrLf;
            return type == LineFeedTypes.Lf ? text.Replace("\r\n", "\n") : text.Replace("\r\n", "*@\r@*").Replace("\n", "\r\n").Replace("*@\r@*", "\r\n");
        }

        /// <summary>
        /// Strips any common white space from all lines of text that have the same
        /// common white space text. Effectively removes common code indentation from
        /// code blocks for example so you can get a left aligned code snippet.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string NormalizeIndentation(string code)
        {
            string[] strArray = code.Replace("\t", "   ").Split(new string[3]
            {
                "\r\n",
                "\r",
                "\n"
            }, StringSplitOptions.None);
            int count = 1000;
            foreach (string str1 in strArray)
            {
                if (str1.Length != 0)
                {
                    int num = 0;
                    string str2 = str1;
                    for (int index = 0; index < str2.Length && str2[index] == ' ' && num < count; ++index)
                        ++num;
                    if (num == 0)
                        return code;
                    count = num;
                }
            }
            string findString = new string(' ', count);
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string origString in strArray)
                stringBuilder.AppendLine(StringUtils.ReplaceStringInstance(origString, findString, "", 1, false));
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Simple Logging method that allows quickly writing a string to a file
        /// </summary>
        /// <param name="output"></param>
        /// <param name="filename"></param>
        /// <param name="encoding">if not specified used UTF-8</param>
        public static void LogString(string output, string filename, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            StreamWriter streamWriter = new StreamWriter(filename, true, encoding);
            streamWriter.WriteLine(DateTime.Now.ToString() + " - " + output);
            streamWriter.Close();
        }

        /// <summary>
        /// Creates a Stream from a string. Internally creates
        /// a memory stream and returns that.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static Stream StringToStream(string text, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.Default;
            MemoryStream memoryStream = new MemoryStream(text.Length * 2);
            byte[] bytes = encoding.GetBytes(text);
            memoryStream.Write(bytes, 0, bytes.Length);
            memoryStream.Position = 0L;
            return (Stream)memoryStream;
        }

        /// <summary>Retrieves a text from an XML-like string</summary>
        /// <param name="propertyString"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetProperty(string propertyString, string key)
        {
            return propertyString.ExtractString("<" + key + ">", "</" + key + ">", false, false, false);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="propertyString"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SetProperty(string propertyString, string key, string value)
        {
            string oldValue = propertyString.ExtractString("<" + key + ">", "</" + key + ">", false, false, false);
            if (string.IsNullOrEmpty(value) && oldValue != string.Empty)
                return propertyString.Replace(oldValue, "");
            string newValue = "<" + key + ">" + value + "</" + key + ">";
            return oldValue != string.Empty ? propertyString.Replace(oldValue, newValue) : propertyString + newValue + "\r\n";
        }
    }

    public enum LineFeedTypes
    {
        Lf,
        CrLf,
        Auto,
    }
}
