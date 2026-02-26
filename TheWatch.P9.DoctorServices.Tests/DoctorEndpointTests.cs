using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P9.DoctorServices.Doctors;
using Xunit;

namespace TheWatch.P9.DoctorServices.Tests;

public class DoctorEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DoctorEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<DoctorProfile> CreateDoctorAsync(string name = "Dr. Smith", List<string>? specs = null)
    {
        var request = new CreateDoctorProfileRequest(name, specs ?? ["General Practice"], "LIC-001", "555-0100", $"{Guid.NewGuid():N}@test.com", 33.45, -112.07);
        var response = await _client.PostAsJsonAsync("/api/doctors", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<DoctorProfile>())!;
    }

    [Fact]
    public async Task CreateDoctor_ReturnsCreated()
    {
        var doctor = await CreateDoctorAsync("Dr. Johnson", ["Cardiology", "Internal Medicine"]);

        doctor.Name.Should().Be("Dr. Johnson");
        doctor.Specializations.Should().Contain("Cardiology");
        doctor.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDoctor_Existing_ReturnsOk()
    {
        var created = await CreateDoctorAsync();
        var response = await _client.GetAsync($"/api/doctors/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDoctor_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/doctors/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListDoctors_ReturnsPaginated()
    {
        await CreateDoctorAsync("Doc A");
        await CreateDoctorAsync("Doc B");

        var result = await _client.GetFromJsonAsync<DoctorListResponse>("/api/doctors?page=1&pageSize=50");
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task BookAppointment_ReturnsCreated()
    {
        var doctor = await CreateDoctorAsync();
        var request = new BookAppointmentRequest(doctor.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), AppointmentType.InPerson, 30);
        var response = await _client.PostAsJsonAsync("/api/appointments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var appt = await response.Content.ReadFromJsonAsync<Appointment>();
        appt!.DoctorId.Should().Be(doctor.Id);
        appt.Status.Should().Be(AppointmentStatus.Scheduled);
    }

    [Fact]
    public async Task ListAppointments_ReturnsList()
    {
        var doctor = await CreateDoctorAsync();
        await _client.PostAsJsonAsync("/api/appointments",
            new BookAppointmentRequest(doctor.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), AppointmentType.InPerson));
        await _client.PostAsJsonAsync("/api/appointments",
            new BookAppointmentRequest(doctor.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(2), AppointmentType.Telehealth));

        var result = await _client.GetFromJsonAsync<List<Appointment>>($"/api/appointments?doctorId={doctor.Id}");
        result!.Count.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ReturnsUpdated()
    {
        var doctor = await CreateDoctorAsync();
        var bookResp = await _client.PostAsJsonAsync("/api/appointments",
            new BookAppointmentRequest(doctor.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), AppointmentType.InPerson));
        var appt = await bookResp.Content.ReadFromJsonAsync<Appointment>();

        var response = await _client.PutAsJsonAsync($"/api/appointments/{appt!.Id}/status",
            new UpdateAppointmentStatusRequest(AppointmentStatus.Confirmed));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Appointment>();
        updated!.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task SearchDoctors_ReturnsBySpecialization()
    {
        await CreateDoctorAsync("Cardiologist", ["Cardiology"]);

        var result = await _client.GetFromJsonAsync<List<DoctorSummary>>("/api/doctors/search?specialization=Cardiology");
        result!.Should().Contain(d => d.Name == "Cardiologist");
    }

    [Fact]
    public async Task CreateTelehealthSession_ReturnsCreated()
    {
        var doctor = await CreateDoctorAsync();
        var bookResp = await _client.PostAsJsonAsync("/api/appointments",
            new BookAppointmentRequest(doctor.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), AppointmentType.Telehealth));
        var appt = await bookResp.Content.ReadFromJsonAsync<Appointment>();

        var response = await _client.PostAsync($"/api/appointments/{appt!.Id}/telehealth", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var session = await response.Content.ReadFromJsonAsync<TelehealthSession>();
        session!.AppointmentId.Should().Be(appt.Id);
        session.RoomUrl.Should().NotBeEmpty();
    }
}
