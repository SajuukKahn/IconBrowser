namespace IconBrowser
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using System.Xml.Linq;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<IconItem> _allIcons = [];

        public MainWindow()
        {
            InitializeComponent();
            LoadIcons();
            IconList.ItemsSource = _allIcons;
        }

        private void LoadIcons()
        {
#if DEBUG
            var root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\source\repos\precisionValve\PathMasterX\";
#else
            var root = Directory.GetCurrentDirectory();
#endif

            var iconsFile = Directory.GetFiles(root, "?cons.xaml", SearchOption.AllDirectories)
                                     .FirstOrDefault(f => f.EndsWith("icons.xaml", StringComparison.OrdinalIgnoreCase));

            if (iconsFile == null)
            {
                MessageBox.Show("icons.xaml not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            var doc = XDocument.Load(iconsFile);
            XNamespace xns = "http://schemas.microsoft.com/winfx/2006/xaml"; // for x:Key

            _allIcons = [.. doc.Descendants()
                           .Where(e => e.Attribute(xns + "Key") != null && !string.IsNullOrWhiteSpace(e.Value))
                           .Select(e => new IconItem
                           {
                               Key = e.Attribute(xns + "Key")?.Value ?? "",
                               PathData = e.Value.Trim()
                           })];
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string filter = SearchBox.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                var filteredList = filter.FindMatchesLimitDistance(_allIcons.Select(i => i.Key), 6);
                var matchedIcons = _allIcons.Where(i => i.Key.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
                var closeMatchIcons = _allIcons.Where(i => filteredList.Contains(i.Key));
                IconList.ItemsSource = matchedIcons.Union(closeMatchIcons).Distinct();
            }
            else
            {
                IconList.ItemsSource = _allIcons;
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PlaceHolderText.Visibility = Visibility.Hidden;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string filter = SearchBox.Text.Trim().ToLower();
            PlaceHolderText.Visibility = filter.Length == 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = new Uri(@"https://yqnn.github.io/svg-path-editor/").AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void Copy_Icon_Name(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is IconItem item)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Copy PathData if Ctrl is held
                    Clipboard.SetText(item.PathData);
                }
                else
                {
                    // Default: copy the Key
                    Clipboard.SetText(item.Key);
                }
            }
        }

        private void Open_Material_Icon_Website(object sender, RoutedEventArgs e)
        {
            Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = new Uri(@"https://pictogrammers.com/library/mdi/").AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }

    /// <summary>
    /// Extension methods for <see langword="string"/> types.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Uses the Levenshtein Distance Algorithm to fuzzy match a source string against a collection of potential matches.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="options">The list of potential matches.</param>
        /// <returns>The single closest match.</returns>
        public static string FindClosestMatch(this string source, IEnumerable<string> options)
        {
            if (string.IsNullOrEmpty(source) || options == null || !options.Any())
            {
                return string.Empty;
            }

            string closestMatch = string.Empty;
            int smallestDistance = int.MaxValue;
            foreach (string option in options)
            {
                int distance = ComputeLevenshteinDistance(source, option);
                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    closestMatch = option;

                    if (smallestDistance == 0)
                    {
                        return closestMatch;
                    }
                }
            }

            Debug.WriteLine($"{source} : Closest Match: {closestMatch} at distance: {smallestDistance}");
            return closestMatch;
        }

        /// <summary>
        /// Finds multiple closest matches.
        /// </summary>
        /// <param name="source">The source string to match.</param>
        /// <param name="options">The list of strings to match against.</param>
        /// <param name="maximumNumberOfMatches">The maximum number of close matches to find.</param>
        /// <returns>A list of the closest n# of matches.</returns>
        public static List<string> FindMatches(this string source, IEnumerable<string> options, int maximumNumberOfMatches)
        {
            var dictionary = new Dictionary<string, int>();
            var list = new List<string>();
            if (string.IsNullOrEmpty(source) || options == null || !options.Any() || maximumNumberOfMatches == 0)
            {
                return list;
            }

            foreach (string option in options)
            {
                dictionary.Add(option, ComputeLevenshteinDistance(source, option));
            }

            list.AddRange(from KeyValuePair<string, int> item in dictionary.OrderBy(x => x.Value).Take(maximumNumberOfMatches)
                          select item.Key);

            return list;
        }

        /// <summary>
        /// Finds multiple closest matches.
        /// </summary>
        /// <param name="source">The source string to match.</param>
        /// <param name="options">The list of strings to match against.</param>
        /// <param name="maxDistance">The maximum distance from the input string to allow.</param>
        /// <returns>A list of the closest n# of matches.</returns>
        public static List<string> FindMatchesLimitDistance(this string source, IEnumerable<string> options, int maxDistance)
        {
            var dictionary = new Dictionary<string, int>();
            var list = new List<string>();
            if (string.IsNullOrEmpty(source) || options == null || !options.Any())
            {
                return list;
            }

            bool query = options.Any(x => !x.EndsWith("Icon"));

            foreach (string option in options)
            {
                string iconTrimmed = option[.. (option.Length - 4)];
                string materialTrimmed = iconTrimmed.EndsWith("ial", StringComparison.OrdinalIgnoreCase) ? iconTrimmed[.. (iconTrimmed.Length - 8)] : iconTrimmed;
                string allTrimmed = materialTrimmed.EndsWith("Custom", StringComparison.OrdinalIgnoreCase) ? materialTrimmed[..(materialTrimmed.Length - 6)] : materialTrimmed;
                if (ComputeLevenshteinDistance(source, allTrimmed) is int distance && distance <= maxDistance)
                {
                    dictionary.Add(option, distance); 
                }
            }

            list.AddRange(from KeyValuePair<string, int> item in dictionary
                          select item.Key);

            return list;
        }

        // taken from https://github.com/DanHarltey/Fastenshtein/blob/master/src/Fastenshtein/StaticLevenshtein.cs MIT license
        private static int ComputeLevenshteinDistance(string source, string option)
        {
            if (option.Length == 0)
            {
                return source.Length;
            }

            int[] costs = new int[option.Length];

            for (int i = 0; i < costs.Length;)
            {
                costs[i] = ++i;
            }

            for (int i = 0; i < source.Length; i++)
            {
                int cost = i;
                int previousCost = i;

                char value1Char = source[i];

                for (int j = 0; j < option.Length; j++)
                {
                    int currentCost = cost;

                    cost = costs[j];

                    if (value1Char != option[j])
                    {
                        if (previousCost < currentCost)
                        {
                            currentCost = previousCost;
                        }

                        if (cost < currentCost)
                        {
                            currentCost = cost;
                        }

                        ++currentCost;
                    }

                    costs[j] = currentCost;
                    previousCost = currentCost;
                }
            }

            return costs[costs.Length - 1];
        }
    }

    public class IconItem
    {
        public string Key { get; set; } = "";
        public string PathData { get; set; } = "";
    }
}
