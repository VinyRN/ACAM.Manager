using ACAM.Domain.DTOs;

namespace ACAM.Domain.Interface.Service
{
    public interface IServicesRegistros
    {
        void ProcessarCsvPorStreaming(string caminhoCsv, int idArquivo);

        void SalvarNoBanco(List<AcamDTO> buffer, int idArquivo);

        IEnumerable<AcamDTO> FiltrarRegistrosPorValor(decimal valorMinimo, int idFile);

        void InserirNaTabelaRestritiva(decimal valorMinimo, int idFile);
    }
}
