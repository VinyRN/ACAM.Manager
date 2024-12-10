using ACAM.Manager.Core.DTOs;
using System.Collections.Generic;

namespace ACAM.Manager.Core.Entities
{
    public class ScreeningResult
    {
        public List<AcamDTO> SemHit { get; set; } = new List<AcamDTO>();
        public List<AcamDTO> ComHit { get; set; } = new List<AcamDTO>();
    }
}
