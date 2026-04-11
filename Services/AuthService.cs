using System;
using System.Threading.Tasks;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using NebulaLauncher;

namespace NebulaLauncher.Services
{
    public class AuthService
    {
        private static AuthService? _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        private JELoginHandler _handler;

        private AuthService()
        {
            _handler = JELoginHandlerBuilder.BuildDefault();
        }

        public async Task<MSession?> LoginMicrosoftAsync()
        {
            try
            {
                var session = await _handler.Authenticate();
                return session;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en autenticación Microsoft: " + ex.Message);
            }
        }

        public MSession LoginOffline(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("El nombre de usuario no puede estar vacío.");
            
            return MSession.CreateOfflineSession(username);
        }

        public void Logout(UserSession session)
        {
            session.Username = "";
            session.AuthMode = "offline";
        }
    }
}
