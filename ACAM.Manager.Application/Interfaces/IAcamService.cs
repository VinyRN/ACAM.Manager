using ACAM.Manager.Core.DTOs;
using ACAM.Manager.Core.Entities;
using System.Collections.Generic;

namespace ACAM.Manager.Application.Interfaces
{
    public interface IAcamService
    {
        IEnumerable<AcamDTO> ProcessarArquivos(string diretorio);
        ScreeningResult RealizarScreening(IEnumerable<AcamDTO> acams, Dictionary<string, decimal> baseRestricao, List<string> listaCaf);
        void AtualizarBanco(AcamDTO acam);
    }
}
