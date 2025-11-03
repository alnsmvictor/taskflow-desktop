using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BCrypt.Net;

namespace taskflowDesktop.Views
{
    public partial class Usuarios : Window
    {
        private string _usuarioLogado;
        private string _perfilUsuario;
        private int _idUsuarioLogado;
        private string _termoBusca = "";
        private string _filtroPerfil = "Todos os perfis";
        private bool _mostrarApenasAtivos = false;
        private List<Usuario> _usuarios = new List<Usuario>();
        private Usuario _usuarioEmEdicao;
        private bool _modoEdicao = false;
        private bool _alterarSenha = false;

        public class Usuario
        {
            public int Id { get; set; }
            public string Nome { get; set; }
            public string Email { get; set; }
            public string PerfilAcesso { get; set; }
            public string SenhaHash { get; set; }
        }

        // Construtor sem parâmetros para o XAML
        public Usuarios()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                // Se os valores não foram definidos pelo construtor com parâmetros, use padrões
                if (string.IsNullOrEmpty(_usuarioLogado))
                    _usuarioLogado = "Usuário";
                if (string.IsNullOrEmpty(_perfilUsuario))
                    _perfilUsuario = "Usuário";
                if (_idUsuarioLogado == 0)
                    _idUsuarioLogado = 0;

