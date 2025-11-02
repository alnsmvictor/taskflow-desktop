using System;
using System.Configuration;
using System.Windows;
using MySql.Data.MySqlClient;

namespace taskflowDesktop.Views
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void abrirCadastro(object sender, RoutedEventArgs e)
        {
            // Fechar a tela de login atual
            Window loginWindow = Window.GetWindow(this);

            // Criar e abrir a tela de cadastro
            Cadastro cadastroWindow = new Cadastro();
            cadastroWindow.Show();

            // Fechar a tela de login
            loginWindow?.Close();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string senha = SenhaPasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                MessageBox.Show("Preencha todos os campos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Buscar dados do usuário incluindo o nome
                    string query = "SELECT Senha_hash, ID_CLIENTE, Nome, Perfil_Acesso FROM CLIENTES WHERE Email=@Email LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string senhaHash = reader["Senha_hash"].ToString();
                                string nomeUsuario = reader["Nome"].ToString();
                                int idUsuario = Convert.ToInt32(reader["ID_CLIENTE"]);
                                string perfil = reader["Perfil_Acesso"].ToString();

                                // Verifica se a senha corresponde ao hash
                                if (BCrypt.Net.BCrypt.Verify(senha, senhaHash))
                                {
                                    MessageBox.Show("Login realizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                                    // Abrir a próxima tela da aplicação passando o usuário logado
                                    Chamados main = new Chamados(nomeUsuario, idUsuario, perfil);
                                    main.Show();
                                    this.Close();
                                }
                                else
                                {
                                    MessageBox.Show("Email ou senha incorretos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Email ou senha incorretos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}