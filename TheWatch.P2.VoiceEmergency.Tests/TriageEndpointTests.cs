using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P2.VoiceEmergency.Emergency;
using Xunit;

namespace TheWatch.P2.VoiceEmergency.Tests;

public class TriageEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TriageEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helper: create test incident for linking ──────────────────────────────

    private async Task<Guid> CreateTestIncidentIdAsync()
    {
        var req = new CreateIncidentRequest(
            Type: EmergencyType.MedicalEmergency,
            Description: "Triage endpoint test incident",
            Location: new Location(40.7128, -74.0060),
            ReporterId: Guid.NewGuid(),
            Severity: 5);
        var resp = await _client.PostAsJsonAsync("/api/incidents", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var incident = await resp.Content.ReadFromJsonAsync<Incident>();
        return incident!.Id;
    }

    // =========================================================================
    // POST /api/triage
    // =========================================================================

    [Fact]
    public async Task LogTriage_MinimalRequest_ReturnsCreated()
    {
        var req = new LogTriageIntakeRequest(
            ReporterId: Guid.NewGuid(),
            Symptoms: "severe chest pain, shortness of breath");

        var response = await _client.PostAsJsonAsync("/api/triage", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var intake = await response.Content.ReadFromJsonAsync<TriageIntake>();
        intake.Should().NotBeNull();
        intake!.Id.Should().NotBeEmpty();
        intake.Symptoms.Should().Be("severe chest pain, shortness of breath");
        intake.InputMethod.Should().Be("text");
        intake.TriageSeverity.Should().Be(0);
        intake.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogTriage_WithAllFields_PersistsCorrectly()
    {
        var incidentId = await CreateTestIncidentIdAsync();
        var reporterId = Guid.NewGuid();

        var req = new LogTriageIntakeRequest(
            ReporterId: reporterId,
            Symptoms: "patient swallowed acetaminophen, nausea and vomiting",
            InputMethod: "stt",
            IncidentId: incidentId,
            SubstanceName: "Acetaminophen",
            MatchedGuidance: "Call Poison Control (1-800-222-1222). Do NOT induce vomiting.",
            TriageSeverity: 5);

        var response = await _client.PostAsJsonAsync("/api/triage", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var intake = await response.Content.ReadFromJsonAsync<TriageIntake>();
        intake.Should().NotBeNull();
        intake!.ReporterId.Should().Be(reporterId);
        intake.IncidentId.Should().Be(incidentId);
        intake.InputMethod.Should().Be("stt");
        intake.SubstanceName.Should().Be("Acetaminophen");
        intake.TriageSeverity.Should().Be(5);
        intake.MatchedGuidance.Should().Contain("Poison Control");
    }

    [Fact]
    public async Task LogTriage_SttInputMethod_AcceptsValue()
    {
        var req = new LogTriageIntakeRequest(
            ReporterId: Guid.NewGuid(),
            Symptoms: "cannot breathe, throat swelling, epipen used",
            InputMethod: "stt");

        var response = await _client.PostAsJsonAsync("/api/triage", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var intake = await response.Content.ReadFromJsonAsync<TriageIntake>();
        intake!.InputMethod.Should().Be("stt");
    }

    [Fact]
    public async Task LogTriage_ResponseLocationHeaderContainsId()
    {
        var req = new LogTriageIntakeRequest(
            ReporterId: Guid.NewGuid(),
            Symptoms: "head injury, loss of consciousness");

        var response = await _client.PostAsJsonAsync("/api/triage", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var intake = await response.Content.ReadFromJsonAsync<TriageIntake>();
        response.Headers.Location!.ToString().Should().Contain(intake!.Id.ToString());
    }

    // =========================================================================
    // GET /api/triage/{id}
    // =========================================================================

    [Fact]
    public async Task GetTriage_ExistingId_ReturnsIntake()
    {
        var req = new LogTriageIntakeRequest(
            ReporterId: Guid.NewGuid(),
            Symptoms: "seizure, jerking movements");
        var created = await _client.PostAsJsonAsync("/api/triage", req);
        var intake = await created.Content.ReadFromJsonAsync<TriageIntake>();

        var response = await _client.GetAsync($"/api/triage/{intake!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await response.Content.ReadFromJsonAsync<TriageIntake>();
        fetched!.Id.Should().Be(intake.Id);
        fetched.Symptoms.Should().Be("seizure, jerking movements");
    }

    [Fact]
    public async Task GetTriage_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/triage/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // GET /api/incidents/{incidentId}/triage
    // =========================================================================

    [Fact]
    public async Task GetTriageForIncident_ReturnsAllIntakesLinkedToIncident()
    {
        var incidentId = await CreateTestIncidentIdAsync();
        var reporterId = Guid.NewGuid();

        // Log two intakes for the same incident
        await _client.PostAsJsonAsync("/api/triage", new LogTriageIntakeRequest(
            ReporterId: reporterId, Symptoms: "first intake", IncidentId: incidentId));
        await _client.PostAsJsonAsync("/api/triage", new LogTriageIntakeRequest(
            ReporterId: reporterId, Symptoms: "second intake", IncidentId: incidentId,
            InputMethod: "stt"));

        var response = await _client.GetAsync($"/api/incidents/{incidentId}/triage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var intakes = await response.Content.ReadFromJsonAsync<List<TriageIntake>>();
        intakes.Should().NotBeNull();
        intakes!.Count.Should().BeGreaterThanOrEqualTo(2);
        intakes.Should().AllSatisfy(i => i.IncidentId.Should().Be(incidentId));
    }

    [Fact]
    public async Task GetTriageForIncident_NoIntakes_ReturnsEmptyList()
    {
        var incidentId = await CreateTestIncidentIdAsync();

        var response = await _client.GetAsync($"/api/incidents/{incidentId}/triage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var intakes = await response.Content.ReadFromJsonAsync<List<TriageIntake>>();
        intakes.Should().NotBeNull();
        intakes!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTriageForIncident_IntakesOrderedByCreatedAt()
    {
        var incidentId = await CreateTestIncidentIdAsync();

        await _client.PostAsJsonAsync("/api/triage", new LogTriageIntakeRequest(
            ReporterId: Guid.NewGuid(), Symptoms: "early intake", IncidentId: incidentId));
        await Task.Delay(50); // ensure distinct timestamps
        await _client.PostAsJsonAsync("/api/triage", new LogTriageIntakeRequest(
            ReporterId: Guid.NewGuid(), Symptoms: "later intake", IncidentId: incidentId));

        var response = await _client.GetAsync($"/api/incidents/{incidentId}/triage");
        var intakes = await response.Content.ReadFromJsonAsync<List<TriageIntake>>();

        intakes!.Should().BeInAscendingOrder(i => i.CreatedAt);
    }
}
