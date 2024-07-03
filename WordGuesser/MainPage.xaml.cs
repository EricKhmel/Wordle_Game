using Microsoft.Maui.Controls.Xaml;
using Newtonsoft.Json;
using SQLite;
using Plugin.Maui.Audio;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WordGuesser;

public partial class MainPage : ContentPage
{
    private int currentColumnIndex = 0;
    private bool isInitializing = false;
    private int currentRowIndex = 0;
    private string targetWord;
    SQLiteConnection conn;
    private readonly IAudioManager audioManager;
    private IAudioPlayer? player;
    private bool isLooping = false;
    private float volume = 0.5f;

    public void CreateConnection()
    {
        string libFolder = FileSystem.AppDataDirectory;
        string fname = System.IO.Path.Combine(libFolder, "games.db");
        conn = new SQLiteConnection(fname);
        conn.CreateTable<Game>();
    }

    public MainPage(IAudioManager audioManager)
    {
        CreateConnection();
        InitializeComponent();
        this.audioManager = audioManager;
        columnsPicker.SelectedItem = 3;
        ChangeBackgroundColor();
        InitializeGrid();
        FetchTargetWord();
    }

    private void ChangeBackgroundColor()
    {
        if (Preferences.ContainsKey("BackgroundColor"))
        {
            string selectedColor = Preferences.Get("BackgroundColor", "Blue");
            colorSwitcher(selectedColor);
        }
    }

    private void InitializeGrid()
    {
        int initialColumns = 3;
        UpdateGridColumns(initialColumns);
    }

    private async void FetchTargetWord()
    {
        int wordLength = 3;
        if (columnsPicker.SelectedItem != null)
        {
            wordLength = int.Parse(columnsPicker.SelectedItem.ToString());
        }
        string requestUrl = $"https://random-word-api.herokuapp.com/word?length={wordLength}";
        using (HttpClient client = new HttpClient())
        {
            string jsonResponse = await client.GetStringAsync(requestUrl);
            targetWord = JsonConvert.DeserializeObject<List<string>>(jsonResponse)[0];
        }
    }

    private void OnPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedColumns = int.Parse(columnsPicker.SelectedItem.ToString());
        ResetGame();
        UpdateGridColumns(selectedColumns);
        FetchTargetWord();
    }

    private void UpdateGridColumns(int columns)
    {
        guessGrid.RowDefinitions.Clear();
        guessGrid.ColumnDefinitions.Clear();

        for (int i = 0; i < columns; i++)
        {
            guessGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = 55 });
        }

        for (int i = 0; i < 6; i++)
        {
            guessGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Star });
        }

        currentColumnIndex = 0;

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                var entry = new Entry
                {
                    Text = " ",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HeightRequest = 45,
                    WidthRequest = 45,
                    IsEnabled = false,
                    MaxLength = 1
                };

                isInitializing = true;
                entry.Text = "";
                isInitializing = false;

                Grid.SetRow(entry, i);
                Grid.SetColumn(entry, j);

                guessGrid.Children.Add(entry);
            }
        }
    }

    private async void OnEntryCompleted(object sender, EventArgs e)
    {
        string word = wordEntry.Text;

        if (word.Length != guessGrid.ColumnDefinitions.Count)
        {
            await DisplayAlert("Error", "Word is too " + (word.Length < guessGrid.ColumnDefinitions.Count ? "short" : "long"), "OK");
            return;
        }

        if (!IsWordValid(word))
        {
            await DisplayAlert("Error", "Please enter a word containing only letters.", "OK");
            return;
        }

        for (int i = 0; i < word.Length; i++)
        {
            var boxView = new BoxView
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                CornerRadius = 4,
                WidthRequest = 45,
                HeightRequest = 45
            };

            var label = new Label
            {
                Text = word[i].ToString().ToUpper(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true,
                TextColor = Colors.Black
            };

            if (word[i] == targetWord[i])
            {
                boxView.Color = Colors.Green;
            }
            else if (targetWord.Contains(word[i]))
            {
                boxView.Color = Colors.Yellow;
            }
            else
            {
                boxView.Color = Colors.Red;
            }

            Grid.SetRow(boxView, currentRowIndex);
            Grid.SetColumn(boxView, i);
            guessGrid.Children.Add(boxView);

            Grid.SetRow(label, currentRowIndex);
            Grid.SetColumn(label, i);
            guessGrid.Children.Add(label);
        }

        currentRowIndex++;

        if (currentRowIndex >= guessGrid.RowDefinitions.Count || IsCorrectWordGuessed())
        {
            wordEntry.IsEnabled = false;
            resetButton.IsVisible = true;
            DisplayCorrectWord();

            Game newGame = new Game { WorL = IsCorrectWordGuessed(), Word = correctWordLabel.Text, Tries = currentRowIndex };
            conn.Insert(newGame);

            await PlayEndGameSoundAsync(IsCorrectWordGuessed());
            player = null;
        }
        else
        {
            wordEntry.Text = "";
        }
    }

    private async Task PlayEndGameSoundAsync(bool isWin)
    {
        if (Preferences.ContainsKey("SoundOn") && Preferences.Get("SoundOn", true))
        {
            string audioFileName = isWin ? "win.wav" : "loss.wav";
            Debug.WriteLine($"Playing sound: {audioFileName}");
            await PlayAudioFileAsync(audioFileName);
        }
    }

    private async Task PlayAudioFileAsync(string fileName)
    {
        try
        {
            if (player == null)
            {
                player = audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync(fileName));
            }

            player.Loop = isLooping;
            player.Volume = volume;

            if (player.IsPlaying)
            {
                player.Pause();
            }
            else
            {
                player.Play();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing audio: {ex.Message}");
        }
    }

    private bool IsCorrectWordGuessed()
    {
        return string.Equals(wordEntry.Text, targetWord, StringComparison.OrdinalIgnoreCase);
    }


    private void OnResetButtonClicked(object sender, EventArgs e)
    {
        ResetGame();
    }

    private void ResetGame()
    {
        wordEntry.Text = "";
        wordEntry.IsEnabled = true;
        resetButton.IsVisible = false;
        currentRowIndex = 0;
        guessGrid.Children.Clear();
        correctWordLabel.Text = string.Empty;
        FetchTargetWord();

        if (columnsPicker.SelectedItem != null)
        {   
            UpdateGridColumns(int.Parse(columnsPicker.SelectedItem.ToString()));
        }
    }

    private void DisplayCorrectWord()
    {
        correctWordLabel.Text = $"Correct Word: {targetWord.ToUpper()}";
    }

    private async void OnHistoryButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HistoryPage(audioManager));
    }

    private async void OnSettingsButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage(audioManager));
    }

    private void colorSwitcher(String s)
    {
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

    private bool IsWordValid(string word)
    {
        return Regex.IsMatch(word, "^[a-zA-Z]+$");
    }

}