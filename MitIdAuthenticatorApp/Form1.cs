using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MitIdAuthenticatorApp
{
    public partial class Form1 : Form
    {
        private List<Tuple<string, string, string>> _users;
        private Dictionary<string, Tuple<string, string, string>> _userDict;
        private List<string> _userKeys;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            _users = await ReadMitIdUsers();
            _userDict = _users.ToDictionary(u => u.Item1);
            _userKeys = _users.Select(u => u.Item1).ToList();
            individDropDown.DataSource = _userKeys;
        }

        private void idInput_TextChanged(object sender, EventArgs e)
        {
            var idInputHasValue = string.IsNullOrWhiteSpace(idInput.Text) is false;
            addIdButtons.Enabled = idInputHasValue;
        }

        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                _userDict.TryGetValue(individDropDown.Text, out var user);
                if (user != null)
                {
                    await PerformAuthorization(user.Item1, user.Item2, user.Item3);
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Oem5)
            {
                var tasks = _users.Select(user => PerformAuthorization(user.Item1, user.Item2, user.Item3));
                await Task.WhenAll(tasks);
            }
            else
            {
                e.Handled = false;
                return;
            }
        }

        private async void individDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            _userDict.TryGetValue(individDropDown.Text, out var user);
            if (user != null)
            {
                await PerformAuthorization(user.Item1, user.Item2, user.Item3);
            }
        }

        private async void sendApprovalButton_Click(object sender, EventArgs e)
        {
            _userDict.TryGetValue(individDropDown.Text, out var user);
            if (user != null)
            {
                await PerformAuthorization(user.Item1, user.Item2, user.Item3);
            }
        }
        private async void sendAllApprovalsButton_Click(object sender, EventArgs e)
        {
            var tasks = _users.Select(user => PerformAuthorization(user.Item1, user.Item2, user.Item3));
            await Task.WhenAll(tasks);
        }

        private void AddLineToOutputLines(string consoleText)
        {
            var now = DateTime.Now.ToString("HH:mm");
            consoleOutput.Lines = consoleOutput.Lines.Append($"{now}: {consoleText}").ToArray();
            consoleOutput.SelectionStart = consoleOutput.TextLength;
            consoleOutput.ScrollToCaret();
        }

        private void consoleOutput_TextChanged(object sender, EventArgs e)
        {

        }


        private void idInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                HandleAddUser();
            }
            e.Handled = true;
        }

        private void addIdButton_Click(object sender, EventArgs e)
        {
            HandleAddUser();
        }

        private async void HandleAddUser()
        {
            var mitidId = idInput.Text;
            if (_userDict.ContainsKey(mitidId))
            {
                AddLineToOutputLines($"Selecting {mitidId}");
                individDropDown.SelectedIndex = _userKeys.IndexOf(mitidId);
            }
            else
            {
                mitidId = await AddUser(new HttpClient(), string.IsNullOrWhiteSpace(mitidId) ? "" : mitidId);
                _users = await ReadMitIdUsers();
                _userDict = _users.ToDictionary(u => u.Item1);
                _userKeys = _users.Select(u => u.Item1).ToList();
                individDropDown.DataSource = _userKeys;
                var selectedIndex = _userKeys.IndexOf(mitidId);
                individDropDown.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

            }
        }

        private void removeIdButton_Click(object sender, EventArgs e)
        {
            AddLineToOutputLines("clicked remove");
        }


        #region Authentication

        async Task PerformAuthorization(string mitid, string uuid, string authenticatorId)
        {
            AddLineToOutputLines($"Starting authenticator for {mitid}");
            HttpClient client = new HttpClient();

            var authKeyResponse = await GetAuthKey(client, uuid, authenticatorId);

            var pullResponse = await GetPullResponse(client, authKeyResponse.ToRequest(), uuid, authenticatorId);

            if (pullResponse.Status == "OK")
            {
                AddLineToOutputLines($"Confirming: {mitid}");
                var performAuthResponse = await GetPerformAuthResponse(client, pullResponse.ToPerformAuthRequest(),
                    uuid, authenticatorId);
                var confirmResponse = await Confirm(client,
                    performAuthResponse.ToConfirmRequest(authKeyResponse.AuthKey, authKeyResponse.EpochOffset + 100,
                        pullResponse.Ticket), uuid, authenticatorId);
                AddLineToOutputLines(confirmResponse);

            }
            else
            {
                AddLineToOutputLines($"No messages for: {mitid}");
            }
        }

        async Task<string> AddUser(HttpClient client, string mitidId)
        {
            try
            {
                var token = await GetToken(client);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var identity = await GetIdentity(mitidId, client);

                if (identity == null)
                {
                    AddLineToOutputLines($"Unable to find: {mitidId}. Creating identity...");

                    mitidId = await CreateTestPerson(client, mitidId);

                    identity = await GetIdentity(mitidId, client);

                    if (identity == null)
                    {
                        AddLineToOutputLines("Error.");
                        return string.Empty;
                    }
                }

                var activeAuthenticator = await CheckForSimulator(client, identity.IdentityId);

                if (activeAuthenticator is null)
                {
                    await CreateSimulator(client, identity.IdentityId, token);
                    var result = await client.GetAsync(
                                            $"https://pp.mitid.dk/administration/v4/identities/{identity.IdentityId}/authenticators");
                    var content = await result.Content.ReadAsStringAsync();
                    var authenticators = JsonSerializer.Deserialize<List<Authenticator>>(content);
                    activeAuthenticator = authenticators?.Where(a => a.State == "ACTIVE").FirstOrDefault();
                }


                if (activeAuthenticator is null)
                {
                    AddLineToOutputLines($"Unable to find authenticator for: {mitidId}");
                }

                await AddMitIdUsers(mitidId, identity.IdentityId, activeAuthenticator.AuthenticatorId);

                AddLineToOutputLines($"Successfully created {mitidId}");
                return mitidId;

            }
            catch (Exception e)
            {
                AddLineToOutputLines("Failed to create identity..." + e.GetBaseException());
                return string.Empty;
            }

        }

        async Task<string> GetToken(HttpClient client, int tries = 0)
        {
            // this is a test token for a public test tool, it is not sensitive information.
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                "NnYzNHY2Y3pjZXhmZGY1dnljbGptdWU4NDVkNndkMDA6MDEwOEQwQTQ1MzlBNjdCOEJBOEM2Mjc5RjJFMTdDMDVDM0UwNDJDQ0Y0NjE1NjBGMUJGRUU4MDk2REM2RUQ1MQ==");
            var url = "https://pp.mitid.dk/mitid-administrative-idp/oauth/token?grant_type=client_credentials";
            var result = await client.PostAsync(url, null);
            if (result.IsSuccessStatusCode is false && tries < 5)
            {
                AddLineToOutputLines(
                    $"MitId returned 403.. Retrying (because this was found to be working. Try: {tries + 1})");
                await Task.Delay(1000);
                return await GetToken(client, tries + 1);
            }
            else if (tries == 5)
            {
                throw new Exception("Failed to create identity");
            }

            var content = await result.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<AuthResponse>(content);
            return response.AccessToken ?? String.Empty;
        }


        async Task<Person?> AutofillPerson(HttpClient httpClient)
        {
            var url = $"https://pp.mitid.dk/mitid-test-api/v4/testpersons";
            var json = JsonSerializer.Serialize(new { countryCode = "DK" });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync(url, httpContent);
            var content = await result.Content.ReadAsStringAsync();
            var person1 = JsonSerializer.Deserialize<Person>(content);
            return person1;
        }


        async Task<Identity?> GetIdentity(string s, HttpClient httpClient)
        {
            var getIdentitiesUrl = $"https://pp.mitid.dk/administration/v5/identities";
            var getIdentitiesRequest = new IdentitiesRequest()
            {
                ExactMatch = true,
                UserId = s
            };
            var json = JsonSerializer.Serialize(getIdentitiesRequest);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var identitiesResult = await httpClient.PostAsync(getIdentitiesUrl, httpContent);
            var identitiesContent = await identitiesResult.Content.ReadAsStringAsync();
            var identitiesResponse = JsonSerializer.Deserialize<IdentitiesResponse>(identitiesContent);

            var identity = identitiesResponse?.Identities.FirstOrDefault();
            return identity;
        }


        async Task<Authenticator?> CheckForSimulator(HttpClient client, string identityId)
        {
            try
            {
                var url =
                        $"https://pp.mitid.dk/mitid-test-api/v4/identities/{identityId}/authenticators/code-app/simulator";
                var result = await client.GetAsync(url);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var authenticators = JsonSerializer.Deserialize<Authenticator[]>(content);
                    return authenticators?.FirstOrDefault(a => a.State == "ACTIVE");
                }
            }
            catch (Exception _)
            {
                return null;
            }

            return null;
        }
        async Task CreateSimulator(HttpClient client, string identityId, string accessToken)
        {
            var url =
                $"https://pp.mitid.dk/mitid-test-api/v4/identities/{identityId}/authenticators/code-app/simulator";
            var json = JsonSerializer.Serialize(new
            {
                activate = true,
                ael = "SUBSTANTIAL",
                device =
                    "eyJvc05hbWUiOiJBbmRyb2lkIiwib3NWZXJzaW9uIjoiMTAiLCJtb2RlbCI6IlNNLUE1MTVGIiwiaHdHZW5LZXkiOiJ0cnVlIiwiamFpbGJyb2tlblN0YXR1cyI6ImZhbHNlIiwibWFsd2FyZU9uRGV2aWNlIjoiZmFsc2UiLCJhcHBOYW1lIjoiTWl0SUQgYXBwIiwiYXBwVmVyc2lvbiI6IjEuMC4wIiwiYXBwSWRlbnQiOiJkay5taXRpZC5hcHAuYW5kcm9pZCIsInNka1ZlcnNpb24iOiIxLjAuMCIsInN3RmluZ2VycHJpbnQiOiI0YjU4ZWVlNDY3MmI0ZWMyOTY4MmZhMzU5MDI1ODlmZGUyYmMwNGJhN2RlODQzYjE5NDFiYTY5MjMzYjc5ODE5IiwiZXh0cmEiOiJ7XCJwYWNrYWdlTmFtZVwiOlwiZGsubWl0aWQuYXBwLmFuZHJvaWRcIn0ifQ==",
                pin = "112233"
            });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync(url, httpContent);

            result.EnsureSuccessStatusCode();
        }

        async Task<string> CreateTestPerson(HttpClient client, string mitId)
        {
            var person = await AutofillPerson(client);

            if (person == null)
            {
                AddLineToOutputLines("Unable to create identity");
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(mitId))
            {
                return person.Identity;
            }
            person.Identity = mitId;

            var url = "https://pp.mitid.dk/mitid-test-api/v4/identities";
            var json = JsonSerializer.Serialize(person);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync(url, httpContent);

            result.EnsureSuccessStatusCode();
            return mitId;
        }


        async Task<AuthKeyResponse> GetAuthKey(HttpClient client, string uuid, string authId)
        {
            var result =
                await client.GetAsync(
                    $"https://pp.mitid.dk/mitid-test-api/v4/identities/{uuid}/authenticators/code-app/simulator/{authId}/auth-key");
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthKeyResponse>(content);
        }

        async Task<PullResponse> GetPullResponse(HttpClient client, PullRequest request, string uuid, string authId)
        {
            var url =
                $"https://pp.mitid.dk/mitid-test-api/v4/identities/{uuid}/authenticators/code-app/{authId}/simulator-notifier/pull";
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync(url, httpContent);
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PullResponse>(content);
        }

        async Task<PerformAuthResponse> GetPerformAuthResponse(HttpClient client, PerformAuthRequest request,
            string uuid, string authId)
        {
            var url =
                $"https://pp.mitid.dk/mitid-test-api/v4/identities/{uuid}/authenticators/code-app/{authId}/simulator-notifier/perform-auth";
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync(url, httpContent);
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PerformAuthResponse>(content);
        }

        async Task<string> Confirm(HttpClient client, ConfirmRequest request, string uuid, string authId)
        {
            var url =
                $"https://pp.mitid.dk/mitid-test-api/v4/identities/{uuid}/authenticators/code-app/{authId}/simulator-notifier/confirm";
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync(url, httpContent);
            var content = await result.Content.ReadAsStringAsync();
            return content;
        }

        async Task<List<Tuple<string, string, string>>> ReadMitIdUsers()
        {

            if (File.Exists("mitid_users.txt"))
            {
                var lines = await File.ReadAllLinesAsync("mitid_users.txt");
                return lines.Select(line => line.Split(','))
                    .Where(l => l.Length is 3)
                    .Select(result => new Tuple<string, string, string>(result[0], result[1], result[2]))
                    .ToList();
            }
            else
            {
                AddLineToOutputLines("Could not find file: 'mitid_users.txt'");
                AddLineToOutputLines("Creating: 'mitid_users.txt'");
                var file = File.CreateText("mitid_users.txt");
                //file.WriteLine();
                file.Close();
                return new List<Tuple<string, string, string>>();
            }
        }

        async Task AddMitIdUsers(string mitId, string identityId, string authenticatorId)
        {
            await using StreamWriter file = new("mitid_users.txt", append: true);
            await file.WriteLineAsync($"{mitId},{identityId},{authenticatorId}");
        }

        record AuthResponse
        {
            [JsonPropertyName("access_token")] public string AccessToken { get; set; }

            [JsonPropertyName("token_type")] public string TokenType { get; set; }

            [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        }


        record Person
        {
            [JsonPropertyName("cprNumber")] public string CprNumber { get; set; }

            [JsonPropertyName("birthDate")] public DateTime BirthDate { get; set; }

            [JsonPropertyName("country")] public string Country { get; set; }

            [JsonPropertyName("firstName")] public string FirstName { get; set; }

            [JsonPropertyName("lastName")] public string LastName { get; set; }

            [JsonPropertyName("desiredIAL")] public string DesiredIAL { get; set; }

            [JsonPropertyName("identity")] public string Identity { get; set; }

            [JsonPropertyName("email")] public string Email { get; set; }

            [JsonPropertyName("countryCallingCode")]
            public string CountryCallingCode { get; set; }

            [JsonPropertyName("phoneNumber")] public string PhoneNumber { get; set; }

            [JsonPropertyName("registrationBy")] public string RegistrationBy { get; set; }

            [JsonPropertyName("preferredLanguage")]
            public string PreferredLanguage { get; set; }

            [JsonPropertyName("notificationLevel")]
            public string NotificationLevel { get; set; }

            [JsonPropertyName("privacySettings")] public string PrivacySettings { get; set; }

            [JsonPropertyName("firstNotificationChannel")]
            public string FirstNotificationChannel { get; set; }

            [JsonPropertyName("secondNotificationChannel")]
            public string SecondNotificationChannel { get; set; }

            [JsonPropertyName("thirdNotificationChannel")]
            public string ThirdNotificationChannel { get; set; }

            [JsonPropertyName("requestedProtectedStatusForName")]
            public bool RequestedProtectedStatusForName { get; set; }

            [JsonPropertyName("requestedProtectedStatusForAddress")]
            public bool RequestedProtectedStatusForAddress { get; set; }

            [JsonPropertyName("paymentModel")] public string PaymentModel { get; set; }
        }

        record CodeAppProperties
        {
            [JsonPropertyName("osName")] public string OSName { get; set; }

            [JsonPropertyName("osVersion")] public string OSVersion { get; set; }

            [JsonPropertyName("model")] public string Model { get; set; }

            [JsonPropertyName("sdkVersion")] public string SDKVersion { get; set; }

            [JsonPropertyName("appVersion")] public string AppVersion { get; set; }
        }

        record Authenticator
        {
            [JsonPropertyName("authenticatorId")] public string AuthenticatorId { get; set; }

            [JsonPropertyName("authenticatorType")]
            public string AuthenticatorType { get; set; }

            [JsonPropertyName("ael")] public string Ael { get; set; }

            [JsonPropertyName("state")] public string State { get; set; }

            [JsonPropertyName("serialNumber")] public object SerialNumber { get; set; }

            [JsonPropertyName("codeAppProperties")]
            public CodeAppProperties CodeAppProperties { get; set; }
        }

        record IdentitiesRequest
        {
            [JsonPropertyName("userId")] public string UserId { get; set; }
            [JsonPropertyName("exactMatch")] public bool ExactMatch { get; set; }
        }

        record Identity
        {

            [JsonPropertyName("identityId")] public string IdentityId { get; set; }

            [JsonPropertyName("identityName")] public string IdentityName { get; set; }

            [JsonPropertyName("dateOfBirth")] public string DateOfBirth { get; set; }

            [JsonPropertyName("postalAddress")] public string PostalAddress { get; set; }

            [JsonPropertyName("postalCode")] public string PostalCode { get; set; }

            [JsonPropertyName("postalDistrict")] public string PostalDistrict { get; set; }

            [JsonPropertyName("postalCountry")] public string PostalCountry { get; set; }

            [JsonPropertyName("identityStatus")] public string IdentityStatus { get; set; }

            [JsonPropertyName("ial")] public string Ial { get; set; }
        }

        record IdentitiesResponse
        {

            [JsonPropertyName("resultsFound")] public int ResultsFound { get; set; }

            [JsonPropertyName("resultsFetched")] public int ResultsFetched { get; set; }

            [JsonPropertyName("identities")] public List<Identity> Identities { get; set; }
        }


        record ConfirmRequest
        {
            [JsonPropertyName("ticket")] public Guid? Ticket { get; set; }
            [JsonPropertyName("confirmed")] public bool Confirmed { get; set; }
            [JsonPropertyName("payload")] public Payload Payload { get; set; }
            [JsonPropertyName("authKey")] public string AuthKey { get; set; }
            [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
        }

        record Payload
        {
            [JsonPropertyName("response")] public string Response { get; set; }

            [JsonPropertyName("responseSignature")]
            public string ResponseSignature { get; set; }
        }


        record PerformAuthResponse
        {
            [JsonPropertyName("response")] public string Response { get; set; }
            [JsonPropertyName("signedResponse")] public string SignedResponse { get; set; }

            [JsonPropertyName("brokerSecurityContext")]
            public string BrokerSecurityContext { get; set; }

            [JsonPropertyName("referenceTextHeader")]
            public string ReferenceTextHeader { get; set; }

            [JsonPropertyName("referenceTextBody")]
            public string ReferenceTextBody { get; set; }

            [JsonPropertyName("serviceProviderName")]
            public string ServiceProviderName { get; set; }

            public ConfirmRequest ToConfirmRequest(string authKey, long timestamp, Guid? ticket)
            {
                return new ConfirmRequest
                {
                    AuthKey = authKey,
                    Confirmed = true,
                    Payload = new Payload()
                    {
                        Response = Response,
                        ResponseSignature = SignedResponse
                    },
                    Ticket = ticket,
                    Timestamp = timestamp
                };
            }
        }

        record PerformAuthRequest
        {
            [JsonPropertyName("pIN")] public string Pin { get; set; }

            [JsonPropertyName("datagram")] public string Datagram { get; set; }

            [JsonPropertyName("msg")] public string Message { get; set; }

            [JsonPropertyName("ticket")] public Guid? Ticket { get; set; }
        }

        record PullResponse
        {
            [JsonPropertyName("ticket")] public Guid? Ticket { get; set; }
            [JsonPropertyName("msg")] public Message? Message { get; set; }
            [JsonPropertyName("receiver")] public string? Receiver { get; set; }
            [JsonPropertyName("url")] public string? Url { get; set; }
            [JsonPropertyName("status")] public string Status { get; set; }
            [JsonPropertyName("expirationTime")] public long? ExpirationTime { get; set; }

            public PerformAuthRequest ToPerformAuthRequest()
            {
                return new PerformAuthRequest()
                { Datagram = Message.Datagram, Message = Message.Msg, Pin = "112233", Ticket = Ticket };
            }

        }

        record Message
        {
            [JsonPropertyName("msg")] public string? Msg { get; set; }
            [JsonPropertyName("datagram")] public string? Datagram { get; set; }
            [JsonPropertyName("lang")] public string? Lang { get; set; }


        }

        record PullRequest
        {
            [JsonPropertyName("authkey")] public string AuthKey { get; set; }

            [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
        }

        record AuthKeyResponse
        {
            [JsonPropertyName("authKey")] public string AuthKey { get; set; }

            [JsonPropertyName("timestamp")] public long EpochOffset { get; set; }

            public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(EpochOffset);

            public PullRequest ToRequest()
            {
                return new PullRequest
                {
                    AuthKey = AuthKey,
                    Timestamp = EpochOffset
                };
            }

            #endregion
        }
    }
}