namespace taskflowDesktop.Models
{
    public class Chamado
    {
        public int IdTicket { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public string TicketStatus { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime? DataFechamento { get; set; }
        public string Prioridade { get; set; }
        public int IdCliente { get; set; }
        public int IdSetor { get; set; }
        public string Tecnico { get; set; }
        public bool IsSelected { get; set; }


        // Campos calculados/demorados (não vêm do banco)
        public string NomeCliente { get; set; }
        public string NomeSetor { get; set; }
    }
}