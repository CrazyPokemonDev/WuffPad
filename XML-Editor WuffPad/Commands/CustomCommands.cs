using System.Windows.Input;

namespace XML_Editor_WuffPad.Commands
{
    public static class CustomCommands
    {
        public static readonly RoutedUICommand LanguageProperties = new RoutedUICommand(
            "Language properties",
            "Language properties",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.L, ModifierKeys.Control)
            }
            );
        public static readonly RoutedUICommand Scripts = new RoutedUICommand(
            "Scripts",
            "Scripts",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)
            }
            );
        public static readonly RoutedUICommand Login = new RoutedUICommand(
            "Login",
            "Login",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Shift)
            }
            );
        public static readonly RoutedUICommand Tab = new RoutedUICommand("Tab", "Tab", typeof(CustomCommands));
    }
}
