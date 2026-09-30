namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class RitmoMesResponseDTO
    {
        public DateTime DataReferencia { get; set; }
        public int DiasDecorridos { get; set; }
        public int DiasNoMes { get; set; }
        public decimal GastoAteHoje { get; set; }

        // Dia usado no mês anterior: o mesmo dia de hoje, limitado ao último dia do mês anterior
        public int DiaComparadoMesAnterior { get; set; }
        public decimal GastoMesAnteriorAteMesmoDia { get; set; }

        // Nulo quando o mês anterior não teve gasto até o dia comparado
        public decimal? VariacaoPercentual { get; set; }
        public decimal GastoTotalMesAnterior { get; set; }

        // Projeção linear: gasto até hoje / dias decorridos × dias do mês
        public decimal ProjecaoFimMes { get; set; }
    }
}
