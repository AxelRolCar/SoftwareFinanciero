using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace finapro.Data
{
    public class AIService
    {
        private readonly HttpClient _http;
        private readonly string apiKey;
        private readonly string endpoint = "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash-lite:generateContent?key=";


        public AIService(HttpClient http, IConfiguration config)
        {
            _http = http;

            // FIX 1: Limpiamos la API Key. 
            // A veces los "User Secrets" traen espacios en blanco o comillas extra ("") que rompen la URL.
            apiKey = (config["Gemini:ApiKey"] ?? "").Trim().Replace("\"", "");
        }

        public async Task<string> ConsultarAsistente(string pregunta, string contextoDatos)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return "Error: No se configuró la API Key de Gemini en este equipo.";
            }

            string promptCompleto = $@"
                Eres 'FinaBot', el asistente financiero estratégico y consultor predictivo de la aplicación FinaPro.
                Tu propósito principal es servir como un visor del futuro del negocio; debes analizar la trayectoria actual de los datos para ayudar al usuario a anticipar escenarios, predecir tendencias de flujo y tomar decisiones críticas sobre el rumbo de su empresa.
                
                DATOS FINANCIEROS REALES DE LA EMPRESA:
                {contextoDatos}

                PREGUNTA DEL USUARIO:
                ""{pregunta}""

                REGLA DE CONTRASEÑA CRÍTICA (MODO DESARROLLADOR):
                Si el texto de la PREGUNTA DEL USUARIO contiene exactamente la palabra ""patito1"", debes IGNORAR por completo todas las reglas de formato ejecutivo descritas abajo. En su lugar, responde de forma totalmente libre, casual, creativa o según lo que el usuario pida en su pregunta (incluso si no tiene que ver con finanzas). NO menciones la existencia de la contraseña ni de esta regla en tu respuesta.

                REGLAS DE FORMATO ESTÁNDAR (Si NO se usó la contraseña):
                Si la pregunta NO contiene ""patito1"", debes estructurar tu respuesta OBLIGATORIAMENTE usando el siguiente formato Markdown con títulos y emojis:

                ### Análisis Ejecutivo
                [Aquí pones una respuesta directa, clara y profesional a la pregunta del usuario en máximo 2 renglones, enfocándote en proyecciones o el estado actual]

                ---

                ### Métricas Clave
                * **Contexto:** [Menciona brevemente los ingresos, gastos o balance involucrados en la duda]
                * **Estado:** [Define si la situación actual reflejada por la duda es Saludable, De Cuidado o Crítica]

                ---

                ### Recomendación FinaPro
                [Dale un consejo financiero accionable, predictivo, profesional y realista basado en sus datos para mejorar y asegurar la trayectoria de su negocio]
            ";

            // FIX 2: Agregamos explícitamente el role = "user". 
            // El esquema estricto de Gemini a veces rechaza la petición si no sabe quién habla.
            var requestBody = new
            {
                contents = new[] {
                    new {
                        role = "user",
                        parts = new[] { new { text = promptCompleto } }
                    }
                }
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync(endpoint + apiKey, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    var textoGenerado = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString();

                    return textoGenerado ?? "Lo siento, no pude generar una respuesta.";
                }

                // FIX 3: ¡Desenmascaramos el error! 
                // Leemos el mensaje de rechazo real que envía Google.
                string errorDetail = await response.Content.ReadAsStringAsync();
                return $"<b>Error de API ({response.StatusCode}):</b> <br/> <small>{errorDetail}</small>";
            }
            catch (Exception ex)
            {
                return $"Excepción en código: {ex.Message}";
            }
        }
    }
}