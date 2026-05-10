namespace finapro.Models
{
    public class Usuario
    {
        public int id { get; set; }
        public string nombre { get; set; } = "";
        public string correo { get; set; } = "";
        public string password { get; set; } = ""; // En tu diagrama dice 'contraseña', pero mejor evita la 'ñ' en código
        public string? telefono { get; set; }
        public DateTime fechaRegistro { get; set; } = DateTime.Now;
    }
}