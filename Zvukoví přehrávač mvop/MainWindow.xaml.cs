using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Shapes;
using TagLib;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using static MaterialDesignThemes.Wpf.Theme;
using System.Threading;

namespace Zvukoví_přehrávač_mvop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private Song currentlyPlayingSong;
        private bool hasPlayedOver75Percent;
        private ObservableCollection<Song> _songs;

        public ObservableCollection<Song> Songs
        {
            get { return _songs; }
            set
            {
                _songs = value;
                OnPropertyChanged(nameof(Songs));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class FolderPaths
        {
            public List<string> Paths { get; set; }
        }


        public class Song
        {
            private bool ratedThisPlay = false;
            private string _title;
            private string _author;
            private string _album;
            private string _genre;
            private int _durationInSeconds;
            private string _durationInMinutes;
            private int _rating;
            private int _playCount;
            private string _filePath;

            // Název písně
            public string Title
            {
                get { return _title; }
                set { _title = value; }
            }

            // Autor písně
            public string Author
            {
                get { return _author; }
                set { _author = value; }
            }

            // Název alba
            public string Album
            {
                get { return _album; }
                set { _album = value; }
            }

            // Hudební žánr
            public string Genre
            {
                get { return _genre; }
                set { _genre = value; }
            }

            // Délka písně (v sekundách)
            public int DurationInSeconds
            {
                get { return _durationInSeconds; }
                set { _durationInSeconds = value; }
            }

            public string DurationInMinutes
            {
                get { return _durationInMinutes; }
                set { _durationInMinutes = value; }
            }

            // Hodnocení písně (např. 1-5)
            public int Rating
            {
                get { return _rating; }
                set { _rating = value; }
            }

            // Počet přehrání
            public int PlayCount
            {
                get { return _playCount; }
                set { _playCount = value; }
            }

            // Cesta k souboru s písní
            public string FilePath
            {
                get { return _filePath; }
                set { _filePath = value; }
            }

            // Konstruktor třídy
            public Song(string title, string author, string album, string  genre, int duration, int rating, int playCount, string filePath)
            {
                Title = title;
                Author = author;
                Album = album;
                Genre = genre;
                DurationInSeconds = duration;
                int minutes = (duration / 60);
                int remainingSeconds = duration - (minutes * 60);
                string formattedResult = remainingSeconds.ToString("D2");
                DurationInMinutes = minutes.ToString() + ":" + formattedResult;
                Rating = rating;
                PlayCount = playCount;
                FilePath = filePath;
            }

            public void UpdatePlayCount(int newPlayCount, MainWindow mainWindow)
            {
                PlayCount = newPlayCount;
                ratedThisPlay = false; // Reset the flag when play count is updated
                mainWindow.UpdateMetadata(this);
            }

            public void UpdateRating(int newRating, MainWindow mainWindow)
            {
                if (!ratedThisPlay)
                {
                    Rating = newRating;
                    ratedThisPlay = true;
                    mainWindow.UpdateMetadata(this);
                }
            }







        }
        public void UpdateMetadata(Song x)
        {

            // Load the file using TagLib
            var file = TagLib.File.Create(x.FilePath);

            // Update the properties if they are not null
            if (x.Title != null)
                file.Tag.Title = x.Title;

            if (x.Author != null)
                file.Tag.Performers = new string[] { x.Author };

            if (x.Album != null)
                file.Tag.Album = x.Album;

            if (x.Genre != null)
                file.Tag.Genres = new string[] { x.Genre };

            // Update the custom comment fields
            var commentObject = new JObject();
            commentObject["Rating"] = x.Rating;
            commentObject["PlayCount"] = x.PlayCount;
            file.Tag.Comment = commentObject.ToString();

            // Save changes

            // Reload the media player with the updated file
            var e = mediaPlayer.Position;
            mediaPlayer.Stop();
            mediaPlayer.Close(); // Dispose the previous instance
            Thread.Sleep(10);
            file.Save();
            InitializeMediaPlayer();
            mediaPlayer.Open(new Uri(x.FilePath)); // Open the updated file
            mediaPlayer.Position = e;
            mediaPlayer.Play();
            ReloadListView(x);
        }


        public void ReloadListView(Song x)
        {
            if (song_ListView != null)
            {
                // Update the ListView by setting its ItemsSource again
                song_ListView.ItemsSource = null;
                song_ListView.ItemsSource = Songs;

                // Update the selected item in the ListView
                song_ListView.SelectedItem = x;
                song_ListView.ScrollIntoView(x);
            }
        }


        public MainWindow()
        {
            KeyDown += MainWindow_KeyDown;
            DataContext = this;
            InitializeMediaPlayer();
            SaveFolderPaths( new List<string> { "C:\\Users\\Tigo\\Music" });
            SaveSongs(LoadSongsFromFolders());
            Songs = LoadSongs();
            InitializeComponent();
            SetVolumeFromSettings();
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.MediaPreviousTrack: // Function key for previous track
                    previousButton_Click(null, null);
                    break;
                case Key.MediaNextTrack: // Function key for next track
                    nextButton_Click(null, null);
                    break;
                case Key.MediaStop: // Function key for stop
                    stopButton_Click(null,null);
                    break;
                case Key.Add:
                    likeButton_Click(null, null);
                    break;
                case Key.Subtract:
                    dislikeButton_Click(null, null);
                    break;
                case Key.VolumeMute:
                    UpdateVolume(0);
                    break;
                default:
                    break;
            }
        }

        private const string FilePath = "..\\..\\..\\folderPaths.json";

        public static void SaveFolderPaths(List<string> paths)
        {
            FolderPaths folderPaths = new FolderPaths { Paths = paths };
            string json = JsonConvert.SerializeObject(folderPaths);
            System.IO.File.WriteAllText(FilePath, json);
        }

        public static List<string> LoadFolderPaths()
        {
            if (System.IO.File.Exists(FilePath))
            {
                string json = System.IO.File.ReadAllText(FilePath);
                FolderPaths folderPaths = JsonConvert.DeserializeObject<FolderPaths>(json);
                return folderPaths.Paths;
            }
            return new List<string>();
        }

        private static void SaveSongs(ObservableCollection<Song> songs)
        {
            string json = JsonConvert.SerializeObject(songs);
            System.IO.File.WriteAllText("..\\..\\..\\songs.json", json);
        }
        private static ObservableCollection<Song> LoadSongs()
        {
            string filePath = "..\\..\\..\\songs.json";
            ObservableCollection<Song> songs = new ObservableCollection<Song>();

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    songs = JsonConvert.DeserializeObject<ObservableCollection<Song>>(json);
                }
                else
                {
                    Console.WriteLine("File not found: " + filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading songs: " + ex.Message);
            }
            var x = new ObservableCollection<Song>(songs.OrderBy(i => i.Title));
            return x;
        }



        public static ObservableCollection<Song> LoadSongsFromFolders()
        {
            List<string> paths = LoadFolderPaths();
            var songs = new ObservableCollection<Song>();

            foreach (var path in paths)
            {
                LoadSongsFromFolderRecursive(path, songs);
            }

            return songs;
        }

        private static void LoadSongsFromFolderRecursive(string folderPath, ObservableCollection<Song> songs)
        {
            if (Directory.Exists(folderPath))
            {
                var songFiles = Directory.GetFiles(folderPath, "*.mp3", SearchOption.AllDirectories); // Recursive search for .mp3 files

                foreach (var songFile in songFiles)
                {
                    var file = TagLib.File.Create(songFile);
                    var title = file.Tag.Title;
                    string artist = file.Tag.Performers.FirstOrDefault<string>();
                    var album = file.Tag.Album;
                    var duration = (int)file.Properties.Duration.TotalSeconds;
                    var bitrate = file.Properties.AudioBitrate;
                    var sampleRate = file.Properties.AudioSampleRate;
                    var rating = 0; // Default value for rating
                    var playCount = 0; // Default value for play count
                    string genres = null;

                    // Check if comments contain rating and play count
                    if (!string.IsNullOrEmpty(file.Tag.Comment))
                    {
                        var commentData = JObject.Parse(file.Tag.Comment);
                        if (commentData != null)
                        {
                            if (commentData["Rating"] != null)
                            {
                                int.TryParse(commentData["Rating"].ToString(), out rating);
                            }

                            if (commentData["PlayCount"] != null)
                            {
                                int.TryParse(commentData["PlayCount"].ToString(), out playCount);
                            }
                        }
                    }

                    // Get the relative path of the song file
                    var relativePath = System.IO.Path.GetRelativePath(folderPath, songFile);

                    // Construct the full path to the song file
                    var fullSongPath = System.IO.Path.Combine(folderPath, relativePath);

                    songs.Add(new Song(title, artist, album, genres, duration, rating, playCount, fullSongPath));
                }
            }
        }

        public static void CreateTestData()
        {
            // Create test folder paths
            List<string> folderPaths = new List<string>
        {
            @"C:\Music\Rock",
            @"C:\Music\Jazz"
        };

            // Create test songs
            ObservableCollection<Song> songs = new ObservableCollection<Song>
            {
            new Song ("song1","a","b",  "b" ,100,10,10,"path"),            new Song ("song1","a","b",  "b" ,100,10,10,"path"),            new Song ("song1","a","b",  "b" ,100,10,10,"path"),
        };

            // Save test data to JSON files
            SaveFolderPaths(folderPaths);
            SaveSongs(songs);
        }






        // Define a field to hold the MediaPlayer instance
        public MediaPlayer mediaPlayer = new MediaPlayer();
        public bool is_media_Player_paused = false;
        private void InitializeMediaPlayer()
        {
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (currentlyPlayingSong != null)
            {
                // Calculate the percentage of the song played
                double percentagePlayed = (mediaPlayer.Position.TotalSeconds / currentlyPlayingSong.DurationInSeconds) * 100;

                // Check if the song has been played for over 75% of its full length
                if (percentagePlayed >= 75 && !hasPlayedOver75Percent)
                {
                    // Update the play count
                    currentlyPlayingSong.UpdatePlayCount(currentlyPlayingSong.PlayCount + 1, this);
                    hasPlayedOver75Percent = true;
                }

                // Reset the currently playing song
                currentlyPlayingSong = null;
            }
            // Select a new song at random and play it
            Random rand = new Random();
            int index = rand.Next(0, Songs.Count); // Adjust 'yourSongList' with your actual list of songs
            Song selectedSong = Songs[index];

            // Update the selected item in the ListView
            song_ListView.SelectedItem = selectedSong;

            mediaPlayer.Open(new Uri(selectedSong.FilePath));
            is_media_Player_paused = false;
            mediaPlayer.Play();
        }







        // Define a field to hold the DispatcherTimer instance
        private DispatcherTimer timer;

        // Define a field to indicate whether the ProgressBar is being dragged by the user
        private bool isDragging = false;

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            Song selectedSong = item.Content as Song;

            // Check if the MediaPlayer is currently playing
            if (mediaPlayer.Source != null)
            {
                // If it ssis playing, stop it
                is_media_Player_paused = true;
                mediaPlayer.Stop();
            }

            // Set the media player's media source to the selected song's file path
            mediaPlayer.Open(new Uri(selectedSong.FilePath));

            // Play the media
            is_media_Player_paused = false;
            currentlyPlayingSong = selectedSong;
            hasPlayedOver75Percent = false;
            mediaPlayer.Play();

            // Initialize the timer if it's null
            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(0.1); // Update interval
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Update the ProgressBar value based on the current position of the MediaPlayer
            if (!isDragging && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                progressBar.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // When the ProgressBar value changes (user drags it), update the MediaPlayer's position
            mediaPlayer.Position = TimeSpan.FromSeconds(progressBar.Value);
        }
        private double initialX;


        private void ProgressBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // Calculate the new value based on the mouse position
                double newX = e.GetPosition(progressBar).X;
                double deltaX = newX - initialX;
                double progressBarWidth = progressBar.ActualWidth;
                double percentChange = deltaX / progressBarWidth;
                double newValue = progressBar.Value + (percentChange * progressBar.Maximum);

                // Ensure the new value stays within the ProgressBar bounds
                if (newValue >= 0 && newValue <= progressBar.Maximum)
                {
                    progressBar.Value = newValue;

                    // Update the MediaPlayer's position while dragging
                    mediaPlayer.Position = TimeSpan.FromSeconds(progressBar.Value);
                }

                // Update the initial X position for the next movement
                initialX = newX;
            }
        }
        private void ProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Capture and store the initial mouse position
            isDragging = true;
            initialX = e.GetPosition(progressBar).X;
            progressBar.CaptureMouse();

            // Pause the media player
            is_media_Player_paused = true;
            mediaPlayer.Pause();

            // Update the progress bar and media player position to where the user clicked
            double progressBarWidth = progressBar.ActualWidth;
            double percentClicked = initialX / progressBarWidth;
            progressBar.Value = percentClicked * progressBar.Maximum;
            mediaPlayer.Position = TimeSpan.FromSeconds(progressBar.Value);
        }

        private void ProgressBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Release the mouse capture and reset the dragging flag
            isDragging = false;
            progressBar.ReleaseMouseCapture();

            // Resume playing if the media player was playing before dragging
            if (mediaPlayer.Source != null && mediaPlayer.Position < mediaPlayer.NaturalDuration)
            {
                is_media_Player_paused = false;
                mediaPlayer.Play();
            }
        }
        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                if (is_media_Player_paused)
                {
                    // If it's paused, resume playing
                    is_media_Player_paused = false;
                    mediaPlayer.Play();
                }
                else
                {
                    // If it's playing, pause it
                    is_media_Player_paused = true;
                    mediaPlayer.Pause();
                }
            }
        }







        private void Settings_button_Click(object sender, RoutedEventArgs e)
        {
            // Open the settings dialog
            SettingsDialog settingsDialog = new SettingsDialog();
            settingsDialog.Owner = this; // Set the owner of the dialog to be the main window
            settingsDialog.ShowDialog();
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentlyPlayingSong != null)
            {
                // Calculate the percentage of the song played
                double percentagePlayed = (mediaPlayer.Position.TotalSeconds / currentlyPlayingSong.DurationInSeconds) * 100;

                // Check if the song has been played for over 75% of its full length
                if (percentagePlayed >= 75 && !hasPlayedOver75Percent)
                {
                    // Update the play count
                    currentlyPlayingSong.UpdatePlayCount(currentlyPlayingSong.PlayCount + 1, this);
                    hasPlayedOver75Percent = true;
                }

                // Reset the currently playing song
                currentlyPlayingSong = null;
            }
            // Check if there is a previous song in the playlist
            if (song_ListView.SelectedIndex > 0)
            {
                // Get the index of the previous song
                int previousIndex = song_ListView.SelectedIndex - 1;

                // Select the previous song in the ListView
                song_ListView.SelectedIndex = previousIndex;

                // Get the selected song and play it
                Song selectedSong = song_ListView.SelectedItem as Song;
                mediaPlayer.Open(new Uri(selectedSong.FilePath));
                currentlyPlayingSong = selectedSong;
                hasPlayedOver75Percent = false;
                PlaySong(selectedSong);
            }
        }




        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentlyPlayingSong != null)
            {
                // Calculate the percentage of the song played
                double percentagePlayed = (mediaPlayer.Position.TotalSeconds / currentlyPlayingSong.DurationInSeconds) * 100;

                // Check if the song has been played for over 75% of its full length
                if (percentagePlayed >= 75 && !hasPlayedOver75Percent)
                {
                    // Update the play count
                    currentlyPlayingSong.UpdatePlayCount(currentlyPlayingSong.PlayCount + 1, this);
                    hasPlayedOver75Percent = true;
                }

                // Reset the currently playing song
                currentlyPlayingSong = null;
            }
            // Check if there is a next song in the playlist
            if (song_ListView.SelectedIndex < song_ListView.Items.Count - 1)
            {
                // Get the index of the next song
                int nextIndex = song_ListView.SelectedIndex + 1;

                // Select the next song in the ListView
                song_ListView.SelectedIndex = nextIndex;

                // Get the selected song and play it
                Song selectedSong = song_ListView.SelectedItem as Song;
                mediaPlayer.Open(new Uri(selectedSong.FilePath));
                currentlyPlayingSong = selectedSong;
                hasPlayedOver75Percent = false;
                PlaySong(selectedSong);
            }
        }


        // Shuffle Option 1: Complete Random
        private void shuffleButton1_Click(object sender, RoutedEventArgs e)
        {
            ShuffleSongs();
            ReloadListView(currentlyPlayingSong);
        }

        // Shuffle Option 2: Random with Lower Play Count More Common
        private void shuffleButton2_Click(object sender, RoutedEventArgs e)
        {
            ShuffleSongsWithBias(x => x.PlayCount);
            ReloadListView(currentlyPlayingSong);
        }

        // Shuffle Option 3: Random with Higher Rated Songs More Common
        // Shuffle Option 3: Random with Higher Rated Songs More Common
        private void shuffleButton3_Click(object sender, RoutedEventArgs e)
        {
            ShuffleSongsWithBias(x => -x.Rating); // Negative rating for higher-rated songs more common
            ReloadListView(currentlyPlayingSong);
        }

        // Shuffle Option 4: Random with Unrated Songs More Common
        private void shuffleButton4_Click(object sender, RoutedEventArgs e)
        {
            ShuffleSongsWithBias(x => x.Rating != 0 ? 2 : 1); // Unrated songs are twice more likely to be selected
            ReloadListView(currentlyPlayingSong);
        }

        // Common method for shuffling songs with different biases
        private void ShuffleSongsWithBias(Func<Song, double> biasFunction)
        {
            Random rand = new Random();
            List<Song> shuffledSongs = null;

            if (currentlyPlayingSong != null)
            {
                // Move currentlyPlayingSong to the first position
                shuffledSongs = Songs.OrderBy(x => x == currentlyPlayingSong ? 0 : (biasFunction(x) + rand.NextDouble() * 0.01)).ToList();
            }
            else
            {
                // Select a random song as currentlyPlayingSong and shuffle
                var randomIndex = rand.Next(0, Songs.Count);
                currentlyPlayingSong = Songs[randomIndex];
                shuffledSongs = Songs.OrderBy(x => x == currentlyPlayingSong ? 0 : (biasFunction(x) + rand.NextDouble() * 0.01)).ToList();

                // Start playing the selected song
                PlaySong(currentlyPlayingSong);
            }

            Songs = new ObservableCollection<Song>(shuffledSongs);
        }


        // Common method for shuffling songs randomly
        private void ShuffleSongs()
        {
            Random rand = new Random();
            List<Song> shuffledSongs = null;

            if (currentlyPlayingSong != null)
            {
                // Move currentlyPlayingSong to the first position
                shuffledSongs = Songs.OrderBy(x => x == currentlyPlayingSong ? 0 : rand.Next()).ToList();
            }
            else
            {
                // Select a random song as currentlyPlayingSong and shuffle
                var randomIndex = rand.Next(0, Songs.Count);
                currentlyPlayingSong = Songs[randomIndex];
                shuffledSongs = Songs.OrderBy(x => x == currentlyPlayingSong ? 0 : rand.Next()).ToList();

                // Start playing the selected song
                PlaySong(currentlyPlayingSong);
            }

            Songs = new ObservableCollection<Song>(shuffledSongs);
        }

        // Method to play the selected song
        private void PlaySong(Song song)
        {
            mediaPlayer.Open(new Uri(song.FilePath));
            currentlyPlayingSong = song;
            hasPlayedOver75Percent = false;
            mediaPlayer.Play();
            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(0.1); // Update interval
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }


        private void likeButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the currently selected song
            Song selectedSong = song_ListView.SelectedItem as Song;

            // Check if a song is selected
            if (selectedSong != null)
            {
                // Increase the rating by one
                selectedSong.UpdateRating(selectedSong.Rating + 1,this);

                // Optionally, update any UI elements reflecting the new rating
            }
        }

        private void dislikeButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the currently selected song
            Song selectedSong = song_ListView.SelectedItem as Song;

            // Check if a song is selected
            if (selectedSong != null)
            {
                // Decrease the rating by one
                selectedSong.UpdateRating(selectedSong.Rating - 1,this);

                // Optionally, update any UI elements reflecting the new rating
            }
        }


        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(Songs);
            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
        // Mouse left button down event handler
        private void VolumeSlider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(VolumeSlider);
            double newValue = mousePosition.X / VolumeSlider.ActualWidth * (VolumeSlider.Maximum - VolumeSlider.Minimum) + VolumeSlider.Minimum;
            VolumeSlider.Value = Math.Max(VolumeSlider.Minimum, Math.Min(newValue, VolumeSlider.Maximum));
            mediaPlayer.Volume = (float)VolumeSlider.Value;
            SaveVolumeSetting();
        }

        // Mouse move event handler
        private void VolumeBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateVolume(e.GetPosition(VolumeSlider).X);
            }
            SaveVolumeSetting();
        }

        // Mouse left button up event handler
        private void VolumeBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UpdateVolume(e.GetPosition(VolumeSlider).X);
            SaveVolumeSetting();
        }

        // Method to update volume based on mouse position
        private void UpdateVolume(double mouseX)
        {
            double sliderWidth = VolumeSlider.ActualWidth;
            double newValue = (mouseX / sliderWidth);
            VolumeSlider.Value = Math.Max(0, Math.Min(1, newValue));
            mediaPlayer.Volume = (float)VolumeSlider.Value;
        }

        // Method to save volume setting to settings.json
        private void SaveVolumeSetting()
        {
            var volumeSetting = new { Volume = VolumeSlider.Value };
            string json = JsonConvert.SerializeObject(volumeSetting);
            System.IO.File.WriteAllText("..\\..\\..\\settings.json", json);
        }
        private void SetVolumeFromSettings()
        {
            string settingsFilePath = "..\\..\\..\\settings.json";

            if (System.IO.File.Exists(settingsFilePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<dynamic>(json);
                    double volume = settings.Volume;

                    // Set the volume slider and media player volume
                    VolumeSlider.Value = Math.Max(0, Math.Min(1, volume));
                    mediaPlayer.Volume = (float)VolumeSlider.Value;
                }
                catch (Exception ex)
                {
                    // Handle exception, e.g., log or display an error message
                    Console.WriteLine("Error reading settings file: " + ex.Message);
                }
            }
            else
            {
                // Settings file doesn't exist, use default volume or let the user adjust it
            }
        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

    }
}
