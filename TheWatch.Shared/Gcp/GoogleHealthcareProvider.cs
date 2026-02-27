using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// Google Healthcare API (FHIR R4) implementation of IHealthDataProvider (Item 135).
/// Uses the REST API for FHIR resource CRUD operations.
/// </summary>
public class GoogleHealthcareProvider : IHealthDataProvider
{
    private readonly GcpServiceOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleHealthcareProvider> _logger;

    private string FhirBaseUrl =>
        $"https://healthcare.googleapis.com/v1/projects/{_options.HealthcareProjectId}" +
        $"/locations/{_options.HealthcareLocation}" +
        $"/datasets/{_options.HealthcareDatasetId}" +
        $"/fhirStores/{_options.HealthcareFhirStoreId}/fhir";

    public GoogleHealthcareProvider(
        GcpServiceOptions options,
        HttpClient httpClient,
        ILogger<GoogleHealthcareProvider> logger)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.HealthcareProjectId) &&
        !string.IsNullOrWhiteSpace(_options.HealthcareDatasetId) &&
        !string.IsNullOrWhiteSpace(_options.HealthcareFhirStoreId);

    public async Task<FhirResourceResult> UpsertPatientAsync(FhirPatient patient, CancellationToken ct)
    {
        _logger.LogInformation("Upserting FHIR Patient: {Given} {Family}", patient.GivenName, patient.FamilyName);

        var fhirPatient = new
        {
            resourceType = "Patient",
            id = patient.Id,
            name = new[]
            {
                new
                {
                    family = patient.FamilyName,
                    given = new[] { patient.GivenName }
                }
            },
            gender = patient.Gender ?? "unknown",
            birthDate = patient.BirthDate?.ToString("yyyy-MM-dd"),
            telecom = BuildTelecom(patient),
            address = patient.Address is not null ? new[]
            {
                new
                {
                    line = patient.Address.Line is not null ? new[] { patient.Address.Line } : Array.Empty<string>(),
                    city = patient.Address.City,
                    state = patient.Address.State,
                    postalCode = patient.Address.PostalCode,
                    country = patient.Address.Country
                }
            } : null,
            identifier = patient.Identifiers.Select(kv => new
            {
                system = kv.Key,
                value = kv.Value
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(fhirPatient, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        var content = new StringContent(json, Encoding.UTF8, "application/fhir+json");

        HttpResponseMessage response;
        if (!string.IsNullOrEmpty(patient.Id))
        {
            response = await _httpClient.PutAsync($"{FhirBaseUrl}/Patient/{patient.Id}", content, ct);
        }
        else
        {
            // Conditional create if identifiers provided
            if (patient.Identifiers.Count > 0)
            {
                var firstId = patient.Identifiers.First();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{FhirBaseUrl}/Patient")
                {
                    Content = content
                };
                request.Headers.TryAddWithoutValidation("If-None-Exist", $"identifier={firstId.Key}|{firstId.Value}");
                response = await _httpClient.SendAsync(request, ct);
            }
            else
            {
                response = await _httpClient.PostAsync($"{FhirBaseUrl}/Patient", content, ct);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("FHIR UpsertPatient failed: {Status} {Error}", response.StatusCode, error);
            return new FhirResourceResult(false, Error: error);
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var resourceId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;

        _logger.LogInformation("FHIR Patient upserted: {Id}", resourceId);
        return new FhirResourceResult(true, resourceId);
    }

    public async Task<FhirPatient?> GetPatientAsync(string patientId, CancellationToken ct)
    {
        _logger.LogInformation("Getting FHIR Patient: {Id}", patientId);

        var response = await _httpClient.GetAsync($"{FhirBaseUrl}/Patient/{patientId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("FHIR GetPatient failed for {Id}: {Status}", patientId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return ParsePatient(doc.RootElement);
    }

    public async Task<List<FhirPatient>> SearchPatientsAsync(FhirSearchParams searchParams, CancellationToken ct)
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(searchParams.Name))
            queryParts.Add($"name={Uri.EscapeDataString(searchParams.Name)}");
        if (!string.IsNullOrWhiteSpace(searchParams.Identifier))
            queryParts.Add($"identifier={Uri.EscapeDataString(searchParams.Identifier)}");
        if (searchParams.BirthDate.HasValue)
            queryParts.Add($"birthdate={searchParams.BirthDate:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(searchParams.Gender))
            queryParts.Add($"gender={searchParams.Gender}");
        queryParts.Add($"_count={searchParams.MaxResults}");

        var query = string.Join("&", queryParts);
        _logger.LogInformation("Searching FHIR Patients: {Query}", query);

        var response = await _httpClient.GetAsync($"{FhirBaseUrl}/Patient?{query}", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("FHIR SearchPatients failed: {Status}", response.StatusCode);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var patients = new List<FhirPatient>();

        if (doc.RootElement.TryGetProperty("entry", out var entries))
        {
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.TryGetProperty("resource", out var resource))
                {
                    var patient = ParsePatient(resource);
                    if (patient is not null)
                        patients.Add(patient);
                }
            }
        }

        return patients;
    }

    public async Task<FhirResourceResult> CreateObservationAsync(FhirObservation observation, CancellationToken ct)
    {
        _logger.LogInformation("Creating FHIR Observation for patient {PatientId}: {Code}",
            observation.PatientId, observation.Code);

        var fhirObs = new
        {
            resourceType = "Observation",
            status = observation.Status,
            code = new
            {
                coding = new[]
                {
                    new
                    {
                        system = observation.CodeSystem,
                        code = observation.Code,
                        display = observation.DisplayName
                    }
                }
            },
            subject = new { reference = $"Patient/{observation.PatientId}" },
            effectiveDateTime = observation.EffectiveDateTime.ToString("O"),
            valueQuantity = observation.ValueQuantity.HasValue ? new
            {
                value = observation.ValueQuantity.Value,
                unit = observation.ValueUnit ?? "",
                system = "http://unitsofmeasure.org"
            } : null,
            valueString = observation.ValueString
        };

        var json = JsonSerializer.Serialize(fhirObs, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        var content = new StringContent(json, Encoding.UTF8, "application/fhir+json");

        var response = await _httpClient.PostAsync($"{FhirBaseUrl}/Observation", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("FHIR CreateObservation failed: {Status} {Error}", response.StatusCode, error);
            return new FhirResourceResult(false, Error: error);
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var resourceId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;

        return new FhirResourceResult(true, resourceId);
    }

    public async Task<List<FhirObservation>> GetObservationsAsync(
        string patientId, string? code, DateTime? since, CancellationToken ct)
    {
        var queryParts = new List<string> { $"subject=Patient/{patientId}" };
        if (!string.IsNullOrWhiteSpace(code))
            queryParts.Add($"code={Uri.EscapeDataString(code)}");
        if (since.HasValue)
            queryParts.Add($"date=ge{since:yyyy-MM-dd}");

        var query = string.Join("&", queryParts);
        var response = await _httpClient.GetAsync($"{FhirBaseUrl}/Observation?{query}", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("FHIR GetObservations failed: {Status}", response.StatusCode);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var observations = new List<FhirObservation>();

        if (doc.RootElement.TryGetProperty("entry", out var entries))
        {
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.TryGetProperty("resource", out var resource))
                {
                    var obs = ParseObservation(resource);
                    if (obs is not null)
                        observations.Add(obs);
                }
            }
        }

        return observations;
    }

    public async Task<FhirResourceResult> CreateEncounterAsync(FhirEncounter encounter, CancellationToken ct)
    {
        _logger.LogInformation("Creating FHIR Encounter for patient {PatientId}", encounter.PatientId);

        var fhirEncounter = new
        {
            resourceType = "Encounter",
            status = encounter.Status,
            @class = new { system = "http://terminology.hl7.org/CodeSystem/v3-ActCode", code = encounter.EncounterClass },
            subject = new { reference = $"Patient/{encounter.PatientId}" },
            participant = new[]
            {
                new { individual = new { reference = $"Practitioner/{encounter.PractitionerId}" } }
            },
            period = new
            {
                start = encounter.StartTime?.ToString("O"),
                end = encounter.EndTime?.ToString("O")
            },
            reasonCode = encounter.ReasonCode is not null ? new[]
            {
                new { text = encounter.ReasonCode }
            } : null
        };

        var json = JsonSerializer.Serialize(fhirEncounter, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        var content = new StringContent(json, Encoding.UTF8, "application/fhir+json");

        var response = await _httpClient.PostAsync($"{FhirBaseUrl}/Encounter", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("FHIR CreateEncounter failed: {Status} {Error}", response.StatusCode, error);
            return new FhirResourceResult(false, Error: error);
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var resourceId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;

        return new FhirResourceResult(true, resourceId);
    }

    public async Task<FhirExportResult> ExportPatientDataAsync(string patientId, CancellationToken ct)
    {
        _logger.LogInformation("Exporting FHIR data for patient {PatientId}", patientId);

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{FhirBaseUrl}/Patient/{patientId}/$export");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
        request.Headers.TryAddWithoutValidation("Prefer", "respond-async");

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("FHIR ExportPatientData failed: {Status} {Error}", response.StatusCode, error);
            return new FhirExportResult(false, Error: error);
        }

        // Async export returns Content-Location header with polling URL
        var exportUrl = response.Headers.Location?.ToString()
            ?? response.Content.Headers.ContentLocation?.ToString();

        _logger.LogInformation("FHIR export initiated. Poll URL: {Url}", exportUrl);
        return new FhirExportResult(true, exportUrl);
    }

    // ─── Parsing helpers ───

    private static FhirPatient? ParsePatient(JsonElement resource)
    {
        if (resource.GetProperty("resourceType").GetString() != "Patient")
            return null;

        var patient = new FhirPatient
        {
            Id = resource.TryGetProperty("id", out var id) ? id.GetString() : null,
            Gender = resource.TryGetProperty("gender", out var g) ? g.GetString() : null,
            BirthDate = resource.TryGetProperty("birthDate", out var bd) && DateTime.TryParse(bd.GetString(), out var date) ? date : null,
        };

        if (resource.TryGetProperty("name", out var names) && names.GetArrayLength() > 0)
        {
            var name = names[0];
            patient = patient with
            {
                FamilyName = name.TryGetProperty("family", out var f) ? f.GetString() ?? "" : "",
                GivenName = name.TryGetProperty("given", out var gn) && gn.GetArrayLength() > 0 ? gn[0].GetString() ?? "" : ""
            };
        }

        if (resource.TryGetProperty("telecom", out var telecoms))
        {
            foreach (var t in telecoms.EnumerateArray())
            {
                var system = t.TryGetProperty("system", out var s) ? s.GetString() : "";
                var value = t.TryGetProperty("value", out var v) ? v.GetString() : "";
                if (system == "phone") patient = patient with { Phone = value };
                if (system == "email") patient = patient with { Email = value };
            }
        }

        return patient;
    }

    private static FhirObservation? ParseObservation(JsonElement resource)
    {
        if (resource.GetProperty("resourceType").GetString() != "Observation")
            return null;

        var obs = new FhirObservation
        {
            Id = resource.TryGetProperty("id", out var id) ? id.GetString() : null,
            Status = resource.TryGetProperty("status", out var s) ? s.GetString() ?? "final" : "final",
        };

        if (resource.TryGetProperty("subject", out var subject) &&
            subject.TryGetProperty("reference", out var subRef))
        {
            var reference = subRef.GetString() ?? "";
            obs = obs with { PatientId = reference.Replace("Patient/", "") };
        }

        if (resource.TryGetProperty("code", out var code) &&
            code.TryGetProperty("coding", out var codings) &&
            codings.GetArrayLength() > 0)
        {
            var coding = codings[0];
            obs = obs with
            {
                Code = coding.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "",
                CodeSystem = coding.TryGetProperty("system", out var cs) ? cs.GetString() ?? "" : "",
                DisplayName = coding.TryGetProperty("display", out var d) ? d.GetString() ?? "" : ""
            };
        }

        if (resource.TryGetProperty("valueQuantity", out var vq))
        {
            obs = obs with
            {
                ValueQuantity = vq.TryGetProperty("value", out var val) ? val.GetDecimal() : null,
                ValueUnit = vq.TryGetProperty("unit", out var u) ? u.GetString() : null
            };
        }
        else if (resource.TryGetProperty("valueString", out var vs))
        {
            obs = obs with { ValueString = vs.GetString() };
        }

        if (resource.TryGetProperty("effectiveDateTime", out var edt) &&
            DateTime.TryParse(edt.GetString(), out var effectiveDt))
        {
            obs = obs with { EffectiveDateTime = effectiveDt };
        }

        return obs;
    }

    private static object[] BuildTelecom(FhirPatient patient)
    {
        var telecoms = new List<object>();
        if (!string.IsNullOrWhiteSpace(patient.Phone))
            telecoms.Add(new { system = "phone", value = patient.Phone });
        if (!string.IsNullOrWhiteSpace(patient.Email))
            telecoms.Add(new { system = "email", value = patient.Email });
        return telecoms.ToArray();
    }
}
