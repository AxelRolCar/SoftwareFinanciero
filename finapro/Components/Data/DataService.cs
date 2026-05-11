using MySql.Data.MySqlClient;
using finapro.Models;
using System.Security.Cryptography;
using System.Text;

namespace finapro.Data
{
    public class DataService
    {
        // Ajusta tu usuario y contraseña de MySQL aquí
        private readonly string cadenaConexion = "Database=FinanzasDB; Data Source=localhost; User Id=root; Password=;";

        // --- MÉTODO PARA REGISTRAR USUARIO ---
        public string RegistrarUsuario(Usuario u)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
                {
                    conn.Open();
                    // Usamos @parametros para EVITAR INYECCIÓN SQL
                    string query = "INSERT INTO Usuario (nombre, correo, password, telefono) VALUES (@nom, @corr, @pass, @tel)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nom", u.nombre);
                        cmd.Parameters.AddWithValue("@corr", u.correo);
                        cmd.Parameters.AddWithValue("@pass", EncriptarPassword(u.password)); // Guardamos el hash, no el texto plano
                        cmd.Parameters.AddWithValue("@tel", u.telefono ?? "");

                        cmd.ExecuteNonQuery();
                        return "Registro exitoso";
                    }
                }
            }
            catch (MySqlException ex)
            {
                // El código 1062 en MySQL significa que hay un registro duplicado (correo Unique)
                if (ex.Number == 1062) return "El correo ya está registrado.";
                return "Error al conectar con la base de datos.";
            }
        }

        // --- MÉTODO PARA INICIAR SESIÓN ---
        public Usuario? Login(string correo, string password)
        {
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                // Consulta parametrizada
                string query = "SELECT id, nombre, correo FROM Usuario WHERE correo = @corr AND password = @pass";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@corr", correo);
                    cmd.Parameters.AddWithValue("@pass", EncriptarPassword(password));

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Usuario
                            {
                                id = reader.GetInt32("id"),
                                nombre = reader.GetString("nombre"),
                                correo = reader.GetString("correo")
                            };
                        }
                    }
                }
            }
            return null; // Si no encuentra nada o la contraseña es incorrecta
        }

        // --- FUNCIÓN DE SEGURIDAD (ENCRIPTACIÓN) ---
        private string EncriptarPassword(string textoPlano)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textoPlano));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); // Convertir a hexadecimal
                }
                return builder.ToString();
            }
        }

        // --- OBTENER CATEGORÍAS ---
        public List<Categoria> ObtenerCategorias()
        {
            List<Categoria> lista = new List<Categoria>();
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                string query = "SELECT * FROM Categoria";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Categoria
                        {
                            id = reader.GetInt32("id"),
                            nombre = reader.GetString("nombre"),
                            tipo = reader.GetString("tipo")
                        });
                    }
                }
            }
            return lista;
        }

        // --- OBTENER TRANSACCIONES (FILTRADO POR EMPRESA) ---
        public List<Transaccion> ObtenerTransacciones(int idEmpresa)
        {
            List<Transaccion> lista = new List<Transaccion>();
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                // Hacemos un JOIN para traernos el nombre de la categoría
                string query = @"SELECT t.*, c.nombre as nombreCategoria 
                         FROM Transaccion t 
                         JOIN Categoria c ON t.idCategoria = c.id 
                         WHERE t.idEmpresa = @idEmpresa 
                         ORDER BY t.fecha DESC";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idEmpresa", idEmpresa);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Transaccion
                            {
                                id = reader.GetInt32("id"),
                                idEmpresa = reader.GetInt32("idEmpresa"),
                                idCategoria = reader.GetInt32("idCategoria"),
                                fecha = reader.GetDateTime("fecha"),
                                concepto = reader.GetString("concepto"),
                                monto = reader.GetDecimal("monto"),
                                tipo = reader.GetString("tipo"),
                                metodoPago = reader.IsDBNull(reader.GetOrdinal("metodoPago")) ? "" : reader.GetString("metodoPago"),
                                estaFacturada = reader.GetBoolean("estaFacturada"),
                                nombreCategoria = reader.GetString("nombreCategoria")
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // --- GUARDAR O ACTUALIZAR TRANSACCIÓN ---
        public void GuardarTransaccion(Transaccion t)
        {
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                string query;

                // Si el id es 0, es un registro nuevo (INSERT). Si tiene id, es modificación (UPDATE).
                if (t.id == 0)
                {
                    query = @"INSERT INTO Transaccion (idEmpresa, idCategoria, fecha, concepto, monto, tipo, metodoPago, estaFacturada) 
                      VALUES (@emp, @cat, @fec, @con, @mon, @tip, @met, @fac)";
                }
                else
                {
                    query = @"UPDATE Transaccion SET idCategoria=@cat, fecha=@fec, concepto=@con, 
                      monto=@mon, tipo=@tip, metodoPago=@met, estaFacturada=@fac 
                      WHERE id=@id AND idEmpresa=@emp";
                }

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    if (t.id != 0) cmd.Parameters.AddWithValue("@id", t.id);
                    cmd.Parameters.AddWithValue("@emp", t.idEmpresa);
                    cmd.Parameters.AddWithValue("@cat", t.idCategoria);
                    cmd.Parameters.AddWithValue("@fec", t.fecha);
                    cmd.Parameters.AddWithValue("@con", t.concepto);
                    cmd.Parameters.AddWithValue("@mon", t.monto);
                    cmd.Parameters.AddWithValue("@tip", t.tipo);
                    cmd.Parameters.AddWithValue("@met", t.metodoPago);
                    cmd.Parameters.AddWithValue("@fac", t.estaFacturada);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // --- ELIMINAR TRANSACCIÓN ---
        public void EliminarTransaccion(int idTransaccion, int idEmpresa)
        {
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                // Validamos el idEmpresa por seguridad, para que nadie borre transacciones de otros
                string query = "DELETE FROM Transaccion WHERE id = @id AND idEmpresa = @emp";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idTransaccion);
                    cmd.Parameters.AddWithValue("@emp", idEmpresa);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}