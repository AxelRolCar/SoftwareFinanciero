namespace finapro.Data
{
    public class AppState
    {
        public Models.Usuario? UsuarioActual { get; private set; }
        public Models.Empresa? EmpresaActiva { get; private set; }

        public event Action? OnChange;

        public void SetUsuario(Models.Usuario usuario)
        {
            UsuarioActual = usuario;
            NotifyStateChanged();
        }

        public void SetEmpresaActiva(Models.Empresa empresa)
        {
            EmpresaActiva = empresa;
            NotifyStateChanged();
        }

        public void Logout()
        {
            UsuarioActual = null;
            EmpresaActiva = null;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}