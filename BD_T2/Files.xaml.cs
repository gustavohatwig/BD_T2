using System;
using System.Collections.Generic;
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

namespace BD_T2
{
    /// <summary>
    /// Interaction logic for Files.xaml
    /// </summary>
    public partial class Files : Window
    {
        String read;
        String readName;

        public Files()
        {
            InitializeComponent();
        }

        private void Correta_Click(object sender, RoutedEventArgs e)
        {
            read = "EscalaCorreta.txt";
            readName = "Escala Correta";
            Close();
        }

        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            read = "EscalaErroLockT1.txt";
            readName = "Lock no T1";
            Close();
        }

        private void Deadlock_Click(object sender, RoutedEventArgs e)
        {
            read = "EscalaDeadlockT1T4.txt";
            readName = "Deadlock";
            Close();
        }

        private void TwoPhaseLock_Click(object sender, RoutedEventArgs e)
        {
            read = "EscalaErro2PLT1.txt";
            readName = "TwoPhase Lock";
            Close();
        }

        public String getReadFile()
        {
            return read;
        }

        public String getReadName()
        {
            return readName;
        }
    }
}
