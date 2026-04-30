using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VrcGroupCreator.Models;

namespace VrcGroupCreator.Services;

public class VRChatApiService
{
    private const string BaseUrl = "https://api.vrchat.cloud/api/1";
    private const string ApiKey = "JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26";
    
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private string? _authCookie;
    private string? _twoFactorCookie;

    public bool IsLoggedIn { get; private set; }
    public string? CurrentUserId { get; private set; }
    public string? DisplayName { get; private set; }

    private List<VrcGroup>? _cachedGroups;
    private DateTime _groupsCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public VRChatApiService()
    {
        _cookieContainer = new CookieContainer();
        // We use a handler that DOES NOT automatically handle cookies so we can do it manually and reliably
        var handler = new HttpClientHandler
        {
            UseCookies = false, 
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl + "/")
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "VRCGroupCreator/1.0.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
    {
        // Manually add cookies to the request
        var cookies = new List<string>();
        if (!string.IsNullOrEmpty(_authCookie)) cookies.Add($"auth={_authCookie}");
        if (!string.IsNullOrEmpty(_twoFactorCookie)) cookies.Add($"twoFactorAuth={_twoFactorCookie}");
        
        if (cookies.Any())
        {
            request.Headers.Add("Cookie", string.Join("; ", cookies));
        }

        var response = await _httpClient.SendAsync(request);
        
        // Manually extract cookies from the response
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                // Set-Cookie headers can be complex; we look for our specific tokens in any part
                var parts = header.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var kv = part.Trim().Split(new[] { '=' }, 2);
                    if (kv.Length < 2) continue;

                    var name = kv[0].Trim();
                    var value = kv[1].Trim();

                    if (name.Equals("auth", StringComparison.OrdinalIgnoreCase)) _authCookie = value;
                    if (name.Equals("twoFactorAuth", StringComparison.OrdinalIgnoreCase)) _twoFactorCookie = value;
                }
            }
        }

        // Buffer the response body so we can both log it AND let callers read it
        var responseBody = await response.Content.ReadAsStringAsync();

        LoggingService.ApiRequest(request.Method.Method, request.RequestUri?.ToString());
        LoggingService.ApiResponse(request.Method.Method, request.RequestUri?.ToString(),
            (int)response.StatusCode, responseBody);

        // Replace content with a new readable copy since the original stream is consumed
        response.Content = new System.Net.Http.StringContent(responseBody,
            System.Text.Encoding.UTF8,
            response.Content.Headers.ContentType?.MediaType ?? "application/json");

        return response;
    }

    public async Task<LoginResult> LoginAsync(string username, string password, bool keep2FACookie = false)
    {
        try
        {
            _authCookie = null;
            if (!keep2FACookie) _twoFactorCookie = null;

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            var request = new HttpRequestMessage(HttpMethod.Get, $"auth/user?apiKey={ApiKey}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await SendRequestAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorObj = JsonConvert.DeserializeObject<JObject>(content);
                return new LoginResult { Success = false, Message = errorObj?["error"]?["message"]?.ToString() ?? "Login failed" };
            }

            var data = JsonConvert.DeserializeObject<JObject>(content);

            if (data?["requiresTwoFactorAuth"] != null)
            {
                return new LoginResult
                {
                    Success = false,
                    Requires2FA = true,
                    TwoFactorTypes = data["requiresTwoFactorAuth"]!.ToObject<List<string>>() ?? new List<string>(),
                    Message = "2FA required"
                };
            }

            SetLoggedInState(data);
            return new LoginResult 
            { 
                Success = true, 
                UserId = CurrentUserId, 
                DisplayName = DisplayName,
                AuthCookie = _authCookie,
                TwoFactorCookie = _twoFactorCookie
            };
        }
        catch (Exception ex)
        {
            return new LoginResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<LoginResult> Verify2FAAsync(string code, string type = "totp")
    {
        try
        {
            var endpoint = $"auth/twofactorauth/{type}/verify";
            var payload = JsonConvert.SerializeObject(new { code });
            var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}?apiKey={ApiKey}")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = await SendRequestAsync(request);
            if (!response.IsSuccessStatusCode) return new LoginResult { Success = false, Message = "Invalid 2FA code" };

            // After 2FA, we need to get user info again to confirm login
            var userRequest = new HttpRequestMessage(HttpMethod.Get, $"auth/user?apiKey={ApiKey}");
            var userResponse = await SendRequestAsync(userRequest);
            var content = await userResponse.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JObject>(content);

            SetLoggedInState(data);
            return new LoginResult 
            { 
                Success = true, 
                UserId = CurrentUserId, 
                DisplayName = DisplayName,
                AuthCookie = _authCookie,
                TwoFactorCookie = _twoFactorCookie
            };
        }
        catch (Exception ex)
        {
            return new LoginResult { Success = false, Message = ex.Message };
        }
    }

    private void SetLoggedInState(JObject? data)
    {
        if (data == null) return;
        CurrentUserId = data["id"]?.ToString();
        DisplayName = data["displayName"]?.ToString();
        IsLoggedIn = true;
    }

    public async Task<bool> SetSessionAsync(string authCookie, string? twoFactorCookie,
        string? username = null, string? password = null,
        AccountInfo? accountInfo = null, AccountService? accountService = null)
    {
        _authCookie = authCookie;
        _twoFactorCookie = twoFactorCookie;

        // --- Step 1: Try restoring with the stored cookie ---
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"auth/user?apiKey={ApiKey}");
            var response = await SendRequestAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<JObject>(content);
                SetLoggedInState(data);
                return true;
            }
        }
        catch { }

        // --- Step 2: Cookie expired — silently re-login with stored credentials ---
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            Console.WriteLine($"[SESSION] Cookie invalid, attempting silent re-login for {username}...");
            var reloginResult = await LoginAsync(username, password, keep2FACookie: true);
            if (reloginResult.Success)
            {
                Console.WriteLine("[SESSION] Silent re-login succeeded. Updating stored cookies.");
                // Persist the fresh cookie back so the NEXT launch also works without a re-login
                if (accountInfo != null && accountService != null)
                {
                    accountInfo.AuthCookie = _authCookie ?? accountInfo.AuthCookie;
                    accountInfo.TwoFactorCookie = _twoFactorCookie ?? accountInfo.TwoFactorCookie;
                    accountService.SaveAccount(accountInfo);
                }
                return true;
            }
            Console.WriteLine("[SESSION] Silent re-login failed — credentials may have changed.");
        }

        IsLoggedIn = false;
        return false;
    }


    public async Task<GroupCreateResult> CreateGroupAsync(GroupCreateRequest request)
    {
        try
        {
            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings 
            { 
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() 
            });
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"groups?apiKey={ApiKey}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            
            var response = await SendRequestAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                var errorObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                return new GroupCreateResult { Error = errorObj?["error"]?["message"]?.ToString() ?? $"Failed with status {response.StatusCode}" };
            }

            var data = JsonConvert.DeserializeObject<JObject>(responseContent);
            return new GroupCreateResult 
            { 
                Id = data?["id"]?.ToString(), 
                Name = data?["name"]?.ToString(), 
                ShortCode = data?["shortCode"]?.ToString() 
            };
        }
        catch (Exception ex)
        {
            return new GroupCreateResult { Error = ex.Message };
        }
    }

    public async Task<List<VrcGroup>> GetMyGroupsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedGroups != null && DateTime.Now - _groupsCacheTime < _cacheDuration)
        {
            return _cachedGroups;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"users/{CurrentUserId}/groups?apiKey={ApiKey}");
            var response = await SendRequestAsync(request);
            if (!response.IsSuccessStatusCode) return new List<VrcGroup>();

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<JObject>>(content);
            
            var groups = new List<VrcGroup>();
            if (data != null)
            {
                foreach (var item in data)
                {
                    groups.Add(new VrcGroup
                    {
                        Id = item["groupId"]?.ToString() ?? string.Empty,
                        Name = item["name"]?.ToString() ?? string.Empty,
                        ShortCode = item["shortCode"]?.ToString() ?? string.Empty,
                        Discriminator = item["discriminator"]?.ToString() ?? string.Empty,
                        Description = item["description"]?.ToString() ?? string.Empty,
                        OwnerId = item["ownerId"]?.ToString() ?? string.Empty,
                        MemberCount = item["memberCount"]?.Value<int>() ?? 0
                    });
                }
            }
            
            _cachedGroups = groups;
            _groupsCacheTime = DateTime.Now;
            return groups;
        }
        catch { return new List<VrcGroup>(); }
    }

    public async Task<bool> DeleteGroupAsync(string groupId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"groups/{groupId}?apiKey={ApiKey}");
            var response = await SendRequestAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
