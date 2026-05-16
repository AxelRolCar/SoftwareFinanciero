namespace finapro.Models
{
    public class Categoria
    {
        public int id { get; set; }
        public int idEmpresa { get; set; } // ¡Nuevo campo añadido!
        public string nombre { get; set; } = "";
        public string tipo { get; set; } = "";
    }
}