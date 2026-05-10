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
    }
}