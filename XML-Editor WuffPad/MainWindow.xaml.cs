using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using XML_Editor_WuffPad.Commands;
using System.Threading.Tasks;
using System.Diagnostics;
using XML_Editor_WuffPad.Properties;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows.Data;
using File = System.IO.File;
using XML_Editor_WuffPad.Dialogs;
using XMLClasses;
using XML_Editor_WuffPad.Core;
using System.Collections.Specialized;

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
        /// <summary>Folder holding the helper files; see <see cref="AppData"/>.</summary>
        internal static string RootDirectory
        {
            get { return AppData.Directory; }
        }
        private static string fileScratchPath { get { return AppData.PathFor(BuiltinResources.LanguageFile); } }
        private static string dictFilePath { get { return AppData.PathFor(BuiltinResources.DescriptionsFile); } }
        private static string defaultKeysFilePath { get { return AppData.PathFor(BuiltinResources.DefaultKeysFile); } }
        private static string emojisFilePath { get { return AppData.PathFor(BuiltinResources.EmojisFile); } }
        private static string authTokenPath { get { return Path.Combine(RootDirectory, "tokens.txt"); } }
        private const string wikiPageUrl = "https://github.com/Olfi01/WuffPad/wiki";
        // The WuffPad web server is gone. Uploads are kept for compatibility, but
        // they will only work again if someone points uploadPath at a live endpoint.
        private const string uploadPath = "http://185.249.197.95/uploadWuffLangfile.php";
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
        private List<Button> emojiButtonsList = new List<Button>();
        private List<string> emojisList = new List<string>();
        private ScriptWindow currentScriptWindow;
        #endregion
        #endregion

        #region Constructor
        #region Normal startup
        public MainWindow()
        {
            InitializeComponent();
            // The helper files used to be downloaded here. The server is gone, so
            // this now writes the copies embedded in the executable and only then
            // tries the (optional) update location in the background.
            BuiltinResources.EnsureExtracted();
            GetDictAndDefaultKeys();
            StartBackgroundUpdate();
            listItemsView.ItemsSource = currentStringsList;
            listValuesView.ItemsSource = currentValuesList;
            InitializeEmojiKeyboard();
            UpdateStatus();
        }
        #endregion
        #region Opening a file
        public MainWindow(string path)
            : this()
        {
            LoadFileFromOutside(path);
        }
        #endregion
        #endregion

        #region Functionable Methods
        #region Checking for errors
        private bool CheckValuesCorrect()
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
                    foreach (string v in s.Values)
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
                        XmlString temp = new XmlString()
                        {
                            Key = s,
                            Description = GetDescription(s)
                        };
                        loadedFile.Strings.Add(temp);
                        currentStringsList = loadedFile.Strings;
                        listItemsView.ScrollIntoView(temp);
                        listItemsView.SelectedItem = temp;
                        return false;
                    }
                }
            }
            #endregion
            #region checking for duplicated strings
            List<string> hadKeys = new List<string>();
            foreach (string s in hasKeys)
            {
                if (hadKeys.Contains(s))
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"The key {s} is duplicated. The second instance won't be used. Join the two instances?",
                        "Duplicated string", MessageBoxButton.YesNoCancel);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            var toJoinValues = new List<string>();
                            bool first = true;
                            var toRemove = new List<XmlString>();
                            foreach (var xs in loadedFile.Strings)
                            {
                                if (xs.Key == s)
                                {
                                    if (!first)
                                    {
                                        toJoinValues.AddRange(xs.Values);
                                        toRemove.Add(xs);
                                    }
                                    else first = false;
                                }
                            }
                            foreach (var r in toRemove)
                            {
                                loadedFile.Strings.Remove(r);
                            }
                            foreach (var xs in loadedFile.Strings)
                            {
                                if (xs.Key == s)
                                {
                                    foreach (string v in toJoinValues)
                                    {
                                        xs.Values.Add(v);
                                    }
                                }
                            }
                            break;
                        case MessageBoxResult.Cancel:
                            return false;
                    }
                }
                hadKeys.Add(s);
            }
            #endregion
            return doSave;
        }
        #endregion
        #region Background update of the helper files
        /// <summary>
        /// Asks <see cref="ResourceUpdater"/> for newer helper files, if an update
        /// location is configured. Never blocks and never fails the start-up.
        /// </summary>
        private void StartBackgroundUpdate()
        {
            ResourceUpdater.TryStartBackgroundUpdate(files =>
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(() => ReloadHelperFiles(files)));
                }
                catch
                {
                    // Window already gone, nothing to refresh.
                }
            });
        }
        /// <summary>
        /// Re-reads descriptions, default keys and emojis after an update and
        /// re-applies the descriptions to the open file.
        /// </summary>
        private void ReloadHelperFiles(IList<string> changed)
        {
            GetDictAndDefaultKeys();
            if (changed.Contains(BuiltinResources.EmojisFile))
            {
                foreach (Button b in emojiButtonsList)
                {
                    emojiGrid.Children.Remove(b);
                }
                emojiButtonsList.Clear();
                emojiGrid.Children.Clear();
                emojiGrid.RowDefinitions.Clear();
                emojiGrid.ColumnDefinitions.Clear();
                InitializeEmojiKeyboard();
            }
            if (fileIsOpen)
            {
                foreach (XmlString s in loadedFile.Strings)
                {
                    s.Description = GetDescription(s.Key);
                }
            }
            UpdateStatus();
        }
        #endregion
        #region Get a missing default key
        private string GetDefaultMissingKey()
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
        private void GetDictAndDefaultKeys()
        {
            // Called again after a background update, so start from scratch instead
            // of appending to what was read before.
            descriptionDic = new Dictionary<string, string>();
            defaultKeysList = new List<string>();
            emojisList = new List<string>();
            if (File.Exists(dictFilePath))
            {
                string input = System.IO.File.ReadAllText(dictFilePath);
                descriptionDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(input);
            }
            if (File.Exists(defaultKeysFilePath))
            {
                string input = System.IO.File.ReadAllText(defaultKeysFilePath);
                string[] inputs = input.Split('\n');
                foreach (string s in inputs)
                {
                    defaultKeysList.Add(s);
                }
            }
            if (File.Exists(emojisFilePath))
            {
                foreach (string s in System.IO.File.ReadAllLines(emojisFilePath, Encoding.UTF8))
                {
                    emojisList.Add(s);
                }
            }
        }
        #endregion
        #region New file
        private void NewFile()
        {
            MessageBoxResult result = MessageBox.Show(
                    "Use the english file as a blueprint?", "New File", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                loadedFile = new XmlStrings();
                try
                {
                    GetFileFromScratch();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load the English blueprint for a new file.\n"
                        + e.Message);
                }
                currentStringsList.Clear();
                foreach (XmlString s in loadedFile.Strings)
                {
                    s.Description = GetDescription(s.Key);
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
            UpdateStatus();
        }
        #endregion
        #region Open file
        private void OpenFile()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                InitialDirectory = loadDirectory,
                DefaultExt = ".xml",
                Filter = "XML Documents (.xml)|*.xml"
            };
            bool? result = ofd.ShowDialog();
            if (result == true)
            {
                loadDirectory = ofd.FileName;
                saveDirectory = ofd.FileName;
                directoryChosen = true;
                /*try
                {*/
                LoadFile();
                /*}
                catch
                {
                    //Message should've already popped up
                }*/
            }
            UpdateStatus();
        }
        #endregion
        #region Load file
        private void LoadFile()
        {
            loadedFile = ReadXmlString(System.IO.File.ReadAllText(loadDirectory));
            if (loadedFile == null) return;
            fileIsOpen = true;
            currentStringsList.Clear();
            foreach (XmlString s in loadedFile.Strings)
            {
                s.Description = GetDescription(s.Key);
                //currentStringsList.Add(s);
            }
            currentStringsList = loadedFile.Strings;
            listItemsView.ItemsSource = currentStringsList;
            UpdateStatus();
        }
        #endregion
        #region Language Dialog
        private void OpenLanguageDialog()
        {
            MessageBoxResult res = MessageBox.Show(
                "Only change the language properties if you know what you are doing" +
                " or the language you are translating was never uploaded before!", "Proceed?",
                MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                LanguagePropertyDialog lpd = new LanguagePropertyDialog(name: loadedFile.Language.Name,
                    owner: loadedFile.Language.Owner, _base: loadedFile.Language.Base,
                    variant: loadedFile.Language.Variant, code: loadedFile.Language.Code,
                    isDefault: loadedFile.Language.IsDefault ? "true" : "false");
                if (lpd.ShowDialog() == true)
                {
                    loadedFile.Language.Base = lpd.LanguageBase;
                    loadedFile.Language.Name = lpd.LanguageName;
                    loadedFile.Language.Owner = lpd.LanguageOwner;
                    loadedFile.Language.Variant = lpd.LanguageVariant;
                    loadedFile.Language.Code = lpd.LanguageCode;
                    loadedFile.Language.IsDefault = lpd.LanguageIsDefault == "true" ? true : false;
                }
            }
        }
        #endregion
        #region Find Dialog
        private void OpenFindDialog()
        {
            //stuff's gotta be added here
            FindDialog fd = new FindDialog(this);
            fd.ShowDialog();
        }
        #endregion
        #region Big Delete Method
        private void ExecuteDeleteCommand()
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
                        ICollectionView view = CollectionViewSource.GetDefaultView(currentStringsList);
                        view.Refresh();
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
        private void InitializeEmojiKeyboard()
        {
            if (emojiGrid.ColumnDefinitions.Count == 0)
            {
                AddEmojiGridColumns();
            }
            int column = -1;
            int row = 0;
            RowDefinition rdBase = new RowDefinition()
            {
                Height = new GridLength(30, GridUnitType.Pixel)
            };
            emojiGrid.RowDefinitions.Add(rdBase);
            foreach (string s in emojisList)
            {
                column++;
                if (column > 5)
                {
                    column = 0;
                    RowDefinition rd = new RowDefinition()
                    {
                        Height = new GridLength(30, GridUnitType.Pixel)
                    };
                    emojiGrid.RowDefinitions.Add(rd);
                    row++;
                }
                Button b = new Button()
                {
                    Content = s,
                    Margin = new Thickness(2.5, 2.5, 2.5, 2.5)
                };
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
                b.IsEnabled = valueIsOpen;
                emojiButtonsList.Add(b);
            }
        }

        private void AddEmojiGridColumns()
        {
            for (int i = 0; i < 6; i++)
            {
                ColumnDefinition cd = new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star)
                };
                emojiGrid.ColumnDefinitions.Add(cd);
            }
        }
        #endregion
        #region Load file from outside
        public void LoadFileFromOutside(string path)
        {
            loadDirectory = path;
            saveDirectory = path;
            directoryChosen = true;
            LoadFile();
        }
        #endregion
        #region Upload file
        private void FileUploadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Upload file?", "Upload", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No) return;
            try
            {
                if (CheckForSaved())
                {
                    WebClient wc = new WebClient();
                    var parameters = new NameValueCollection();
                    if (File.Exists(authTokenPath)) parameters.Add("token", File.ReadAllText(authTokenPath));
                    wc.QueryString = parameters;
                    var responseBytes = wc.UploadFile(uploadPath, saveDirectory);
                    string response = Encoding.ASCII.GetString(responseBytes);
#if DEBUG
                    //MessageBox.Show(response);
#endif
                    MessageBox.Show("File was sent to translation group. It will be uploaded as soon as an admin"
                        + " comes across it.");
                }
            }
            catch
            {
                MessageBox.Show("The upload server is no longer available: WuffPad was shut down in 2025.\n"
                    + "Send the file to the translation group yourself instead.");
            }
        }
        #endregion
        #region Script Window
        private void OpenScriptWindow()
        {
            if (currentScriptWindow?.IsActive ?? false)
            {
                if (!currentScriptWindow.IsFocused) currentScriptWindow.Focus();
                return;
            }
            currentScriptWindow = new ScriptWindow(this.Height, ref loadedFile);
            currentScriptWindow.Show();
        }
        #endregion
        #region Login
        private void Login()
        {
            MessageBox.Show("Logging in is no longer possible: the WuffPad server was shut down in 2025.\n"
                + "Uploading is disabled for everyone; see the Wiki for the current translation work flow.");
        }
        #endregion
        #endregion

        #region Status Updating
        public void UpdateStatus()
        {
            if (fileIsOpen)
            {
                fileCloseMenuItem.IsEnabled = true;
                fileSaveMenuItem.IsEnabled = true;
                listItemsView.IsEnabled = true;
                editLanguageMenuItem.IsEnabled = true;
                fileUploadMenuItem.IsEnabled = true;
                editFindMenuItem.IsEnabled = true;
                fileFindMenuItem.IsEnabled = true;
                scriptMenuItem.IsEnabled = true;
            }
            else
            {
                fileCloseMenuItem.IsEnabled = false;
                fileSaveMenuItem.IsEnabled = false;
                listItemsView.IsEnabled = false;
                editLanguageMenuItem.IsEnabled = false;
                fileUploadMenuItem.IsEnabled = false;
                editFindMenuItem.IsEnabled = false;
                fileFindMenuItem.IsEnabled = false;
                scriptMenuItem.IsEnabled = false;
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
                foreach (Button b in emojiButtonsList)
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
        private void ChooseDirectory()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                InitialDirectory = saveDirectory,
                DefaultExt = ".xml",
                Filter = "XML Documents (.xml)|*.xml"
            };
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
        private XmlStrings ReadXmlString(string fileString)
        {
            /*string[] splitted = fileString.Split('\n');
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
                    //if (!s2.Trim().EndsWith("-->")) s2 += "-->";
                    commentDic[lastKey].Add(s2);
                }
                else if (s.StartsWith("<string "))
                {
                    int index = s.IndexOf("key=\"") + 5;
                    int length = s.Substring(index).IndexOf('"');
                    lastKey = s.Substring(index, length);
                }
            }*/
            foreach (Match match in Regex.Matches(fileString,
                $@"(?s){Regex.Escape("<!--")}((?!{Regex.Escape("-->")}).)*{Regex.Escape("-->")}"))
            {
                int index = fileString.Remove(match.Index).LastIndexOf("key=\"") + 5;
                int length = fileString.Substring(index).IndexOf('"');
                string key = fileString.Substring(index, length);
                if (!commentDic.ContainsKey(key))
                {
                    commentDic.Add(key, new List<string>());
                }
                commentDic[key].Add(match.ToString());
            }
            XmlStrings result;
#if DEBUG
#else
            try
            {
#endif
            XmlSerializer serializer = new XmlSerializer(typeof(XmlStrings));
            using (TextReader tr = new StringReader(fileString))
            {
                result = (XmlStrings)serializer.Deserialize(tr);
            }
            return result;
#if DEBUG
#else
            }
            catch
            {
                MessageBox.Show("Failed to load file");
                return null;
            }
#endif
        }
        #endregion
        #region Serialize xml to string
        private string SerializeXmlToString()
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
        private void SaveXmlFile()
        {
            if (!directoryChosen)
            {
                ChooseDirectory();
            }
            string path = saveDirectory;
            string toWrite = SerializeXmlToString();
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
        private string GetDescription(string key)
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
        private bool CheckForSaved()
        {
            if (textHasChanged)
            {
                MessageBoxResult result = MessageBox.Show("File not saved!\nSave?", "", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    SaveXmlFile();
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
        private void GetFileFromScratch()
        {
            if (!System.IO.File.Exists(fileScratchPath)
                || new FileInfo(fileScratchPath).Length == 0)
            {
                BuiltinResources.Extract(BuiltinResources.LanguageFile, fileScratchPath);
            }
            if (System.IO.File.Exists(fileScratchPath)) loadedFile = ReadXmlString(System.IO.File.ReadAllText(fileScratchPath));
            else throw new Exception("The English blueprint is missing from this build.");
        }
        #endregion
        #endregion

        #region XAML Stuff
        #region Text box text changed
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
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
            UpdateStatus();
        }
        #endregion
        #region Window closing
        private void WuffPadWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (textHasChanged)
            {
                MessageBoxResult result = MessageBox.Show("File not saved!\nSave?", "", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    if (CheckValuesCorrect())
                    {
                        SaveXmlFile();
                    }
                }
                else if (result == MessageBoxResult.Cancel)
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
                bool closed = true;
                if (fileIsOpen)
                {
                    closed = CloseFile();
                }
                if (closed) OpenFile();
            }
            else if (e.Command == ApplicationCommands.Save)
            {
                if (fileIsOpen)
                {
                    if (CheckValuesCorrect())
                    {
                        SaveXmlFile();
                    }
                }
            }
            else if (e.Command == ApplicationCommands.Close)
            {
                if (fileIsOpen) CloseFile();
            }
            else if (e.Command == ApplicationCommands.New)
            {
                if (CloseFile()) NewFile();
            }
            else if (e.Command == ApplicationCommands.Delete)
            {
                if (fileIsOpen) ExecuteDeleteCommand();
            }
            else if (e.Command == CustomCommands.LanguageProperties)
            {
                if (fileIsOpen) OpenLanguageDialog();
            }
            else if (e.Command == ApplicationCommands.Find)
            {
                if (fileIsOpen) OpenFindDialog();
            }
            else if (e.Command == CustomCommands.Scripts)
            {
                if (fileIsOpen) OpenScriptWindow();
            }
            else if (e.Command == CustomCommands.Login)
            {
                Login();
            }
            else
            {
                throw new NotImplementedException(
                    "Tried to call a command that was not implemented");
            }
            UpdateStatus();
        }
        #endregion
        #region List of strings - selection changed
        private void ListItemsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                UpdateStatus();
                lastClicked = clickedItems;
                if (currentValuesList.Count > 0) listValuesView.SelectedIndex = 0;
            }
            else
            {
                fromTextBox = false;
            }
        }
        #endregion
        #region List of values - selection changed
        private void ListValuesView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!fromTextBox)
            {
                int index = listValuesView.SelectedIndex;
                if (index >= 0)
                {
                    ShowValue(currentString.Values[index]);
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
                UpdateStatus();
                lastClicked = clickedValues;
            }
            else
            {
                fromTextBox = false;
            }
        }
        #endregion
        #region Add a string
        private void CmItemsAdd_Click(object sender, RoutedEventArgs e)
        {
            NewStringDialog nsd = new NewStringDialog(GetDefaultMissingKey());
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
                    XmlString xs = new XmlString()
                    {
                        Key = key
                    };
                    xs.Description = GetDescription(xs.Key);
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
        private void NotList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastClicked = -1;
        }
        #endregion
        #region Add value
        private void CmValuesAdd_Click(object sender, RoutedEventArgs e)
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
                ShowValues(currentString);
                ShowValue(currentValue);
                ICollectionView view = CollectionViewSource.GetDefaultView(currentStringsList);
                view.Refresh();
                valueIsOpen = true;
                listValuesView.SelectedIndex = currentString.Values.Count - 1;
                listValuesView.ScrollIntoView(currentString.Values[currentString.Values.Count - 1]);
                UpdateStatus();
            }
        }
        #endregion
        #region #closedlist
        private void ClosedlistItem_Click(object sender, RoutedEventArgs e)
        {
            ShowTelegramList("CURRENT CLOSEDLIST", "closedlist");
        }
        #endregion
        #region #underdev
        private void UnderdevItem_Click(object sender, RoutedEventArgs e)
        {
            ShowTelegramList("LANGFILES UNDER DEVELOPMENT", "underdev");
        }
        #endregion
        #region Telegram list helper
        /// <summary>
        /// Shows the content of a public Telegram channel the way it was shown
        /// before. The WuffPad server that used to proxy these lists is gone, so the
        /// public preview page of the channel is read directly instead.
        /// </summary>
        private void ShowTelegramList(string header, string channel)
        {
            string content = TelegramChannelFeed.GetMessages(
                TelegramChannelFeed.DownloadChannel(channel));
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("Failed to fetch #" + channel + ".\n"
                    + "The WuffPad server is gone and t.me/" + channel
                    + " could not be reached. Check your internet connection.");
                return;
            }
            ClosedlistWindow cw = new ClosedlistWindow(header, content);
            cw.ShowDialog();
        }
        #endregion
        #region Wiki
        private void WikiItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(wikiPageUrl);
        }
        #endregion
        #region Feedback
        private void FeedbackItem_Click(object sender, RoutedEventArgs e)
        {
            FeedbackDialog fd = new FeedbackDialog();
            fd.ShowDialog();
        }
        #endregion
        #region Login clicked
        private void LoginMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }
        #endregion
        #endregion

        #region Display Control
        #region Show values
        private void ShowValues(XmlString s)
        {
            currentString = s;
        }
        #endregion
        #region Show value
        private void ShowValue(string s)
        {
            valueHasChanged = true;
            textBox.Text = s;
            currentValue = s;
        }
        #endregion
        #region Close file
        private bool CloseFile()
        {
            if (textHasChanged)
            {
                MessageBoxResult result = MessageBox.Show("File not saved!\nSave?", "", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    SaveXmlFile();
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