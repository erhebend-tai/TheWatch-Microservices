# Microservice map

This map summarizes each service, its primary responsibility, and where to look for code or tests. Use it alongside the solution file `TheWatch.sln`.

| Service | Responsibility | Code | Tests |
| --- | --- | --- | --- |
| P1 — CoreGateway | Entry API gateway, request routing, orchestration, and cross-service aggregation. | `TheWatch.P1.CoreGateway/` | `TheWatch.P1.CoreGateway.Tests/` |
| P2 — VoiceEmergency | Voice/SOS ingestion, incident creation, dispatch triggers, SignalR hubs for incidents/dispatch. | `TheWatch.P2.VoiceEmergency/` | `TheWatch.P2.VoiceEmergency.Tests/` |
| P3 — MeshNetwork | Offline/mesh communications, Bluetooth/mesh relays, disaster fallback transport. | `TheWatch.P3.MeshNetwork/` | `TheWatch.P3.MeshNetwork.Tests/` |
| P4 — Wearable | Device provisioning, heartbeats, firmware/telemetry ingestion for wearable devices. | `TheWatch.P4.Wearable/` | `TheWatch.P4.Wearable.Tests/` |
| P5 — AuthSecurity | Identity, MFA, RBAC, token issuance/rotation, authentication events. | `TheWatch.P5.AuthSecurity/` | `TheWatch.P5.AuthSecurity.Tests/` |
| P6 — FirstResponder | Responder profiles, availability, dispatching, and live responder location streaming. | `TheWatch.P6.FirstResponder/` | `TheWatch.P6.FirstResponder.Tests/` |
| P7 — FamilyHealth | Family groups, member check-ins, vitals tracking, notifications to families. | `TheWatch.P7.FamilyHealth/` | `TheWatch.P7.FamilyHealth.Tests/` |
| P8 — DisasterRelief | Disaster event coordination, shelters/resources, evacuation routing. | `TheWatch.P8.DisasterRelief/` | `TheWatch.P8.DisasterRelief.Tests/` |
| P9 — DoctorServices | Telehealth/doctor scheduling, appointments, and session management. | `TheWatch.P9.DoctorServices/` | `TheWatch.P9.DoctorServices.Tests/` |
| P10 — Gamification | Engagement mechanics (badges, challenges, leaderboards) to increase participation. | `TheWatch.P10.Gamification/` | `TheWatch.P10.Gamification.Tests/` |
| P11 — Surveillance | Remote monitoring/telemetry, camera/vision streams, safety analytics. | `TheWatch.P11.Surveillance/` | `TheWatch.P11.Surveillance.Tests/` |

## Supporting components

- **TheWatch.Shared**: cross-cutting infrastructure (logging, messaging, helpers).
- **TheWatch.Contracts.\***: typed contracts per service for client/server parity.
- **TheWatch.Generators**: source generators used across the solution.
- **TheWatch.Geospatial**: geospatial engine and APIs (`TheWatch.Geospatial.Tests` for coverage).
- **Dashboard & Admin surfaces**: `TheWatch.Dashboard`, `TheWatch.Admin`, `TheWatch.Admin.CLI`, and `TheWatch.Admin.RestAPI`.
- **Client apps**: `TheWatch.Mobile` (MAUI) with `TheWatch.Mobile.Tests`.
- **Aspire orchestration**: `TheWatch.Aspire.AppHost` and `TheWatch.Aspire.ServiceDefaults` for local composition.

## Navigation tips

- Use the solution file `TheWatch.sln` to load all projects in an IDE.
- Build/test per service when iterating locally to reduce resource usage.
- Contracts and shared libraries are referenced by most services—update them cautiously and run at least one downstream service test suite when changing them.
