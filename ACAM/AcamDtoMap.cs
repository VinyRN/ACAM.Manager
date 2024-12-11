using CsvHelper.Configuration;

namespace ACAM
{
    public sealed class AcamDtoMap : ClassMap<AcamDTO>
    {
        public AcamDtoMap()
        {
            Map(m => m.Client).Name("Client");
            Map(m => m.Pix_Key).Name("pix key"); 
            Map(m => m.cpf_name).Name("cpf_name");
            Map(m => m.Amount).Name("amount");
            Map(m => m.TrnDate).Name("trndate");
        }
    }
}
