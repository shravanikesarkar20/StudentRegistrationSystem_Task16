<%@ WebHandler Language="C#" Class="CaptchaHandler" %>

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;

/// <summary>
/// Generates a distorted CAPTCHA image for the Student Login page and stores the
/// corresponding code in Session["CaptchaCode"] so Login.aspx.cs can validate it.
/// </summary>
public class CaptchaHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
{
    private const string CHARSET = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // ambiguous chars removed

    public void ProcessRequest(HttpContext context)
    {
        string code = GenerateCode(6);
        context.Session["CaptchaCode"] = code;

        context.Response.ContentType = "image/png";
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        context.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));

        using (Bitmap bmp = new Bitmap(160, 50))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            Random rnd = new Random();
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.WhiteSmoke);

            // Noise lines behind the text
            for (int i = 0; i < 6; i++)
            {
                using (Pen pen = new Pen(Color.FromArgb(rnd.Next(150, 220), rnd.Next(150, 220), rnd.Next(150, 220)), 1))
                {
                    g.DrawLine(pen, rnd.Next(bmp.Width), rnd.Next(bmp.Height), rnd.Next(bmp.Width), rnd.Next(bmp.Height));
                }
            }

            using (Font font = new Font(FontFamily.GenericSansSerif, 22, FontStyle.Bold))
            {
                float x = 10;
                foreach (char c in code)
                {
                    Color textColor = Color.FromArgb(rnd.Next(20, 90), rnd.Next(20, 90), rnd.Next(20, 90));
                    using (Brush brush = new SolidBrush(textColor))
                    {
                        var state = g.Save();
                        g.TranslateTransform(x, 10);
                        g.RotateTransform(rnd.Next(-18, 18));
                        g.DrawString(c.ToString(), font, brush, 0, 0);
                        g.Restore(state);
                    }
                    x += 24;
                }
            }

            // Noise dots on top of the text
            for (int i = 0; i < 70; i++)
            {
                bmp.SetPixel(rnd.Next(bmp.Width), rnd.Next(bmp.Height),
                    Color.FromArgb(rnd.Next(120, 200), rnd.Next(120, 200), rnd.Next(120, 200)));
            }

            bmp.Save(context.Response.OutputStream, ImageFormat.Png);
        }
    }

    private string GenerateCode(int length)
    {
        Random rnd = new Random(Guid.NewGuid().GetHashCode());
        char[] buffer = new char[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = CHARSET[rnd.Next(CHARSET.Length)];
        }
        return new string(buffer);
    }

    public bool IsReusable
    {
        get { return false; }
    }
}
