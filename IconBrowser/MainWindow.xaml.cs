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
            var root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\source\repos\PathMasterX\";
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

            IconList.ItemsSource = string.IsNullOrEmpty(filter) ? _allIcons : [.. _allIcons.Where(i => i.Key.Contains(filter, StringComparison.InvariantCultureIgnoreCase))]; 
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

    public class IconItem
    {
        public string Key { get; set; } = "";
        public string PathData { get; set; } = "";
    }
}
