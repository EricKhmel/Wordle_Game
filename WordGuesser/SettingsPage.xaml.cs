using Plugin.Maui.Audio;
using SQLite;
using System;

namespace WordGuesser;

public partial class SettingsPage : ContentPage
{
    SQLiteConnection conn;
    private readonly IAudioManager audioManager;

    public void CreateConnection()
    {
        string libFolder = FileSystem.AppDataDirectory;
        string fname = System.IO.Path.Combine(libFolder, "games.db");
        conn = new SQLiteConnection(fname);
    }

    public SettingsPage(IAudioManager audioManager)
    {
        InitializeComponent();
        this.audioManager = audioManager;
        CreateConnection();
        LoadPreferences();

        backgroundPicker.SelectedIndexChanged += BackgroundPicker_SelectedIndexChanged;
        soundSwitch.Toggled += SoundSwitch_Toggled;
    }

    private void LoadPreferences()
    {
        if (Preferences.ContainsKey("BackgroundColor"))
        {
            string bgColor = Preferences.Get("BackgroundColor", "Blue");
            backgroundPicker.SelectedItem = bgColor;
            colorSwitcher(bgColor);
        }

        if (Preferences.ContainsKey("SoundOn"))
        {
            bool soundOn = Preferences.Get("SoundOn", true);
            soundSwitch.IsToggled = soundOn;
        }
    }

    private void SaveBackgroundColorPreference()
    {
        string selectedColor = backgroundPicker.SelectedItem.ToString();
        Preferences.Set("BackgroundColor", selectedColor);
    }

    private void SaveSoundPreference()
    {
        bool soundOn = soundSwitch.IsToggled;
        Preferences.Set("SoundOn", soundOn);
    }

    private void BackgroundPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        SaveBackgroundColorPreference();
        ChangeBackgroundColor();
    }

    private void ChangeBackgroundColor()
    {
        if (Preferences.ContainsKey("BackgroundColor"))
        {
            string selectedColor = Preferences.Get("BackgroundColor", "Blue");
            colorSwitcher(selectedColor);
        }
    }

    private void colorSwitcher(String s) {
        switch (s)
        {
            case "Blue":
                App.Current.Resources["PageBackgroundColor"] = Colors.DarkBlue;
                break;
            case "Red":
                App.Current.Resources["PageBackgroundColor"] = Colors.DarkRed;
                break;
            case "Green":
                App.Current.Resources["PageBackgroundColor"] = Colors.DarkGreen;
                break;
            case "Yellow":
                App.Current.Resources["PageBackgroundColor"] = Colors.YellowGreen;
                break;
            case "Orange":
                App.Current.Resources["PageBackgroundColor"] = Colors.OrangeRed;
                break;
            case "Grey":
                App.Current.Resources["PageBackgroundColor"] = Colors.DarkSlateGrey;
                break;
            default:
                App.Current.Resources["PageBackgroundColor"] = Colors.DarkBlue;
                break;
        }
    }

    private void SoundSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        SaveSoundPreference();
    }

    private async void OnDeleteButtonClickedAsync(object sender, EventArgs e)
    {
        if (await DisplayAlert("Confirmation", "Are you sure you want to delete all entries?", "Yes", "No"))
        {
            await Task.Run(() => conn.DeleteAll<Game>());
            conn.Execute("DELETE FROM sqlite_sequence WHERE name = 'game'");
        }
    }

    private async void OnHomeButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage(audioManager));
    }
}