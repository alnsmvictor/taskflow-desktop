using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using taskflowDesktop.Models;
using static taskflowDesktop.Views.Usuarios;


namespace taskflowDesktop.Views
{
    public partial class Chamados : Window
    {
        private string _filtroStatus = "Todos";
        private string _filtroPrioridade = "Todas";
        private string _termoBusca = "";
        private string _usuarioLogado;
        private List<Chamado> _chamadosSelecionados = new List<Chamado>();
        private List<Chamado> chamados = new List<Chamado>();
        private string PrioridadeSelecionada = "Média";
        private bool _mostrarApenasMeusChamados = false;
        private string _perfilUsuario;
        private int _idUsuarioLogado;
        private Chamado _chamadoEmEdicao;
        private bool _modoEdicao = false;
        public Chamados(string usuarioLogado, int idUsuarioLogado, string perfilUsuario)
        {
            InitializeComponent();
            _usuarioLogado = usuarioLogado;
            _idUsuarioLogado = idUsuarioLogado;
            _perfilUsuario = perfilUsuario;

            // Aguarda o carregamento completo
            this.Loaded += (s, e) =>
            {
                InitializeFiltros();
                ConfigurarModal();
                ConfigurarPermissoes();
                CarregarTickets();

                // Opcional: Mostrar o nome do usuário logado em algum lugar
                Console.WriteLine($"Usuário logado: {_usuarioLogado} (ID: {_idUsuarioLogado})");
            };
        }

