﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Clifton.Tools.Strings.Extensions
{
	public static class StringHelpersExtensions
	{
		public static bool IsInt32(this String src)
		{
			int result;
			bool ret = Int32.TryParse(src, out result);

			return ret;
		}

		public static string ParseQuote(this String src)
		{
			return src.Replace("\"", "'");
		}

		public static string Quote(this String src)
		{
			return "\"" + src + "\"";
		}

		public static string Brace(this String src)
		{
			return "[" + src + "]";
		}

		public static string Between(this String src, char c1, char c2)
		{
			return Clifton.Tools.Strings.StringHelpers.Between(src, c1, c2);
		}

		public static string Between(this String src, string s1, string s2)
		{
			return src.RightOf(s1).LeftOf(s2);
		}

		public static string RightOf(this String src, char c)
		{
			return Clifton.Tools.Strings.StringHelpers.RightOf(src, c);
		}

		public static bool BeginsWith(this String src, string s)
		{
			return src.StartsWith(s);
		}

		public static string RightOf(this String src, string s)
		{
			string ret = String.Empty;
			int idx = src.IndexOf(s);

			if (idx != -1)
			{
				ret = src.Substring(idx + s.Length);
			}

			return ret;
		}

		public static string RightOfRightmostOf(this String src, char c)
		{
			return Clifton.Tools.Strings.StringHelpers.RightOfRightmostOf(src, c);
		}

		public static string LeftOf(this String src, char c)
		{
			return Clifton.Tools.Strings.StringHelpers.LeftOf(src, c);
		}

		public static string LeftOf(this String src, string s)
		{
			string ret = s;
			int idx = src.IndexOf(s);

			if (idx != -1)
			{
				ret = src.Substring(0, idx);
			}

			return ret;
		}

		public static string LeftOfRightmostOf(this String src, char c)
		{
			return Clifton.Tools.Strings.StringHelpers.LeftOfRightmostOf(src, c);
		}

		public static string LeftOfRightmostOf(this String src, string s)
		{
			string ret = src;
			int idx = src.IndexOf(s);
			int idx2 = idx;

			while (idx2 != -1)
			{
				idx2 = src.IndexOf(s, idx + s.Length);

				if (idx2 != -1)
				{
					idx = idx2;
				}
			}

			if (idx != -1)
			{
				ret = src.Substring(0, idx);
			}

			return ret;
		}

		public static string RightOfRightmostOf(this String src, string s)
		{
			string ret = src;
			int idx = src.IndexOf(s);
			int idx2 = idx;

			while (idx2 != -1)
			{
				idx2 = src.IndexOf(s, idx + s.Length);

				if (idx2 != -1)
				{
					idx = idx2;
				}
			}

			if (idx != -1)
			{
				ret = src.Substring(idx + s.Length, src.Length - (idx + s.Length));
			}

			return ret;
		}

		public static char Rightmost(this String src)
		{
			return Clifton.Tools.Strings.StringHelpers.Rightmost(src);
		}

		public static string TrimLastChar(this String src)
		{
			string ret = String.Empty;
			int len = src.Length;

			if (len > 1)
			{
				ret = src.Substring(0, len - 1);
			}

			return ret;
		}

		public static bool IsBlank(this string src)
		{
			return String.IsNullOrEmpty(src) || (src.Trim()==String.Empty);
		}

		/// <summary>
		/// Returns the first occurance of any token given the list of tokens.
		/// </summary>
		public static string Contains(this String src, string[] tokens)
		{
			string ret = String.Empty;
			int firstIndex=9999;

			// Find the index of the first index encountered.
			foreach (string token in tokens)
			{
				int idx = src.IndexOf(token);

				if ( (idx != -1) && (idx < firstIndex) )
				{
					ret = token;
					firstIndex = idx;
				}
			}

			return ret;
		}

		public static int to_i(this string src)
		{
			return Convert.ToInt32(src);
		}

		public static bool to_b(this string src)
		{
			return Convert.ToBoolean(src);
		}

		public static T ToEnum<T>(this string src)
		{
			T enumVal = (T)Enum.Parse(typeof(T), src);

			return enumVal;
		}
	}
}
