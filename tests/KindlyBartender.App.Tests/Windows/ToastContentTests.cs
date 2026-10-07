using System.Xml.Linq;
using KindlyBartender.App.Windows;

namespace KindlyBartender.App.Tests.Windows;

public class ToastContentTests
{
    [Fact]
    public void Builds_a_generic_toast_with_title_and_body()
    {
        var toast = XElement.Parse(ToastContent.Build("상점 단계가 시작되었습니다", "선택하면 하스스톤으로 돌아갑니다.", silent: false));

        var texts = toast.Descendants("text").Select(t => t.Value).ToList();
        Assert.Equal(["상점 단계가 시작되었습니다", "선택하면 하스스톤으로 돌아갑니다."], texts);
        Assert.Equal("ToastGeneric", toast.Element("visual")!.Element("binding")!.Attribute("template")!.Value);
        Assert.Null(toast.Element("audio"));
    }

    [Fact]
    public void Silent_toast_turns_off_its_sound()
    {
        var toast = XElement.Parse(ToastContent.Build("t", "b", silent: true));

        Assert.Equal("true", toast.Element("audio")!.Attribute("silent")!.Value);
    }

    [Fact]
    public void Markup_in_text_is_escaped_not_interpreted()
    {
        var toast = XElement.Parse(ToastContent.Build("<audio silent=\"true\"/>", "a & b", silent: false));

        Assert.Equal("<audio silent=\"true\"/>", toast.Descendants("text").First().Value);
        Assert.Null(toast.Element("audio"));
    }
}
