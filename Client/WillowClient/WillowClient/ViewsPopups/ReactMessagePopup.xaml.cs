using CommunityToolkit.Maui.Views;

namespace WillowClient.ViewsPopups;

public partial class ReactMessagePopup : Popup
{
	public ReactMessagePopup()
	{
		InitializeComponent();     
	}

    public ReactMessagePopup(string messageText) {
        InitializeComponent();
        this.labelMessage.Text = messageText;
    }

    private async void Popup_Opened(object sender, CommunityToolkit.Maui.Core.PopupOpenedEventArgs e) {
        //await this.Anchor.TranslateTo(this.Anchor.X + 60, this.Anchor.Y - 20, 1000, Easing.Linear);
    }

    //Return the emoji that was clicked
    private void EmojiSmile_Clicked(object sender, EventArgs e) {
        this.Close("😀");
    }
    private void EmojiLaughTear_Clicked(object sender, EventArgs e) {
        this.Close("😂");
    }
    private void EmojiSmileRed_Clicked(object sender, EventArgs e) {
        this.Close("😊");
    }
    private void EmojiHearts_Clicked(object sender, EventArgs e) {
        this.Close("🥰");
    }
    private void EmojiTongue_Clicked(object sender, EventArgs e) {
        this.Close("😝");
    }
}