using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using XMLClasses;

namespace XML_Editor_WuffPad.Dialogs
{
    /// <summary>
    /// Interaktionslogik für ScriptWindow.xaml
    /// </summary>
    public partial class ScriptWindow : Window
    {
        private XmlStrings File;
        private CSharpCodeProvider provider;
        private const string defaultCode = @"using XMLClasses;

namespace Scripts
{
    class Script
    {
        public static XmlStrings Run(XmlStrings file)
        {
            return file;
        }
    }
}";

        public ScriptWindow(ref XmlStrings file)
        {
            InitializeComponent();
            File = file;
            provider = new CSharpCodeProvider();
            codeBox.Text = defaultCode;
        }

        public ScriptWindow(double height, ref XmlStrings file) : this(ref file)
        {
            Height = height;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var parameters = new CompilerParameters() { GenerateInMemory = true };
            parameters.ReferencedAssemblies.Add("System.Xml.dll");
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("XmlClasses.dll");
            parameters.ReferencedAssemblies.Add("System.Linq.dll");
            var results = provider.CompileAssemblyFromSource(parameters, codeBox.Text);
            if (results.Errors.HasErrors)
            {
                string error = "";
                foreach (CompilerError err in results.Errors)
                {
                    error += err.ErrorText + "\n";
                }
                MessageBox.Show(error);
                return;
            }
            Type Script = results.CompiledAssembly.GetType("Scripts.Script");
            MethodInfo Run = Script.GetMethod("Run");
            File = (XmlStrings)Run.Invoke(null, new object[] { File });
        }

        private void Tab_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (codeBox.IsFocused)
            {
                var temp = codeBox.SelectionStart;
                codeBox.Text = codeBox.Text.Remove(codeBox.SelectionStart) + "    " 
                    + codeBox.Text.Substring(codeBox.SelectionStart + codeBox.SelectionLength);
                codeBox.SelectionStart = temp + 4;
                codeBox.SelectionLength = 0;
            }
        }
    }
}
