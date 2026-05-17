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
        public List<Categoria> ObtenerCategorias(int idEmpresa)
        {
            List<Categoria> lista = new List<Categoria>();
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                // Filtramos por la empresa activa o categorías del sistema (NULL)
                string query = "SELECT * FROM Categoria WHERE idEmpresa = @idEmpresa OR idEmpresa IS NULL";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idEmpresa", idEmpresa);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Categoria
                            {
                                id = reader.GetInt32("id"),
                                // Validamos si es nulo en la BD (categoría global) para que no truene
                                idEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? 0 : reader.GetInt32("idEmpresa"),
                                nombre = reader.GetString("nombre"),
                                tipo = reader.GetString("tipo")
                            });
                        }
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
                                nombreCategoria = reader.GetString("nombreCategoria"),
                                idFactura = reader["idFactura"]?.ToString()
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
                    query = @"INSERT INTO Transaccion (idEmpresa, idCategoria, fecha, concepto, monto, tipo, metodoPago, estaFacturada, idFactura) 
                      VALUES (@emp, @cat, @fec, @con, @mon, @tip, @met, @fac, @idFactura)";
                }
                else
                {
                    query = @"UPDATE Transaccion SET idCategoria=@cat, fecha=@fec, concepto=@con, 
                      monto=@mon, tipo=@tip, metodoPago=@met, estaFacturada=@fac, idFactura=@idFactura
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
                    cmd.Parameters.AddWithValue("@idFactura", t.idFactura ?? (object)DBNull.Value);
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

        public void InsertarCategoria(Categoria c)
        {
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                // Añadimos idEmpresa a los campos a insertar
                string query = "INSERT INTO Categoria (idEmpresa, nombre, tipo) VALUES (@idEmpresa, @nom, @tip)";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idEmpresa", c.idEmpresa);
                    cmd.Parameters.AddWithValue("@nom", c.nombre);
                    cmd.Parameters.AddWithValue("@tip", c.tipo);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void EliminarCategoria(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                // Nota: Esto fallará si hay transacciones usando esta categoría (integridad referencial)
                string query = "DELETE FROM Categoria WHERE id = @id";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // --- OBTENER EMPRESAS DEL USUARIO ---
        public List<Empresa> ObtenerEmpresas(int idUsuario)
        {
            List<Empresa> lista = new List<Empresa>();
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();
                string query = "SELECT * FROM Empresa WHERE idUsuario = @idUsuario ORDER BY fechaC ASC";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Empresa
                            {
                                id = reader.GetInt32("id"),
                                idUsuario = reader.GetInt32("idUsuario"),
                                nombre = reader.GetString("nombre"),
                                rfc = reader.IsDBNull(reader.GetOrdinal("rfc")) ? null : reader.GetString("rfc"),
                                direccion = reader.IsDBNull(reader.GetOrdinal("direccion")) ? null : reader.GetString("direccion"),
                                fechaC = reader.GetDateTime("fechaC")
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // --- CREAR EMPRESA CON LÍMITE Y PROTECCIÓN DE NULOS ---
        public string AgregarEmpresa(Empresa e)
        {
            using (MySqlConnection conn = new MySqlConnection(cadenaConexion))
            {
                conn.Open();

                // 1. Validar límite de 3 empresas
                string countQuery = "SELECT COUNT(*) FROM Empresa WHERE idUsuario = @idUsuario";
                using (MySqlCommand countCmd = new MySqlCommand(countQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@idUsuario", e.idUsuario);
                    int total = Convert.ToInt32(countCmd.ExecuteScalar());
                    if (total >= 3) return "Has alcanzado el límite máximo de 3 empresas.";
                }

                // 2. Insertar. Usamos DBNull.Value para evitar que la aplicación crashee si rfc o direccion vienen nulos
                string query = "INSERT INTO Empresa (idUsuario, nombre, rfc, direccion) VALUES (@idUsr, @nom, @rfc, @dir)";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idUsr", e.idUsuario);
                    cmd.Parameters.AddWithValue("@nom", e.nombre);
                    cmd.Parameters.AddWithValue("@rfc", string.IsNullOrWhiteSpace(e.rfc) ? DBNull.Value : e.rfc);
                    cmd.Parameters.AddWithValue("@dir", string.IsNullOrWhiteSpace(e.direccion) ? DBNull.Value : e.direccion);

                    cmd.ExecuteNonQuery();
                    return "OK";
                }
            }
        }

        public string ObtenerResumenParaIA(int idEmpresa)
        {
            var transacciones = ObtenerTransacciones(idEmpresa); // Reutilizamos el método que ya tienes
            if (transacciones.Count == 0) return "La empresa no tiene transacciones registradas aún.";

            decimal totalIngresos = transacciones.Where(t => t.tipo == "Ingreso").Sum(t => t.monto);
            decimal totalGastos = transacciones.Where(t => t.tipo == "Gasto").Sum(t => t.monto);
            decimal balance = totalIngresos - totalGastos;

            string resumen = $"Total de Ingresos: ${totalIngresos}. Total de Gastos: ${totalGastos}. Balance actual: ${balance}. ";
            resumen += "Últimos movimientos: ";

            // Tomamos las últimas 5 transacciones para darle contexto de en qué se gasta
            foreach (var t in transacciones.OrderByDescending(x => x.fecha).Take(5))
            {
                resumen += $"[{t.fecha.ToString("dd/MM")}] {t.concepto} ({t.tipo}): ${t.monto}. ";
            }

            return resumen;
        }

        public List<int> ObtenerLeccionesCompletadas(int idUsuario)
        {
            List<int> completadas = new List<int>();
            string query = "SELECT id_leccion FROM progreso_aprendizaje WHERE id_usuario = @idUsuario";

            using (var connection = new MySqlConnection(cadenaConexion))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@idUsuario", idUsuario);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            completadas.Add(reader.GetInt32("id_leccion"));
                        }
                    }
                }
            }
            return completadas;
        }

        // 2. Método para registrar que una lección ha sido finalizada con éxito
        public void GuardarProgresoLeccion(int idUsuario, int idLeccion)
        {
            // Usamos INSERT IGNORE o ON DUPLICATE KEY para evitar errores si el usuario repite el quiz
            string query = @"INSERT INTO progreso_aprendizaje (id_usuario, id_leccion) 
                     VALUES (@idUsuario, @idLeccion) 
                     ON DUPLICATE KEY UPDATE fecha_completado = CURRENT_TIMESTAMP";

            using (var connection = new MySqlConnection(cadenaConexion))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@idUsuario", idUsuario);
                    command.Parameters.AddWithValue("@idLeccion", idLeccion);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}