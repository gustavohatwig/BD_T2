using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BD_T2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables Declaration

        String s_operations;
        List<char> i_lockList = new List<char>();
        StringReader reader;
        private TwoPhaseLocker twopl;
        private Files file;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            w_ExecuteStepButton.IsEnabled = false;
            w_ExecuteAllButton.IsEnabled = false;
            w_ClearExecutionButton.IsEnabled = false;
            w_ExecuteFileButton.IsEnabled = false;
        }

        #region File Reading

        private void w_ReadFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                file = new Files();
                file.ShowDialog();

                w_ExecuteAllButton.IsEnabled = false;
                w_ExecuteStepButton.IsEnabled = false;
                w_ClearExecutionButton.IsEnabled = false;
                s_operations = String.Empty;
                w_FileOperations.Text = String.Empty;
                w_OperationResult.Text = "<< Results >>";

                StreamReader i_fileReader = new StreamReader(file.getReadFile());
                String line = String.Empty;
                s_operations = String.Empty;

                while((line = i_fileReader.ReadLine()) != null)
                {
                    s_operations += line +"\n";
                }

                w_FileOperations.Text = s_operations;
                w_FileReadStatus.Text = file.getReadName();
                w_ExecuteFileButton.IsEnabled = true;
                reader = new StringReader(s_operations);
            }
            // NÃO VAI MAIS ENTRAR AQUI -> ASSIM ESPERA-SE
            catch (Exception ex)
            {
                w_FileReadStatus.Text = "Could not read the file.";
            }
        }

        #endregion

        private void w_ExecuteFileButton_Click(object sender, RoutedEventArgs e)
        {
            w_ExecuteFileButton.IsEnabled = false;
            w_ExecuteAllButton.IsEnabled = true;
            w_ExecuteStepButton.IsEnabled = true;
            w_OperationResult.Text = string.Empty;
            twopl = new TwoPhaseLocker(s_operations);
        }

        private void w_ExecuteAllButton_Click(object sender, RoutedEventArgs e)
        {
            w_ExecuteStepButton.IsEnabled = false;
            w_ClearExecutionButton.IsEnabled = true;

            while (true)
            {
                string r = twopl.Step();
                if (r != null)
                    w_OperationResult.Text = r;
                else
                {
                    w_OperationResult.Text += "\nFim!";
                    break;
                }
            }
        }

        private void w_ExecuteStepButton_Click(object sender, RoutedEventArgs e)
        {
            w_ExecuteAllButton.IsEnabled = false;
            w_ClearExecutionButton.IsEnabled = true;
            string r = twopl.Step();
            w_ExecuteStepButton.IsEnabled = r != null;
            if(r != null)
                w_OperationResult.Text = r;
            else
                w_OperationResult.Text += "\nFim!";
        }

        private void w_ClearExecutionButton_Click(object sender, RoutedEventArgs e)
        {
            w_ClearExecutionButton.IsEnabled = false;
            w_ExecuteFileButton.IsEnabled = true;
            w_ExecuteAllButton.IsEnabled = false;
            w_ExecuteStepButton.IsEnabled = false;
            w_OperationResult.Text = "<< Results >>";
        }
    }
}
