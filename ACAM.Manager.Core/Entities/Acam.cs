using System;

namespace ACAM.Manager.Core.Entities
{
    public class Acam
    {
        public string Tipo { get; set; } // FX ou EFX
        public string Cpf { get; set; }
        public string Nome { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataTransacao { get; set; }
        public string Merchant { get; set; }
        public string MotivoHit { get; set; } // "Restrição por Valor", "Lista CAF", etc.
        public decimal ValorRestricao { get; set; } // Para hits por valor acumulado
        public string DetalhesCAF { get; set; } // Para hits na lista CAF
        public decimal valorAcumulado { get; set; }
    }
}
