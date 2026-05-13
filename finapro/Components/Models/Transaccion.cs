namespace finapro.Models
{
    public class Transaccion
    {
        public int id { get; set; }
        public int idEmpresa { get; set; }
        public int idCategoria { get; set; }
        public string? idFactura { get; set; }
        public DateTime fecha { get; set; } = DateTime.Now;
        public string concepto { get; set; } = "";
        public decimal monto { get; set; }
        public string tipo { get; set; } = "Ingreso"; // 'Ingreso' o 'Gasto'
        public string metodoPago { get; set; } = "Efectivo";
        public bool estaFacturada { get; set; }

        // Propiedad extra que no está en la tabla, pero nos sirve para mostrar el nombre en la vista
        public string nombreCategoria { get; set; } = "";
    }
}