        private void AbrirUser(object sender, RoutedEventArgs e)
        {
            // Verifica se o usuário é admin
            if (_perfilUsuario?.ToLower() != "admin")
            {
                MessageBox.Show("Apenas administradores podem acessar a gestão de usuários.",
                              "Acesso Negado",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            // Só abre a tela se for admin
            Usuarios usuariosWindow = new Usuarios(_usuarioLogado, _idUsuarioLogado, _perfilUsuario);
            usuariosWindow.Show();
            this.Close();
        }
        private void ConfigurarPermissoes()
        {
            // Todos os perfis podem ver ambas as tabs
            BtnTodosChamados.Visibility = Visibility.Visible;
            BtnMeusChamados.Visibility = Visibility.Visible;

            // Configura visibilidade do botão de usuários na sidebar
            if (_perfilUsuario?.ToLower() != "admin")
            {
                BtnUsuariosSidebar.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnUsuariosSidebar.Visibility = Visibility.Visible;
            }

            // Não força mostrar apenas os próprios chamados para usuários comuns
            _mostrarApenasMeusChamados = false;

            // Atualiza o estilo das tabs
            AtualizarEstiloTabs();

            Console.WriteLine($"Usuário logado: {_usuarioLogado} (Perfil: {_perfilUsuario})");

            // Debug para verificar o perfil
            Console.WriteLine($"Perfil em lowercase: {_perfilUsuario?.ToLower()}");
        }
        private void InitializeFiltros()
        {
            // Configura os valores iniciais explicitamente
            FiltroStatusComboBox.SelectedValue = "Todos";
            FiltroPrioridadeComboBox.SelectedValue = "Todas";

            // Atualiza as variáveis de filtro
            _filtroStatus = "Todos";
            _filtroPrioridade = "Todas";

            // Força a renderização
            Dispatcher.BeginInvoke(new Action(() =>
            {
                FiltroStatusComboBox.ApplyTemplate();
                FiltroPrioridadeComboBox.ApplyTemplate();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        private void LimparFiltros_Click(object sender, RoutedEventArgs e)
        {
            // Limpa a busca
            BuscarTextBox.Text = "";
            _termoBusca = "";

            // Reseta os comboboxs
            FiltroStatusComboBox.SelectedValue = "Todos";
            FiltroPrioridadeComboBox.SelectedValue = "Todas";

            // Volta para "Todos os Chamados"
            _mostrarApenasMeusChamados = false;
            AtualizarEstiloTabs();

            // Aplica os filtros
            AplicarFiltros();

            MessageBox.Show("Filtros limpos!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ConfigurarModal()
        {
            // Configura valores padrão para o modal
            DataAberturaPicker.SelectedDate = DateTime.Now;
            SetorComboBox.SelectedIndex = 0;
            StatusComboBox.SelectedIndex = 0;
        }
        private void CarregarTickets()
        {
            try
            {
                Console.WriteLine("DEBUG: Iniciando CarregarTickets...");

                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
                Console.WriteLine($"DEBUG: String de conexão: {connStr}");

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    Console.WriteLine("DEBUG: Conexão com banco aberta");

                    CarregarSetores(conn);

                    string query = @"
                SELECT 
                    ID_CHAMADO,
                    Nome_Cliente,
                    Titulo,
                    Descricao,
                    ChamadoStatus,
                    Data_Abertura,
                    Data_Fechamento,
                    Prioridade,
                    ID_CLIENTE,
                    ID_SETOR,
                    Tecnico
                FROM CHAMADOS
                ORDER BY Data_Abertura DESC";

                    Console.WriteLine("DEBUG: Executando query...");
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("DEBUG: Reader executado");
                            chamados.Clear();

                            int count = 0;
                            while (reader.Read())
                            {
                                count++;
                                try
                                {
                                    var chamado = new Chamado();

                                    // DEBUG: Verificar cada campo individualmente
                                    Console.WriteLine($"DEBUG: Processando registro {count}");

                                    // ID_CHAMADO
                                    if (!reader.IsDBNull(reader.GetOrdinal("ID_CHAMADO")))
                                    {
                                        chamado.IdTicket = reader.GetInt32("ID_CHAMADO");
                                        Console.WriteLine($"DEBUG: ID_CHAMADO: {chamado.IdTicket}");
                                    }
                                    else
                                    {
                                        Console.WriteLine("DEBUG: ID_CHAMADO é NULL, pulando registro");
                                        continue;
                                    }

                                    // Titulo - DEBUG DETALHADO
                                    int tituloIndex = reader.GetOrdinal("Titulo");
                                    if (!reader.IsDBNull(tituloIndex))
                                    {
                                        chamado.Titulo = reader.GetString(tituloIndex);
                                        Console.WriteLine($"DEBUG: Titulo: '{chamado.Titulo}'");
                                    }
                                    else
                                    {
                                        chamado.Titulo = "Sem título";
                                        Console.WriteLine("DEBUG: Titulo é NULL, usando valor padrão");
                                    }

                                    // Nome_Cliente
                                    if (!reader.IsDBNull(reader.GetOrdinal("Nome_Cliente")))
                                    {
                                        chamado.NomeCliente = reader.GetString("Nome_Cliente");
                                        Console.WriteLine($"DEBUG: Nome_Cliente: '{chamado.NomeCliente}'");
                                    }
                                    else
                                    {
                                        chamado.NomeCliente = "Sem nome";
                                        Console.WriteLine("DEBUG: Nome_Cliente é NULL, usando valor padrão");
                                    }

                                    // Descricao
                                    if (!reader.IsDBNull(reader.GetOrdinal("Descricao")))
                                    {
                                        chamado.Descricao = reader.GetString("Descricao");
                                        Console.WriteLine($"DEBUG: Descricao: '{chamado.Descricao}'");
                                    }
                                    else
                                    {
                                        chamado.Descricao = "Sem descrição";
                                        Console.WriteLine("DEBUG: Descricao é NULL, usando valor padrão");
                                    }

                                    // ChamadoStatus
                                    if (!reader.IsDBNull(reader.GetOrdinal("ChamadoStatus")))
                                    {
                                        chamado.TicketStatus = reader.GetString("ChamadoStatus");
                                        Console.WriteLine($"DEBUG: ChamadoStatus: '{chamado.TicketStatus}'");
                                    }
                                    else
                                    {
                                        chamado.TicketStatus = "Aberto";
                                        Console.WriteLine("DEBUG: ChamadoStatus é NULL, usando valor padrão");
                                    }

                                    // Data_Abertura
                                    if (!reader.IsDBNull(reader.GetOrdinal("Data_Abertura")))
                                    {
                                        chamado.DataAbertura = reader.GetDateTime("Data_Abertura");
                                        Console.WriteLine($"DEBUG: Data_Abertura: {chamado.DataAbertura}");
                                    }
                                    else
                                    {
                                        chamado.DataAbertura = DateTime.Now;
                                        Console.WriteLine("DEBUG: Data_Abertura é NULL, usando valor padrão");
                                    }

                                    // Data_Fechamento (pode ser nulo)
                                    if (!reader.IsDBNull(reader.GetOrdinal("Data_Fechamento")))
                                    {
                                        chamado.DataFechamento = reader.GetDateTime("Data_Fechamento");
                                        Console.WriteLine($"DEBUG: Data_Fechamento: {chamado.DataFechamento}");
                                    }

                                    // Prioridade
                                    if (!reader.IsDBNull(reader.GetOrdinal("Prioridade")))
                                    {
                                        chamado.Prioridade = reader.GetString("Prioridade");
                                        Console.WriteLine($"DEBUG: Prioridade: '{chamado.Prioridade}'");
                                    }
                                    else
                                    {
                                        chamado.Prioridade = "Média";
                                        Console.WriteLine("DEBUG: Prioridade é NULL, usando valor padrão");
                                    }

                                    // ID_CLIENTE
                                    if (!reader.IsDBNull(reader.GetOrdinal("ID_CLIENTE")))
                                    {
                                        chamado.IdCliente = reader.GetInt32("ID_CLIENTE");
                                        Console.WriteLine($"DEBUG: ID_CLIENTE: {chamado.IdCliente}");
                                    }
                                    else
                                    {
                                        chamado.IdCliente = 0;
                                        Console.WriteLine("DEBUG: ID_CLIENTE é NULL, usando valor padrão");
                                    }

                                    // ID_SETOR
                                    if (!reader.IsDBNull(reader.GetOrdinal("ID_SETOR")))
                                    {
                                        chamado.IdSetor = reader.GetInt32("ID_SETOR");
                                        Console.WriteLine($"DEBUG: ID_SETOR: {chamado.IdSetor}");
                                    }
                                    else
                                    {
                                        chamado.IdSetor = 0;
                                        Console.WriteLine("DEBUG: ID_SETOR é NULL, usando valor padrão");
                                    }

                                    // Tecnico
                                    if (!reader.IsDBNull(reader.GetOrdinal("Tecnico")))
                                    {
                                        chamado.Tecnico = reader.GetString("Tecnico");
                                        Console.WriteLine($"DEBUG: Tecnico: '{chamado.Tecnico}'");
                                    }
                                    else
                                    {
                                        chamado.Tecnico = "A definir";
                                        Console.WriteLine("DEBUG: Tecnico é NULL, usando valor padrão");
                                    }

                                    // Campo calculado
                                    chamado.NomeSetor = ObterNomeSetor(chamado.IdSetor);
                                    Console.WriteLine($"DEBUG: NomeSetor: '{chamado.NomeSetor}'");

                                    chamados.Add(chamado);
                                    Console.WriteLine($"DEBUG: Chamado {count} adicionado à lista");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"DEBUG: ERRO ao processar registro {count}: {ex.Message}");
                                    Console.WriteLine($"DEBUG: StackTrace: {ex.StackTrace}");
                                    continue;
                                }
                            }

                            Console.WriteLine($"DEBUG: Total de chamados carregados: {chamados.Count}");
                        }
                    }
                }

                Console.WriteLine("DEBUG: Chamando AplicarFiltros...");
                AtualizarEstiloTabs();
                AplicarFiltros();

                if (chamados.Count == 0)
                {
                    Console.WriteLine("DEBUG: Nenhum chamado encontrado no banco");
                    MessageBox.Show("Nenhum chamado encontrado no banco de dados.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Console.WriteLine($"DEBUG: {chamados.Count} chamados processados com sucesso");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: ERRO em CarregarTickets: {ex.Message}");
                Console.WriteLine($"DEBUG: StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Erro ao carregar chamados: {ex.Message}\n\nDetalhes: {ex.StackTrace}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CarregarSetores(MySqlConnection conn)
        {
            try
            {
                SetorComboBox.Items.Clear();
                DetalhesSetorComboBox.Items.Clear();

                string query = "SELECT ID_SETOR, Nome FROM SETORES ORDER BY Nome";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int idSetor = reader.GetInt32("ID_SETOR");
                            string nomeSetor = reader.GetString("Nome");

                            // Garante que o Tag está sendo setado corretamente
                            var itemNovo = new ComboBoxItem
                            {
                                Content = nomeSetor,
                                Tag = idSetor // IMPORTANTE: Tag com o ID correto
                            };
                            SetorComboBox.Items.Add(itemNovo);

                            var itemDetalhes = new ComboBoxItem
                            {
                                Content = nomeSetor,
                                Tag = idSetor // IMPORTANTE: Tag com o ID correto
                            };
                            DetalhesSetorComboBox.Items.Add(itemDetalhes);
                        }
                    }
                }

                // Seleciona o primeiro item se existir
                if (SetorComboBox.Items.Count > 0)
                    SetorComboBox.SelectedIndex = 0;

                if (DetalhesSetorComboBox.Items.Count > 0)
                    DetalhesSetorComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar setores: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AdicionarSetoresPadrao();
            }
        }
        private void AdicionarSetoresPadrao()
        {
            string[] setoresPadrao = { "Suporte", "Financeiro", "TI" };

            foreach (string setor in setoresPadrao)
            {
                SetorComboBox.Items.Add(new ComboBoxItem { Content = setor });
                DetalhesSetorComboBox.Items.Add(new ComboBoxItem { Content = setor });
            }

            if (SetorComboBox.Items.Count > 0)
                SetorComboBox.SelectedIndex = 0;

            if (DetalhesSetorComboBox.Items.Count > 0)
                DetalhesSetorComboBox.SelectedIndex = 0;
        }
        private string ObterNomeSetor(int idSetor)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT Nome FROM SETORES WHERE ID_SETOR = @IdSetor";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdSetor", idSetor);
                        var result = cmd.ExecuteScalar();
                        return result?.ToString() ?? $"Setor {idSetor}";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter nome do setor: {ex.Message}");
                return $"Setor {idSetor}";
            }
        }
        private int ObterIdSetor(string nomeSetor)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT ID_SETOR FROM SETORES WHERE Nome = @NomeSetor";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@NomeSetor", nomeSetor);
                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                    }
                }

