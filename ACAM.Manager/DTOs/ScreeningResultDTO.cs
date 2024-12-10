using System.Collections.Generic;

namespace ACAM.Manager.DTOs
{
    public class ScreeningResultDTO
    {
        public List<AcamDTO> SemHit { get; set; } = new List<AcamDTO>();
        public List<AcamDTO> ComHit { get; set; } = new List<AcamDTO>();
    }
}
