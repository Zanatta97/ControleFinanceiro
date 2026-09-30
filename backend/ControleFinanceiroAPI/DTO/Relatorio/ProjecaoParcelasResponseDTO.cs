namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class ProjecaoParcelasMesDTO
    {
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string NomeMes { get; set; } = string.Empty;
        public decimal TotalParcelas { get; set; }
        public int QuantidadeParcelas { get; set; }

        // Soma das parcelas que são a última da compra (N == M) nesta competência
        public decimal TotalParcelasTerminando { get; set; }
        public int QuantidadeParcelasTerminando { get; set; }

        // Total do mês anterior da série − total deste mês. Positivo = o compromisso caiu;
        // negativo = subiu (ex.: compra cuja primeira parcela cai neste mês).
        // No primeiro mês a comparação é com o mês de referência.
        public decimal ReducaoEmRelacaoAoMesAnterior { get; set; }
    }

    public class CompraParceladaAtivaDTO
    {
        public string? Descricao { get; set; }
        public Guid ContaId { get; set; }
        public string NomeConta { get; set; } = string.Empty;
        public Guid CategoriaId { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string? Cor { get; set; }

        // Data da parcela lançada de menor número. No cadastro pelo app todas as parcelas guardam a data
        // da compra; em parcelas importadas de fatura cada uma pode ter a sua, e aí vale a da mais antiga lançada.
        public DateTime DataCompra { get; set; }

        // Valor da próxima parcela (a primeira depois do mês de referência)
        public decimal ValorParcela { get; set; }

        // Última parcela com competência até o mês de referência; 0 quando a compra ainda não começou
        public int ParcelaAtual { get; set; }
        public int TotalParcelas { get; set; }

        // Parcelas com competência depois do mês de referência: lançadas + projetadas
        public int ParcelasRestantes { get; set; }

        /// <summary>
        /// Quantas das ParcelasRestantes não estão lançadas no banco e foram inferidas: a partir da última
        /// parcela lançada N/M da compra, as parcelas N+1..M entram com o mesmo valor, uma por competência.
        /// 0 quando todas as parcelas restantes já estão lançadas.
        /// </summary>
        public int ParcelasProjetadas { get; set; }

        // Soma das parcelas restantes (lançadas + projetadas)
        public decimal ValorRestante { get; set; }
        public DateOnly UltimaCompetencia { get; set; }
    }

    public class ProjecaoParcelasResponseDTO
    {
        // Mês de referência: a projeção começa no mês seguinte a ele
        public int MesReferencia { get; set; }
        public int AnoReferencia { get; set; }
        public int QuantidadeMeses { get; set; }

        // Total em parcelas na competência de referência (base de comparação do primeiro mês)
        public decimal TotalParcelasMesReferencia { get; set; }

        // Soma de tudo o que ainda falta pagar em parcelas depois do mês de referência (inclui além do horizonte).
        // Nos totais do relatório (mês de referência, série e restante) entram parcelas lançadas e projetadas.
        public decimal TotalRestante { get; set; }

        public IEnumerable<ProjecaoParcelasMesDTO> Meses { get; set; } = [];
        public IEnumerable<CompraParceladaAtivaDTO> ComprasAtivas { get; set; } = [];
    }
}
