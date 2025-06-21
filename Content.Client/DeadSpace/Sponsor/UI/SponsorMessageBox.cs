using Content.Client.Eui;
using Content.Shared.DeadSpace.Sponsor;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.DeadSpace.Sponsor.UI;

public sealed class SponsorMessageBox : DefaultWindow
{
    public readonly Button OkButton;

    public SponsorMessageBox(string message, string title="Внимание!")
    {
        Title = title;
        OkButton = new Button
        {
            Text = "Ок",
            VerticalAlignment = VAlignment.Bottom,
            HorizontalAlignment = HAlignment.Center,
        };

        OkButton.OnPressed += _ => Close();
        HorizontalAlignment = HAlignment.Stretch;
        VerticalAlignment = VAlignment.Stretch;
        MinWidth = 400;
        MinHeight = 100;
        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Children =
                    {
                        new RichTextLabel()
                        {
                            HorizontalAlignment = HAlignment.Center,
                            VerticalAlignment = VAlignment.Center,
                            Text = message,
                        },
                        new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Horizontal,
                            Align = BoxContainer.AlignMode.Center,
                            Children =
                            {
                                OkButton,
                            },
                        },
                    },
                },
            },
        });
    }
}
