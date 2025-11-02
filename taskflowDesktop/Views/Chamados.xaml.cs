using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using taskflowDesktop.Models;
using System.Windows.Controls.Primitives;


namespace taskflowDesktop.Views
{
    public partial class Chamados : Window
    {
        private List<Chamado> _chamadosSelecionados = new List<Chamado>();
        private List<Chamado> chamados = new List<Chamado>();
        private string PrioridadeSelecionada = "Média"; // Valor padrão

        public Chamados()
        {
            InitializeComponent();
            CarregarTickets();
            ConfigurarModal();
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
                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

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

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            chamados.Clear();

                            while (reader.Read())
                            {
                                try
                                {
                                    var chamado = new Chamado();

                                    // ID_CHAMADO - deve ser obrigatório
                                    if (!reader.IsDBNull(reader.GetOrdinal("ID_CHAMADO")))
                                        chamado.IdTicket = reader.GetInt32("ID_CHAMADO");
                                    else
                                        continue; // Pula registros sem ID

                                    // Titulo
                                    if (!reader.IsDBNull(reader.GetOrdinal("Titulo")))
                                        chamado.Titulo = reader.GetString("Titulo");
                                    else
                                        chamado.Titulo = "Sem título";

                                    if (!reader.IsDBNull(reader.GetOrdinal("Nome_Cliente")))
                                        chamado.NomeCliente = reader.GetString("Nome_Cliente");
                                    else
                                        chamado.NomeCliente = "Sem título";

                                    // Descricao
                                    if (!reader.IsDBNull(reader.GetOrdinal("Descricao")))
                                        chamado.Descricao = reader.GetString("Descricao");
                                    else
                                        chamado.Descricao = "Sem descrição";

                                    // ChamadoStatus
                                    if (!reader.IsDBNull(reader.GetOrdinal("ChamadoStatus")))
                                        chamado.TicketStatus = reader.GetString("ChamadoStatus");
                                    else
                                        chamado.TicketStatus = "Aberto";

                                    // Data_Abertura
                                    if (!reader.IsDBNull(reader.GetOrdinal("Data_Abertura")))
                                        chamado.DataAbertura = reader.GetDateTime("Data_Abertura");
                                    else
                                        chamado.DataAbertura = DateTime.Now;

                                    // Data_Fechamento (pode ser nulo)
                                    if (!reader.IsDBNull(reader.GetOrdinal("Data_Fechamento")))
                                        chamado.DataFechamento = reader.GetDateTime("Data_Fechamento");

                                    // Prioridade
                                    if (!reader.IsDBNull(reader.GetOrdinal("Prioridade")))
                                        chamado.Prioridade = reader.GetString("Prioridade");
                                    else
                                        chamado.Prioridade = "Média";

                                    // ID_CLIENTE
                                    if (!reader.IsDBNull(reader.GetOrdinal("ID_CLIENTE")))
                                        chamado.IdCliente = reader.GetInt32("ID_CLIENTE");
                                    else
                                        chamado.IdCliente = 0;

                                    // ID_SETOR
                                    if (!reader.IsDBNull(reader.GetOrdinal("ID_SETOR")))
                                        chamado.IdSetor = reader.GetInt32("ID_SETOR");
                                    else
                                        chamado.IdSetor = 0;

                                    // Tecnico
                                    if (!reader.IsDBNull(reader.GetOrdinal("Tecnico")))
                                        chamado.Tecnico = reader.GetString("Tecnico");
                                    else
                                        chamado.Tecnico = "A definir";

                                    // Campos calculados
                                    //chamado.NomeCliente = $"Cliente {chamado.IdCliente}";
                                    chamado.NomeSetor = ObterNomeSetor(chamado.IdSetor);

                                    chamados.Add(chamado);
                                }
                                catch (Exception ex)
                                {
                                    // Log do erro mas continua processando outros registros
                                    Console.WriteLine($"Erro ao processar registro: {ex.Message}");
                                    continue;
                                }
                            }
                        }
                    }
                }

                PreencherCards();

                if (chamados.Count == 0)
                {
                    MessageBox.Show("Nenhum chamado encontrado no banco de dados.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar chamados: {ex.Message}\n\nDetalhes: {ex.StackTrace}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ObterNomeSetor(int idSetor)
        {
            return idSetor switch
            {
                1 => "Suporte",
                2 => "Financeiro",
                3 => "TI",
                _ => $"Setor {idSetor}"
            };
        }

        private int ObterIdSetor(string nomeSetor)
        {
            return nomeSetor.ToLower() switch
            {
                "suporte" => 1,
                "financeiro" => 2,
                "ti" => 3,
                _ => 1 // Default para Suporte
            };
        }

        private void PreencherCards()
        {
            CardsStackPanel.Children.Clear();

            foreach (var chamado in chamados)
            {
                var card = CriarCard(chamado);
                CardsStackPanel.Children.Add(card);
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

        // Método para selecionar/deselecionar todos (opcional - adicione um CheckBox no cabeçalho)
        private void CheckBoxSelecionarTodos_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                bool isChecked = checkBox.IsChecked == true;

                foreach (var child in CardsStackPanel.Children)
                {
                    if (child is Border card)
                    {
                        var cardCheckBox = EncontrarCheckBoxNoCard(card);
                        if (cardCheckBox != null)
                            cardCheckBox.IsChecked = isChecked;
                    }
                }
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

        // Método auxiliar para encontrar controles filhos
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
                string setor = (SetorComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string nomeClienteFixo = "Murilo Coelho";
                DateTime dataAbertura = DataAberturaPicker.SelectedDate ?? DateTime.Now;
                DateTime? dataFechamento = DataFechamentoPicker.SelectedDate;

                // Validações
                if (string.IsNullOrEmpty(titulo))
                {
                    MessageBox.Show("O título é obrigatório.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SetorComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Selecione um setor.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string connStr = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Verifica e cria dados necessários se não existirem
                    VerificarECriarDadosNecessarios(conn);

                    // Insere o chamado
                    string insertQuery = @"
                    INSERT INTO CHAMADOS 
                    (Titulo, Nome_Cliente, Descricao, ChamadoStatus, Data_Abertura, Data_Fechamento, Prioridade, ID_CLIENTE, ID_SETOR)
                    VALUES (@Titulo, @NomeCliente, @Descricao, @ChamadoStatus, @DataAbertura, @DataFechamento, @Prioridade, @IdCliente, @IdSetor)";

                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Titulo", titulo);
                        cmd.Parameters.AddWithValue("@NomeCliente", nomeClienteFixo);
                        cmd.Parameters.AddWithValue("@Descricao", string.IsNullOrEmpty(descricao) ? "Sem descrição" : descricao);
                        cmd.Parameters.AddWithValue("@ChamadoStatus", status ?? "Aberto");
                        cmd.Parameters.AddWithValue("@DataAbertura", dataAbertura);
                        cmd.Parameters.AddWithValue("@DataFechamento", dataFechamento.HasValue ? (object)dataFechamento.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Prioridade", PrioridadeSelecionada ?? "Média");
                        cmd.Parameters.AddWithValue("@IdCliente", 1); // Cliente padrão
                        cmd.Parameters.AddWithValue("@IdSetor", ObterIdSetor(setor));

                        int resultado = cmd.ExecuteNonQuery();

                        if (resultado > 0)
                        {
                            MessageBox.Show("Chamado salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                            ModalNovoChamado.Visibility = Visibility.Collapsed;
                            CarregarTickets(); // Recarrega a listagem
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar chamado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExpandirTicket(Chamado chamado)
        {
            // Em vez do MessageBox, abre o modal personalizado
            AbrirModalDetalhes(chamado);
        }

        private void AbrirModalDetalhes(Chamado chamado)
        {
            try
            {
                // Preenche os campos com os dados do chamado
                DetalhesTituloTextBox.Text = chamado.Titulo;
                DetalhesDescricaoTextBox.Text = chamado.Descricao;
                DetalhesIdChamadoTextBox.Text = chamado.IdTicket.ToString();
                DetalhesClienteTextBox.Text = chamado.NomeCliente;
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

                // Abre o modal
                ModalDetalhesChamado.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar detalhes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigurarSetorDetalhes(int idSetor)
        {
            string setor = idSetor switch
            {
                1 => "Suporte",
                2 => "Financeiro",
                3 => "TI",
                _ => "Suporte"
            };

            foreach (ComboBoxItem item in DetalhesSetorComboBox.Items)
            {
                if (item.Content.ToString() == setor)
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

        private void FecharModalDetalhes_Click(object sender, RoutedEventArgs e)
        {
            ModalDetalhesChamado.Visibility = Visibility.Collapsed;
        }
 
        private void EditarChamado_Click(object sender, RoutedEventArgs e)
        {
            // Aqui você pode implementar a funcionalidade de edição
            MessageBox.Show("Funcionalidade de edição em desenvolvimento...", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void VerificarECriarDadosNecessarios(MySqlConnection conn)
        {
            // Verifica setores
            string verificarSetores = "SELECT COUNT(*) FROM SETORES WHERE ID_SETOR IN (1, 2, 3)";
            using (MySqlCommand cmdVerificar = new MySqlCommand(verificarSetores, conn))
            {
                int countSetores = Convert.ToInt32(cmdVerificar.ExecuteScalar());

                if (countSetores < 3)
                {
                    CriarSetoresPadrao(conn);
                }
            }

            // Verifica cliente
            string verificarClientes = "SELECT COUNT(*) FROM CLIENTES WHERE ID_CLIENTE = 1";
            using (MySqlCommand cmdVerificar = new MySqlCommand(verificarClientes, conn))
            {
                int countClientes = Convert.ToInt32(cmdVerificar.ExecuteScalar());

                if (countClientes == 0)
                {
                    CriarClientePadrao(conn);
                }
            }
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

        private void CriarClientePadrao(MySqlConnection conn)
        {
            string insertCliente = "INSERT INTO CLIENTES (ID_CLIENTE, Nome, Email, Senha_hash) VALUES (1, 'Cliente Padrão', 'cliente@email.com', 'senha')";
            using (MySqlCommand cmd = new MySqlCommand(insertCliente, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void BuscarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BuscarTextBox.Text))
            {
                // Se estiver vazio, mostra todos os chamados
                PreencherCards();
            }
            else
            {
                // Filtra os chamados
                FiltrarChamados(BuscarTextBox.Text.Trim().ToLower());
            }
        }

        private void FiltrarChamados(string termoBusca)
        {
            var chamadosFiltrados = chamados.Where(c =>
                c.Titulo.ToLower().Contains(termoBusca) ||
                c.Descricao.ToLower().Contains(termoBusca) ||
                c.NomeCliente.ToLower().Contains(termoBusca) ||
                c.TicketStatus.ToLower().Contains(termoBusca) ||
                c.Prioridade.ToLower().Contains(termoBusca) ||
                c.NomeSetor.ToLower().Contains(termoBusca) ||
                (c.Tecnico?.ToLower().Contains(termoBusca) ?? false)
            ).ToList();

            CardsStackPanel.Children.Clear();

            foreach (var chamado in chamadosFiltrados)
            {
                var card = CriarCard(chamado);
                CardsStackPanel.Children.Add(card);
            }
        }
    }
}