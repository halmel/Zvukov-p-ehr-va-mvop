using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Zvukoví_přehrávač_mvop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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


        public class Song
        {
            private string _title;
            private string _author;
            private string _album;
            private string _genre;
            private int _durationInSeconds;
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
            public Song(string title, string author, string album, string genre, int duration, int rating, int playCount, string filePath)
            {
                Title = title;
                Author = author;
                Album = album;
                Genre = genre;
                DurationInSeconds = duration;
                Rating = rating;
                PlayCount = playCount;
                FilePath = filePath;
            }
        }

        public MainWindow()
        {
            DataContext = this;
            Songs = new ObservableCollection<Song>
        {
            new Song
            (
                "Bohemian Rhapsody",
                "Queen",
                "A Night at the Opera",
                "Rock",
                355,
                  5,
                100,
                "path/to/bohemian_rhapsody.mp3"
            ),
            new Song
            (
                "Shape of You",
                "Ed Sheeran",
                "÷",
                "Pop",
                233,
                4,
                200,
               "path/to/shape_of_you.mp3"
            )
        };


            InitializeComponent();
        }
    }
}
