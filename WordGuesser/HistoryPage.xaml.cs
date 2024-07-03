using Plugin.Maui.Audio;
using SQLite;

namespace WordGuesser;

public partial class HistoryPage : ContentPage
{
    SQLiteConnection conn;
    private readonly IAudioManager audioManager;

    public void CreateConnection()
    {
        string libFolder = FileSystem.AppDataDirectory;
        string fname = System.IO.Path.Combine(libFolder, "games.db");
        conn = new SQLiteConnection(fname);
    }

    public HistoryPage(IAudioManager audioManager)
	{
		InitializeComponent();
        this.audioManager = audioManager;
        CreateConnection();
        lv.ItemsSource = conn.Table<Game>().ToList();
	}

    private async void OnHomeButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage(audioManager));
    }
}