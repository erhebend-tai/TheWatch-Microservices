using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for SITREP parsing and formatting logic.
/// Reimplements the pure keyword-detection and formatting algorithms
/// from SitrepService without MAUI dependencies.
/// </summary>
public class SitrepServiceTests
{
    // =========================================================================
    // ParseFromText — Keyword Detection
    // =========================================================================

    [Fact]
    public void ParseFromText_SituationKeyword_PopulatesSituation()
    {
        var report = ParseFromText("The situation is dire, building on fire");

        report.Situation.Should().Be("The situation is dire, building on fire");
        report.Incident.Should().BeEmpty();
    }

    [Fact]
    public void ParseFromText_WhatHappenedKeyword_PopulatesSituation()
    {
        var report = ParseFromText("Here's what happened: car accident at intersection");

        report.Situation.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_IncidentKeyword_PopulatesIncident()
    {
        var report = ParseFromText("The incident type is a chemical spill");

        report.Incident.Should().Be("The incident type is a chemical spill");
        report.Situation.Should().BeEmpty();
    }

    [Fact]
    public void ParseFromText_TypeOfKeyword_PopulatesIncident()
    {
        var report = ParseFromText("The type of emergency is medical");

        report.Incident.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_TerrainKeyword_PopulatesTerrain()
    {
        var report = ParseFromText("The terrain is mountainous and rocky");

        report.Terrain.Should().Be("The terrain is mountainous and rocky");
    }

    [Fact]
    public void ParseFromText_LocationKeyword_PopulatesTerrain()
    {
        var report = ParseFromText("The location is 5th and Main Street");

        report.Terrain.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_EnvironmentKeyword_PopulatesTerrain()
    {
        var report = ParseFromText("The environment is urban, high-rise buildings");

        report.Terrain.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_ResourceKeyword_PopulatesResources()
    {
        var report = ParseFromText("We need resource allocation for 3 ambulances");

        report.Resources.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_NeedKeyword_PopulatesResources()
    {
        var report = ParseFromText("We need 5 fire trucks and 10 EMTs");

        report.Resources.Should().Be("We need 5 fire trucks and 10 EMTs");
    }

    [Fact]
    public void ParseFromText_RequireKeyword_PopulatesResources()
    {
        var report = ParseFromText("The team will require additional water supplies");

        report.Resources.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_EvacuationKeyword_PopulatesEvacuation()
    {
        var report = ParseFromText("Begin evacuation of north wing immediately");

        report.Evacuation.Should().Be("Begin evacuation of north wing immediately");
    }

    [Fact]
    public void ParseFromText_ExitKeyword_PopulatesEvacuation()
    {
        var report = ParseFromText("Exit route is blocked by debris");

        report.Evacuation.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_RouteKeyword_PopulatesEvacuation()
    {
        var report = ParseFromText("The escape route through Highway 9 is clear");

        report.Evacuation.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_PersonnelKeyword_PopulatesPersonnel()
    {
        var report = ParseFromText("Personnel count is 15 officers on scene");

        report.Personnel.Should().Be("Personnel count is 15 officers on scene");
    }

    [Fact]
    public void ParseFromText_PeopleKeyword_PopulatesPersonnel()
    {
        var report = ParseFromText("There are approximately 200 people in the building");

        report.Personnel.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_VictimKeyword_PopulatesPersonnel()
    {
        var report = ParseFromText("Two victim reported with burns");

        report.Personnel.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_InjuredKeyword_PopulatesPersonnel()
    {
        var report = ParseFromText("Three people injured, one critical");

        report.Personnel.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_UnrecognizedText_DefaultsToSituation()
    {
        var report = ParseFromText("Everything is going well, no issues");

        report.Situation.Should().Be("Everything is going well, no issues");
    }

    [Fact]
    public void ParseFromText_CaseInsensitiveMatching()
    {
        var report = ParseFromText("The TERRAIN is MOUNTAINOUS");

        report.Terrain.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseFromText_FirstKeywordWins()
    {
        // "situation" is checked before "terrain" — even though both could match
        var report = ParseFromText("The situation at this location is stable");

        report.Situation.Should().NotBeEmpty();
        report.Terrain.Should().BeEmpty(); // "location" keyword present but "situation" matched first
    }

    // =========================================================================
    // FormatReport — Output Formatting
    // =========================================================================

    [Fact]
    public void FormatReport_AllSections_FormatsCorrectly()
    {
        var report = new SitrepReport
        {
            Situation = "Building collapse",
            Incident = "Structural failure",
            Terrain = "Urban downtown",
            Resources = "5 ambulances needed",
            Evacuation = "North exit clear",
            Personnel = "3 injured"
        };

        var formatted = FormatReport(report);

        formatted.Should().Contain("SITUATION: Building collapse");
        formatted.Should().Contain("INCIDENT: Structural failure");
        formatted.Should().Contain("TERRAIN/ENVIRONMENT: Urban downtown");
        formatted.Should().Contain("RESOURCES: 5 ambulances needed");
        formatted.Should().Contain("EVACUATION: North exit clear");
        formatted.Should().Contain("PERSONNEL: 3 injured");
    }

    [Fact]
    public void FormatReport_EmptySections_OmitsEmptySections()
    {
        var report = new SitrepReport
        {
            Situation = "Fire in warehouse",
            Resources = "Water tanker needed"
        };

        var formatted = FormatReport(report);

        formatted.Should().Contain("SITUATION:");
        formatted.Should().Contain("RESOURCES:");
        formatted.Should().NotContain("INCIDENT:");
        formatted.Should().NotContain("TERRAIN:");
        formatted.Should().NotContain("EVACUATION:");
        formatted.Should().NotContain("PERSONNEL:");
    }

    [Fact]
    public void FormatReport_AllEmpty_ReturnsEmptyString()
    {
        var report = new SitrepReport();

        var formatted = FormatReport(report);

        formatted.Should().BeEmpty();
    }

    [Fact]
    public void FormatReport_SectionsJoinedWithDoubleNewline()
    {
        var report = new SitrepReport
        {
            Situation = "A",
            Incident = "B"
        };

        var formatted = FormatReport(report);

        formatted.Should().Be("SITUATION: A\n\nINCIDENT: B");
    }

    // =========================================================================
    // SitrepReport model tests
    // =========================================================================

    [Fact]
    public void SitrepReport_Defaults_AllEmpty()
    {
        var report = new SitrepReport();

        report.Situation.Should().BeEmpty();
        report.Incident.Should().BeEmpty();
        report.Terrain.Should().BeEmpty();
        report.Resources.Should().BeEmpty();
        report.Evacuation.Should().BeEmpty();
        report.Personnel.Should().BeEmpty();
        report.IncidentId.Should().BeNull();
        report.AttachedEvidenceIds.Should().BeEmpty();
    }

    [Fact]
    public void SitrepReport_CanAttachEvidence()
    {
        var report = new SitrepReport();
        var evidenceId = Guid.NewGuid();

        report.AttachedEvidenceIds.Add(evidenceId);

        report.AttachedEvidenceIds.Should().Contain(evidenceId);
    }

    // =========================================================================
    // Mirrors SitrepService logic
    // =========================================================================

    private static SitrepReport ParseFromText(string text)
    {
        var report = new SitrepReport();
        var lower = text.ToLowerInvariant();

        if (lower.Contains("situation") || lower.Contains("what happened"))
            report.Situation = text;
        else if (lower.Contains("incident") || lower.Contains("type of"))
            report.Incident = text;
        else if (lower.Contains("terrain") || lower.Contains("location") || lower.Contains("environment"))
            report.Terrain = text;
        else if (lower.Contains("resource") || lower.Contains("need") || lower.Contains("require"))
            report.Resources = text;
        else if (lower.Contains("evacuat") || lower.Contains("exit") || lower.Contains("route"))
            report.Evacuation = text;
        else if (lower.Contains("personnel") || lower.Contains("people") || lower.Contains("victim") || lower.Contains("injur"))
            report.Personnel = text;
        else
            report.Situation = text;

        return report;
    }

    private static string FormatReport(SitrepReport report)
    {
        var sections = new List<string>();

        if (!string.IsNullOrWhiteSpace(report.Situation))
            sections.Add($"SITUATION: {report.Situation}");
        if (!string.IsNullOrWhiteSpace(report.Incident))
            sections.Add($"INCIDENT: {report.Incident}");
        if (!string.IsNullOrWhiteSpace(report.Terrain))
            sections.Add($"TERRAIN/ENVIRONMENT: {report.Terrain}");
        if (!string.IsNullOrWhiteSpace(report.Resources))
            sections.Add($"RESOURCES: {report.Resources}");
        if (!string.IsNullOrWhiteSpace(report.Evacuation))
            sections.Add($"EVACUATION: {report.Evacuation}");
        if (!string.IsNullOrWhiteSpace(report.Personnel))
            sections.Add($"PERSONNEL: {report.Personnel}");

        return string.Join("\n\n", sections);
    }
}

/// <summary>
/// Mirror of the SitrepReport class from TheWatch.Mobile.Services
/// </summary>
public class SitrepReport
{
    public string Situation { get; set; } = "";
    public string Incident { get; set; } = "";
    public string Terrain { get; set; } = "";
    public string Resources { get; set; } = "";
    public string Evacuation { get; set; } = "";
    public string Personnel { get; set; } = "";
    public Guid? IncidentId { get; set; }
    public List<Guid> AttachedEvidenceIds { get; set; } = [];
}
