using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Task 10, Requirement 9/10: server-side defense-in-depth sanitizer for Rich Text Editor
/// content. TinyMCE already restricts what a well-behaved browser client can produce
/// (valid_elements / extended_valid_elements in RichTextEditorEdit.aspx), but the server
/// must never trust client-supplied HTML — a malicious client could bypass the editor
/// entirely and POST arbitrary markup straight to Save.aspx.cs. This runs a strict
/// allow-list pass over the HTML before it is ever written to or read back from the
/// database, so stored/reflected XSS via &lt;script&gt;, event handlers, javascript: URLs,
/// or embedded frames/objects is not possible even if the client-side check is skipped.
///
/// Deliberately dependency-free (no HtmlAgilityPack / external NuGet package) so the
/// project keeps building offline; this is a regex/allow-list pass, not a full HTML
/// parser, but it is applied consistently on every save and is safe to run twice.
/// </summary>
public static class RichTextSanitizer
{
    // Tags that are always stripped completely, including their content — these have no
    // legitimate place inside a rich-text document body and are common XSS vectors.
    private static readonly string[] DangerousTagsWithContent =
    {
        "script", "style", "iframe", "object", "embed", "applet", "form", "svg", "math"
    };

    // Tags that are stripped but whose inner text/content is kept (unwrap, don't delete).
    private static readonly string[] DangerousTagsUnwrapOnly =
    {
        "meta", "link", "base", "title", "head", "html", "body", "noscript"
    };

    // Attributes that are never allowed on any surviving element.
    private static readonly Regex EventHandlerAttr =
        new Regex(@"\s+on\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StyleAttr =
        new Regex(@"\s+style\s*=\s*(""[^""]*""|'[^']*')", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JavascriptHrefOrSrc =
        new Regex(@"(href|src|xlink:href)\s*=\s*(""|')\s*javascript:[^""']*(""|')", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DataUrlNonImage =
        new Regex(@"(href|src)\s*=\s*(""|')\s*data:(?!image/(png|jpe?g|gif|webp|svg\+xml);base64,)[^""']*(""|')", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExprBinding =
        new Regex(@"expression\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CommentTag =
        new Regex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Whitelisted inline "style" declarations we allow to survive (this is what lets
    /// TinyMCE's font color/size, alignment, line-height and paragraph-spacing controls,
    /// which are expressed as inline styles, round-trip through the sanitizer safely).
    /// </summary>
    private static readonly Regex SafeStyleDecl = new Regex(
        @"^\s*(color|background-color|font-family|font-size|font-weight|font-style|" +
        @"text-align|text-decoration|line-height|margin(-top|-bottom|-left|-right)?|" +
        @"padding(-top|-bottom|-left|-right)?|width|height|border(-collapse)?|" +
        @"vertical-align|list-style-type)\s*:\s*[a-zA-Z0-9#%.,\s\-]+\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes rich-text HTML for safe storage and safe re-display. Idempotent — safe
    /// to call again on content that has already passed through this method.
    /// </summary>
    public static string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        string result = html;

        // 1. Drop HTML comments (can be used to smuggle conditional-comment IE exploits).
        result = CommentTag.Replace(result, string.Empty);

        // 2. Strip dangerous tags entirely, including their inner content.
        foreach (string tag in DangerousTagsWithContent)
        {
            result = Regex.Replace(result, @"<" + tag + @"\b[^>]*>[\s\S]*?</" + tag + @"\s*>",
                string.Empty, RegexOptions.IgnoreCase);
            // Also remove any stray unmatched/self-closed opening tag of the same name.
            result = Regex.Replace(result, @"<" + tag + @"\b[^>]*/?>",
                string.Empty, RegexOptions.IgnoreCase);
        }

        // 3. Unwrap (but keep content of) structural tags that should never appear inside a
        //    fragment stored as a document body.
        foreach (string tag in DangerousTagsUnwrapOnly)
        {
            result = Regex.Replace(result, @"</?" + tag + @"\b[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        }

        // 4. Remove every on*="..." inline event handler attribute (onclick, onerror, onload...).
        result = EventHandlerAttr.Replace(result, string.Empty);

        // 5. Neutralize javascript: URLs in href/src and non-image data: URLs.
        result = JavascriptHrefOrSrc.Replace(result, "$1=\"#\"");
        result = DataUrlNonImage.Replace(result, "$1=\"\"");

        // 6. CSS expression()/behavior injection guard inside any surviving style attribute.
        result = ExprBinding.Replace(result, "no-expr(");

        // 7. Filter inline style="" attributes down to a safe declaration whitelist instead
        //    of stripping style entirely, since TinyMCE relies on inline styles for color,
        //    alignment, line-height and paragraph spacing.
        result = StyleAttr.Replace(result, m => FilterStyleAttribute(m.Value));

        return result;
    }

    private static string FilterStyleAttribute(string attrText)
    {
        // attrText looks like: style="color:#ff0000; margin-top:10px; behavior:url(evil)"
        int firstQuote = attrText.IndexOfAny(new[] { '"', '\'' });
        if (firstQuote < 0) return string.Empty;
        char quoteChar = attrText[firstQuote];
        int lastQuote = attrText.LastIndexOf(quoteChar);
        if (lastQuote <= firstQuote) return string.Empty;

        string inner = attrText.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
        string[] declarations = inner.Split(';');
        List<string> kept = new List<string>();

        foreach (string decl in declarations)
        {
            string trimmed = decl.Trim();
            if (trimmed.Length == 0) continue;
            if (SafeStyleDecl.IsMatch(trimmed) && !ExprBinding.IsMatch(trimmed) && trimmed.IndexOf("url(", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                kept.Add(trimmed);
            }
        }

        if (kept.Count == 0) return string.Empty;
        return " style=\"" + string.Join("; ", kept) + "\"";
    }

    /// <summary>
    /// Strips all HTML tags for use in plain-text contexts (e.g. a search-result snippet),
    /// leaving only decoded-safe text content.
    /// </summary>
    public static string StripTags(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        string text = Regex.Replace(html, "<[^>]*>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
