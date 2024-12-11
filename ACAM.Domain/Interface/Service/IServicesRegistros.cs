using ACAM.Domain.DTOs;

namespace ACAM.Domain.Interface.Service
{
    public interface IServicesRegistros
    {
        void ProcessarCsvPorStreaming(string caminhoCsv, int idArquivo);

        void SalvarNoBanco(List<AcamDTO> buffer, int idArquivo);
    }
}
