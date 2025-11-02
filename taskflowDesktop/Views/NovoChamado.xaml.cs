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

namespace taskflowDesktop.Views
{
    /// <summary>
    /// Lógica interna para Window1.xaml
    /// </summary>
    public partial class NovoChamado : Window
    {

        private void AbrirModal_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;
        }

        private void FecharModal_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        public NovoChamado()
        {
            InitializeComponent();
        }
    }
}
