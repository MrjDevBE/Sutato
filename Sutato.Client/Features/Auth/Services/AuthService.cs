using Microsoft.JSInterop;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sutato.Client.Features.Auth.Services
{
    public class AuthState
    {
        private readonly IJSRuntime _js;
        private CancellationTokenSource _cts;

        public AuthState(IJSRuntime js)
        {
            _js = js;
        }

        public bool IsLoggedIn { get; private set; }
        public string Username { get; private set; }   // ✅ added
        public string Role { get; private set; }       // ✅ added
        private string _token;
        private string[] _roles = Array.Empty<string>();
        private bool _extendedSession;

        public event Action OnSessionWarning;
        public event Action OnSessionExpired;
        public event Action OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();

        public async Task LoadStateAsync()
        {
            var value = await _js.InvokeAsync<string>("localStorage.getItem", "isLoggedIn");
            IsLoggedIn = value == "true";

            _token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (!string.IsNullOrEmpty(_token))
            {
                ParseToken(_token);
                StartExpiryTimer(_token);
            }

            NotifyStateChanged();
        }

        public async Task SetLoggedIn(bool value, string token = null)
        {
            IsLoggedIn = value;
            await _js.InvokeVoidAsync("localStorage.setItem", "isLoggedIn", value ? "true" : "false");

            if (!string.IsNullOrEmpty(token))
            {
                _token = token;
                await _js.InvokeVoidAsync("localStorage.setItem", "authToken", token);
                ParseToken(token);   // ✅ decode token immediately
                StartExpiryTimer(token);
            }

            NotifyStateChanged();
        }

        //public async Task Logout()
        //{
        //    IsLoggedIn = false;
        //    _token = null;
        //    Username = null;
        //    Role = null;
        //    _roles = Array.Empty<string>();
        //    _cts?.Cancel();

        //    await _js.InvokeVoidAsync("localStorage.removeItem", "isLoggedIn");
        //    await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");

        //    NotifyStateChanged();
        //}
        public async Task Logout()
        {
            try
            {
                IsLoggedIn = false;
                _token = null;
                Username = null;
                Role = null;
                _roles = Array.Empty<string>();
                _cts?.Cancel();

                // Clear all relevant stored data
                await _js.InvokeVoidAsync("localStorage.removeItem", "isLoggedIn");
                await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await _js.InvokeVoidAsync("localStorage.removeItem", "userInfo"); // optional, if you store user details

                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
            }
        }

        public async Task<string> GetTokenAsync()
        {
            if (string.IsNullOrEmpty(_token))
                _token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");
            return _token;
        }

        private void StartExpiryTimer(string token)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var expiry = jwt.ValidTo.ToUniversalTime();

            var warningTime = expiry.AddMinutes(-1) - DateTime.UtcNow;
            var remainingTime = expiry - DateTime.UtcNow;

            if (warningTime.TotalMilliseconds > 0)
                _ = Task.Delay(warningTime, _cts.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        OnSessionWarning?.Invoke();
                });

            if (remainingTime.TotalMilliseconds > 0)
                _ = Task.Delay(remainingTime, _cts.Token).ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                    {
                        OnSessionExpired?.Invoke();
                        await Logout();
                    }
                });
        }

        private void ParseToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                // Username (depends on backend claim names)
                Username = jwt.Claims.FirstOrDefault(c =>
                    c.Type == ClaimTypes.Name ||
                    c.Type == "unique_name" ||
                    c.Type == "sub" ||
                    c.Type == "name")?.Value;

                // Roles (multiple allowed)
                _roles = jwt.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
                    .Select(c => c.Value)
                    .ToArray();

                Role = _roles.FirstOrDefault(); // ✅ main role
            }
            catch
            {
                Username = null;
                Role = null;
                _roles = Array.Empty<string>();
            }
        }

        // Helpers
        public bool HasRole(string role) => _roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        public bool HasAnyRole(params string[] roles) => roles.Any(r => _roles.Contains(r, StringComparer.OrdinalIgnoreCase));
        public string[] GetRoles() => _roles;

        public void ExtendSession() => _extendedSession = true;
        public string GetToken() => _token;
    }
}
