using System.Windows.Controls;
using KindlyBartender.App.Views;

namespace KindlyBartender.App.Tests.Views;

public class MessageCardTests
{
    [Fact]
    public void Clearing_extras_keeps_the_message()
    {
        string? text = null;
        var shown = false;
        RunOnSta(() =>
        {
            var card = new MessageCard();
            card.Show("A new version is available: 0.1.1", warning: false);
            card.Extras.Add(new TextBlock { Text = "Old link" });

            card.Extras.Clear();
            card.Extras.Add(new TextBlock { Text = "New link" });

            text = card.Text;
            shown = ContainsText(card, "A new version is available: 0.1.1");
        });

        Assert.Equal("A new version is available: 0.1.1", text);
        Assert.True(shown);
    }

    private static bool ContainsText(System.Windows.DependencyObject root, string text)
    {
        if (root is TextBlock block && block.Text == text)
        {
            return true;
        }

        return System.Windows.LogicalTreeHelper.GetChildren(root).OfType<System.Windows.DependencyObject>().Any(child => ContainsText(child, text));
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                failure = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }
}
