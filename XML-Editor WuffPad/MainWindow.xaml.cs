using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using XML_Editor_WuffPad.Commands;
using XML_Editor_WuffPad.XMLClasses;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading.Tasks;
using System.Diagnostics;
using XML_Editor_WuffPad.Properties;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables and constants
        #region Constants
        private const long UploadChatId = -1001074012132;
        internal static string RootDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        private static readonly string serverBaseUrl = "http://www.meyer-alpers.de/florian/WuffPad/Resources/";
        private static readonly string fileScratchPath = Path.Combine(RootDirectory, "Resources\\language.xml");
        private static readonly string fileScratchPathOnline = serverBaseUrl + "language.xml";
        private static readonly string dictFilePath = Path.Combine(RootDirectory, "Resources\\descriptions.dict");
        private static readonly string dictFilePathOnline = serverBaseUrl + "descriptions.dict";
        private static readonly string defaultKeysFilePath = Path.Combine(RootDirectory, "Resources\\standardKeys.db");
        private static readonly string defaultKeysFilePathOnline = serverBaseUrl + "standardKeys.db";
        private static readonly string versionFilePath = Path.Combine(RootDirectory, "Resources\\versions.txt");
        private static readonly string versionFilePathOnline = serverBaseUrl + "versions.txt";
        private static readonly string tokenFilePath = Path.Combine(RootDirectory, "Resources\\token.cod");
        private static readonly string tokenFilePathOnline = serverBaseUrl + "token.cod";
        private static readonly string emojisFilePath = Path.Combine(RootDirectory, "Resources\\emojis.txt");
        private static readonly string emojisFilePathOnline = serverBaseUrl + "emojis.txt";
        private static readonly string[] filesNames = { "language.xml", "descriptions.dict", "standardKeys.db", "token.cod", "emojis.txt" };
        private static readonly Dictionary<string, string[]> namesPathsDict = new Dictionary<string, string[]>()
        {
            {"language.xml", new string[] { fileScratchPathOnline, fileScratchPath } },
            {"descriptions.dict", new string[] { dictFilePathOnline, dictFilePath } },
            {"standardKeys.db", new string[] { defaultKeysFilePathOnline, defaultKeysFilePath } },
            {"token.cod", new string[] { tokenFilePathOnline, tokenFilePath } },
            {"emojis.txt", new string[] { emojisFilePathOnline, emojisFilePath } }
        };
        private const string closedlistPath = "http://88.198.66.60/getClosedlist.php";
        private const string underdevPath = "http://88.198.66.60/getUnderdev.php";
        private const string wikiPageUrl = "https://github.com/Olfi01/WuffPad/wiki";
        #endregion
        #region Variables
        private bool fileIsOpen = false;
        private bool textHasChanged = false;
        private bool itemIsOpen = false;
        private bool valueIsOpen = false;
        private bool valueHasChanged = false;
        private string saveDirectory = Environment.CurrentDirectory + "language.xml";
        private string loadDirectory = Environment.CurrentDirectory + "language.xml";
        private XmlStrings loadedFile = new XmlStrings();
        public ObservableCollection<XmlString> currentStringsList = new ObservableCollection<XmlString>();
        public ObservableCollection<string> currentValuesList = new ObservableCollection<string>();
        private XmlString currentString = new XmlString();
        private int currentStringIndex;
        private string currentValue;
        private int currentValueIndex;
        private bool directoryChosen = false;
        private Dictionary<string, string> descriptionDic = new Dictionary<string, string>();
        private List<string> defaultKeysList = new List<string>();
        private int lastClicked = -1;
        private const int clickedItems = 0;
        private const int clickedValues = 1;
        private List<string> commentLines = new List<string>();
        private Dictionary<string, List<string>> commentDic = new Dictionary<string, List<string>>();
        private bool fromTextBox = false;
        private string token = "";
        private List<Button> emojiButtonsList = new List<Button>();
        private List<string> emojisList = new List<string>();
        #endregion
        #endregion

        #region Constructor
        #region Normal startup
        public MainWindow()
        {
            InitializeComponent();
            try { fetchNewestFiles(); }
            catch /*(Exception e)*/ { /*MessageBox.Show(e.ToString() +e.Message + e.StackTrace);*/ }
            getDictAndDefaultKeys();
            listItemsView.ItemsSource = currentStringsList;
            listValuesView.ItemsSource = currentValuesList;
            initializeEmojiKeyboard();
            updateStatus();
        }
        #endregion
        #region Opening a file
        public MainWindow(string path)
        {
            InitializeComponent();
            try { fetchNewestFiles(); }
            catch /*(Exception e)*/ { /*MessageBox.Show(e.ToString() +e.Message + e.StackTrace);*/ }
            getDictAndDefaultKeys();
            listItemsView.ItemsSource = currentStringsList;
            listValuesView.ItemsSource = currentValuesList;
            initializeEmojiKeyboard();
            updateStatus();
            loadFileFromOutside(path);
        }
        #endregion
        #endregion

        #region Functionable Methods
        #region Checking for errors
        private bool checkValuesCorrect()
        {
            bool doSave = true;
            List<string> hasKeys = new List<string>();
            #region {0} and so on + gif string length check + collecting keys
            foreach (XmlString s in loadedFile.Strings)
            {
                hasKeys.Add(s.Key);
                #region Gif string length check
                if (s.Isgif)
                {
                    foreach(string v in s.Values)
                    {
                        if (v.Length > 200)
                        {
                            MessageBoxResult result = MessageBox.Show("A value of " + s.Key +
                                    " exceeds the 200 character limit for gifs.\n" +
                                    "Save anyway? Press cancel to jump there.",
                                    "Warning", MessageBoxButton.YesNoCancel);
                            if (result == MessageBoxResult.No)
                            {
                                doSave = false;
                            }
                            else if (result == MessageBoxResult.Cancel)
                            {
                                listItemsView.ScrollIntoView(s);
                                listItemsView.SelectedItem = s;
                                return false;
                            }
                        }
                    }
                }
                if (!doSave) break;
                #endregion
                #region parentheses check
                bool hadIt = true;
                int parenthCount = 0;
                do
                {
                    if (s.Description.Contains("{" + parenthCount.ToString() + "}"))
                    {
                        parenthCount++;
                    }
                    else
                    {
                        hadIt = false;
                    }
                } while (hadIt);
                if (parenthCount > 0)
                {
                    foreach (string str in s.Values)
                    {
                        for (int i = 0; i < parenthCount; i++)
                        {
                            if (!str.Contains("{" + i + "}"))
                            {
                                MessageBoxResult result = MessageBox.Show("A value of " + s.Key +
                                    " does not contain a {" + i + "}.\n" + 
                                    "Save anyway? Press cancel to jump there.",
                                    "Warning", MessageBoxButton.YesNoCancel);
                                if (result == MessageBoxResult.No)
                                {
                                    doSave = false;
                                    break;
                                }
                                else if (result == MessageBoxResult.Cancel)
                                {
                                    listItemsView.ScrollIntoView(s);
                                    listItemsView.SelectedItem = s;
                                    return false;
                                }
                            }
                        }
                    }
                }
                if (!doSave) break;
                #endregion
            }
            #endregion
            #region checking for missing strings
            foreach (string s in defaultKeysList)
            {
                if (!hasKeys.Contains(s) && s.Trim() != "")
                {
                    MessageBoxResult result = MessageBox.Show("A value for " + s +
                                    " is still missing.\nSave anyway? " +
                                    "Press cancel to create it and jump there.",
                                    "Warning", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.No)
                    {
                        doSave = false;
                        break;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        XmlString temp = new XmlString();
                        temp.Key = s;
                        temp.Description = getDescription(s);
                        loadedFile.Strings.Add(temp);
                        currentStringsList = loadedFile.Strings;
                        listItemsView.ScrollIntoView(temp);
                        listItemsView.SelectedItem = temp;
                        return false;
                    }
                }
            }
            #endregion
            return doSave;
        }
        #endregion
        #region Download file by name
        private void downloadFileByName(string name)
        {
            downloadFile(namesPathsDict[name][0], namesPathsDict[name][1]);
        }
        #endregion
        #region Fetch Newest Files
        private void fetchNewestFiles()
        {
            bool versionFileExists = System.IO.File.Exists(versionFilePath);
            List<string[]> version_old = new List<string[]>();
            Dictionary<string, int> version_oldDict = new Dictionary<string, int>();
            if (versionFileExists)
            {
                foreach (string s in System.IO.File.ReadAllLines(versionFilePath))
                {
                    string[] strs = s.Split(':');
                    version_old.Add(strs);
                }
                foreach (string[] s in version_old)
                {
                    version_oldDict.Add(s[0], Convert.ToInt16(s[1]));
                }
            }
            WebClient wc = new WebClient();
            wc.DownloadFile(versionFilePathOnline, "version.txt");
            string versionFilePathRaw = versionFilePath.Remove(versionFilePath.LastIndexOf('\\') + 1);
            if (!Directory.Exists(versionFilePathRaw))
            {
                Directory.CreateDirectory(versionFilePathRaw);
            }
            if (System.IO.File.Exists(versionFilePath)) System.IO.File.Delete(versionFilePath);
            System.IO.File.Move("version.txt", versionFilePath);
            List<string[]> version = new List<string[]>();
            foreach (string s in System.IO.File.ReadAllLines(versionFilePath))
            {
                string[] strs = s.Split(':');
                version.Add(strs);
            }
            Dictionary<string, int> versionDict = new Dictionary<string, int>();
            foreach (string[] s in version)
            {
                versionDict.Add(s[0], Convert.ToInt16(s[1]));
            }
            foreach (string s in filesNames)
            {
                if (version_oldDict.ContainsKey(s))
                {
                    if (version_oldDict[s] < versionDict[s])
                    {
                        downloadFileByName(s);
                    }
                }
                else
                {
                    downloadFileByName(s);
                }
            }
        }
        #endregion
        #region Download a file
        private void downloadFile(string url, string pathTo)
        {
            WebClient wc = new WebClient();
            wc.DownloadFile(url, "temp");
            string pathToRaw = pathTo.Remove(pathTo.LastIndexOf('\\') + 1);
            if (!Directory.Exists(pathToRaw))
            {
                Directory.CreateDirectory(pathToRaw);
            }
            if (System.IO.File.Exists(pathTo)) System.IO.File.Delete(pathTo);
            System.IO.File.Move("temp", pathTo);
        }
        #endregion
        #region Set setting
        private void setSetting(string key, bool value)
        {
            switch (key)
            {
                case "commentWarningDisable":
                    Settings.Default.doNotShowWarningAgain = value;
                    break;
            }
        }
        #endregion
        #region Check Setting
        private bool checkSetting(string key)
        {
            switch (key)
            {
                case "commentWarningDisable":
                    return Settings.Default.doNotShowWarningAgain;
            }
            throw new Exception("Setting not found");
        }
        #endregion
        #region Get a missing default key
        private string getDefaultMissingKey()
        {
            foreach (string s in defaultKeysList)
            {
                bool isPresent = false;
                foreach (XmlString xs in loadedFile.Strings)
                {
                    if (xs.Key == s) isPresent = true;
                }
                if (!isPresent)
                {
                    return s;
                }
            }
            return "";
        }
        #endregion
        #region Extract Dictionary, default keys, emojis and token
        private void getDictAndDefaultKeys()
        {
            if (System.IO.File.Exists(dictFilePath))
            {
                string input = System.IO.File.ReadAllText(dictFilePath);
                descriptionDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(input);
            }
            if (System.IO.File.Exists(defaultKeysFilePath))
            {
                string input = System.IO.File.ReadAllText(defaultKeysFilePath);
                string[] inputs = input.Split('\n');
                foreach (string s in inputs)
                {
                    defaultKeysList.Add(s);
                }
            }
            if (System.IO.File.Exists(tokenFilePath))
            {
                token = System.IO.File.ReadAllText(tokenFilePath);
                System.IO.File.Delete(tokenFilePath);
            }
            if (System.IO.File.Exists(emojisFilePath))
            {
                foreach (string s in System.IO.File.ReadAllLines(emojisFilePath, Encoding.UTF8))
                {
                    emojisList.Add(s);
                }
            }
        }
        #endregion
        #region New file
        private void newFile()
        {
            MessageBoxResult result = MessageBox.Show(
                    "Use the english file as a blueprint?", "New File", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                loadedFile = new XmlStrings();
                try
                {
                    getFileFromScratch();
                }
                catch
                {
                    MessageBox.Show("Failed to load blueprint, connect to the internet and restart.");
                }
                currentStringsList.Clear();
                foreach (XmlString s in loadedFile.Strings)
                {
                    s.Description = getDescription(s.Key);
                }
                currentStringsList = loadedFile.Strings;
            }
            fileIsOpen = true;
            textHasChanged = true;
            LanguagePropertyDialog lpd = new LanguagePropertyDialog(firstTime: true);
            if (lpd.ShowDialog() == true)
            {
                loadedFile.Language.Base = lpd.LanguageBase;
                loadedFile.Language.Name = lpd.LanguageName;
                loadedFile.Language.Owner = lpd.LanguageOwner;
                loadedFile.Language.Variant = lpd.LanguageVariant;
            }
            listItemsView.ItemsSource = currentStringsList;
            updateStatus();
        }
        #endregion
        #region Open file
        private void openFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = loadDirectory;
            ofd.DefaultExt = ".xml";
            ofd.Filter = "XML Documents (.xml)|*.xml";
            bool? result = ofd.ShowDialog();
            if (result == true)
            {
                loadDirectory = ofd.FileName;
                saveDirectory = ofd.FileName;
                directoryChosen = true;
                /*try
                {*/
                    loadFile();
                /*}
                catch
                {
                    //Message should've already popped up
                }*/
            }
            updateStatus();
        }
        #endregion
        #region Load file
        private void loadFile()
        {
            loadedFile = readXmlString(System.IO.File.ReadAllText(loadDirectory));
            fileIsOpen = true;
            currentStringsList.Clear();
            foreach (XmlString s in loadedFile.Strings)
            {
                s.Description = getDescription(s.Key);
                //currentStringsList.Add(s);
            }
            currentStringsList = loadedFile.Strings;
            listItemsView.ItemsSource = currentStringsList;
            updateStatus();
        }
        #endregion
        #region Language Dialog
        private void openLanguageDialog()
        {
            MessageBoxResult res = MessageBox.Show(
                "Only change the language properties if you know what you are doing" +
                " or the language you are translating was never uploaded before!", "Proceed?",
                MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                LanguagePropertyDialog lpd = new LanguagePropertyDialog(name: loadedFile.Language.Name, 
                    owner: loadedFile.Language.Owner, _base: loadedFile.Language.Base, 
                    variant: loadedFile.Language.Variant);
                if (lpd.ShowDialog() == true)
                {
                    loadedFile.Language.Base = lpd.LanguageBase;
                    loadedFile.Language.Name = lpd.LanguageName;
                    loadedFile.Language.Owner = lpd.LanguageOwner;
                    loadedFile.Language.Variant = lpd.LanguageVariant;
                }
            }
        }
        #endregion
        #region Find Dialog
        private void openFindDialog()
        {
            //stuff's gotta be added here
            FindDialog fd = new FindDialog(this);
            fd.ShowDialog();
        }
        #endregion
        #region Big Delete Method
        private void executeDeleteCommand()
        {
            if (lastClicked >= 0)
            {
                switch (lastClicked)
                {
                    case clickedItems:
                        loadedFile.Strings.Remove((XmlString)listItemsView.SelectedItem);
                        currentStringsList = loadedFile.Strings;
                        currentValuesList = new ObservableCollection<string>();
                        listValuesView.ItemsSource = currentValuesList;
                        currentString = new XmlString();
                        valueHasChanged = true;
                        textBox.Text = "";
                        break;
                    case clickedValues:
                        string s = currentString.Values[currentValueIndex];
                        currentString.Values.Remove(s);
                        currentValuesList = currentString.Values;
                        var temp = currentStringIndex;
                        loadedFile.Strings[currentStringIndex] = currentString;
                        listItemsView.SelectedIndex = temp;
                        currentStringsList = loadedFile.Strings;
                        valueHasChanged = true;
                        textBox.Text = "";
                        break;
                }
                textHasChanged = true;
            }
        }
        #endregion
        #region Initialize emoji keyboard
        private void initializeEmojiKeyboard()
        {
            //this is only testing stuff yet
            for (int i=0; i<6; i++)
            {
                ColumnDefinition cd = new ColumnDefinition();
                cd.Width = new GridLength(1, GridUnitType.Star);
                emojiGrid.ColumnDefinitions.Add(cd);
            }
            int column = -1;
            int row = 0;
            foreach (string s in emojisList)
            {
                column++;
                if (column > 5)
                {
                    column = 0;
                    RowDefinition rd = new RowDefinition();
                    rd.Height = new GridLength(30, GridUnitType.Pixel);
                    emojiGrid.RowDefinitions.Add(rd);
                    row++;
                }
                Button b = new Button();
                b.Content = s;
                b.Margin = new Thickness(2.5, 2.5, 2.5, 2.5);
                Grid.SetColumn(b, column);
                Grid.SetRow(b, row);
                emojiGrid.Children.Add(b);
                b.Click += delegate (object sender, RoutedEventArgs e)
                {
                    var selStart = textBox.SelectionStart;
                    var selLength = textBox.SelectionLength;
                    textBox.Text = textBox.Text.Remove(selStart, selLength).Insert(selStart, b.Content.ToString());
                    textBox.Focus();
                    textBox.SelectionStart = selStart + 1;
                };
                b.IsEnabled = false;
                emojiButtonsList.Add(b);
            }
        }
        #endregion
        #region Load file from outside
        public void loadFileFromOutside(string path)
        {
            loadDirectory = path;
            saveDirectory = path;
            directoryChosen = true;
            loadFile();
        }
        #endregion
        #endregion

        #region Status Updating
        public void updateStatus()
        {
            if (fileIsOpen)
            {
                fileCloseMenuItem.IsEnabled = true;
                fileSaveMenuItem.IsEnabled = true;
                listItemsView.IsEnabled = true;
                editLanguageMenuItem.IsEnabled = true;
                fileUploadMenuItem.IsEnabled = true;
                editFindMenuItem.IsEnabled = true;
            }
            else
            {
                fileCloseMenuItem.IsEnabled = false;
                fileSaveMenuItem.IsEnabled = false;
                listItemsView.IsEnabled = false;
                editLanguageMenuItem.IsEnabled = false;
                fileUploadMenuItem.IsEnabled = false;
                editFindMenuItem.IsEnabled = false;
            }
            if (itemIsOpen)
            {
                listValuesView.IsEnabled = true;
                contentColumn.Width = 500;
                contentColumn.Width = double.NaN;
            }
            else
            {
                listValuesView.IsEnabled = false;
                contentColumn.Width = 500;
            }
            if (valueIsOpen)
            {
                textBox.IsEnabled = true;
                foreach(Button b in emojiButtonsList)
                {
                    b.IsEnabled = true;
                }
            }
            else
            {
                textBox.IsEnabled = false;
                foreach (Button b in emojiButtonsList)
                {
                    b.IsEnabled = false;
                }
            }
        }
        #endregion

        #region File and Xml Methods
        #region Choose directory
        private void chooseDirectory()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = saveDirectory;
            sfd.DefaultExt = ".xml";
            sfd.Filter = "XML Documents (.xml)|*.xml";
            bool? result = sfd.ShowDialog();
            if (result == true)
            {
                loadDirectory = sfd.FileName;
                saveDirectory = sfd.FileName;
                directoryChosen = true;
            }
        }
        #endregion
        #region Read xml string
        private XmlStrings readXmlString(string fileString)
        {
            string[] splitted = fileString.Split('\n');
            string lastKey = "";
            foreach (string s1 in splitted)
            {
                string s = s1.Trim();
                if (s.StartsWith("<!--"))
                {
                    if (!commentDic.ContainsKey(lastKey))
                    {
                        commentDic.Add(lastKey, new List<string>());
                    }
                    string s2 = s1;
                    if (!s2.EndsWith("-->")) s2 += "-->";
                    commentDic[lastKey].Add(s2);
                }
                else if (s.StartsWith("<string "))
                {
                    int index = s.IndexOf("key=\"") + 5;
                    int length = s.Substring(index).IndexOf('"');
                    lastKey = s.Substring(index, length);
                }
            }
            XmlStrings result;
            /*try
            {*/
                XmlSerializer serializer = new XmlSerializer(typeof(XmlStrings));
                using (TextReader tr = new StringReader(fileString))
                {
                    result = (XmlStrings)serializer.Deserialize(tr);
                }
                return result;
            /*}
            catch
            {
                MessageBox.Show("Failed to load file");
                return null;
            }*/
        }
        #endregion
        #region To utf-8 (deprecated?)
        private string toUtf8(string input)
        {
            byte[] bytes = Encoding.Default.GetBytes(input);
            input = Encoding.UTF8.GetString(bytes);
            return input;
        }
        #endregion
        #region Serialize xml to string
        private string serializeXmlToString()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlStrings));
            using (TextWriter tw = new StringWriter())
            {
                serializer.Serialize(tw, loadedFile);
                string[] results = tw.ToString().Split('\n');
                string result = results[0];
                if (commentDic.ContainsKey(""))
                {
                    foreach (string s in commentDic[""])
                    {
                        result += "\n" + s;
                    }
                }
                bool firstString = true;
                foreach (string s in results)
                {
                    if (!firstString)
                    {
                        result += "\n" + s;
                        if (s.Trim().StartsWith("<string "))
                        {
                            int index = s.IndexOf("key=\"") + 5;
                            int length = s.Substring(index).IndexOf('"');
                            string key = s.Substring(index, length);
                            if (commentDic.ContainsKey(key))
                            {
                                foreach (string c in commentDic[key])
                                {
                                    result += "\n" + c;
                                }
                            }
                        }
                    }
                    firstString = false;
                }
                //result = Utf16ToUtf8(result);
                return result.Replace("utf-16", "utf-8");
            }
        }
        #endregion
        #region Save xml file
        private void saveXmlFile()
        {
            if (!directoryChosen)
            {
                chooseDirectory();
            }
            string path = saveDirectory;
            string toWrite = serializeXmlToString();
            try
            {
                System.IO.File.WriteAllText(saveDirectory, toWrite, Encoding.UTF8);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            textHasChanged = false;
        }
        #endregion
        #region Get description
        private string getDescription(string key)
        {
            if (key != null)
            {
                if (descriptionDic.ContainsKey(key))
                {
                    return descriptionDic[key];
                }
                else
                {
                    return "No description yet.";
                }
            }
            return "No description yet.";
        }
        #endregion
        #region Check if saved
        private bool checkForSaved()
        {
            if (textHasChanged)
            {
                MessageBoxResult result = MessageBox.Show("File not saved!\nSave?", "", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    saveXmlFile();
                    return true;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
        #region Get file from scratch
        private void getFileFromScratch()
        {
            if (System.IO.File.Exists(fileScratchPath)) loadedFile = readXmlString(System.IO.File.ReadAllText(fileScratchPath));
            else throw new Exception("Failed to load file");
        }
        #endregion
        #endregion

        #region XAML Stuff
        #region Text box text changed
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (valueIsOpen && !valueHasChanged)
            {
                textHasChanged = true;
                int temp = currentValueIndex;
                fromTextBox = true;
                currentString.Values[temp] = textBox.Text;
                currentValuesList = currentString.Values;
                currentValueIndex = temp;
                temp = currentStringIndex;
                fromTextBox = true;
                loadedFile.Strings[temp] = currentString;
                currentStringIndex = temp;
                currentStringsList = loadedFile.Strings;
            }
            if (valueHasChanged)
            {
                valueHasChanged = false;
            }
            fromTextBox = true;
            listItemsView.SelectedIndex = currentStringIndex;
            fromTextBox = true;
            listValuesView.SelectedIndex = currentValueIndex;
            fromTextBox = false;
            updateStatus();
        }
        #endregion
        #region Window closing
        private void wuffPadWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (textHasChanged)
            {
                MessageBoxResult result = MessageBox.Show("File not saved!\nSave?", "", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    if (checkValuesCorrect())
                    {
                        saveXmlFile();
                    }
                }
                else if(result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
        #endregion
        #region Command executed
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Open)
            {
                if (!checkSetting("commentWarningDisable"))
                {
                    CommentWarningDialog cwd = new CommentWarningDialog();
                    if (cwd.ShowDialog() == true)
                    {
                        setSetting("commentWarningDisable", cwd.DoNotShowAgain);
                    }
                }
                bool closed = true;
                if (fileIsOpen)
                {
                    closed = closeFile();
                }
                if (closed) openFile();
            }
            else if(e.Command == ApplicationCommands.Save)
            {
                if (fileIsOpen)
                {
                    if (checkValuesCorrect())
                    {
                        saveXmlFile();
                    }
                }
            }
            else if(e.Command == ApplicationCommands.Close)
            {
                if (fileIsOpen) closeFile();
            }
            else if(e.Command == ApplicationCommands.New)
            {
                if(closeFile()) newFile();
            }
            else if (e.Command == ApplicationCommands.Delete)
            {
                if (fileIsOpen) executeDeleteCommand();
            }
            else if (e.Command == CustomCommands.LanguageProperties)
            {
                if (fileIsOpen) openLanguageDialog();
            }
            else if (e.Command == ApplicationCommands.Find)
            {
                if (fileIsOpen) openFindDialog();
            }
            else
            {
                throw new NotImplementedException(
                    "Tried to call a command that was not implemented");
            }
            updateStatus();
        }
        #endregion
        #region List of strings - selection changed
        private void listItemsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!fromTextBox)
            {
                int index = listItemsView.SelectedIndex;
                if (index >= 0)
                {
                    if (index != currentStringIndex)
                    {
                        valueIsOpen = false;
                    }
                    currentStringIndex = index;
                    currentString = loadedFile.Strings[index];
                    currentValuesList = currentString.Values;
                    listValuesView.ItemsSource = currentValuesList;
                    itemIsOpen = true;
                }
                else
                {
                    itemIsOpen = false;
                    currentStringIndex = -1;
                }
                listItemsView.SelectedIndex = currentStringIndex;
                updateStatus();
                lastClicked = clickedItems;
            }
            else
            {
                fromTextBox = false;
            }
        }
        #endregion
        #region List of values - selection changed
        private void listValuesView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!fromTextBox)
            {
                int index = listValuesView.SelectedIndex;
                if (index >= 0)
                {
                    showValue(currentString.Values[index]);
                    valueHasChanged = true;
                    textBox.Text = currentString.Values[index];
                    currentValueIndex = index;
                    valueIsOpen = true;
                    valueHasChanged = true;
                }
                else
                {
                    currentValueIndex = -1;
                }
                listValuesView.SelectedIndex = currentValueIndex;
                updateStatus();
                lastClicked = clickedValues;
            }
            else
            {
                fromTextBox = false;
            }
        }
        #endregion
        #region Add a string
        private void cmItemsAdd_Click(object sender, RoutedEventArgs e)
        {
            NewStringDialog nsd = new NewStringDialog(getDefaultMissingKey());
            if (nsd.ShowDialog() == true)
            {
                string key = nsd.Key;
                bool isPresent = false;
                foreach (XmlString s in loadedFile.Strings)
                {
                    if (s.Key == key) isPresent = true;
                }
                if (!isPresent)
                {
                    XmlString xs = new XmlString();
                    xs.Key = key;
                    xs.Description = getDescription(xs.Key);
                    loadedFile.Strings.Add(xs);
                    currentStringsList = loadedFile.Strings;
                    currentString = xs;
                    currentStringIndex = currentStringsList.IndexOf(xs);
                    currentValue = null;
                    textHasChanged = true;
                    listItemsView.SelectedIndex = loadedFile.Strings.Count - 1;
                    listItemsView.ScrollIntoView(loadedFile.Strings[loadedFile.Strings.Count - 1]);
                }
                else
                {
                    MessageBox.Show("A string with that key is already present");
                }
            }
        }
        #endregion
        #region Clicked on not list (deprecated?)
        private void notList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastClicked = -1;
        }
        #endregion
        #region Add value
        private void cmValuesAdd_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Create a new value?", "", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (XmlString s in loadedFile.Strings)
                {
                    if (s.Key == currentString.Key)
                    {
                        s.Values.Add("Add text here");
                        break;
                    }
                }
                currentValuesList = currentString.Values;
                currentValueIndex = currentValuesList.IndexOf("Add text here");
                currentValue = currentString.Values[currentValueIndex];
                valueHasChanged = true;
                textBox.Text = "Add text here";
                textHasChanged = true;
                showValues(currentString);
                showValue(currentValue);
                valueIsOpen = true;
                listValuesView.SelectedIndex = currentString.Values.Count - 1;
                listValuesView.ScrollIntoView(currentString.Values[currentString.Values.Count - 1]);
                updateStatus();
            }
        }
        #endregion
        #region #closedlist
        private void closedlistItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(closedlistPath);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream resStream = response.GetResponseStream();
                string closedlist;
                using (StreamReader sr = new StreamReader(resStream))
                {
                    closedlist = sr.ReadToEnd();
                }
                ClosedlistWindow cw = new ClosedlistWindow("CURRENT CLOSEDLIST", 
                    closedlist.Replace(":", ": "));
                cw.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Failed to fetch #closedlist.");
            }
        }
        #endregion
        #region #underdev
        private void underdevItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(underdevPath);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream resStream = response.GetResponseStream();
                string underdev;
                using (StreamReader sr = new StreamReader(resStream))
                {
                    underdev = sr.ReadToEnd();
                }
                ClosedlistWindow cw = new ClosedlistWindow("LANGFILES UNDER DEVELOPMENT",
                    underdev.Replace(":", ": "));
                cw.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Failed to fetch #underdev.");
            }
        }
        #endregion
        #region Upload file
        private void fileUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Upload file?", "Upload", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No) return;
            try
            {
                if (checkForSaved())
                {
                    TelegramBotClient client = new TelegramBotClient(token);
                    string[] splitted = saveDirectory.Split('\\');
                    FileStream fs = System.IO.File.OpenRead(saveDirectory);
                    FileToSend fts = new FileToSend(splitted[splitted.Length - 1], fs);
                    Task t = client.SendDocumentAsync(UploadChatId, fts,
                        "Please forward this message to this chat again, so the other bot can see it.");
                    t.Wait();
                    MessageBox.Show("File was sent to translation group. It will be uploaded as soon as an admin"
                        + " comes across it.");
                }
            }
            catch
            {
                MessageBox.Show("An error occurred.");
            }
        }
        #endregion
        #region Wiki
        private void wikiItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(wikiPageUrl);
        }
        #endregion
        #region Feedback
        private void feedbackItem_Click(object sender, RoutedEventArgs e)
        {
            FeedbackDialog fd = new FeedbackDialog(token);
            fd.ShowDialog();
        }
        #endregion
        #endregion

        #region Display Control
        #region Show values (deprecated?)
        private void showValues(XmlString s)
        {
            currentString = s;
        }
        #endregion
        #region Show value (deprecated?)
        private void showValue(string s)
        {
            valueHasChanged = true;
            textBox.Text = s;
            currentValue = s;
        }
        #endregion
        #region Close file
        private bool closeFile()
        {
            if (textHasChanged)
            {
                MessageBoxResult result = MessageBox.Show("File not saved!\nSave?", "", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    saveXmlFile();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }
            textBox.Clear();
            textHasChanged = false;
            currentString = new XmlString();
            currentStringIndex = -1;
            loadedFile.Strings = new ObservableCollection<XmlString>();
            currentStringsList = loadedFile.Strings;
            currentValue = null;
            currentValueIndex = -1;
            currentString.Values.Clear();
            fileIsOpen = false;
            textHasChanged = false;
            itemIsOpen = false;
            valueIsOpen = false;
            directoryChosen = false;
            lastClicked = -1;
            valueHasChanged = false;
            return true;
        }
        #endregion

        #endregion
    }
}