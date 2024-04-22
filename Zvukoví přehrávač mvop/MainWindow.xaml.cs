using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Zvukoví_přehrávač_mvop
{
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

            public string Title
            {
                get { return _title; }
                set { _title = value; }
            }

            public string Author
            {
                get { return _author; }
                set { _author = value; }
            }

            public string Album
            {
                get { return _album; }
                set { _album = value; }
            }

            public string Genre
            {
                get { return _genre; }
                set { _genre = value; }
            }

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

            public int Rating
            {
                get { return _rating; }
                set { _rating = value; }
            }

            public int PlayCount
            {
                get { return _playCount; }
                set { _playCount = value; }
            }

            public string FilePath
            {
                get { return _filePath; }
                set { _filePath = value; }
            }

            public Song(string title, string author, string album, string genre, int duration, int rating, int playCount, string filePath)
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
                ratedThisPlay = false; mainWindow.UpdateMetadata(this);
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

            var file = TagLib.File.Create(x.FilePath);

            if (x.Title != null)
                file.Tag.Title = x.Title;

            if (x.Author != null)
                file.Tag.Performers = new string[] { x.Author };

            if (x.Album != null)
                file.Tag.Album = x.Album;

            if (x.Genre != null)
                file.Tag.Genres = new string[] { x.Genre };

            var commentObject = new JObject();
            commentObject["Rating"] = x.Rating;
            commentObject["PlayCount"] = x.PlayCount;
            file.Tag.Comment = commentObject.ToString();


            var e = mediaPlayer.Position;
            mediaPlayer.Stop();
            mediaPlayer.Close(); Thread.Sleep(10);
            file.Save();
            InitializeMediaPlayer();
            mediaPlayer.Open(new Uri(x.FilePath)); mediaPlayer.Position = e;
            mediaPlayer.Play();
            ReloadListView(x);
        }


        public void ReloadListView(Song x)
        {
            if (song_ListView != null)
            {
                song_ListView.ItemsSource = null;
                song_ListView.ItemsSource = Songs;

                song_ListView.SelectedItem = x;
                song_ListView.ScrollIntoView(x);
            }
        }


        public MainWindow()
        {
            KeyDown += MainWindow_KeyDown;
            DataContext = this;
            InitializeMediaPlayer();
            SaveFolderPaths(LoadFolderPaths());
            SaveSongs(LoadSongsFromFolders());
            Songs = LoadSongs();
            InitializeComponent();
            SetVolumeFromSettings();
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.MediaPreviousTrack:
                    previousButton_Click(null, null);
                    break;
                case Key.MediaNextTrack:
                    nextButton_Click(null, null);
                    break;
                case Key.MediaStop:
                    stopButton_Click(null, null);
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

        private const string FilePath = "folderPaths.json";

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
                if (folderPaths != null)
                {
                    return folderPaths.Paths;

                }
                else
                {
                    string musicFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                    return new List<string> { musicFolderPath };
                }
            }
            return new List<string>();
        }

        private static void SaveSongs(ObservableCollection<Song> songs)
        {
            string json = JsonConvert.SerializeObject(songs);
            System.IO.File.WriteAllText("songs.json", json);
        }
        private static ObservableCollection<Song> LoadSongs()
        {
            string filePath = "songs.json";
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
                var songFiles = Directory.GetFiles(folderPath, "*.mp3", SearchOption.AllDirectories);
                foreach (var songFile in songFiles)
                {
                    var file = TagLib.File.Create(songFile);
                    var title = file.Tag.Title;
                    string artist = file.Tag.Performers.FirstOrDefault<string>();
                    var album = file.Tag.Album;
                    var duration = (int)file.Properties.Duration.TotalSeconds;
                    var bitrate = file.Properties.AudioBitrate;
                    var sampleRate = file.Properties.AudioSampleRate;
                    var rating = 0; var playCount = 0; string genres = null;

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

                    var relativePath = System.IO.Path.GetRelativePath(folderPath, songFile);

                    var fullSongPath = System.IO.Path.Combine(folderPath, relativePath);

                    songs.Add(new Song(title, artist, album, genres, duration, rating, playCount, fullSongPath));
                }
            }
        }

        public static void CreateTestData()
        {
            List<string> folderPaths = new List<string>
        {
            @"C:\Music\Rock",
            @"C:\Music\Jazz"
        };

            ObservableCollection<Song> songs = new ObservableCollection<Song>
            {
            new Song ("song1","a","b",  "b" ,100,10,10,"path"),            new Song ("song1","a","b",  "b" ,100,10,10,"path"),            new Song ("song1","a","b",  "b" ,100,10,10,"path"),
        };

            SaveFolderPaths(folderPaths);
            SaveSongs(songs);
        }






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
                double percentagePlayed = (mediaPlayer.Position.TotalSeconds / currentlyPlayingSong.DurationInSeconds) * 100;

                if (percentagePlayed >= 75 && !hasPlayedOver75Percent)
                {
                    currentlyPlayingSong.UpdatePlayCount(currentlyPlayingSong.PlayCount + 1, this);
                    hasPlayedOver75Percent = true;
                }

                currentlyPlayingSong = null;
            }
            Random rand = new Random();
            int index = rand.Next(0, Songs.Count); Song selectedSong = Songs[index];

            song_ListView.SelectedItem = selectedSong;

            mediaPlayer.Open(new Uri(selectedSong.FilePath));
            is_media_Player_paused = false;
            mediaPlayer.Play();
        }







        private DispatcherTimer timer;

        private bool isDragging = false;

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            Song selectedSong = item.Content as Song;

            if (mediaPlayer.Source != null)
            {
                is_media_Player_paused = true;
                mediaPlayer.Stop();
            }

            mediaPlayer.Open(new Uri(selectedSong.FilePath));

            is_media_Player_paused = false;
            currentlyPlayingSong = selectedSong;
            hasPlayedOver75Percent = false;
            mediaPlayer.Play();

            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(0.1); timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                progressBar.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Position = TimeSpan.FromSeconds(progressBar.Value);
        }
        private double initialX;


        private void ProgressBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                double newX = e.GetPosition(progressBar).X;
                double deltaX = newX - initialX;
                double progressBarWidth = progressBar.ActualWidth;
                double percentChange = deltaX / progressBarWidth;
                double newValue = progressBar.Value + (percentChange * progressBar.Maximum);

                if (newValue >= 0 && newValue <= progressBar.Maximum)
                {
                    progressBar.Value = newValue;

                    mediaPlayer.Position = TimeSpan.FromSeconds(progressBar.Value);
                }

                initialX = newX;
            }
        }
        private void ProgressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            initialX = e.GetPosition(progressBar).X;
            progressBar.CaptureMouse();

            is_media_Player_paused = true;
            mediaPlayer.Pause();

            double progressBarWidth = progressBar.ActualWidth;
            double percentClicked = initialX / progressBarWidth;
            progressBar.Value = percentClicked * progressBar.Maximum;
            mediaPlayer.Position = TimeSpan.FromSeconds(progressBar.Value);
        }
        private void ProgressBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            progressBar.ReleaseMouseCapture();

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
                    is_media_Player_paused = false;
                    mediaPlayer.Play();
                }
                else
                {
                    is_media_Player_paused = true;
                    mediaPlayer.Pause();
                }
            }
        }
        private void Settings_button_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog settingsDialog = new SettingsDialog();
            settingsDialog.Owner = this;
            bool? dialogResult = settingsDialog.ShowDialog();

            if (dialogResult == true)
            {
                string newPath = settingsDialog.PathToSongFolder;
                UpdateSongFolder(newPath);
                ReloadMainWindow();
            }
        }
        public void ReloadMainWindow()
        {
            InitializeMediaPlayer();
            SaveFolderPaths(LoadFolderPaths());
            SaveSongs(LoadSongsFromFolders());
            Songs = LoadSongs();
            SetVolumeFromSettings();
            song_ListView.ItemsSource = null;
            song_ListView.ItemsSource = Songs;

        }
        private void UpdateSongFolder(string newPath)
        {
            SaveFolderPaths(new List<string> { newPath });

            Songs = LoadSongsFromFolders();
        }
        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentlyPlayingSong != null)
            {
                double percentagePlayed = (mediaPlayer.Position.TotalSeconds / currentlyPlayingSong.DurationInSeconds) * 100;

                if (percentagePlayed >= 75 && !hasPlayedOver75Percent)
                {
                    currentlyPlayingSong.UpdatePlayCount(currentlyPlayingSong.PlayCount + 1, this);
                    hasPlayedOver75Percent = true;
                }

                currentlyPlayingSong = null;
            }
            if (song_ListView.SelectedIndex > 0)
            {
                int previousIndex = song_ListView.SelectedIndex - 1;

                song_ListView.SelectedIndex = previousIndex;

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
                double percentagePlayed = (mediaPlayer.Position.TotalSeconds / currentlyPlayingSong.DurationInSeconds) * 100;

                if (percentagePlayed >= 75 && !hasPlayedOver75Percent)
                {
                    currentlyPlayingSong.UpdatePlayCount(currentlyPlayingSong.PlayCount + 1, this);
                    hasPlayedOver75Percent = true;
                }

                currentlyPlayingSong = null;
            }
            if (song_ListView.SelectedIndex < song_ListView.Items.Count - 1)
            {
                int nextIndex = song_ListView.SelectedIndex + 1;

                song_ListView.SelectedIndex = nextIndex;

                Song selectedSong = song_ListView.SelectedItem as Song;
                mediaPlayer.Open(new Uri(selectedSong.FilePath));
                currentlyPlayingSong = selectedSong;
                hasPlayedOver75Percent = false;
                PlaySong(selectedSong);
            }
        }


        private void shuffleButton1_Click(object sender, RoutedEventArgs e)
        {
            ShuffleSongs();
            ReloadListView(currentlyPlayingSong);
        }

        private void shuffleButton2_Click(object sender, RoutedEventArgs e)
        {
            ShuffleSongsWithBias(x => x.PlayCount);
            ReloadListView(currentlyPlayingSong);
        }

        private void shuffleButton3_Click(object sender, RoutedEventArgs e)
        {
            ShuffleSongsWithBias(x => -x.Rating); ReloadListView(currentlyPlayingSong);
        }

        private void shuffleButton4_Click(object sender, RoutedEventArgs e)
        {
            ShuffleSongsWithBias(x => x.Rating != 0 ? 2 : 1); ReloadListView(currentlyPlayingSong);
        }

        private void ShuffleSongsWithBias(Func<Song, double> biasFunction)
        {
            Random rand = new Random();
            List<Song> shuffledSongs = null;

            if (currentlyPlayingSong != null)
            {
                shuffledSongs = Songs.OrderBy(x => x == currentlyPlayingSong ? 0 : (biasFunction(x) + rand.NextDouble() * 0.01)).ToList();
            }
            else if (Songs.Count > 0)
            {
                var randomIndex = rand.Next(0, Songs.Count);
                currentlyPlayingSong = Songs[randomIndex];
                shuffledSongs = Songs.OrderBy(x => x == currentlyPlayingSong ? 0 : (biasFunction(x) + rand.NextDouble() * 0.01)).ToList();

                PlaySong(currentlyPlayingSong);
            }
            else
            {
                return;
            }

            Songs = new ObservableCollection<Song>(shuffledSongs);
        }


        private void ShuffleSongs()
        {
            Random rand = new Random();
            List<Song> shuffledSongs = null;

            if (currentlyPlayingSong != null)
            {
                shuffledSongs = Songs.OrderBy(x => x == currentlyPlayingSong ? 0 : rand.Next()).ToList();
            }
            else if (Songs.Count > 0)
            {
                var randomIndex = rand.Next(0, Songs.Count);
                currentlyPlayingSong = Songs[randomIndex];
                shuffledSongs = Songs.OrderBy(x => x == currentlyPlayingSong ? 0 : rand.Next()).ToList();

                PlaySong(currentlyPlayingSong);
            }
            else
            {
                return;
            }

            Songs = new ObservableCollection<Song>(shuffledSongs);
        }

        private void PlaySong(Song song)
        {
            mediaPlayer.Open(new Uri(song.FilePath));
            currentlyPlayingSong = song;
            hasPlayedOver75Percent = false;
            mediaPlayer.Play();
            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(0.1); timer.Tick += Timer_Tick;
                timer.Start();
            }
        }


        private void likeButton_Click(object sender, RoutedEventArgs e)
        {
            Song selectedSong = song_ListView.SelectedItem as Song;

            if (selectedSong != null)
            {
                selectedSong.UpdateRating(selectedSong.Rating + 1, this);

            }
        }

        private void dislikeButton_Click(object sender, RoutedEventArgs e)
        {
            Song selectedSong = song_ListView.SelectedItem as Song;

            if (selectedSong != null)
            {
                selectedSong.UpdateRating(selectedSong.Rating - 1, this);

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
        private void VolumeSlider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(VolumeSlider);
            double newValue = mousePosition.X / VolumeSlider.ActualWidth * (VolumeSlider.Maximum - VolumeSlider.Minimum) + VolumeSlider.Minimum;
            VolumeSlider.Value = Math.Max(VolumeSlider.Minimum, Math.Min(newValue, VolumeSlider.Maximum));
            mediaPlayer.Volume = (float)VolumeSlider.Value;
            SaveVolumeSetting();
        }

        private void VolumeBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateVolume(e.GetPosition(VolumeSlider).X);
            }
            SaveVolumeSetting();
        }


        private void VolumeBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UpdateVolume(e.GetPosition(VolumeSlider).X);
            SaveVolumeSetting();
        }

        private void UpdateVolume(double mouseX)
        {
            double sliderWidth = VolumeSlider.ActualWidth;
            double newValue = (mouseX / sliderWidth);
            VolumeSlider.Value = Math.Max(0, Math.Min(1, newValue));
            mediaPlayer.Volume = (float)VolumeSlider.Value;
        }

        private void SaveVolumeSetting()
        {
            var volumeSetting = new { Volume = VolumeSlider.Value };
            string json = JsonConvert.SerializeObject(volumeSetting);
            System.IO.File.WriteAllText("settings.json", json);
        }
        private void SetVolumeFromSettings()
        {
            string settingsFilePath = "settings.json";

            if (System.IO.File.Exists(settingsFilePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<dynamic>(json);
                    double volume = settings.Volume;
                    VolumeSlider.Value = Math.Max(0, Math.Min(1, volume));
                    mediaPlayer.Volume = (float)VolumeSlider.Value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading settings file: " + ex.Message);
                }
            }
            else
            {
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
