using System.Xml.Linq;

namespace KindlyBartender.App.Windows;

/// <summary>Builds the XML of a Windows notification. Text is escaped, so no string can change the XML.</summary>
internal static class ToastContent
{
    public static string Build(string title, string body, bool silent)
    {
        var toast = new XElement(
            "toast",
            new XElement(
                "visual",
                new XElement(
                    "binding",
                    new XAttribute("template", "ToastGeneric"),
                    new XElement("text", title),
                    new XElement("text", body))));

        if (silent)
        {
            toast.Add(new XElement("audio", new XAttribute("silent", "true")));
        }

        return toast.ToString(SaveOptions.DisableFormatting);
    }
}