                // Fallback: retorna o primeiro setor disponível
                return ObterPrimeiroSetorDisponivel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter ID do setor: {ex.Message}");
                return ObterPrimeiroSetorDisponivel();
            }
        }
        private int ObterPrimeiroSetorDisponivel()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT ID_SETOR FROM SETORES ORDER BY ID_SETOR LIMIT 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 1;
                    }
                }
            }
            catch
            {
                return 1;
            }
        }
        private Border CriarCard(Chamado chamado)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Height = 70,
                Tag = chamado.IdTicket
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            // CheckBox
            var checkBox = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = chamado.IdTicket
            }; Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            // Descrição
            var descricaoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) };
            var tituloText = new TextBlock
            {
                Text = chamado.Titulo.Length > 50 ? chamado.Titulo.Substring(0, 50) + "..." : chamado.Titulo,
                FontWeight = FontWeights.Bold,
                FontSize = 15
            };
            var setorText = new TextBlock
            {
                Text = chamado.NomeSetor,
                FontSize = 13,
                Foreground = Brushes.Gray
            };
            descricaoStack.Children.Add(tituloText);
            descricaoStack.Children.Add(setorText);
            Grid.SetColumn(descricaoStack, 1);
            grid.Children.Add(descricaoStack);

            // Aberto por
            var abertoPorText = new TextBlock
            {
                Text = chamado.NomeCliente,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(abertoPorText, 2);
            grid.Children.Add(abertoPorText);

            // Prioridade
            var prioridadeBorder = CriarBadgePrioridade(chamado.Prioridade);
            Grid.SetColumn(prioridadeBorder, 3);
            grid.Children.Add(prioridadeBorder);

            // Status
            var statusBorder = CriarBadgeStatus(chamado.TicketStatus);
            Grid.SetColumn(statusBorder, 4);
            grid.Children.Add(statusBorder);

            // Responsável
            var responsavelText = new TextBlock
            {
                Text = chamado.Tecnico,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(responsavelText, 5);
            grid.Children.Add(responsavelText);

            // Botão Expandir
            var expandButton = new Button
            {
                Content = new TextBlock { Text = "↗", FontSize = 16 },
                Style = (Style)FindResource("MaterialDesignFlatButton"),
                Foreground = Brushes.Gray,
                ToolTip = "Expandir",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            expandButton.Click += (s, e) => ExpandirTicket(chamado);
            Grid.SetColumn(expandButton, 6);
            grid.Children.Add(expandButton);

            card.Child = grid;
            return card;
        }
        private Border CriarBadgePrioridade(string prioridade)
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
                Text = prioridade,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            switch (prioridade.ToLower())
            {
                case "alta":
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    border.BorderBrush = Brushes.Red;
                    text.Foreground = Brushes.Red;
                    break;
                case "média":
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 248, 225));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(251, 140, 0));
                    text.Foreground = new SolidColorBrush(Color.FromRgb(251, 140, 0));
                    break;
                case "baixa":
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
        private Border CriarBadgeStatus(string status)
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
                Text = status,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            switch (status.ToLower())
            {
                case "aberto":
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(251, 140, 0));
                    text.Foreground = new SolidColorBrush(Color.FromRgb(251, 140, 0));
                    break;
                case "em andamento":
                    border.Background = new SolidColorBrush(Color.FromRgb(225, 245, 254));
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 136, 209));
                    text.Foreground = new SolidColorBrush(Color.FromRgb(2, 136, 209));
                    break;
                case "fechado":
                case "concluído":
                    border.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    border.BorderBrush = Brushes.Green;
                    text.Foreground = Brushes.Green;
                    break;
                default:
                    border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    border.BorderBrush = Brushes.Gray;
                    text.Foreground = Brushes.Gray;
                    break;
            }

            border.BorderThickness = new Thickness(1);
            border.Child = text;
            return border;
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            RemoverChamadosSelecionados();
        }
        private void RemoverChamadosSelecionados()
        {
            try
            {
                // Obter chamados selecionados
                var chamadosParaRemover = ObterChamadosSelecionados();

                if (chamadosParaRemover.Count == 0)
                {
                    MessageBox.Show("Selecione pelo menos um chamado para remover.", "Aviso",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirmar exclusão
                string mensagem = chamadosParaRemover.Count == 1
                    ? $"Tem certeza que deseja remover o chamado '{chamadosParaRemover[0].Titulo}'?"
                    : $"Tem certeza que deseja remover {chamadosParaRemover.Count} chamados?";

                var resultado = MessageBox.Show(mensagem, "Confirmar Exclusão",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    RemoverChamadosDoBanco(chamadosParaRemover);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao remover chamados: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private List<Chamado> ObterChamadosSelecionados()
        {
            var selecionados = new List<Chamado>();

            foreach (var child in CardsStackPanel.Children)
            {
                if (child is Border card)
                {
                    var checkBox = EncontrarCheckBoxNoCard(card);
                    if (checkBox?.IsChecked == true)
                    {
                        var chamado = ObterChamadoDoCard(card);
                        if (chamado != null)
                            selecionados.Add(chamado);
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
        private Chamado ObterChamadoDoCard(Border card)
        {
            if (card.Child is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is StackPanel stackPanel && stackPanel.Children.Count > 0)
                    {
                        if (stackPanel.Children[0] is TextBlock tituloText)
                        {
                            string titulo = tituloText.Text;
                            // Remove "..." se existir
                            if (titulo.EndsWith("..."))
                                titulo = titulo.Substring(0, titulo.Length - 3);

                            return chamados.FirstOrDefault(c =>
                                c.Titulo.StartsWith(titulo) ||
                                titulo.StartsWith(c.Titulo));
                        }
                    }
                }
            }
            return null;
        }
        private void RemoverChamadosDoBanco(List<Chamado> chamadosParaRemover)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    foreach (var chamado in chamadosParaRemover)
                    {
                        string deleteQuery = "DELETE FROM CHAMADOS WHERE ID_CHAMADO = @IdChamado";

                        using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@IdChamado", chamado.IdTicket);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show($"{chamadosParaRemover.Count} chamado(s) removido(s) com sucesso!", "Sucesso",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // Recarrega a lista
                CarregarTickets();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao remover chamados do banco: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void NovoChamado_Click(object sender, RoutedEventArgs e)
        {
            // Limpa os campos ao abrir o modal
            TituloTextBox.Text = "";
            DescricaoTextBox.Text = "";
            SetorComboBox.SelectedIndex = 0;
            StatusComboBox.SelectedIndex = 0;
            DataAberturaPicker.SelectedDate = DateTime.Now;
            DataFechamentoPicker.SelectedDate = null;

            // Reseta prioridade para padrão
            PrioridadeSelecionada = "Média";
            ConfigurarBotoesPrioridade();

            ModalNovoChamado.Visibility = Visibility.Visible;
        }
        private void ConfigurarBotoesPrioridade()
        {
            // Encontra os botões de prioridade no modal
            var toggleButtons = FindVisualChildren<ToggleButton>(ModalNovoChamado);

            foreach (var button in toggleButtons)
            {
                if (button.Content?.ToString() == "Baixa" ||
                    button.Content?.ToString() == "Média" ||
                    button.Content?.ToString() == "Alta")
                {
                    button.Click += (s, e) =>
                    {
                        // Desmarca todos os outros botões
                        foreach (var btn in toggleButtons)
                        {
                            if (btn != button && (btn.Content?.ToString() == "Baixa" ||
                                btn.Content?.ToString() == "Média" ||
                                btn.Content?.ToString() == "Alta"))
                            {
                                btn.IsChecked = false;
                            }
                        }

                        PrioridadeSelecionada = button.Content.ToString();
                        button.IsChecked = true;
                    };

                    // Define "Média" como padrão
                    if (button.Content?.ToString() == "Média")
                    {
                        button.IsChecked = true;
                    }
                }
            }
        }
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        private void FecharModal_Click(object sender, RoutedEventArgs e)
        {
            ModalNovoChamado.Visibility = Visibility.Collapsed;
        }
        private void SalvarChamado_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string titulo = TituloTextBox.Text.Trim();
                string descricao = DescricaoTextBox.Text.Trim();

                // Obtém o setor selecionado do ComboBox
                if (SetorComboBox.SelectedItem is not ComboBoxItem selectedSetor)
                {
                    MessageBox.Show("Selecione um setor.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string nomeSetor = selectedSetor.Content.ToString();
                int idSetor = selectedSetor.Tag != null ? (int)selectedSetor.Tag : ObterIdSetor(nomeSetor);

                string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string nomeCliente = _usuarioLogado;

                DateTime dataAbertura = DataAberturaPicker.SelectedDate ?? DateTime.Now;
                DateTime? dataFechamento = DataFechamentoPicker.SelectedDate;

                // Validações
                if (string.IsNullOrEmpty(titulo))
                {
                    MessageBox.Show("O título é obrigatório.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Se for técnico ou admin, pode atribuir a si mesmo como técnico
                    string tecnico = "A definir";
                    if (_perfilUsuario.ToLower() == "técnico" || _perfilUsuario.ToLower() == "admin")
                    {
                        tecnico = _usuarioLogado;
                    }

                    // Insere o chamado
                    string insertQuery = @"
                INSERT INTO CHAMADOS 
                (Titulo, Nome_Cliente, Descricao, ChamadoStatus, Data_Abertura, Data_Fechamento, Prioridade, ID_CLIENTE, ID_SETOR, Tecnico)
                VALUES (@Titulo, @NomeCliente, @Descricao, @ChamadoStatus, @DataAbertura, @DataFechamento, @Prioridade, @IdCliente, @IdSetor, @Tecnico)";

                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Titulo", titulo);
                        cmd.Parameters.AddWithValue("@NomeCliente", nomeCliente);
                        cmd.Parameters.AddWithValue("@Descricao", string.IsNullOrEmpty(descricao) ? "Sem descrição" : descricao);
                        cmd.Parameters.AddWithValue("@ChamadoStatus", status ?? "Aberto");
                        cmd.Parameters.AddWithValue("@DataAbertura", dataAbertura);
                        cmd.Parameters.AddWithValue("@DataFechamento", dataFechamento.HasValue ? (object)dataFechamento.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Prioridade", PrioridadeSelecionada ?? "Média");
                        cmd.Parameters.AddWithValue("@IdCliente", _idUsuarioLogado);
                        cmd.Parameters.AddWithValue("@IdSetor", idSetor);
                        cmd.Parameters.AddWithValue("@Tecnico", tecnico);

                        int resultado = cmd.ExecuteNonQuery();

                        if (resultado > 0)
                        {
                            MessageBox.Show("Chamado salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                            ModalNovoChamado.Visibility = Visibility.Collapsed;
                            CarregarTickets();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar chamado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string ObterPrioridadeSelecionada()
        {
            if (DetalhesPrioridadeAlta.IsChecked == true)
                return "Alta";
            else if (DetalhesPrioridadeBaixa.IsChecked == true)
                return "Baixa";
            else
                return "Média"; // Padrão
        }
        private void FecharModalDetalhes_Click(object sender, RoutedEventArgs e)
        {
            ModalDetalhesChamado.Visibility = Visibility.Collapsed;
            _chamadoEmEdicao = null;
            _modoEdicao = false;
        }
        private void SalvarEdicaoChamado_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validações básicas
                if (string.IsNullOrEmpty(DetalhesTituloTextBox.Text.Trim()))
                {
                    MessageBox.Show("O título é obrigatório.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Obtém o setor selecionado
                if (DetalhesSetorComboBox.SelectedItem is not ComboBoxItem selectedSetor)
                {
                    MessageBox.Show("Selecione um setor válido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string nomeSetor = selectedSetor.Content.ToString();
                int idSetor = selectedSetor.Tag != null ? (int)selectedSetor.Tag : ObterIdSetor(nomeSetor);

                // VALIDAÇÃO CRÍTICA: Verifica se o setor existe
                if (!SetorExiste(idSetor))
                {
                    MessageBox.Show("Setor selecionado não existe. Por favor, selecione um setor válido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Resto do código de atualização...
                string titulo = DetalhesTituloTextBox.Text.Trim();
                string descricao = DetalhesDescricaoTextBox.Text.Trim();
                string status = (DetalhesStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string prioridade = ObterPrioridadeSelecionada();
                DateTime? dataFechamento = DetalhesDataFechamentoPicker.SelectedDate;

                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string updateQuery = @"
                UPDATE CHAMADOS 
                SET Titulo = @Titulo,
                    Descricao = @Descricao,
                    ChamadoStatus = @ChamadoStatus,
                    Data_Fechamento = @DataFechamento,
                    Prioridade = @Prioridade,
                    ID_SETOR = @IdSetor
                WHERE ID_CHAMADO = @IdChamado";

                    using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Titulo", titulo);
                        cmd.Parameters.AddWithValue("@Descricao", descricao);
                        cmd.Parameters.AddWithValue("@ChamadoStatus", status);
                        cmd.Parameters.AddWithValue("@DataFechamento", dataFechamento.HasValue ? (object)dataFechamento.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Prioridade", prioridade);
                        cmd.Parameters.AddWithValue("@IdSetor", idSetor);
                        cmd.Parameters.AddWithValue("@IdChamado", _chamadoEmEdicao.IdTicket);

                        int resultado = cmd.ExecuteNonQuery();

                        if (resultado > 0)
                        {
                            MessageBox.Show("Chamado atualizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                            ModalDetalhesChamado.Visibility = Visibility.Collapsed;
                            CarregarTickets();
                        }
                        else
                        {
                            MessageBox.Show("Nenhum chamado foi atualizado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar chamado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool SetorExiste(int idSetor)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(1) FROM SETORES WHERE ID_SETOR = @IdSetor";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdSetor", idSetor);
                        long count = (long)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        private void ExpandirTicket(Chamado chamado)
        {
            // Verifica se o usuário tem permissão para editar este chamado
            bool podeEditar = VerificarPermissaoEdicao(chamado);

            // Abre o modal de detalhes
            AbrirModalDetalhes(chamado, podeEditar);
        }
        private bool VerificarPermissaoEdicao(Chamado chamado)
        {
            if (chamado == null) return false;

            string perfil = _perfilUsuario?.ToLower() ?? "usuário";

            Console.WriteLine($"DEBUG Permissão: Perfil={perfil}, IdClienteChamado={chamado.IdCliente}, IdUsuarioLogado={_idUsuarioLogado}");

            switch (perfil)
            {
                case "admin":
                case "técnico":
                    Console.WriteLine("DEBUG: Permissão concedida - Admin/Técnico");
                    return true;

                case "usuário":
                    bool podeEditar = chamado.IdCliente == _idUsuarioLogado;
                    Console.WriteLine($"DEBUG: Usuário comum - Pode editar: {podeEditar}");
                    return podeEditar;

                default:
                    Console.WriteLine("DEBUG: Permissão negada - Perfil desconhecido");
                    return false;
            }
        }
        private void AbrirModalDetalhes(Chamado chamado, bool podeEditar = false)
        {
            try
            {
                _chamadoEmEdicao = chamado;
                _modoEdicao = false;

                // Preenche os campos com os dados do chamado
                DetalhesTituloTextBox.Text = chamado.Titulo;
                DetalhesDescricaoTextBox.Text = chamado.Descricao;
                DetalhesIdChamadoTextBox.Text = chamado.IdTicket.ToString();
                DetalhesClienteTextBox.Text = chamado.NomeCliente;
                DetalhesTecnicoTextBox.Text = chamado.Tecnico;
                DetalhesDataAberturaPicker.SelectedDate = chamado.DataAbertura;

                // Data de fechamento (pode ser nula)
                if (chamado.DataFechamento.HasValue)
                {
                    DetalhesDataFechamentoPicker.SelectedDate = chamado.DataFechamento.Value;
                }
                else
                {
                    DetalhesDataFechamentoPicker.SelectedDate = null;
                }

                // Configura o setor
                ConfigurarSetorDetalhes(chamado.IdSetor);

                // Configura o status
                ConfigurarStatusDetalhes(chamado.TicketStatus);

                // Configura a prioridade
                ConfigurarPrioridadeDetalhes(chamado.Prioridade);

                // Configura permissões iniciais (modo visualização)
                ConfigurarModoVisualizacao(podeEditar);

                // Abre o modal
                ModalDetalhesChamado.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar detalhes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ConfigurarModoEdicao()
        {
            _modoEdicao = true;

            // Atualiza título do modal
            DetalhesTituloModal.Text = "Editando Chamado";

            // Configura campos como editáveis
            DetalhesTituloTextBox.IsReadOnly = false;
            DetalhesTituloTextBox.Background = Brushes.White;
            DetalhesDescricaoTextBox.IsReadOnly = false;
            DetalhesDescricaoTextBox.Background = Brushes.White;
            DetalhesSetorComboBox.IsEnabled = true;
            DetalhesStatusComboBox.IsEnabled = true;
            DetalhesDataFechamentoPicker.IsEnabled = true;

            // Habilita prioridades
            DetalhesPrioridadeBaixa.IsEnabled = true;
            DetalhesPrioridadeMedia.IsEnabled = true;
            DetalhesPrioridadeAlta.IsEnabled = true;

            // Configura visibilidade dos botões
            DetalhesEditarButton.Visibility = Visibility.Collapsed;
            DetalhesSalvarButton.Visibility = Visibility.Visible;
            DetalhesCancelarButton.Visibility = Visibility.Visible;
            DetalhesFecharButton.Visibility = Visibility.Collapsed;
        }
        private void ConfigurarModoVisualizacao(bool podeEditar)
        {
            _modoEdicao = false;

            // Atualiza título do modal
            DetalhesTituloModal.Text = "Detalhes do Chamado";

            // Configura campos como somente leitura
            DetalhesTituloTextBox.IsReadOnly = true;
            DetalhesTituloTextBox.Background = Brushes.LightGray;
            DetalhesDescricaoTextBox.IsReadOnly = true;
            DetalhesDescricaoTextBox.Background = Brushes.LightGray;
            DetalhesSetorComboBox.IsEnabled = false;
            DetalhesStatusComboBox.IsEnabled = false;
            DetalhesDataFechamentoPicker.IsEnabled = false;

            // Habilita/desabilita prioridades
            DetalhesPrioridadeBaixa.IsEnabled = false;
            DetalhesPrioridadeMedia.IsEnabled = false;
            DetalhesPrioridadeAlta.IsEnabled = false;

            // Configura visibilidade dos botões
            DetalhesEditarButton.Visibility = podeEditar ? Visibility.Visible : Visibility.Collapsed;
            DetalhesSalvarButton.Visibility = Visibility.Collapsed;
            DetalhesCancelarButton.Visibility = Visibility.Collapsed;
            DetalhesFecharButton.Visibility = Visibility.Visible;
        }
        private void ConfigurarSetorDetalhes(int idSetor)
        {
            // Procura o setor pelo ID nos itens do ComboBox
            foreach (ComboBoxItem item in DetalhesSetorComboBox.Items)
            {
                if (item.Tag != null && (int)item.Tag == idSetor)
                {
                    DetalhesSetorComboBox.SelectedItem = item;
                    return;
                }
            }

            // Fallback: procura pelo nome (caso o Tag não esteja disponível)
            string nomeSetor = ObterNomeSetor(idSetor);
            foreach (ComboBoxItem item in DetalhesSetorComboBox.Items)
            {
                if (item.Content.ToString() == nomeSetor)
                {
                    DetalhesSetorComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        private void ConfigurarStatusDetalhes(string status)
        {
            foreach (ComboBoxItem item in DetalhesStatusComboBox.Items)
            {
                if (item.Content.ToString() == status)
                {
                    DetalhesStatusComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        private void ConfigurarPrioridadeDetalhes(string prioridade)
        {
            switch (prioridade.ToLower())
            {
                case "alta":
                    DetalhesPrioridadeAlta.IsChecked = true;
                    break;
                case "baixa":
                    DetalhesPrioridadeBaixa.IsChecked = true;
                    break;
                default:
                    DetalhesPrioridadeMedia.IsChecked = true;
                    break;
            }
        }
        private void EditarChamado_Click(object sender, RoutedEventArgs e)
        {
            ConfigurarModoEdicao();
        }
        private void CancelarEdicaoChamado_Click(object sender, RoutedEventArgs e)
        {
            // Recarrega os dados originais do chamado
            AbrirModalDetalhes(_chamadoEmEdicao, VerificarPermissaoEdicao(_chamadoEmEdicao));
        }
        private void CriarSetoresPadrao(MySqlConnection conn)
        {
            string[] setores = { "Suporte", "Financeiro", "TI" };

            for (int i = 0; i < setores.Length; i++)
            {
                string insertSetor = "INSERT IGNORE INTO SETORES (ID_SETOR, Nome) VALUES (@Id, @Nome)";
                using (MySqlCommand cmd = new MySqlCommand(insertSetor, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", i + 1);
                    cmd.Parameters.AddWithValue("@Nome", setores[i]);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _termoBusca = BuscarTextBox.Text.Trim().ToLower();
            AplicarFiltros();
        }
        private void FiltroStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FiltroStatusComboBox.SelectedValue != null)
            {
                _filtroStatus = FiltroStatusComboBox.SelectedValue.ToString();
                AplicarFiltros();
            }
        }
        private void FiltroPrioridadeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FiltroPrioridadeComboBox.SelectedValue != null)
            {
                _filtroPrioridade = FiltroPrioridadeComboBox.SelectedValue.ToString();
                AplicarFiltros();
            }
        }
        private void BtnTodosChamados_Click(object sender, RoutedEventArgs e)
        {
            _mostrarApenasMeusChamados = false;
            AtualizarEstiloTabs();
            AplicarFiltros();
        }
        private void BtnMeusChamados_Click(object sender, RoutedEventArgs e)
        {
            _mostrarApenasMeusChamados = true;
            AtualizarEstiloTabs();
            AplicarFiltros();
        }
        private void AtualizarEstiloTabs()
        {
            if (_mostrarApenasMeusChamados)
            {
                // Meus Chamados ativo
                BtnMeusChamados.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                BtnMeusChamados.FontWeight = FontWeights.Bold;

                BtnTodosChamados.Foreground = new SolidColorBrush(Color.FromRgb(119, 119, 119));
                BtnTodosChamados.FontWeight = FontWeights.Normal;
            }
            else
            {
                // Todos os Chamados ativo
                BtnTodosChamados.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                BtnTodosChamados.FontWeight = FontWeights.Bold;

                BtnMeusChamados.Foreground = new SolidColorBrush(Color.FromRgb(119, 119, 119));
                BtnMeusChamados.FontWeight = FontWeights.Normal;
            }
        }
        private void AplicarFiltros()
        {
            try
            {
                // Verifica se há chamados para filtrar
                if (chamados == null || !chamados.Any())
                    return;

                var chamadosFiltrados = chamados.AsEnumerable();

                // Aplica filtro de "Meus Chamados" - agora é opcional para todos
                if (_mostrarApenasMeusChamados)
                {
                    // Todos os perfis podem usar o filtro "Meus Chamados"
                    chamadosFiltrados = chamadosFiltrados.Where(c =>
                        c.Tecnico?.ToLower() == _usuarioLogado.ToLower() ||
                        c.IdCliente == _idUsuarioLogado
                    );
                }

                // Aplica filtro de busca
                if (!string.IsNullOrWhiteSpace(_termoBusca))
                {
                    chamadosFiltrados = chamadosFiltrados.Where(c =>
                        (c.Titulo?.ToLower().Contains(_termoBusca) ?? false) ||
                        (c.Descricao?.ToLower().Contains(_termoBusca) ?? false) ||
                        (c.NomeCliente?.ToLower().Contains(_termoBusca) ?? false) ||
                        (c.TicketStatus?.ToLower().Contains(_termoBusca) ?? false) ||
                        (c.Prioridade?.ToLower().Contains(_termoBusca) ?? false) ||
                        (c.NomeSetor?.ToLower().Contains(_termoBusca) ?? false) ||
                        (c.Tecnico?.ToLower().Contains(_termoBusca) ?? false)
                    );
                }

                // Aplica filtro de status
                if (_filtroStatus != "Todos")
                {
                    chamadosFiltrados = chamadosFiltrados.Where(c =>
                        (_filtroStatus == "Aberto" && c.TicketStatus?.ToLower() == "aberto") ||
                        (_filtroStatus == "Em andamento" && c.TicketStatus?.ToLower() == "em andamento") ||
                        (_filtroStatus == "Fechado" && c.TicketStatus?.ToLower() == "fechado") ||
                        (_filtroStatus == "Concluído" && c.TicketStatus?.ToLower() == "concluído")
                    );
                }

                // Aplica filtro de prioridade
                if (_filtroPrioridade != "Todas")
                {
                    chamadosFiltrados = chamadosFiltrados.Where(c =>
                        (_filtroPrioridade == "Baixa" && c.Prioridade?.ToLower() == "baixa") ||
                        (_filtroPrioridade == "Média" && c.Prioridade?.ToLower() == "média") ||
                        (_filtroPrioridade == "Alta" && c.Prioridade?.ToLower() == "alta")
                    );
                }

                // Cria os cards na tela
                CardsStackPanel.Children.Clear();

                foreach (var chamado in chamadosFiltrados.ToList())
                {
                    var card = CriarCard(chamado);
                    CardsStackPanel.Children.Add(card);
                }

                // Atualiza contador
                AtualizarContadorChamados(chamadosFiltrados.Count());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao aplicar filtros: {ex.Message}");
            }
        }        // Método opcional para mostrar contador
        private void AtualizarContadorChamados(int quantidade)
        {
            string modo = _mostrarApenasMeusChamados ? "Meus Chamados" : "Todos os Chamados";
            // Você pode exibir isso em um TextBlock se quiser
            Console.WriteLine($"{modo}: {quantidade} chamados");
        }
    }
}