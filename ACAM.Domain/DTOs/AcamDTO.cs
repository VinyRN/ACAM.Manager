namespace ACAM.Domain.DTOs
{
    public class AcamDTO
    {
        public string Client { get; set; }

        public string Pix_Key { get; set; }
        public string cpf_name { get; set; }
        public string Amount { get; set; }
        public DateTime? TrnDate { get; set; }

        public int Id_file { get; set; }
    }
}
