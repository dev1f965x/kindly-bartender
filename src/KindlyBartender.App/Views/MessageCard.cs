using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace KindlyBartender.App.Views;

/// <summary>
/// A card with an info or warning mark and wrapped text. The caution color is only on the mark, so the text
/// carries the meaning (DESIGN.md, Layout rules). Changes are announced to screen readers.
/// </summary>
internal sealed class MessageCard : Border
{
    private const string InfoGlyph = "";
    private const string WarningGlyph = "";

    private readonly TextBlock _mark = new()
    {
        FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
        FontSize = 16,
        Margin = new Thickness(0, 2, 12, 0),
        VerticalAlignment = VerticalAlignment.Top,
    };

    private readonly TextBlock _text = new() { TextWrapping = TextWrapping.Wrap, FontSize = 14 };
    private readonly StackPanel _body = new();

    public MessageCard()
    {
        SetResourceReference(StyleProperty, "Card");
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
        _text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        _body.Children.Add(_text);
        Grid.SetColumn(_body, 1);
        grid.Children.Add(_mark);
        grid.Children.Add(_body);
        Child = grid;
        Visibility = Visibility.Collapsed;
    }

    /// <summary>Extra content under the text, such as a link.</summary>
    public UIElementCollection Extras => _body.Children;

    public string Text => _text.Text;

    public void Show(string text, bool warning)
    {
        _text.Text = text;
        _mark.Text = warning ? WarningGlyph : InfoGlyph;
        _mark.SetResourceReference(TextBlock.ForegroundProperty, warning ? "SystemFillColorCautionBrush" : "TextFillColorSecondaryBrush");
        AutomationProperties.SetName(this, text);
        Visibility = Visibility.Visible;
        if (AutomationPeer() is { } peer)
        {
            peer.RaiseAutomationEvent(System.Windows.Automation.Peers.AutomationEvents.LiveRegionChanged);
        }
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    private System.Windows.Automation.Peers.AutomationPeer? AutomationPeer() =>
        System.Windows.Automation.Peers.UIElementAutomationPeer.FromElement(this)
        ?? System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(this);
}
