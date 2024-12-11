using ACAM.Domain.DTOs;

namespace ACAM.Domain.Interface.Repository
{
    public interface IRepositoryRegistros
    {
        void ProcessarCsvPorStreaming(string caminhoCsv, int idArquivo);

        void SalvarNoBanco(List<AcamDTO> buffer, int idArquivo);

        IEnumerable<AcamDTO> FiltrarRegistrosPorValor(decimal valorMinimo, int idFile);

        void InserirNaTabelaRestritiva(decimal valorMinimo, int idFile);
    }
}
