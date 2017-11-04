using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NbuLibrary.Core.NotificationModule
{
    public class HtmlProcessor
    {
        static string[] allowedTags = new[] { "b", "p", "i", "ul", "ol", "li", "br", "strong", "em" };

        /// <summary>
        /// Decodes the permited "safe" html tags in an encoded string
        /// </summary>
        /// <param name="encodedHtml">The encoded html content to be processed.</param>
        /// <returns>HTML content with only the allowed tags decoded.</returns>
        public static string ProcessEncodedHtml(string encodedHtml)
        {
            StringBuilder sb = new StringBuilder(encodedHtml);
            foreach (var tag in allowedTags)
            {
                sb.Replace(string.Format("&lt;{0}&gt;", tag), string.Format("<{0}>", tag));
                sb.Replace(string.Format("&lt;/{0}&gt;", tag), string.Format("</{0}>", tag));
                sb.Replace(string.Format("&lt;{0}/&gt;", tag), string.Format("<{0}/>", tag));
            }
            sb.Replace("&nbsp;", " ");

            string result = sb.ToString();
            try
            {
                Regex beginAnchor = new Regex("&lt;a", RegexOptions.Compiled);
                Regex targetAttr = new Regex("target=&quot;(blank|_parent|_self|_top)&quot;", RegexOptions.Compiled);
                Regex hrefAttr = new Regex("href=&quot;", RegexOptions.Compiled);
                var r2 = new StringBuilder();
                int lastMatchEnd = -1; ;
                foreach (Match m in beginAnchor.Matches(result))
                {
                    r2.Append(result.Substring(0, m.Index));
                    int end = result.IndexOf("&gt;", m.Index)+4;
                    string a = result.Substring(m.Index, end - m.Index);
                    Match mHref = hrefAttr.Match(a);
                    int startHref = mHref.Index + mHref.Length;
                    int endHref = a.IndexOf("&quot;", startHref);
                    string hrefValue = a.Substring(startHref, endHref - startHref);
                    if (hrefValue.StartsWith("https://") || hrefValue.StartsWith("http://"))
                    {
                        var target = targetAttr.Match(a);
                        var resultTarget = target.Success ? target.Value.Replace("&quot;", "\"") : "";
                        r2.AppendFormat("<a {0} href=\"{1}\">", resultTarget, hrefValue);
                        var closingTag = result.IndexOf("&lt;/a&gt;", end);
                        r2.Append(result.Substring(end, closingTag - end));
                        r2.Append("</a>");                   
                        lastMatchEnd = closingTag + "&lt;/a&gt;".Length;
                    }
                    else
                    {
                        lastMatchEnd = m.Index;
                    }
                }

                if (lastMatchEnd >= 0)
                {
                    r2.Append(result.Substring(lastMatchEnd));
                    result = r2.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }

            return result;
        }
    }
}
