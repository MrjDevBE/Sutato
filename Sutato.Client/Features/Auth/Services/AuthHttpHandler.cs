using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;

namespace Sutato.Client.Features.Auth.Services
{
    public class AuthHttpHandler : DelegatingHandler
    {
        private readonly AuthState _authState;
        private readonly NavigationManager _nav;

        public AuthHttpHandler(AuthState authState, NavigationManager nav)
        {
            _authState = authState;
            _nav = nav;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _authState.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized &&
                response.Headers.Contains("Token-Expired"))
            {
                await _authState.Logout();
                _nav.NavigateTo("/login");
            }

            return response;
        }
    }
}
