namespace finapro.Models
{
    public class Empresa
    {
        public int id { get; set; }
        public int idUsuario { get; set; }
        public string nombre { get; set; } = "";
        public string? rfc { get; set; }
        public string? direccion { get; set; }
        public DateTime fechaC { get; set; } = DateTime.Now;
    }
}