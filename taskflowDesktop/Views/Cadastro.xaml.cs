using System;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Configuration;
using BCrypt.Net;

namespace taskflowDesktop.Views
{
    public partial class Cadastro : Window
    {
        public Cadastro()
        {
            InitializeComponent();
        }

        private void CadastrarUsuario()
        {
            // Pegando os valores dos campos (substitua pelos nomes corretos dos seus TextBox/PasswordBox)
            string email = EmailTextBox.Text;
            string confirmarEmail = ConfirmarEmailTextBox.Text;
            string senha = SenhaPasswordBox.Password;
            string confirmarSenha = ConfirmarSenhaPasswordBox.Password;

            // Validações básicas
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                MessageBox.Show("Preencha todos os campos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //if (email != confirmarEmail)
            //{
            //    MessageBox.Show("Os emails não conferem.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            if (senha != confirmarSenha)
            {
                MessageBox.Show("As senhas não conferem.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Pega a connection string do App.config
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string senhaHash = BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 12);


                    string query = "INSERT INTO CLIENTES (Nome, Email, Senha_hash) VALUES (@Nome, @Email, @Senha)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nome", email);
                        cmd.Parameters.AddWithValue("@Email", confirmarEmail);
                        cmd.Parameters.AddWithValue("@Senha", senhaHash); 

                        int resultado = cmd.ExecuteNonQuery();
                        if (resultado > 0)
                        {
                            MessageBox.Show("Cadastro realizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                            Login loginWindow = new Login();
                            this.Close();
                            loginWindow.Show();
                        }
                        else
                        {
                            MessageBox.Show("Falha ao cadastrar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void abrirLogin(object sender, RoutedEventArgs e)
        {
            // Fechar a tela de cadastro atual
            Window cadastroWindow = Window.GetWindow(this);

            // Criar e abrir a tela de login
            Login loginWindow = new Login();
            loginWindow.Show();

            // Fechar a tela de cadastro
            cadastroWindow?.Close();
        }
        private void CadastrarButton_Click(object sender, RoutedEventArgs e)
        {
            CadastrarUsuario();
        }
    }
}
