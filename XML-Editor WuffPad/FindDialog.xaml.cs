using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XML_Editor_WuffPad.XMLClasses;

namespace XML_Editor_WuffPad
{
    /// <summary>
    /// Interaktionslogik für FindDialog.xaml
    /// </summary>
    public partial class FindDialog : Window
    {
        private MainWindow main;
        public FindDialog(MainWindow mw)
        {
            InitializeComponent();
            main = mw;
        }

        #region Find Button
        private void okayButton_Click(object sender, RoutedEventArgs e)
        {
            if (keysButton.IsChecked == null) keysButton.IsChecked = false;
            bool keysChecked = (bool)keysButton.IsChecked;
            if (descriptionsButton.IsChecked == null) descriptionsButton.IsChecked = false;
            bool descriptionsChecked = (bool)descriptionsButton.IsChecked;
            if (valuesButton.IsChecked == null) valuesButton.IsChecked = false;
            bool valuesChecked = (bool)valuesButton.IsChecked;
            string search = textBox.Text;
            bool foundSth = false;
            for (int i = main.listItemsView.SelectedIndex + 1; i <= main.currentStringsList.Count; i++)
            {
                #region Check end of file
                if (!foundSth && i == main.currentStringsList.Count)
                {
                    MessageBoxResult res =
                        MessageBox.Show("Search reached end of file. Start over?",
                        "No results", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                    {
                        i = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                #endregion
                #region Keys
                if (keysChecked && main.currentStringsList[i].Key.Contains(search))
                {
                    main.listItemsView.ScrollIntoView(main.currentStringsList[i]);
                    main.listItemsView.SelectedItem = main.currentStringsList[i];
                    foundSth = true;
                    break;
                }
                #endregion
                #region Descriptions
                if (descriptionsChecked && main.currentStringsList[i].Description.Contains(search))
                {
                    main.listItemsView.ScrollIntoView(main.currentStringsList[i]);
                    main.listItemsView.SelectedItem = main.currentStringsList[i];
                    foundSth = true;
                    break;
                }
                #endregion
                #region Values
                if (valuesChecked)
                {
                    foreach(string v in main.currentStringsList[i].Values)
                    {
                        if (v.Contains(search))
                        {
                            main.listItemsView.ScrollIntoView(main.currentStringsList[i]);
                            main.listItemsView.SelectedItem = main.currentStringsList[i];
                            main.listValuesView.ScrollIntoView(v);
                            main.listValuesView.SelectedItem = v;
                            foundSth = true;
                            break;
                        }
                    }
                    if (foundSth) break;
                }
                #endregion
            }
        }
        #endregion

        #region Close Button
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        #endregion
    }
}
