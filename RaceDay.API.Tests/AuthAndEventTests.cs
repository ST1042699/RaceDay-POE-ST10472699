using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.API.DTOs;
using Xunit;

namespace RaceDay.API.Tests
{
    public class AuthAndEventTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public AuthAndEventTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true   // important for session cookies
            });
        }


        [Fact]
        public async Task Register_ValidOrganiser_ReturnsCreated()
        {
            var dto = new RegisterDto
            {
                FullName = "Test Organiser",
                Email = $"organiser{Guid.NewGuid()}@test.com",
                Password = "Password123",
                RoleId = 1
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {
            var email = $"dup{Guid.NewGuid()}@test.com";

            var dto = new RegisterDto
            {
                FullName = "First User",
                Email = email,
                Password = "Password123",
                RoleId = 2
            };

            await _client.PostAsJsonAsync("/api/auth/register", dto);

            
            var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkAndSetsSession()
        {
            var email = $"login{Guid.NewGuid()}@test.com";

            
            await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Login User",
                Email = email,
                Password = "Password123",
                RoleId = 2
            });

            
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = email,
                Password = "Password123"
            });

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            
            var profileResponse = await _client.GetAsync("/api/users/profile");
            Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            var email = $"badpass{Guid.NewGuid()}@test.com";

            await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Bad Pass",
                Email = email,
                Password = "CorrectPass",
                RoleId = 2
            });

            var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = email,
                Password = "WrongPassword"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProfile_WithoutLogin_ReturnsUnauthorized()
        {
            
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/users/profile");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

       

        [Fact]
        public async Task CreateEvent_AsOrganiser_ReturnsCreated()
        {
            var client = await LoginAsOrganiser();

            var dto = new CreateEventDto
            {
                Title = "Test Marathon",
                Description = "Unit test event",
                EventType = "Run",
                Distance = 42.2m,
                EventDate = DateTime.Now.AddMonths(2),
                Location = "Johannesburg"
            };

            var response = await client.PostAsJsonAsync("/api/events", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_ReturnsForbidden()
        {
            var client = await LoginAsParticipant();

            var dto = new CreateEventDto
            {
                Title = "Should Fail",
                EventType = "Run",
                Distance = 10,
                EventDate = DateTime.Now.AddMonths(1),
                Location = "Cape Town"
            };

            var response = await client.PostAsJsonAsync("/api/events", dto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithoutLogin_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var dto = new CreateEventDto
            {
                Title = "No Auth",
                EventType = "Walk",
                Distance = 5,
                EventDate = DateTime.Now.AddDays(30),
                Location = "Durban"
            };

            var response = await client.PostAsJsonAsync("/api/events", dto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        

        [Fact]
        public async Task Enrol_AsParticipant_ReturnsCreated()
        {
            
            var organiserClient = await LoginAsOrganiser();

            var eventDto = new CreateEventDto
            {
                Title = "Enrolment Test Event",
                EventType = "Run",
                Distance = 21.1m,
                EventDate = DateTime.Now.AddMonths(1),
                Location = "Pretoria"
            };

            var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", eventDto);
            eventResponse.EnsureSuccessStatusCode();

            var eventContent = await eventResponse.Content.ReadFromJsonAsync<JsonElement>();
            int eventId = eventContent.GetProperty("eventID").GetInt32();

           
            var catDto = new CreateCategoryDto
            {
                CategoryName = "Half Marathon",
                DistanceKM = 21.1m,
                EntryFee = 250
            };

            var catResponse = await organiserClient.PostAsJsonAsync($"/api/events/{eventId}/categories", catDto);
            catResponse.EnsureSuccessStatusCode();

            var catContent = await catResponse.Content.ReadFromJsonAsync<JsonElement>();
            int categoryId = catContent.GetProperty("categoryID").GetInt32();

            
            var participantClient = await LoginAsParticipant();

            var enrolDto = new EnrolDto { CategoryId = categoryId };
            var enrolResponse = await participantClient.PostAsJsonAsync("/api/enrolments", enrolDto);

            Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);
        }

        [Fact]
        public async Task Enrol_Duplicate_ReturnsConflict()
        {
            
            var organiserClient = await LoginAsOrganiser();

            var eventDto = new CreateEventDto
            {
                Title = "Duplicate Enrol Event",
                EventType = "Cycle",
                Distance = 50,
                EventDate = DateTime.Now.AddMonths(1),
                Location = "Cape Town"
            };

            var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", eventDto);
            var eventContent = await eventResponse.Content.ReadFromJsonAsync<JsonElement>();
            int eventId = eventContent.GetProperty("eventID").GetInt32();

            var catResponse = await organiserClient.PostAsJsonAsync($"/api/events/{eventId}/categories",
                new CreateCategoryDto { CategoryName = "Full", DistanceKM = 50, EntryFee = 400 });
            var catContent = await catResponse.Content.ReadFromJsonAsync<JsonElement>();
            int categoryId = catContent.GetProperty("categoryID").GetInt32();

            var participantClient = await LoginAsParticipant();

           
            await participantClient.PostAsJsonAsync("/api/enrolments", new EnrolDto { CategoryId = categoryId });

            
            var second = await participantClient.PostAsJsonAsync("/api/enrolments", new EnrolDto { CategoryId = categoryId });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Enrol_AsOrganiser_ReturnsForbidden()
        {
            var client = await LoginAsOrganiser();

            var response = await client.PostAsJsonAsync("/api/enrolments", new EnrolDto { CategoryId = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        

        private async Task<HttpClient> LoginAsOrganiser()
        {
            var email = $"org{Guid.NewGuid()}@test.com";

            await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Test Organiser",
                Email = email,
                Password = "Password123",
                RoleId = 1
            });

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = email,
                Password = "Password123"
            });

            return client;
        }

        private async Task<HttpClient> LoginAsParticipant()
        {
            var email = $"part{Guid.NewGuid()}@test.com";

            await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Test Participant",
                Email = email,
                Password = "Password123",
                RoleId = 2
            });

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = email,
                Password = "Password123"
            });

            return client;
        }
    }
}