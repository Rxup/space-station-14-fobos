using Content.Client.Eui;
using Content.Shared.DeadSpace.Sponsor;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.DeadSpace.Sponsor.UI;

public sealed class SponsorMessageBox : DefaultWindow
{
    public readonly Button OkButton;

    public SponsorMessageBox(string message, string title="Подтверждение")
    {
        Title = title;
        OkButton = new Button
        {
            Text = "Ок",
        };

        OkButton.OnPressed += _ => Close();

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
                        new Label()
                        {
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