                ConfigurarPermissoes();
                CarregarUsuarios();
                AtualizarEstiloTabs();
            };
        }

        // Construtor com parâmetros para ser chamado de outras telas
        public Usuarios(string usuarioLogado, int idUsuarioLogado, string perfilUsuario) : this()
        {
            _usuarioLogado = usuarioLogado;
            _idUsuarioLogado = idUsuarioLogado;
            _perfilUsuario = perfilUsuario;
        }

        private void ConfigurarPermissoes()
        {
            if (_perfilUsuario?.ToLower() != "admin")
            {
                NovoUsuarioButton.Visibility = Visibility.Collapsed;
                RemoverUsuarioButton.Visibility = Visibility.Collapsed;
            }

            Console.WriteLine($"Usuário logado: {_usuarioLogado} (Perfil: {_perfilUsuario})");
        }

        private string GerarHashSenha(string senha)
        {
            return BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 12);
        }

        private void CarregarUsuarios()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            ID_CLIENTE,
                            Nome,
                            Email,
                            Perfil_Acesso,
                            Senha_Hash
                        FROM CLIENTES
                        ORDER BY Nome";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            _usuarios.Clear();

                            while (reader.Read())
                            {
                                var usuario = new Usuario();

                                if (!reader.IsDBNull(reader.GetOrdinal("ID_CLIENTE")))
                                    usuario.Id = reader.GetInt32("ID_CLIENTE");

                                if (!reader.IsDBNull(reader.GetOrdinal("Nome")))
                                    usuario.Nome = reader.GetString("Nome");
                                else
                                    usuario.Nome = "Sem nome";

                                if (!reader.IsDBNull(reader.GetOrdinal("Email")))
                                    usuario.Email = reader.GetString("Email");
                                else
                                    usuario.Email = "Sem email";

                                if (!reader.IsDBNull(reader.GetOrdinal("Perfil_Acesso")))
                                    usuario.PerfilAcesso = reader.GetString("Perfil_Acesso");
                                else
                                    usuario.PerfilAcesso = "Usuário";

                                if (!reader.IsDBNull(reader.GetOrdinal("Senha_Hash")))
                                    usuario.SenhaHash = reader.GetString("Senha_Hash");

                                _usuarios.Add(usuario);
                            }
                        }
                    }
                }

                AplicarFiltros();

                if (_usuarios.Count == 0)
                {
                    MessageBox.Show("Nenhum usuário encontrado no banco de dados.", "Informação",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar usuários: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CriarCard(Usuario usuario)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Tag = usuario.Id
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            // CheckBox
            var checkBox = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = usuario.Id
            };
            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            // Nome
            var nomeStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) };
            var nomeText = new TextBlock
            {
                Text = usuario.Nome,
                FontWeight = FontWeights.Bold,
                FontSize = 15
            };
            var emailText = new TextBlock
            {
                Text = usuario.Email,
                FontSize = 13,
                Foreground = Brushes.Gray
            };
            nomeStack.Children.Add(nomeText);
            nomeStack.Children.Add(emailText);
            Grid.SetColumn(nomeStack, 1);
            grid.Children.Add(nomeStack);

            // Email
            var emailDetailText = new TextBlock
            {
                Text = usuario.Email,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(emailDetailText, 2);
            grid.Children.Add(emailDetailText);

            // Perfil
            var perfilBorder = CriarBadgePerfil(usuario.PerfilAcesso);
            Grid.SetColumn(perfilBorder, 3);
            grid.Children.Add(perfilBorder);

            // Status (sempre ativo para simplificar, já que não temos campo ativo)
            var statusBorder = CriarBadgeStatus(true);
            Grid.SetColumn(statusBorder, 4);
            grid.Children.Add(statusBorder);

            // Botão Editar
            var editarButton = new Button
            {
                Content = "✏️",
                Style = (Style)FindResource("MaterialDesignFlatButton"),
                ToolTip = "Editar usuário",
                Tag = usuario.Id,
                FontSize = 16
            };
            editarButton.Click += (s, e) => EditarUsuario(usuario);
            Grid.SetColumn(editarButton, 5);
            grid.Children.Add(editarButton);

            // Botão Remover
            var removerButton = new Button
            {
                Content = "🗑️",
                Style = (Style)FindResource("MaterialDesignFlatButton"),
                Foreground = Brushes.Red,
                ToolTip = "Remover usuário",
                Tag = usuario.Id,
                FontSize = 16
            };
            removerButton.Click += (s, e) => RemoverUsuario(usuario);
            Grid.SetColumn(removerButton, 6);
            grid.Children.Add(removerButton);

            card.Child = grid;
            return card;
        }

        private Border CriarBadgePerfil(string perfil)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80
            };

            var text = new TextBlock
            {
                Text = perfil,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            switch (perfil.ToLower())
            {
                case "admin":
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    border.BorderBrush = Brushes.Red;
                    text.Foreground = Brushes.Red;
                    break;
                case "técnico":
                    border.Background = new SolidColorBrush(Color.FromRgb(225, 245, 254));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 136, 209));
                    text.Foreground = new SolidColorBrush(Color.FromRgb(2, 136, 209));
                    break;
                case "usuário":
                default:
                    border.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    border.BorderBrush = Brushes.Green;
                    text.Foreground = Brushes.Green;
                    break;
            }

            border.BorderThickness = new Thickness(1);
            border.Child = text;
            return border;
        }

        private Border CriarBadgeStatus(bool ativo)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80
            };

            var text = new TextBlock
            {
                Text = "Ativo", // Sempre ativo já que não temos campo ativo
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            border.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
            border.BorderBrush = Brushes.Green;
            text.Foreground = Brushes.Green;

            border.BorderThickness = new Thickness(1);
            border.Child = text;
            return border;
        }

        private void AplicarFiltros()
        {
            try
            {
                if (_usuarios == null || !_usuarios.Any())
                    return;

                var usuariosFiltrados = _usuarios.AsEnumerable();

                // Aplica filtro de busca
                if (!string.IsNullOrWhiteSpace(_termoBusca))
                {
                    usuariosFiltrados = usuariosFiltrados.Where(u =>
                        (u.Nome?.ToLower().Contains(_termoBusca) ?? false) ||
                        (u.Email?.ToLower().Contains(_termoBusca) ?? false) ||
                        (u.PerfilAcesso?.ToLower().Contains(_termoBusca) ?? false)
                    );
                }

                // Aplica filtro de perfil
                if (_filtroPerfil != "Todos os perfis")
                {
                    usuariosFiltrados = usuariosFiltrados.Where(u =>
                        u.PerfilAcesso?.ToLower() == _filtroPerfil.ToLower()
                    );
                }

                // Cria os cards na tela
                CardsStackPanel.Children.Clear();

                foreach (var usuario in usuariosFiltrados.ToList())
                {
                    var card = CriarCard(usuario);
                    CardsStackPanel.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao aplicar filtros: {ex.Message}");
            }
        }

        private void AtualizarEstiloTabs()
        {
            // Como não temos campo ativo, as tabs são apenas visuais
            BtnTodosUsuarios.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            BtnTodosUsuarios.FontWeight = FontWeights.Bold;

            BtnUsuariosAtivos.Foreground = new SolidColorBrush(Color.FromRgb(119, 119, 119));
            BtnUsuariosAtivos.FontWeight = FontWeights.Normal;
        }

        private void NovoUsuario_Click(object sender, RoutedEventArgs e)
        {
            AbrirModalNovoUsuario();
        }

        private void AbrirModalNovoUsuario()
        {
            _modoEdicao = false;
            _usuarioEmEdicao = null;
            _alterarSenha = false;
            ModalTitulo.Text = "Novo Usuário";
            SalvarUsuarioButton.Content = "Salvar";

            // Limpa os campos
            NomeTextBox.Text = "";
            EmailTextBox.Text = "";
            PerfilComboBox.SelectedIndex = 0;
            SenhaPasswordBox.Password = "";
            ConfirmarSenhaPasswordBox.Password = "";

            // Mostra campos de senha para novo usuário
            SenhaStackPanel.Visibility = Visibility.Visible;
            ConfirmarSenhaStackPanel.Visibility = Visibility.Visible;

            // Remove o checkbox de ativo já que não temos esse campo
            AtivoCheckBox.Visibility = Visibility.Collapsed;

            // Esconde o botão de alterar senha (só aparece na edição)
            AlterarSenhaButton.Visibility = Visibility.Collapsed;

            ModalUsuario.Visibility = Visibility.Visible;
        }

        private void EditarUsuario(Usuario usuario)
        {
            if (_perfilUsuario?.ToLower() != "admin")
            {
                MessageBox.Show("Apenas administradores podem editar usuários.", "Acesso Negado",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _modoEdicao = true;
            _usuarioEmEdicao = usuario;
            _alterarSenha = false; // Inicialmente não alterar senha
            ModalTitulo.Text = "Editar Usuário";
            SalvarUsuarioButton.Content = "Atualizar";

            // Preenche os campos
            NomeTextBox.Text = usuario.Nome;
            EmailTextBox.Text = usuario.Email;

            // Configura perfil
            foreach (ComboBoxItem item in PerfilComboBox.Items)
            {
                if (item.Content.ToString() == usuario.PerfilAcesso)
                {
                    PerfilComboBox.SelectedItem = item;
                    break;
                }
            }

            // Configura campos de senha para edição
            ConfigurarCamposSenhaEdicao();

            // Remove o checkbox de ativo
            AtivoCheckBox.Visibility = Visibility.Collapsed;

            ModalUsuario.Visibility = Visibility.Visible;
        }

        private void ConfigurarCamposSenhaEdicao()
        {
            if (_alterarSenha)
            {
                // Modo alteração de senha ativado
                SenhaStackPanel.Visibility = Visibility.Visible;
                ConfirmarSenhaStackPanel.Visibility = Visibility.Visible;
                AlterarSenhaButton.Content = "Cancelar Alteração de Senha";
                SenhaPasswordBox.Password = "";
                ConfirmarSenhaPasswordBox.Password = "";
            }
            else
            {
                // Modo alteração de senha desativado
                SenhaStackPanel.Visibility = Visibility.Collapsed;
                ConfirmarSenhaStackPanel.Visibility = Visibility.Collapsed;
                AlterarSenhaButton.Content = "Alterar Senha";
                AlterarSenhaButton.Visibility = Visibility.Visible;
            }
        }

        private void AlterarSenha_Click(object sender, RoutedEventArgs e)
        {
            _alterarSenha = !_alterarSenha;
            ConfigurarCamposSenhaEdicao();
        }

        private void RemoverUsuario(Usuario usuario)
        {
            if (_perfilUsuario?.ToLower() != "admin")
            {
                MessageBox.Show("Apenas administradores podem remover usuários.", "Acesso Negado",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Não permite remover a si mesmo
            if (usuario.Id == _idUsuarioLogado)
            {
                MessageBox.Show("Você não pode remover seu próprio usuário.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"Tem certeza que deseja remover o usuário '{usuario.Nome}'?",
                "Confirmar Remoção",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                RemoverUsuarioDoBanco(usuario.Id);
            }
        }

        private void RemoverUsuario_Click(object sender, RoutedEventArgs e)
        {
            var usuariosParaRemover = ObterUsuariosSelecionados();

            if (usuariosParaRemover.Count == 0)
            {
                MessageBox.Show("Selecione pelo menos um usuário para remover.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Não permite remover a si mesmo
            if (usuariosParaRemover.Any(u => u.Id == _idUsuarioLogado))
            {
                MessageBox.Show("Você não pode remover seu próprio usuário.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"Tem certeza que deseja remover {usuariosParaRemover.Count} usuário(s)?",
                "Confirmar Remoção",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                foreach (var usuario in usuariosParaRemover)
                {
                    RemoverUsuarioDoBanco(usuario.Id);
                }
            }
        }

        private List<Usuario> ObterUsuariosSelecionados()
        {
            var selecionados = new List<Usuario>();

            foreach (var child in CardsStackPanel.Children)
            {
                if (child is Border card)
                {
                    var checkBox = EncontrarCheckBoxNoCard(card);
                    if (checkBox?.IsChecked == true)
                    {
                        var usuario = ObterUsuarioDoCard(card);
                        if (usuario != null)
                            selecionados.Add(usuario);
                    }
                }
            }

            return selecionados;
        }

        private CheckBox EncontrarCheckBoxNoCard(Border card)
        {
            if (card.Child is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is CheckBox checkBox)
                        return checkBox;
                }
            }
            return null;
        }

        private Usuario ObterUsuarioDoCard(Border card)
        {
            if (card.Tag != null && int.TryParse(card.Tag.ToString(), out int idUsuario))
            {
                return _usuarios.FirstOrDefault(u => u.Id == idUsuario);
            }
            return null;
        }

        private void RemoverUsuarioDoBanco(int idUsuario)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string deleteQuery = "DELETE FROM CLIENTES WHERE ID_CLIENTE = @IdUsuario";

                    using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        int resultado = cmd.ExecuteNonQuery();

                        if (resultado > 0)
                        {
                            MessageBox.Show("Usuário removido com sucesso!", "Sucesso",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                            CarregarUsuarios();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao remover usuário: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SalvarUsuario_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validações
                if (string.IsNullOrEmpty(NomeTextBox.Text.Trim()))
                {
                    MessageBox.Show("O nome é obrigatório.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(EmailTextBox.Text.Trim()))
                {
                    MessageBox.Show("O email é obrigatório.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string perfil = (PerfilComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Usuário";

                if (_modoEdicao)
                {
                    AtualizarUsuario(perfil);
                }
                else
                {
                    CriarNovoUsuario(perfil);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar usuário: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CriarNovoUsuario(string perfil)
        {
            // Validações específicas para novo usuário
            if (string.IsNullOrEmpty(SenhaPasswordBox.Password))
            {
                MessageBox.Show("A senha é obrigatória.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SenhaPasswordBox.Password != ConfirmarSenhaPasswordBox.Password)
            {
                MessageBox.Show("As senhas não coincidem.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SenhaPasswordBox.Password.Length < 6)
            {
                MessageBox.Show("A senha deve ter pelo menos 6 caracteres.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();

                // Verifica se email já existe
                string verificaEmailQuery = "SELECT COUNT(1) FROM CLIENTES WHERE Email = @Email";
                using (MySqlCommand cmdVerifica = new MySqlCommand(verificaEmailQuery, conn))
                {
                    cmdVerifica.Parameters.AddWithValue("@Email", EmailTextBox.Text.Trim());
                    long existe = (long)cmdVerifica.ExecuteScalar();

                    if (existe > 0)
                    {
                        MessageBox.Show("Este email já está em uso.", "Aviso",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Gera o hash da senha
                string senhaHash = GerarHashSenha(SenhaPasswordBox.Password);

                // Insere novo usuário
                string insertQuery = @"
                    INSERT INTO CLIENTES 
                    (Nome, Email, Perfil_Acesso, Senha_Hash)
                    VALUES (@Nome, @Email, @PerfilAcesso, @SenhaHash)";

                using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Nome", NomeTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@Email", EmailTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@PerfilAcesso", perfil);
                    cmd.Parameters.AddWithValue("@SenhaHash", senhaHash);

                    int resultado = cmd.ExecuteNonQuery();

                    if (resultado > 0)
                    {
                        MessageBox.Show("Usuário criado com sucesso!", "Sucesso",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        ModalUsuario.Visibility = Visibility.Collapsed;
                        CarregarUsuarios();
                    }
                }
            }
        }

        private void AtualizarUsuario(string perfil)
        {
            // Validações específicas para edição com alteração de senha
            if (_alterarSenha)
            {
                if (string.IsNullOrEmpty(SenhaPasswordBox.Password))
                {
                    MessageBox.Show("A senha é obrigatória quando alterar senha está ativado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SenhaPasswordBox.Password != ConfirmarSenhaPasswordBox.Password)
                {
                    MessageBox.Show("As senhas não coincidem.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SenhaPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("A senha deve ter pelo menos 6 caracteres.", "Aviso",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();

                // Verifica se email já existe (excluindo o usuário atual)
                string verificaEmailQuery = "SELECT COUNT(1) FROM CLIENTES WHERE Email = @Email AND ID_CLIENTE != @IdUsuario";
                using (MySqlCommand cmdVerifica = new MySqlCommand(verificaEmailQuery, conn))
                {
                    cmdVerifica.Parameters.AddWithValue("@Email", EmailTextBox.Text.Trim());
                    cmdVerifica.Parameters.AddWithValue("@IdUsuario", _usuarioEmEdicao.Id);
                    long existe = (long)cmdVerifica.ExecuteScalar();

                    if (existe > 0)
                    {
                        MessageBox.Show("Este email já está em uso.", "Aviso",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Declara a variável fora do bloco if
                string novaSenhaHash = null;

                // Atualiza usuário - com ou sem senha
                string updateQuery;
                if (_alterarSenha)
                {
                    // Gera o hash da nova senha
                    novaSenhaHash = GerarHashSenha(SenhaPasswordBox.Password);

                    updateQuery = @"
                UPDATE CLIENTES 
                SET Nome = @Nome,
                    Email = @Email,
                    Perfil_Acesso = @PerfilAcesso,
                    Senha_Hash = @SenhaHash
                WHERE ID_CLIENTE = @IdUsuario";
                }
                else
                {
                    updateQuery = @"
                UPDATE CLIENTES 
                SET Nome = @Nome,
                    Email = @Email,
                    Perfil_Acesso = @PerfilAcesso
                WHERE ID_CLIENTE = @IdUsuario";
                }

                using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Nome", NomeTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@Email", EmailTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@PerfilAcesso", perfil);
                    cmd.Parameters.AddWithValue("@IdUsuario", _usuarioEmEdicao.Id);

                    // Só adiciona o parâmetro de senha se estiver alterando
                    if (_alterarSenha)
                    {
                        cmd.Parameters.AddWithValue("@SenhaHash", novaSenhaHash);
                    }

                    int resultado = cmd.ExecuteNonQuery();

                    if (resultado > 0)
                    {
                        string mensagem = _alterarSenha ?
                            "Usuário e senha atualizados com sucesso!" :
                            "Usuário atualizado com sucesso!";

                        MessageBox.Show(mensagem, "Sucesso",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        ModalUsuario.Visibility = Visibility.Collapsed;
                        CarregarUsuarios();
                    }
                }
            }
        }
        private void FecharModal_Click(object sender, RoutedEventArgs e)
        {
            ModalUsuario.Visibility = Visibility.Collapsed;
            _usuarioEmEdicao = null;
            _modoEdicao = false;
            _alterarSenha = false;
        }

        // Event Handlers
        private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _termoBusca = BuscarTextBox.Text.Trim().ToLower();
            AplicarFiltros();
        }

        private void FiltroPerfilComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FiltroPerfilComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _filtroPerfil = selectedItem.Content.ToString();
                AplicarFiltros();
            }
        }

        private void LimparFiltros_Click(object sender, RoutedEventArgs e)
        {
            BuscarTextBox.Text = "";
            _termoBusca = "";
            FiltroPerfilComboBox.SelectedIndex = 0;
            _filtroPerfil = "Todos os perfis";
            AplicarFiltros();
        }

        private void BtnTodosUsuarios_Click(object sender, RoutedEventArgs e)
        {
            AtualizarEstiloTabs();
            AplicarFiltros();
        }

        private void BtnUsuariosAtivos_Click(object sender, RoutedEventArgs e)
        {
            // Como não temos campo ativo, esta tab não faz filtro
            AtualizarEstiloTabs();
            AplicarFiltros();
        }

        // Navegação
        private void BtnChamados_Click(object sender, RoutedEventArgs e)
        {
            var chamadosWindow = new Chamados(_usuarioLogado, _idUsuarioLogado, _perfilUsuario);
            chamadosWindow.Show();
            this.Close();
        }

        private void BtnConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de configurações em desenvolvimento.", "Info",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPerfil_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Usuário: {_usuarioLogado}\nPerfil: {_perfilUsuario}", "Perfil",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}