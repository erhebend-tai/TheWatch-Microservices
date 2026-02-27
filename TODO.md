# TheWatch — Master TODO List

> 365 items organized by stage. Status: `[ ]` pending, `[x]` done, `[~]` in progress.
> See `ROADMAP.md` for full stage descriptions. See `DOD_SECURITY_ANALYSIS.md` for compliance gap analysis.

---

## Stage 5: Database Layer (Items 1–25)

### 5A. Entity Framework Core Integration
- [x] 1. Add `Microsoft.EntityFrameworkCore.SqlServer` to all 10 service .csproj files
- [x] 2. Add `Microsoft.EntityFrameworkCore.Tools` for migrations CLI
- [x] 3. Create `WatchCoreDbContext` for P1 CoreGateway with UserProfile + PlatformConfig entities
- [x] 4. Create `VoiceEmergencyDbContext` for P2 with Incident + Dispatch entities
- [x] 5. Create `MeshNetworkDbContext` for P3 with MeshNode + Message + Channel entities
- [x] 6. Create `WearableDbContext` for P4 with Device + HeartbeatRecord entities
- [x] 7. Create `AuthSecurityDbContext` for P5 with User + RefreshToken + Role entities
- [x] 8. Create `FirstResponderDbContext` for P6 with Responder + CheckIn entities
- [x] 9. Create `FamilyHealthDbContext` for P7 with FamilyGroup + Member + CheckIn + Vital + Alert entities
- [x] 10. Create `DisasterReliefDbContext` for P8 with DisasterEvent + Shelter + Resource + EvacRoute entities
- [x] 11. Create `DoctorServicesDbContext` for P9 with DoctorProfile + Appointment + TelehealthSession entities
- [x] 12. Create `GamificationDbContext` for P10 with Player + Badge + Challenge + Leaderboard entities
- [x] 13. Add `IEntityTypeConfiguration<T>` for all entities (indexes, constraints, value converters)
- [x] 14. Create initial EF migration per service (`dotnet ef migrations add InitialCreate`)

### 5B. SQL Server Schema
> **NOTE**: Items 15-21 are still pending and should be prioritized before Stage 9 work begins. These schema adaptations ensure the EfRepository layer (converted in Session 13) operates against correct table structures with proper indexes, constraints, and seed data. Without these, all 10 services run against auto-generated EF schemas that lack the production-grade constraints defined in the spec documents.

- [x] 15. Map P0 `UniversalMeasurementsDB` tables from `00_Schema.sql`–`10_Devices.sql` to EF seed data
- [x] 16. Adapt P2 DDL from `Watch_Program_02_VoiceEmergency.md` to EF entity configs
- [x] 17. Adapt P3 DDL from `Watch-MeshNetwork.md` (19 tables) to EF entity configs
- [x] 18. Adapt P4 DDL from `Program4_Watch_Wearable.sql` to EF entity configs
- [x] 19. Adapt P5 DDL from `Watch-AuthSecurity/` (4 parts) to EF entity configs
- [x] 20. Adapt P8 DDL from `Watch-DisasterRelief/` to EF entity configs (18 tables)
- [x] 21. Create seed data scripts for development (test users, sample incidents, demo families)

### 5C. Repository Pattern + Aspire
- [x] 22. Extract `IRepository<T>` generic interface from existing service interfaces
- [x] 23. Implement EF-backed repository classes for all 10 services
- [x] 24. Add SQL Server container resource to Aspire AppHost (`AddSqlServer`)
- [x] 25. Configure connection strings via Aspire service discovery for all services

---

## Stage 6: Real-Time & Events (Items 26–45)

### 6A. SignalR
- [x] 26. Add `Microsoft.AspNetCore.SignalR` to P2, P6, P7 services (built into ASP.NET Core; CORS updated for SignalR)
- [x] 27. Create `IncidentHub` in P2 — real-time incident status feed (via SignalRGenerator)
- [x] 28. Create `DispatchHub` in P2 — responder dispatch status streaming (via SignalRGenerator)
- [x] 29. Create `ResponderHub` in P6 — live responder GPS streaming (via SignalRGenerator)
- [x] 30. Create `CheckInHub` in P7 — check-in notifications to family members (via SignalRGenerator)
- [x] 31. Add SignalR client to MAUI app (`HubConnectionBuilder`)
- [x] 32. Add SignalR client to Dashboard for live incident/health updates
- [x] 33. Implement reconnection logic with exponential backoff in both clients

### 6B. Kafka Event Bus
- [x] 34. Add Kafka container to Aspire AppHost
- [x] 35. Create `IEventPublisher` interface in TheWatch.Shared
- [x] 36. Implement Kafka publisher with `Confluent.Kafka` producer
- [x] 37. Publish `IncidentCreated` event from P2 on incident creation
- [x] 38. Publish `DispatchRequested` event from P2 on dispatch creation
- [x] 39. Consume `IncidentCreated` in P6 to auto-query nearby responders
- [x] 40. Consume `DispatchRequested` in P3 to broadcast mesh alert
- [x] 41. Implement dead letter queue for failed event processing
- [x] 42. Add event sourcing audit log for all domain events

### 6C. Push Notifications
- [x] 43. Add Firebase Admin SDK to P2 for FCM push notifications
- [x] 44. Create `NotificationService` in TheWatch.Shared for cross-service notifications
- [x] 45. Implement MAUI push notification handlers (Android `FirebaseMessagingService`, iOS delegate)

---

## Stage 7: Geospatial Engine (Items 46–60)

### 7A. PostGIS
- [x] 46. Add PostgreSQL + PostGIS container to Aspire AppHost
- [x] 47. Deploy `11_Geospatial.sql` schema (28 tables, 5 schemas, 12 spatial functions)
- [x] 48. Create `GeospatialDbContext` with Npgsql + NetTopologySuite for geometry types
- [x] 49. Create `IGeospatialService` interface (nearest-N, within-radius, route calculation)
- [x] 50. Implement `PostGisGeospatialService` with `ST_DWithin`, `ST_Distance`, `ST_MakePoint`

### 7B. Mapping Service
- [x] 51. Create `TheWatch.Geospatial` microservice project (new project in solution)
- [x] 52. Implement nearest-N responder query with expanding radius algorithm
- [x] 53. Implement incident zone polygon creation and querying
- [x] 54. Implement evacuation route calculation for P8 disaster scenarios
- [x] 55. Implement geofencing for P7 family check-in zones
- [x] 56. Add geospatial service to Aspire AppHost orchestration

### 7C. Client Maps
- [x] 57. Add Leaflet.js map library to MAUI project (via JS interop — more suitable for Blazor Hybrid than native Maps control)
- [x] 58. Create `MapPage.razor` with responder placement pins and incident zones
- [x] 59. Implement real-time responder movement on map (SignalR + map updates)
- [x] 60. Add Leaflet map component to Dashboard for incident overview

---

## Stage 8: Auth & Security Hardening (Items 61–80)

### 8A. Identity Provider
- [x] 61. Replace P5 in-memory user store with EF-backed `AspNetCore.Identity`
- [x] 62. Implement Argon2id password hashing (replace PBKDF2)
- [x] 63. Implement TOTP-based MFA (Google Authenticator compatible)
- [x] 64. Implement SMS MFA via Azure Communication Services
- [x] 65. Implement email magic link authentication
- [x] 66. Implement biometric passkey authentication (WebAuthn/FIDO2)
- [x] 67. Implement JWT sliding window expiration with configurable lifetime
- [x] 68. Implement refresh token rotation with automatic revocation of old tokens
- [x] 69. Add EULA versioning and acceptance tracking (from P5 spec)
- [x] 70. Add onboarding tutorial progress tracking (from P5 spec)

### 8B. Authorization
- [x] 71. Implement RBAC: Admin, Responder, FamilyMember, Doctor, Patient roles
- [x] 72. Add claims-based authorization policies to all service endpoints
- [x] 73. Create API key authentication for inter-service communication
- [x] 74. Implement rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`)
- [x] 75. Add IP-based throttling for login/register endpoints

### 8C. Security Monitoring
- [x] 76. Implement audit logging for all authentication events (login, logout, MFA, token refresh)
- [x] 77. Implement brute force detection with progressive account lockout
- [x] 78. Implement device trust scoring based on login history and location
- [x] 79. Add STRIDE threat model checks to security monitoring agent
- [x] 80. Add MITRE ATT&CK technique detection rules

---

## Stage 9: MAUI Mobile Production (Items 81–105)

### 9A. Platform Speech Recognition
- [x] 81. Implement Android `SpeechRecognizer` with continuous recognition in `SpeechListenerService`
  - Create `Platforms/Android/Services/AndroidSpeechRecognizer.cs` implementing an `ISpeechRecognitionEngine` interface with `StartAsync`/`StopAsync`/`OnResult` callback
  - Use `Android.Speech.SpeechRecognizer` with `RecognizerIntent.ActionRecognizeSpeech` and `EXTRA_PARTIAL_RESULTS` for streaming transcription
  - Wire platform recognizer into `SpeechListenerService.ListenLoopAsync()` (currently a no-op polling loop at line 61-82 of `Services/SpeechListenerService.cs`) to replace `Task.Delay(2000)` with event-driven callbacks via `ProcessTranscript()`
  - Register platform implementation in `MauiProgram.cs` using `#if ANDROID` conditional DI (following the pattern in `WatchPushNotificationService.GetPlatform()`)
- [x] 82. Implement iOS `SFSpeechRecognizer` with on-device model for privacy
  - Create `Platforms/iOS/Services/IosSpeechRecognizer.cs` using `Speech.SFSpeechRecognizer` with `SFSpeechAudioBufferRecognitionRequest` for continuous on-device recognition
  - Configure `requiresOnDeviceRecognition = true` to avoid sending audio to Apple servers (privacy requirement from memorandum)
  - Set up `AVAudioEngine` tap on input node to feed audio buffers into the recognition request
  - Call `SpeechListenerService.ProcessTranscript()` from the `SFSpeechRecognitionTask` result handler
  - Add `NSSpeechRecognitionUsageDescription` and `NSMicrophoneUsageDescription` to `Info.plist`
- [x] 83. Implement Windows `SpeechRecognizer` for desktop testing
  - Create `Platforms/Windows/Services/WindowsSpeechRecognizer.cs` using `Windows.Media.SpeechRecognition.SpeechRecognizer` with `ContinuousRecognitionSession`
  - Use `SpeechRecognitionTopicConstraint` with `WebSearch` scenario for broad vocabulary coverage
  - Wire `ResultGenerated` event to `SpeechListenerService.ProcessTranscript()`
  - Add speech capability declaration to Windows app manifest
- [x] 84. Add microphone + speech recognition runtime permission requests
  - Extend the existing `Permissions.RequestAsync<Permissions.Microphone>()` call in `SpeechListenerService.StartListeningAsync()` (line 29) to also request `Permissions.Speech` on iOS
  - Create `Helpers/PermissionHelper.cs` with a unified `RequestSpeechPermissionsAsync()` that handles platform differences: Android `RECORD_AUDIO` manifest permission, iOS `SFSpeechRecognizer.RequestAuthorization()`, Windows speech capability
  - Show user-facing explanation dialog (via `Permissions.ShouldShowRationale`) before requesting, explaining why perpetual listening is needed for emergency activation
- [x] 85. Implement battery-aware throttling (reduce polling when battery < 20%)
  - Create `Services/BatteryMonitorService.cs` that subscribes to `Battery.BatteryInfoChanged` (MAUI Essentials)
  - Define three modes: `FullListening` (continuous recognition), `ReducedListening` (5s on / 10s off), `MinimalListening` (keyword-only, 2s on / 30s off)
  - Inject `BatteryMonitorService` into `SpeechListenerService` and modify `ListenLoopAsync()` to check `Battery.ChargeLevel` and switch modes
  - Register as singleton in `MauiProgram.cs`; expose `CurrentMode` property for UI display on `HomePage.razor`
- [x] 86. Implement background service for perpetual listening (Android foreground service, iOS background task)
  - Create `Platforms/Android/Services/SpeechForegroundService.cs` extending `Android.App.Service` with `StartForeground()` and a persistent notification ("TheWatch is listening for emergencies")
  - Configure the foreground service type as `microphone` in `AndroidManifest.xml` (required for Android 14+ microphone access from background)
  - Create `Platforms/iOS/Services/IosSpeechBackgroundTask.cs` using `BGAppRefreshTaskRequest` + `BGProcessingTaskRequest` for periodic wake-up (iOS limits continuous background audio)
  - Add `UIBackgroundModes` audio entry to `Info.plist` for iOS background audio session
  - Wire the background service lifecycle into `SpeechListenerService.StartListeningAsync()` / `StopListeningAsync()` with platform-conditional `#if` blocks

### 9B. Offline Resilience
- [x] 87. Add SQLite local database to MAUI project for offline storage
  - Add `sqlite-net-pcl` (or `Microsoft.EntityFrameworkCore.Sqlite`) NuGet package to `TheWatch.Mobile.csproj`
  - Create `Data/WatchLocalDbContext.cs` with tables: `CachedIncident`, `CachedFamilyGroup`, `CachedFamilyMember`, `CachedCheckIn`, `CachedVitalReading`, `CachedUserProfile`, `OfflineQueueItem`
  - Store the database file at `FileSystem.AppDataDirectory + "/watch_local.db"` (MAUI Essentials path)
  - Register as singleton in `MauiProgram.cs` and initialize schema on startup
- [x] 88. Implement offline queue: buffer API requests when no connectivity
  - Create `Services/OfflineQueueService.cs` with `EnqueueAsync(string method, string url, string jsonBody)` that writes to `OfflineQueueItem` table with priority, timestamp, and retry count
  - Modify `AuthDelegatingHandler` (in `MauiProgram.cs`, line 64-78) to catch `HttpRequestException` and route failed requests to `OfflineQueueService` instead of throwing
  - Create `Services/ConnectivityMonitorService.cs` wrapping `Connectivity.ConnectivityChanged` (MAUI Essentials) to detect online/offline transitions
  - Display offline indicator badge on `MainLayout.razor` when `Connectivity.NetworkAccess != NetworkAccess.Internet`
- [x] 89. Implement sync engine: reconcile local SQLite with server on reconnect
  - Create `Services/SyncEngine.cs` with `SyncAllAsync()` that: (1) drains `OfflineQueueService` in FIFO order, (2) fetches latest server data for cached entities, (3) updates local SQLite
  - Wire `ConnectivityMonitorService.OnReconnected` event to trigger `SyncEngine.SyncAllAsync()` automatically
  - Add retry with exponential backoff for each queue item (max 5 retries before moving to dead letter)
  - Log sync results via `ILogger<SyncEngine>` using the existing Serilog infrastructure
- [x] 90. Implement conflict resolution: server-wins with local notification
  - Add `LastModifiedUtc` column to all cached entity tables in `WatchLocalDbContext`
  - In `SyncEngine`, compare local `LastModifiedUtc` with server response timestamps; if server is newer, overwrite local and raise `OnConflictResolved` event
  - Display a Radzen `NotificationService.Notify()` toast (same pattern as `SOSPage.razor` line 153) when a conflict is auto-resolved, showing what changed
  - Store conflict history in a `ConflictLog` SQLite table for debugging
- [x] 91. Cache user profile, family data, and recent incidents locally
  - Create `Services/CacheService.cs` with typed methods: `CacheUserProfileAsync(UserInfoDto)`, `CacheFamilyGroupAsync(FamilyGroupDto)`, `CacheIncidentsAsync(List<IncidentDto>)`, `CacheVitalsAsync(Guid memberId, List<VitalReadingDto>)`
  - Modify `WatchApiClient` methods (`GetCurrentUserAsync`, `GetFamilyGroupAsync`, `GetRecentIncidentsAsync`, `GetMemberVitalsAsync`) to write-through to cache on successful API calls and fall back to cache on failure
  - Pre-populate cache on login (after successful `WatchAuthService.LoginAsync()`) by fetching and caching core data
  - Add cache TTL (configurable, default 24 hours) with stale-while-revalidate pattern
- [x] 92. Offline emergency mode: activate P3 mesh network fallback when no internet
  - Create `Services/MeshFallbackService.cs` that activates when `ConnectivityMonitorService` detects no internet for > 5 seconds
  - Use Bluetooth LE (via `Plugin.BLE` NuGet or MAUI Essentials `IBluetoothAdapter`) to discover nearby TheWatch devices running mesh mode
  - Implement a simplified mesh relay: broadcast SOS messages as BLE advertisements containing incident type, location, and user ID (matching P3 MeshNetwork message format from `TheWatch.P3.MeshNetwork.Mesh` namespace)
  - Display "Mesh Mode Active" indicator on `HomePage.razor` and `SOSPage.razor` when fallback is engaged

### 9C. Native Features
- [x] 93. Implement camera integration for evidence photo/video capture
  - Create `Services/CameraService.cs` wrapping `MediaPicker.CapturePhotoAsync()` and `MediaPicker.CaptureVideoAsync()` (MAUI Essentials)
  - Add `Permissions.RequestAsync<Permissions.Camera>()` before capture, with rationale dialog explaining evidence collection
  - Return a `CapturedMedia` record containing file path, MIME type, file size, and capture timestamp
  - Configure Android `FileProvider` in `AndroidManifest.xml` for secure file URI sharing; add `NSCameraUsageDescription` to iOS `Info.plist`
- [x] 94. Implement high-accuracy GPS mode during active emergencies
  - Create `Services/EmergencyLocationService.cs` that switches from `Geolocation.GetLastKnownLocationAsync()` (currently used in `SOSPage.razor` line 129 and `MapPage.razor` line 69) to `Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5)))`
  - Start a location streaming loop (`while active`) that pushes updates every 3 seconds to `WatchApiClient` and updates the `WatchMap` component on `MapPage.razor`
  - Auto-activate when SOS is triggered (from `SOSPage.OnSOSActivated()`), auto-deactivate when incident is resolved
  - Respect battery throttling from `BatteryMonitorService` — drop to 10s interval when battery < 15%
- [x] 95. Implement haptic feedback patterns (SOS confirm, alert received, check-in reminder)
  - Create `Services/HapticService.cs` defining named patterns: `SOSConfirm` (3 strong pulses), `AlertReceived` (2 medium pulses), `CheckInReminder` (1 soft pulse), `EmergencyAlarm` (rapid 5-pulse)
  - Use `HapticFeedback.Perform(HapticFeedbackType.LongPress)` (MAUI Essentials) for basic patterns; fall back to `Vibration.Vibrate(TimeSpan)` with timed sequences for complex patterns (extending the existing vibration call in `SOSPage.razor` line 150)
  - Inject `HapticService` into `SOSPage.razor`, `HomePage.razor`, and `WatchPushNotificationService` to trigger patterns on appropriate events
  - Add user preference toggle in `ProfilePage.razor` to disable haptics, stored via `Preferences.Set("haptics_enabled", bool)`
- [x] 96. Implement biometric authentication gate (fingerprint/face before app access)
  - Create `Services/BiometricGateService.cs` wrapping `WebAuthenticator` or platform-specific biometric APIs
  - On Android: use `AndroidX.Biometric.BiometricPrompt` with `BIOMETRIC_STRONG` authenticator type
  - On iOS: use `LocalAuthentication.LAContext.EvaluatePolicyAsync(LAPolicy.DeviceOwnerAuthenticationWithBiometrics)`
  - Integrate into `MobileAuthStateProvider.GetAuthenticationStateAsync()` — after token validation, prompt biometric if `Preferences.Get("biometric_gate", false)` is enabled
  - Add biometric toggle to `ProfilePage.razor` alongside existing logout functionality
- [x] 97. Implement background location tracking with user consent flow
  - Create `Services/LocationTrackingService.cs` with opt-in consent flow using a dedicated `ConsentPage.razor` explaining data usage, retention, and sharing policies. EXPLAIN HOW VITAL SERVICES WORK AND REQUIRE LOCATION.
  - On Android: request LOCATION ALWAYS. Require at least `ACCESS_BACKGROUND_LOCATION` permission (separate from foreground); create a `LocationForegroundService` paired with `SpeechForegroundService` (item 86) to share the foreground notification
  - On iOS: request location ALWAYS. REQUIRE at least request `NSLocationAlwaysAndWhenInUseUsageDescription`; use `CLLocationManager` with `AllowsBackgroundLocationUpdates = true` and `SignificantLocationChangeMonitoring` for battery efficiency
  - Store location history in local SQLite (`CachedLocation` table) and batch-upload to P6 responder tracking endpoint every 30 seconds when online
- [x] 98. Add deep link handling for push notification tap → specific page
  - Extend `PushNotificationData.GetDeepLinkRoute()` (in `Services/WatchPushNotificationService.cs`, line 146-165) to cover all page routes: `/sos`, `/map`, `/health`, `/profile`, `/evidence/{id}`, `/incidents/{id}`, `/report/{id}`
  - In `WatchPushNotificationService.HandleNotificationTapped()`, call `NavigationManager.NavigateTo(data.GetDeepLinkRoute())` to navigate to the target page
  - On Android: handle notification tap intents in `MainActivity.cs` `OnNewIntent()` override, extract data extras, and forward to `WatchPushNotificationService.HandleNotificationTapped()`
  - On iOS: the existing `WatchPushNotificationDelegate.DidReceiveNotificationResponse()` already extracts data — wire its `HandleNotificationTapped()` call to trigger navigation via `MainThread.BeginInvokeOnMainThread()`

### 9D. Evidence Collection
- [x] 99. Create `EvidencePage.razor` for video recording with GPS/timestamp overlay
  - Create `Components/Pages/EvidencePage.razor` at route `/evidence` with `@attribute [Authorize]`; add nav entry to `NavMenu.razor` (currently has Home, SOS, Map, Health, Profile tabs)
  - Embed a camera preview via Blazor JS interop calling `navigator.mediaDevices.getUserMedia({video: true})` in `wwwroot/js/evidence-capture.js`
  - Overlay GPS coordinates (from `EmergencyLocationService`) and UTC timestamp on the video feed using a positioned `<div>` rendered by Blazor
  - Use `MediaRecorder` API in JS to record video chunks; pass them back to C# via JS interop for local storage
  - Wire start/stop recording controls with `RadzenButton` components following the Radzen patterns in `SOSPage.razor`
- [x] 100. Implement photo capture with automatic metadata (GPS, time, device ID)
  - Create `Services/EvidenceMetadataService.cs` that stamps each capture with: GPS lat/lon/accuracy (from `Geolocation`), UTC timestamp, `DeviceInfo.Current.Model`, `DeviceInfo.Current.Manufacturer`, `DeviceInfo.Current.Platform`, and authenticated user ID (from `WatchAuthService.CurrentUser`)
  - After `CameraService.CapturePhotoAsync()`, create an `EvidenceRecord` containing the file path, metadata JSON, and SHA-256 hash (for chain-of-custody, item 102)
  - Store `EvidenceRecord` in local SQLite (`WatchLocalDbContext`) with status: `Captured`, `Queued`, `Uploaded`, `Verified`
  - Write EXIF metadata into JPEG files using `ExifLib` or similar NuGet where platform-supported
- [x] 101. Implement speech-to-text incident reporting in SITREP format
  - Create `Services/SitrepService.cs` that structures speech-to-text output into the SITREP framework fields: Situation, Incident, Terrain, Resources, Evacuation, Personnel (per memorandum requirements)
  - Reuse the platform speech recognizer (items 81-83) via `SpeechListenerService.ProcessTranscript()` in a dedicated dictation mode (longer utterances, no phrase matching)
  - Create `Components/Shared/SitrepForm.razor` with fields pre-populated from voice dictation, editable by user before submission; use `RadzenTextArea` and `RadzenFormField` components
  - Submit completed SITREP to P2 VoiceEmergency via `WatchApiClient.CreateIncidentAsync()` with the SITREP text in the `Description` field
- [x] 102. Implement chain-of-custody: SHA-256 hash + timestamp + device signature per artifact
  - Create `Services/ChainOfCustodyService.cs` with `ComputeHashAsync(Stream fileStream)` using `System.Security.Cryptography.SHA256`
  - Generate a `CustodyRecord` containing: SHA-256 hash, UTC timestamp, device ID (`DeviceInfo.Current.Idiom` + unique identifier), user ID, GPS location at time of capture, and a digital signature (HMAC-SHA256 using a device-local key stored in `SecureStorage`)
  - Append `CustodyRecord` entries to a local chain (SQLite table `CustodyChain`) — each new entry references the previous entry's hash, forming a linked hash chain
  - Include the `CustodyRecord` JSON as metadata when uploading evidence to the server (item 104)
- [x] 103. Implement client-side content moderation (nudity detection before upload)
  - Add `Microsoft.ML.OnnxRuntime` NuGet to `TheWatch.Mobile.csproj` for on-device ML inference
  - Create `Services/ContentModerationService.cs` that loads a lightweight NSFW classification ONNX model (e.g., Yahoo Open NSFW) from the app bundle (`Resources/Raw/`)
  - Run inference on captured photos before upload; if confidence exceeds threshold (configurable, default 0.7), flag the evidence and prompt user with a warning dialog before proceeding
  - Log moderation results in the `EvidenceRecord` metadata (flagged, confidence score, model version) for audit trail
- [x] 104. Create evidence upload queue with retry and progress tracking
  - Create `Services/EvidenceUploadService.cs` that reads `EvidenceRecord` entries with status `Captured` or `Queued` from local SQLite and uploads them via `WatchApiClient` with multipart form data
  - Implement chunked upload for large video files (>10MB) with resumable upload support using `Content-Range` headers
  - Add per-file progress tracking using `IProgress<double>` and expose `UploadProgressChanged` event for UI binding
  - Wire into `ConnectivityMonitorService` — pause uploads when offline, resume when online; respect `BatteryMonitorService` — pause uploads when battery < 10%
  - Display upload queue status on `EvidencePage.razor` with a `RadzenProgressBar` per item (similar to loading pattern in `SOSPage.razor`)
- [x] 105. Create `IncidentReportPage.razor` — post-incident questionnaire (SITREP framework)
  - Create `Components/Pages/IncidentReportPage.razor` at route `/report/{IncidentId:guid}` with `@attribute [Authorize]`
  - Build a multi-step wizard using `RadzenSteps` component with sections: (1) Situation Overview, (2) Incident Details, (3) Terrain/Environment, (4) Resources Used, (5) Evacuation Actions, (6) Personnel Involved, (7) Attached Evidence
  - Pre-populate fields from the incident data via `WatchApiClient.GetRecentIncidentsAsync()` and from local `EvidenceRecord` entries associated with the incident
  - Include `SitrepForm.razor` (from item 101) in the wizard for voice-dictated sections
  - On submit, POST the completed SITREP report to P2 VoiceEmergency and navigate to a confirmation page; store a local copy in SQLite for offline access

---

## Stage 10: Containerization & CI/CD (Items 106–120)

### 10A. Docker
- [x] 106. Create multi-stage `Dockerfile` for each of the 10 microservices
- [x] 107. Create `Dockerfile` for Dashboard (Blazor Server)
- [x] 108. Create `docker-compose.yml` for full local development stack (services + SQL + Redis + Kafka)
- [x] 109. Create `docker-compose.override.yml` for development-specific config (ports, volumes)
- [x] 110. Add `.dockerignore` files to prevent including bin/obj/node_modules

### 10B. Kubernetes / Helm
- [x] 111. Create Helm chart template with deployment, service, ingress per microservice
- [x] 112. Configure HPA for P2 VoiceEmergency and P6 FirstResponder (emergency surge scaling)
- [x] 113. Create ConfigMaps for service configuration (feature flags, endpoints)
- [x] 114. Create Kubernetes Secrets for database credentials and JWT signing keys
- [x] 115. Configure Ingress Controller with TLS termination and path-based routing

### 10C. CI/CD
- [x] 116. Create GitHub Actions workflow: build + test on every PR
- [x] 117. Create GitHub Actions workflow: build Docker images and push to Azure Container Registry
- [x] 118. Create GitHub Actions workflow: deploy to staging on merge to `develop`
- [x] 119. Create GitHub Actions workflow: deploy to production on merge to `main` (manual approval gate)
- [x] 120. Add CodeQL security scanning and dependency review to PR workflow

---

## Stage 11: Cloud Deployment (Items 121–140)

### 11A. Azure Infrastructure
- [x] 121. Create Terraform/Bicep module for Azure SQL Database (10 databases, geo-replicated)
- [x] 122. Create Terraform/Bicep module for Azure Cosmos DB (MongoDB API, multi-region writes)
- [x] 123. Create Terraform/Bicep module for Azure Redis Cache (session store, rate limiting)
- [x] 124. Create Terraform/Bicep module for Azure Service Bus (event queues)
- [x] 125. Create Terraform/Bicep module for Azure Key Vault (secrets, certificates, JWT signing keys)
- [x] 126. Create Terraform/Bicep module for Azure Container Apps or AKS cluster
- [x] 127. Create Terraform/Bicep module for Azure Storage (evidence blobs)

### 11B. Azure Service Integration
- [x] 128. Integrate Azure SignalR Service (config toggle, additive to self-hosted SignalR)
- [x] 129. Integrate Azure Maps for geospatial (config toggle, alternative to PostGIS)
- [x] 130. Integrate Azure Communication Services for SMS/email (config toggle, alternative to Firebase)
- [x] 131. Integrate Application Insights for distributed tracing and APM (config toggle, additive to Serilog)

### 11C. GCP Services
- [x] 132. Integrate Google Speech-to-Text API for P2 voice recognition (server-side processing)
- [x] 133. Integrate Google Vision API for evidence analysis and content moderation
- [x] 134. Integrate Firebase Cloud Messaging for push notifications
- [x] 135. Integrate Google Healthcare API (FHIR) for P7/P9 health data interoperability

### 11D. Cloudflare Edge
- [x] 136. Configure Cloudflare CDN for static assets (MAUI WebView, Dashboard)
- [x] 137. Deploy Cloudflare Workers for edge authentication validation
- [x] 138. Configure Cloudflare WAF rules for API protection
- [x] 139. Set up Cloudflare Zero Trust for admin/dashboard access
- [x] 140. Configure Argo Tunnels for secure service exposure without public IPs

---

## Stage 12: Advanced Features & Compliance (Items 141–150)

### 12A. ML/AI
- [x] 141. Train gunshot detection audio classifier for P2 active shooter scenarios
- [x] 142. Implement fall detection from wearable accelerometer data stream (P4)
- [x] 143. Implement vital sign anomaly detection with configurable thresholds (P7)
- [x] 144. Implement responder dispatch optimization (minimize response time across geography)

### 12B. Compliance
- [x] 145. Implement HIPAA-compliant data handling for P7/P9 health records (encryption at rest + transit, access logging, BAA requirements)
- [x] 146. Implement COPPA compliance for P7 child data (parental consent, data minimization)
- [x] 147. Implement GDPR right-to-erasure across all services (cascade delete with audit)
- [x] 148. Implement SOX-expanded audit framework from memorandum (quarterly reporting, signed attestations)

### 12C. Graph & Observability
- [x] 149. Deploy `Watch-GraphDB.sql` graph tables (node/edge) for social graph and incident correlation
- [x] 150. Wire 10 monitoring agents into CI/CD with Prometheus metrics, Grafana dashboards, and PagerDuty alerting

---

## Stage 13: AWS Infrastructure & Services (Items 151–180)

### 13A. AWS Compute & Networking
- [x] 151. Create Terraform module for AWS VPC with public/private subnets across 3 AZs, NAT gateways, and VPC flow logs enabled to S3 for network forensics
- [x] 152. Create Terraform module for ECS Fargate cluster with 10 service task definitions (one per microservice), each with CPU/memory limits matching Helm chart resource requests, and ECS Exec enabled for debugging
- [x] 153. Create Terraform module for Application Load Balancer with path-based routing rules mapping `/api/core/*` to P1, `/api/voice/*` to P2, etc., with TLS termination using ACM certificates and HTTP-to-HTTPS redirect
- [x] 154. Configure ECS Service Connect (AWS Cloud Map) for inter-service discovery — each service registers a DNS name (e.g., `p1-core.thewatch.local`) replacing Aspire service discovery in AWS deployments
- [x] 155. Create Terraform module for AWS App Mesh (Envoy sidecar) with mTLS between all 10 services, circuit breaker policies (5xx threshold: 5 in 30s), and retry policies (3 retries, 2s timeout) per virtual service
- [x] 156. Configure ECS auto-scaling policies: P2 VoiceEmergency and P6 FirstResponder scale on `ECSServiceAverageCPUUtilization > 60%` (min 2, max 20 tasks); all other services scale on request count per target (min 1, max 10)
- [x] 157. Create Terraform module for Amazon API Gateway (HTTP API type) as alternative ingress to ALB — configure JWT authorizer using Cognito user pool, request throttling (10,000 rps burst, 5,000 rps sustained), and usage plans with API keys for third-party integrations
- [x] 158. Configure AWS Global Accelerator with two endpoints (us-east-1, us-west-2) for anycast routing, health checks on `/health` endpoint per service, and automatic failover with less than 30s detection

### 13B. AWS Data Services
- [x] 159. Create Terraform module for Amazon RDS SQL Server (Multi-AZ, `db.r6i.xlarge`) with 10 databases matching the EF DbContext schemas, automated backups (35-day retention), and Performance Insights enabled
- [x] 160. Create Terraform module for Amazon Aurora PostgreSQL (PostGIS extension) for TheWatch.Geospatial service — configure read replicas in 2 AZs, enable Babelfish for SQL Server compatibility layer if needed for migration
- [x] 161. Create Terraform module for Amazon ElastiCache Redis cluster (cluster mode enabled, 3 shards x 2 replicas) for session store, rate limiting counters, and SignalR backplane — configure encryption at rest (KMS) and in transit (TLS)
- [x] 162. Create Terraform module for Amazon MSK (Managed Streaming for Kafka) cluster with 3 brokers across 3 AZs, topic auto-creation disabled, and IAM authentication — create topics matching existing Kafka event bus configuration
- [x] 163. Create Terraform module for Amazon S3 buckets: `thewatch-evidence-{env}` (Intelligent-Tiering, versioning, object lock for legal hold), `thewatch-backups-{env}` (Glacier Deep Archive lifecycle at 90 days), `thewatch-static-{env}` (CloudFront origin)
- [x] 164. Configure S3 event notifications to SQS to Lambda pipeline for evidence processing: on `PutObject` to evidence bucket, trigger Lambda that validates SHA-256 chain-of-custody hash, runs content moderation via Rekognition, and writes metadata to DynamoDB
- [x] 165. Create Terraform module for Amazon DynamoDB table `thewatch-audit-log` with on-demand capacity, partition key `ServiceName#Date`, sort key `Timestamp#EventId`, TTL on `ExpiresAt` (365 days), and DynamoDB Streams enabled for real-time audit forwarding

### 13C. AWS Security & Identity
- [x] 166. Create Terraform module for Amazon Cognito User Pool with custom attributes mapping to TheWatch roles (Admin, Responder, FamilyMember, Doctor, Patient), MFA enforcement (TOTP + SMS), and advanced security features (compromised credentials detection, adaptive authentication)
- [x] 167. Create IAM roles with least-privilege policies for each ECS task: P2 gets `rekognition:DetectModerationLabels` + `transcribe:StartStreamTranscription`, P7/P9 get `healthlake:*`, P5 gets `cognito-idp:Admin*`, all get `s3:PutObject` scoped to their service prefix
- [x] 168. Create Terraform module for AWS KMS customer-managed keys: `thewatch-data-key` (RDS, ElastiCache, S3 encryption), `thewatch-jwt-key` (JWT signing), `thewatch-evidence-key` (evidence bucket SSE with key rotation every 365 days)
- [x] 169. Configure AWS Secrets Manager for all service secrets: database connection strings, Kafka SASL credentials, Redis AUTH tokens, Firebase server key, third-party API keys — create rotation Lambda for database credentials (30-day rotation)
- [x] 170. Create Terraform module for AWS WAF v2 WebACL attached to ALB/API Gateway: rate-based rule (2,000 requests/5min per IP), SQL injection rule set, XSS rule set, known bad inputs rule set, geo-restriction (block OFAC-sanctioned countries), and IP reputation list
- [x] 171. Configure AWS GuardDuty with S3 protection, ECS runtime monitoring, and Malware Protection — create EventBridge rule to forward HIGH and CRITICAL findings to SNS topic linked to PagerDuty integration
- [x] 172. Configure AWS Security Hub with CIS AWS Foundations Benchmark v1.4, AWS Foundational Security Best Practices, and NIST 800-53 standards — create custom actions for automated remediation of non-compliant resources via Lambda

### 13D. AWS Observability & DevOps
- [x] 173. Create Terraform module for Amazon CloudWatch: log groups per service (`/ecs/thewatch/{service}`, 90-day retention), metric filters for error rate and latency percentiles, composite alarms for service health, and Synthetics canaries for endpoint monitoring
- [x] 174. Configure AWS X-Ray tracing integrated with OpenTelemetry SDK in each service — create X-Ray groups per service, sampling rules (1% for health checks, 100% for errors, 5% for normal traffic), and service map dashboards
- [x] 175. Create CodePipeline with CodeBuild stages: Source (GitHub webhook) to Build (dotnet publish + Docker build) to Test (dotnet test + Trivy container scan) to Deploy-Staging (ECS rolling update) to Manual-Approval to Deploy-Prod (ECS blue/green via CodeDeploy)
- [x] 176. Create Terraform module for Amazon ECR repositories (one per service + dashboard) with lifecycle policies (keep last 20 tagged images, expire untagged after 7 days), image scanning on push, and cross-region replication to DR region
- [x] 177. Configure AWS Backup plan: daily RDS snapshots (35-day retention), weekly DynamoDB backups (90-day retention), monthly S3 cross-region copy to DR region — create backup vault with vault lock (72-hour cool-off, 365-day min retention)
- [x] 178. Create CloudFormation StackSets for multi-account deployment: `thewatch-dev`, `thewatch-staging`, `thewatch-prod` accounts with Service Control Policies preventing region usage outside us-east-1 and us-west-2
- [x] 179. Configure Amazon EventBridge event bus `thewatch-events` with rules routing domain events (IncidentCreated, DispatchRequested, SOSActivated) to targets: CloudWatch Logs (audit), Lambda (processing), SNS (notifications), Step Functions (orchestration)
- [x] 180. Create AWS Step Functions state machine for incident lifecycle orchestration: SOS received to validate caller to dispatch nearest responder to start evidence collection to monitor resolution to generate post-incident report — with error handling, retries, and timeouts at each step

---

## Stage 14: P11 Surveillance Integration (Items 181–195)

> P11.Surveillance microservice was added in Session 34 but never fully integrated into infrastructure, monitoring, or testing. These items close that gap.

### 14A. P11 Infrastructure Wiring
- [x] 181. Add P11.Surveillance service definition to `docker-compose.yml` (port 5112, depends_on sql-server + kafka + redis, environment variables matching P1-P10 pattern)
- [x] 182. Add `WatchSurveillanceDB` to `docker-compose.yml` sql-init sidecar database creation script
- [x] 183. Add `p11-surveillance` to Azure Terraform `locals.container_apps` in `terraform/main.tf` (cpu=0.5, memory=1Gi, min=1, max=10) and `WatchSurveillanceDB` to `locals.databases` (tier=standard)
- [x] 184. Add `p11-surveillance` to AWS Terraform ECS task definitions, ALB path routing (`/api/surveillance/*`), and RDS database list
- [x] 185. Add `p11-surveillance` to GCP Terraform Cloud Run service definitions
- [x] 186. Add P11 service entry to Helm `values.yaml` services map, HPA config, and ingress path rules
- [x] 187. Add `footage-submitted` and `crime-location-reported` Kafka topics to Azure Service Bus and AWS MSK Terraform modules

### 14B. P11 Monitoring & Testing
- [x] 188. Add `ISurveillanceClient` to `HealthController.cs` parallel health check array (currently only 11 services checked)
- [x] 189. Add P11 Surveillance to Dashboard `ServiceHealth.razor` and `Home.razor` service grid
- [x] 190. Create `TheWatch.P11.Surveillance.Tests` project with `WebApplicationFactory<Program>` integration tests (camera CRUD, footage submission, detection retrieval, crime location)
- [x] 191. Add P11 to CI/CD matrix: `ci.yml` build/test matrix, `docker-publish.yml` image build, deploy workflows
- [x] 192. Create EF Core migration for P11 (`dotnet ef migrations add InitialCreate`)

### 14C. P11 Data & Events
- [x] 193. Create `SurveillanceSeeder.cs` seed data class (sample cameras, test footage entries, demo crime locations) and register as `IWatchDataSeeder`
- [x] 194. Add Kafka consumer in P2 VoiceEmergency for `footage-submitted` events (auto-correlate footage with active incidents by proximity)
- [x] 195. Add Kafka consumer in P6 FirstResponder for `crime-location-reported` events (alert nearby responders)

---

## Stage 15: Cloud Provider Implementation (Items 196–215)

> Cloud provider stubs (GCP, Cloudflare) throw `NotImplementedException`. These items replace stubs with real SDK integrations.

### 15A. GCP Provider Implementations
- [x] 196. Implement `GoogleSpeechToTextProvider.TranscribeAsync()` — replace stub with real `Google.Cloud.Speech.V2.SpeechClient` call (`RecognizeAsync` with `RecognitionConfig`)
- [x] 197. Implement `GoogleSpeechToTextProvider.StartStreamingAsync()` — bidirectional streaming via `StreamingRecognize` with `StreamingRecognitionConfig`
- [x] 198. Implement `GoogleVisionProvider.AnalyzeImageAsync()` — real `Google.Cloud.Vision.V1.ImageAnnotatorClient` with `SafeSearchAnnotation` for content moderation
- [x] 199. Implement `GoogleVisionProvider.ExtractTextAsync()` and `DetectLabelsAsync()` — OCR and label detection via Vision API
- [x] 200. Implement `GoogleHealthcareProvider` FHIR methods — `UpsertPatientAsync`, `GetPatientAsync`, `SearchPatientsAsync`, `CreateObservationAsync`, `GetObservationsAsync`, `CreateEncounterAsync`, `ExportPatientDataAsync` using Google Healthcare API FHIR store

### 15B. Cloudflare Provider Implementations
- [x] 201. Implement `CloudflareCdnService` — `PurgeCacheAsync`, `PurgeAllAsync`, `PurgeByTagAsync`, `GetAnalyticsAsync` via Cloudflare API v4
- [x] 202. Implement `CloudflareWorkerAuthService` — `ValidateAccessTokenAsync`, `GetIdentityAsync` via Cloudflare Access JWT validation
- [x] 203. Implement `CloudflareWafService` — `DeployRulesAsync`, `DeployRateLimitsAsync`, `GetRecentEventsAsync`, `BlockIpAsync` via Cloudflare Firewall Rules API
- [x] 204. Implement `CloudflareTunnelService` — `GetTunnelStatusAsync`, `GetConnectionsAsync` via Cloudflare Tunnels API
- [x] 205. Implement `CloudflareZeroTrustService.ValidateServiceTokenAsync()` via Cloudflare Zero Trust API

### 15C. Azure Provider Completions
- [ ] 206. Implement `AzureMapsGeospatialService` remaining methods — replace any ConcurrentDictionary caching with Redis-backed distributed cache
- [ ] 207. End-to-end provider toggle tests — verify each service starts correctly with `Azure:Enabled=true`, `Gcp:Enabled=true`, and `Cloudflare:Enabled=true` flags in appsettings

---

## Stage 16: Contract Client Wiring (Items 208–220)

> Per-service contract libraries exist (TheWatch.Contracts.*) but consuming services still use raw HttpClient in some cases. These items complete the typed client integration.

### 16A. Inter-Service Typed Clients
- [ ] 208. Wire `IVoiceEmergencyClient` into P6 FirstResponder for incident correlation on dispatch
- [ ] 209. Wire `ICoreGatewayClient` into P2 VoiceEmergency for reporter profile lookup on incident creation
- [ ] 210. Wire `IFirstResponderClient` into P2 VoiceEmergency for nearest-responder query on dispatch
- [ ] 211. Wire `IGeospatialClient` into P6 FirstResponder for spatial proximity calculations
- [ ] 212. Wire `IAuthSecurityClient` into Admin.RestAPI gateway for user management passthrough (replace direct HTTP)
- [ ] 213. Wire `IFamilyHealthClient` into P9 DoctorServices for patient family context during appointments
- [ ] 214. Wire `IDisasterReliefClient` into P6 FirstResponder for shelter location during disaster dispatches
- [ ] 215. Wire `ISurveillanceClient` into P2 VoiceEmergency for footage correlation with active incidents

### 16B. Contract Validation
- [ ] 216. Add contract compatibility tests — verify each contract DTO matches the server's actual response schema
- [ ] 217. Add Polly resilience policies (retry, circuit breaker, timeout) to all typed clients in `ServiceClientBase`
- [ ] 218. Add distributed tracing correlation headers (`X-Correlation-Id`) to all inter-service calls via `ServiceClientBase`
- [ ] 219. Add service-to-service API key authentication to all typed clients (use `ApiKeyAuthHandler` from Shared)
- [ ] 220. Integration test: full incident lifecycle across P2→P6→P3→P11 via typed clients

---

## Stage 17: Production Hardening (Items 221–250)

> Items that close the gap between "works in development" and "survives production traffic."

### 17A. Database Production Readiness
- [ ] 221. Run `dotnet ef migrations add` for P5 AuthSecurity (Identity tables migration missing — currently relies on `EnsureCreatedAsync`)
- [ ] 222. Create SQL Server maintenance plan: index rebuild schedule, statistics update, log file management for all 11 databases
- [ ] 223. Add ROWVERSION/concurrency tokens to all entities that support concurrent updates (Incident, Dispatch, Responder, FamilyMember)
- [x] 224. Add database connection resilience: `EnableRetryOnFailure(5)` in all DbContext configurations (currently only Aspire default retry)
- [ ] 225. Replace remaining ConcurrentDictionary stores: P1 ConfigService (move to distributed config), P1 device registrations (move to DB), P5 IpThrottling and SmsMfa OTP tracking (move to Redis)

### 17B. API Production Quality
- [ ] 226. Add FluentValidation to all request DTOs across P1-P11 (currently only P5 AuthModels has `[Required]` annotations)
- [x] 227. Add global exception handler middleware to all services (structured ProblemDetails responses per RFC 9457)
- [x] 228. Add request/response logging middleware with PII redaction (mask SSN, phone, email in logs)
- [ ] 229. Add API versioning (`Asp.Versioning.Http`) to all services — v1 prefix for current endpoints, header-based version negotiation
- [x] 230. Add response compression (Brotli + gzip) to all services
- [ ] 231. Add ETag/If-None-Match conditional response support for GET endpoints

### 17C. Mobile Production Readiness
- [x] 232. Implement `SyncEngine.SendAsync()` — already implemented with full HTTP method dispatch and retry logic
- [ ] 233. Bundle ONNX model for content moderation (ContentModerationService currently has framework but no model file in Resources/Raw/)
- [ ] 234. Add app crash reporting integration (Sentry or AppCenter) with breadcrumb logging
- [ ] 235. Add app update check flow — compare installed version against server minimum version, force update if below
- [ ] 236. Implement certificate pinning for API calls (prevent MITM on evidence uploads)
- [ ] 237. Add accessibility: screen reader labels on all interactive elements, high contrast mode, dynamic font scaling

### 17D. Security Hardening
- [x] 238. Add security headers middleware to all services: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Strict-Transport-Security`, `Content-Security-Policy`, `Referrer-Policy`
- [ ] 239. Implement request signing for evidence uploads (prevent replay attacks on chain-of-custody)
- [ ] 240. Add JWT key rotation mechanism — support multiple valid signing keys during rotation window
- [ ] 241. Pen test preparation: run OWASP ZAP against all service endpoints, fix findings
- [ ] 242. Add anti-forgery tokens to Dashboard and Admin portal forms
- [ ] 243. Implement secrets rotation runbook: database passwords, JWT keys, API keys, Firebase credentials — document zero-downtime rotation procedure

### 17E. Observability Production Readiness
- [ ] 244. Add custom Prometheus metrics to all services: request duration histograms, active incident gauge, dispatch response time, SOS activation counter
- [ ] 245. Create Grafana dashboard JSON templates for each service (import via CI/CD)
- [ ] 246. Add health check dependencies: verify SQL Server, Redis, Kafka, PostGIS connectivity in `/health` endpoint (not just HTTP 200)
- [ ] 247. Add distributed tracing span enrichment: user ID, incident ID, device ID on all traces
- [ ] 248. Implement log-based alerting rules: >5 auth failures/min, >10 5xx errors/min, evidence upload failure spike, SOS endpoint latency >2s
- [ ] 249. Add canary endpoints for synthetic monitoring (return known payload for comparison)
- [ ] 250. Create runbook documentation: incident response, rollback procedure, database recovery, secret rotation

---

## Stage 18: Integration & End-to-End Testing (Items 251–265)

> No end-to-end tests exist that exercise the full incident lifecycle across services. These items build confidence that the system works as a whole.

### 18A. End-to-End Test Infrastructure
- [x] 251. Create `TheWatch.Integration.Tests` project with docker-compose test harness (spin up all services + infra, run tests, tear down)
- [x] 252. Implement test fixture: register user via P5, login, get JWT, use token for all subsequent calls
- [x] 253. Implement test: full SOS lifecycle — create incident (P2) → dispatch responder (P2/P6) → submit evidence (P2) → resolve incident → generate SITREP report
- [x] 254. Implement test: family health flow — create family (P7) → add members → submit vital readings → trigger medical alert → doctor appointment (P9)
- [x] 255. Implement test: disaster relief flow — declare disaster (P8) → open shelters → allocate resources → activate mesh network (P3) → track evacuees

### 18B. Load & Stress Testing
- [x] 256. Create k6 or NBomber load test scripts for P2 incident creation (target: 1,000 concurrent SOS activations)
- [x] 257. Create k6 load test for P5 auth (target: 500 concurrent logins with MFA)
- [x] 258. Create k6 load test for SignalR hub connections (target: 10,000 concurrent WebSocket connections)
- [x] 259. Create k6 load test for evidence upload (target: 100 concurrent 50MB video uploads)
- [x] 260. Benchmark sub-2-second SOS activation: measure end-to-end latency from SOS button press to first responder notification

### 18C. Chaos & Resilience Testing
- [ ] 261. Test SQL Server failover: kill primary, verify services recover via Aspire retry/failover
- [ ] 262. Test Kafka broker failure: kill broker, verify events queue and replay on recovery
- [ ] 263. Test Redis failure: kill Redis, verify rate limiting degrades gracefully (not blocking requests)
- [ ] 264. Test inter-service failure: kill P6, verify P2 dispatch degrades gracefully with appropriate error messages
- [ ] 265. Test MAUI offline mode end-to-end: disconnect network, trigger SOS, verify mesh fallback, reconnect, verify sync

---

## Stage 19: FIPS Cryptographic Compliance (Items 266–280)

> **Source:** NIST 800-171 SC-12/SC-13, DISA STIG V-222570-572, CMMC Level 2. FIPS 140-2/140-3 validated cryptography is the #1 audit failure for DoD contractors. Every item in this stage is **CRITICAL** or **HIGH**.

### 19A. FIPS Mode & TLS Hardening
- [x] 266. Enable FIPS-compliant TLS across all services — configure Kestrel `SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13` in a shared `ConfigureWatchKestrel()` extension method. Disable TLS 1.0/1.1 globally. Restrict cipher suites to FIPS-approved only: TLS_AES_256_GCM_SHA384, TLS_AES_128_GCM_SHA256, TLS_CHACHA20_POLY1305_SHA256. Disable CBC mode ciphers. Apply to all 12 service `Program.cs` files + Admin.RestAPI + Dashboard. Files: new shared extension, 14 `Program.cs` files. [NIST SC-8, STIG V-222596] **CRITICAL**
- [ ] 267. Enable .NET FIPS mode — add `"System.Security.Cryptography.UseFipsAlgorithms": true` to `runtimeconfig.template.json` for all 14 web projects. Verify no crypto calls use non-FIPS algorithms (MD5, DES, RC2, RijndaelManaged). Add Roslyn analyzer rule to ban non-FIPS crypto APIs via `Directory.Build.props`. Files: 14 `runtimeconfig.template.json`, `Directory.Build.props`. [NIST SC-13, STIG V-222570] **CRITICAL**
- [ ] 268. Audit all `System.Security.Cryptography` usage across codebase — grep for MD5, SHA1, DES, TripleDES, RC2, RijndaelManaged, Rfc2898DeriveBytes (PBKDF2 with SHA1). Replace any non-FIPS calls with FIPS-validated equivalents (SHA256, SHA512, AES-GCM). Document each replacement with STIG finding ID. Files: full codebase scan + targeted fixes. [NIST SC-13, STIG V-222570] **HIGH**

### 19B. JWT Asymmetric Key Migration
- [x] 269. Migrate JWT signing from symmetric HMAC-SHA256 to asymmetric RSA-2048 (or ECDSA P-256) — P5 AuthSecurity signs tokens with RSA private key; all consumer services validate with RSA public key only. This eliminates the shared-secret risk where compromising any one service compromises all JWT validation. Files: `WatchAuthExtensions.cs`, `SecurityGenerator.cs`, P5 `AuthService`, P5 `Program.cs`. [NIST SC-12, SC-13, STIG V-222641] **CRITICAL**
- [ ] 270. Implement JWT key rotation — support multiple valid signing keys during rotation window via `IssuerSigningKeys` (plural) in `TokenValidationParameters`. Add `kid` (Key ID) header to issued tokens. Create `JwtKeyRotationService` that reads active/previous keys from Key Vault. Define 90-day maximum key lifetime per NIST 800-57. Files: `WatchAuthExtensions.cs`, `SecurityGenerator.cs`, new `JwtKeyRotationService.cs` in Shared. [NIST SC-12, STIG V-222641] **HIGH**
- [ ] 271. Store JWT signing keys in HSM/Key Vault — create Azure Key Vault key (RSA-2048, HSM-backed) and AWS KMS equivalent. P5 AuthSecurity loads signing key from Key Vault at startup. Consumer services load public key from Key Vault or well-known JWKS endpoint (`/.well-known/jwks.json`). Add JWKS endpoint to P5. Files: P5 `Program.cs`, new `JwksEndpoint.cs`, Terraform Key Vault modules. [NIST SC-12, STIG V-222570] **HIGH**

### 19C. Mutual TLS & Inter-Service Encryption
- [x] 272. Implement mutual TLS (mTLS) between all microservices — each service presents a client certificate and validates the peer's certificate. Configure Kestrel `ClientCertificateMode.RequireCertificate` on all inter-service endpoints. Use a shared CA (self-signed for dev, PKI-issued for production). Create `ConfigureWatchMtls()` shared extension. Files: shared extension, 14 `Program.cs`, certificate generation scripts. [NIST SC-8, STIG V-222641, IA-5(2)] **CRITICAL**
- [ ] 273. Configure Kafka SASL_SSL — change `KAFKA_LISTENER_SECURITY_PROTOCOL_MAP` from PLAINTEXT to SASL_SSL. Generate broker and client keystores/truststores. Update `KafkaEventBusGenerator.cs` to emit SASL_SSL producer/consumer config when `build_property.WatchKafkaSecurity` is set. Update `docker-compose.yml` Kafka service definition. Files: `docker-compose.yml`, `KafkaEventBusGenerator.cs`, Shared Kafka config. [NIST SC-8, STIG V-222596] **HIGH**
- [ ] 274. Configure Redis AUTH + TLS — add `requirepass` to Redis container, enable TLS on port 6380, disable unencrypted port 6379. Update all StackExchange.Redis connection strings to include `password=...,ssl=true,sslProtocols=tls12`. Files: `docker-compose.yml`, all `appsettings.json` Redis connection strings. [NIST SC-28, AC-3] **HIGH**
- [ ] 275. Configure PostgreSQL SSL — enable `ssl = on` in PostGIS container, generate server certificate. Update Npgsql connection strings with `SSL Mode=Require;Trust Server Certificate=false`. Files: `docker-compose.yml`, Geospatial `appsettings.json`. [NIST SC-8, STIG V-222596] **HIGH**

### 19D. Password Hashing FIPS Assessment
- [x] 276. Evaluate Argon2id FIPS compliance — document risk acceptance for using Argon2id (not FIPS-validated but cryptographically superior) OR implement FIPS-approved PBKDF2 fallback with HMAC-SHA-512 and 600,000+ iterations (per OWASP 2024 guidance). If PBKDF2 fallback chosen, implement alongside Argon2id with `PasswordVerificationResult.SuccessRehashNeeded` for transparent migration (same pattern as existing D040). Create a configuration toggle `Security:UseFipsPasswordHashing` in appsettings. Files: P5 `Argon2PasswordHasher.cs`, new `FipsPbkdf2PasswordHasher.cs`, P5 `Program.cs`. [NIST SC-13, STIG V-222570] **HIGH**

### 19E. Certificate Management
- [x] 277. Add CAC/PIV/x.509 certificate authentication scheme — add `AddCertificate()` to authentication builder in `WatchAuthExtensions.cs`. Create `CacCertificateValidator` that validates against DoD PKI certificate chain (DoD Root CA 3, DoD Intermediate CAs). Map certificate subject CN/UPN to WatchUser identity. Add `X509Certificate2` extraction middleware. Files: `WatchAuthExtensions.cs`, new `CacCertificateValidator.cs`, P5 `Program.cs`. [NIST IA-2(12), STIG V-222524] **HIGH**
- [ ] 278. Create certificate management infrastructure — generate dev CA + per-service certificates using `openssl` or `dotnet dev-certs`. Create `docker/certs/` directory with generation script. Add certificate volume mounts to `docker-compose.yml`. Document certificate renewal procedure. Files: new `docker/certs/generate-certs.sh`, `docker-compose.yml` volume mounts. [NIST SC-12] **MEDIUM**

### 19F. Mobile Cryptography
- [ ] 279. Implement certificate pinning in MAUI `WatchApiClient` — pin the server's TLS certificate public key hash in `HttpClientHandler.ServerCertificateCustomValidationCallback`. Store pin hashes in `SecureStorage`. Support pin rotation with backup pins. Files: `TheWatch.Mobile/MauiProgram.cs` HttpClient configuration. [NIST SC-8, OWASP A02] **HIGH**
- [ ] 280. Encrypt local SQLite database in MAUI — use SQLCipher or `Microsoft.Data.Sqlite` with `Password=` connection string to encrypt the `watch_local.db` file at rest. Derive encryption key from device-specific secret stored in `SecureStorage`. Files: `TheWatch.Mobile/Data/WatchLocalDbContext.cs`. [NIST SC-28, STIG V-222588] **HIGH**

---

## Stage 20: Data Protection — CUI at Rest & in Transit (Items 281–295)

> **Source:** NIST 800-171 SC-28, DISA STIG V-222588-589, CMMC Level 2. Any data qualifying as CUI (incident reports, medical records, location data, evidence) must be encrypted at rest with approved algorithms.

### 20A. SQL Server Encryption
- [ ] 281. Enable Transparent Data Encryption (TDE) on all 11 SQL Server databases — create database encryption keys using AES-256. For Docker dev: configure TDE via init script after database creation. For production: use Azure SQL TDE (automatic) and AWS RDS TDE (KMS-managed). Document that SQL Server Developer edition supports TDE for testing but production requires Standard or Enterprise. Files: `docker/sql/init/` scripts, Terraform Azure/AWS RDS modules. [NIST SC-28, STIG V-222588] **CRITICAL**
- [x] 282. Implement column-level encryption for PII/CUI fields — use SQL Server Always Encrypted or application-level AES-256-GCM encryption for: `WatchUser.Email`, `WatchUser.PhoneNumber`, `VitalReading.Value`, `MedicalAlert.Description`, `Incident.ReporterPhone`, `Evidence.GpsLatitude/GpsLongitude`, `DoctorProfile.LicenseNumber`, `FamilyMember.DateOfBirth`. Create `IFieldEncryptor` interface in Shared with `EncryptAsync`/`DecryptAsync`. Wire into EF value converters. Files: new `FieldEncryptor.cs` in Shared, EF `IEntityTypeConfiguration` updates across P2/P5/P7/P8/P9. [NIST SC-28, STIG V-222589] **CRITICAL**
- [ ] 283. Encrypt database connection strings at rest — remove plaintext connection strings from all `appsettings.json` files. Use .NET User Secrets for development, Docker secrets for compose, Key Vault references for production. Verify no connection strings are committed to Git (add pattern to `.gitignore` and secret scanner). Files: all `appsettings.json`, `docker-compose.yml`, `.gitignore`. [NIST SC-28, STIG V-222542] **HIGH**

### 20B. PostgreSQL & Backup Encryption
- [ ] 284. Enable PostgreSQL encryption at rest for PostGIS — configure LUKS full-disk encryption on the PostgreSQL data volume in Docker. For production: use Azure Database for PostgreSQL (encryption automatic) and AWS RDS PostgreSQL (KMS-managed). Enable `pgcrypto` extension for application-level column encryption of geospatial data. Files: `docker-compose.yml` PostGIS volume config, Terraform modules. [NIST SC-28, STIG V-222588] **HIGH**
- [ ] 285. Encrypt all database backups — modify `docker-compose.sqlbackup.yml` to encrypt backup files with AES-256 using `openssl enc`. Store encryption keys separately from backup media (Key Vault or separate secrets file). For cloud: AWS S3 SSE-KMS, Azure Blob SSE. Add backup integrity verification (SHA-256 checksum per backup file). Files: `docker-compose.sqlbackup.yml`, backup scripts. [NIST MP-4, SC-28] **MEDIUM**
- [ ] 286. Implement backup rotation and secure deletion — define backup retention: daily backups for 35 days, weekly for 90 days, monthly for 1 year. Auto-delete expired backups with secure overwrite. For cloud: use S3 lifecycle policies and Azure Blob lifecycle management (already in Terraform but verify encryption). Files: backup scripts, Terraform lifecycle policies. [NIST MP-6, SC-28] **MEDIUM**

### 20C. Secrets Management
- [x] 287. Remove all default/fallback passwords from source code — eliminate `SQL_SA_PASSWORD:-Watch@Str0ngP4ss!`, `POSTGRES_PASSWORD:-Watch@Geo2024!`, and any other default credentials from `docker-compose.yml` and `docker-compose.override.yml`. Require `.env` file (gitignored) for local development. Create `.env.example` with placeholder values. Files: `docker-compose.yml`, `docker-compose.override.yml`, new `.env.example`, `.gitignore`. [NIST IA-5, STIG V-222662] **HIGH**
- [ ] 288. Create Docker secrets for sensitive configuration — migrate database passwords, JWT keys, API keys, Redis passwords from environment variables to Docker secrets (`docker secret create`). Update compose files to use `secrets:` stanza. For Kubernetes: already using K8s Secrets (verify they're encrypted at rest with etcd encryption). Files: `docker-compose.yml`, `docker-compose.override.yml`. [NIST SC-28, IA-5] **MEDIUM**
- [ ] 289. Implement secrets rotation automation — create `SecretRotationService` or PowerShell scripts that rotate: database passwords (SQL Server `ALTER LOGIN`), Redis AUTH password, Kafka SASL credentials, JWT signing keys (item 270), API keys. Define zero-downtime rotation procedure where new and old secrets are valid during rotation window. Document rotation schedule: 90 days for JWT keys, 30 days for database passwords. Files: new rotation scripts, `TheWatch.Admin.CLI` cmdlets. [NIST IA-5, STIG V-222662] **MEDIUM**

### 20D. Data Classification & CUI Marking
- [x] 290. Create CUI data classification matrix — document every entity/field that contains or may contain CUI. Categories: PII (names, emails, phones, DOB), PHI (vital readings, medical alerts, doctor notes), law enforcement sensitive (incident details, evidence, surveillance footage), geolocation (GPS coordinates of incidents/responders). Map each to NIST 800-171 CUI category and required protection level. Files: new `docs/data-classification-matrix.md`. [NIST SC-28, MP-4] **HIGH**
- [x] 291. Implement CUI data marking in API responses — add `X-CUI-Category` response header to endpoints that return CUI data. Create `CuiMarkingMiddleware` that inspects response content type and route to determine CUI classification. Log all CUI access events to audit trail. Files: new `CuiMarkingMiddleware.cs` in Shared, wire into all services. [NIST MP-3] **MEDIUM**
- [ ] 292. Implement data-at-rest encryption verification — create a health check that verifies TDE is enabled on all databases, Redis TLS is active, Kafka SASL_SSL is configured. Add to `/health` endpoint as a security subsystem check. Fail health check in production if any encryption is disabled. Files: shared health check class, all `Program.cs` health registrations. [NIST SC-28] **MEDIUM**

### 20E. Data Retention & Disposal
- [ ] 293. Define data retention policies per entity type — PII: retain while account active + 30 days after deletion request (GDPR). PHI: 6 years per HIPAA. Evidence: 7 years per legal hold. Audit logs: 1 year minimum. Geolocation: 90 days. Implement `IDataRetentionPolicy` interface with per-entity TTLs. Files: new `DataRetentionPolicy.cs` in Shared, `docs/data-retention-policy.md`. [NIST MP-6, SI-12] **MEDIUM**
- [ ] 294. Implement automated data purge jobs — create Hangfire recurring jobs per service that delete expired data per retention policy. Use soft-delete (mark as deleted) with a 30-day grace period before hard delete. Log all purge events to audit trail. Run SHA-256 verification on evidence before purge to confirm chain-of-custody integrity. Files: new purge jobs in P2/P5/P7/P8/P9/P11. [NIST MP-6, SI-12] **MEDIUM**
- [ ] 295. Implement NIST 800-88 media sanitization for deleted CUI — when hard-deleting CUI records from SQL Server, overwrite the data pages (not just mark as deallocated). For file storage (evidence), overwrite file contents before deletion. For Redis, ensure `DEL` commands remove data from memory immediately (verify `lazyfree-lazy-expire no`). Files: data purge implementation, Redis config. [NIST MP-6] **LOW**

---

## Stage 21: Authentication & Password Hardening — STIG Compliance (Items 296–310)

> **Source:** DISA STIG V-222536-546 (passwords), V-222524-534 (auth), NIST 800-171 IA-2/IA-5. Password requirements are highly specific in STIG.

### 21A. STIG Password Policy
- [x] 296. Increase minimum password length to 15 characters — change `options.Password.RequiredLength = 8` to `RequiredLength = 15` in P5 `Program.cs` line 62. Update `RegisterRequest` DTO `[MinLength]` annotation from 8 to 15. Update MAUI `LoginPage.razor` and `RegisterPage` UI validation messages. Files: P5 `Program.cs`, P5 `AuthModels.cs`, MAUI auth pages. [STIG V-222536] **HIGH**
- [x] 297. Reduce max failed login attempts from 5 to 3 — change `options.Lockout.MaxFailedAccessAttempts = 5` to `MaxFailedAccessAttempts = 3` in P5 `Program.cs` line 66. Implement progressive lockout escalation: 1st lockout = 15 min, 2nd = 1 hour, 3rd = 24 hours, 4th+ = admin unlock required. Track escalation level in `WatchUser.LockoutEscalationLevel` column. Files: P5 `Program.cs`, `WatchUser` model, P5 `AuthService`. [STIG V-222432] **HIGH**
- [x] 298. Implement password history — create `PasswordHistory` entity with columns: `UserId`, `HashedPassword`, `ChangedAtUtc`. On password change, hash the new password and compare against last 5 stored hashes. Reject if any match. Store new hash after successful change. Files: new `PasswordHistory.cs` entity, `AuthIdentityDbContext`, P5 `AuthService.ChangePasswordAsync()`, EF migration. [STIG V-222546] **HIGH**
- [ ] 299. Implement password age limits — add `PasswordLastChangedUtc` and `PasswordMinAgeEnforcedUntil` columns to `WatchUser`. Enforce 60-day maximum age: on login, if password is >60 days old, return `PasswordExpired` flag in `LoginResponse` and redirect to password change. Enforce 24-hour minimum age: reject password changes within 24 hours of last change (prevents rapid cycling to exhaust history). Files: `WatchUser` model, P5 `AuthService`, `LoginResponse` DTO, EF migration. [STIG V-222544, V-222545] **HIGH**
- [ ] 300. Enforce password change delta — on password change, verify the new password differs from the old password in at least 8 character positions (per STIG V-222541). Implement Levenshtein distance or character-position comparison. Reject changes that are too similar. Files: P5 `AuthService.ChangePasswordAsync()`. [STIG V-222541] **MEDIUM**
- [ ] 301. Add password strength meter to MAUI and Dashboard registration — use zxcvbn-style password strength estimator. Display strength indicator (Weak/Fair/Good/Strong) during registration and password change. Reject passwords that score below "Good" threshold. Files: MAUI registration page, Dashboard registration form, new `PasswordStrengthService.cs`. [STIG V-222536-540, OWASP A07] **LOW**

### 21B. Session Management (STIG)
- [ ] 302. Enforce `HttpOnly` and `Secure` flags on all cookies — audit all `CookieOptions` in Dashboard and Admin portal. Set `HttpOnly = true`, `Secure = true`, `SameSite = SameSiteMode.Strict` on every cookie. Verify SignalR connection cookies also have these flags. Files: Dashboard `Program.cs`, Admin `Program.cs`, any cookie-setting middleware. [STIG V-222575, V-222576] **HIGH**
- [ ] 303. Implement session timeout and idle timeout — for Dashboard/Admin Blazor Server: configure circuit timeout to 15 minutes of inactivity (per STIG). For JWT: access token lifetime maximum 30 minutes, refresh token maximum 8 hours (not 7 days). Force re-authentication after 8 hours regardless of activity. Files: Dashboard/Admin `Program.cs` circuit options, P5 JWT configuration. [NIST AC-12, STIG V-222578] **HIGH**
- [x] 304. Prevent session fixation — ensure JWT token IDs (`jti` claim) are unique per issuance. On login, invalidate any existing refresh tokens for the user before issuing new ones. Track active sessions per user in Redis with maximum concurrent session limit (default: 5). Files: P5 `AuthService.LoginAsync()`, P5 `AuthService.RefreshAsync()`. [STIG V-222579] **MEDIUM**

### 21C. Authentication Strengthening
- [ ] 305. Move SMS OTP storage from ConcurrentDictionary to Redis — replace in-memory `ConcurrentDictionary<string, (string Code, DateTime Expiry)>` in P5 `SmsMfaService` with Redis hash entries using TTL. Key format: `otp:{phone}:{code_hash}`. Limit verification attempts to 3 per code (track in Redis counter). Files: P5 `SmsMfaService.cs`. [NIST IA-2, STIG V-222530] **HIGH**
- [ ] 306. Enforce device fingerprint binding on refresh tokens — make `DeviceFingerprint` required (not optional) on `RefreshTokenRequest`. Store fingerprint hash with refresh token in database. On refresh, reject if fingerprint doesn't match. Log device change attempts as security events. Files: P5 `AuthModels.cs`, P5 `AuthService.RefreshAsync()`. [NIST IA-5, SC-23] **MEDIUM**
- [ ] 307. Implement account recovery flow — create secure account recovery that doesn't weaken MFA: require email verification + security questions + admin approval for privileged accounts. Prevent recovery flow from bypassing MFA. Log all recovery attempts. Files: new recovery endpoints in P5, `AccountRecoveryService.cs`. [NIST IA-5, STIG V-222522] **MEDIUM**
- [x] 308. Implement concurrent session management — track active JWT sessions per user in Redis. Enforce maximum 5 concurrent sessions. On new login beyond limit, either reject or terminate oldest session. Provide `/api/auth/sessions` endpoint for users to view and revoke active sessions. Files: P5 `AuthService`, new `SessionManagementService.cs`, Redis session store. [NIST AC-10, AC-12] **MEDIUM**

### 21D. Authentication Audit
- [x] 309. Enhance auth audit logging to STIG requirements — every auth event must log: timestamp (UTC), user identity (or attempted identity), source IP address, source port, event type (login/logout/fail/lockout/MFA/token-refresh), success/failure, device fingerprint, user agent string. Verify all fields present in `AuditService.LogAsync()`. Files: P5 `AuditService.cs`, audit event model. [STIG V-222441-449] **HIGH**
- [x] 310. Implement login banner/consent — display DoD-required login banner before authentication: "You are accessing a U.S. Government information system. By using this system you consent to monitoring..." Require explicit acceptance before proceeding. Store acceptance in audit log. Files: MAUI `LoginPage.razor`, Dashboard login page, P5 login flow. [NIST AC-8] **MEDIUM**

---

## Stage 22: Input Validation, Error Handling & API Hardening (Items 311–325)

> **Source:** DISA STIG V-222606-611, OWASP A03/A05, NIST 800-171 SI-10/SI-11. Input validation is the largest code-level gap.

### 22A. FluentValidation Across All Services
- [x] 311. Add FluentValidation to P1 CoreGateway — add `FluentValidation.AspNetCore` NuGet package, create validators for all request DTOs (UserProfile CRUD, PlatformConfig, DeviceRegistration). Wire `AddFluentValidationAutoValidation()` in `Program.cs`. Files: P1 `.csproj`, new `Validators/` directory, P1 `Program.cs`. [STIG V-222606, OWASP A03] **HIGH**
- [x] 312. Add FluentValidation to P2 VoiceEmergency — validators for IncidentCreate, DispatchCreate, ExpandRadius, EvidenceUpload DTOs. Validate GPS coordinates are within valid ranges (-90/90 lat, -180/180 lon). Validate phone numbers match E.164 format. Files: P2 `.csproj`, new `Validators/`, P2 `Program.cs`. [STIG V-222606] **HIGH**
- [x] 313. Add FluentValidation to P3-P4 (MeshNetwork, Wearable) — validators for mesh node registration, channel creation, device pairing, heartbeat submission. Validate MAC addresses, device IDs, signal strength ranges. Files: P3/P4 `.csproj`, new `Validators/`, P3/P4 `Program.cs`. [STIG V-222606] **HIGH**
- [x] 314. Add FluentValidation to P5 AuthSecurity — enhance existing `[Required]`/`[MaxLength]` annotations with cross-field validators: password complexity rules, email domain allowlist for DoD (`.mil`, `.gov`), phone number E.164 validation, TOTP code format (exactly 6 digits). Files: P5 `.csproj`, new `Validators/`, P5 `Program.cs`. [STIG V-222606] **HIGH**
- [x] 315. Add FluentValidation to P6-P7 (FirstResponder, FamilyHealth) — validators for responder registration, check-in, vital reading submission, family group management. Validate vital sign ranges (heart rate 20-300, temp 85-110F, SpO2 0-100%). Files: P6/P7 `.csproj`, new `Validators/`, P6/P7 `Program.cs`. [STIG V-222606] **HIGH**
- [x] 316. Add FluentValidation to P8-P10 (DisasterRelief, DoctorServices, Gamification) — validators for disaster declaration, shelter management, appointment scheduling, badge award. Validate date ranges, enum values, resource quantities. Files: P8/P9/P10 `.csproj`, new `Validators/`, P8/P9/P10 `Program.cs`. [STIG V-222606] **HIGH**
- [x] 317. Add FluentValidation to P11 Surveillance + Geospatial — validators for camera registration, footage submission, crime location reporting, spatial queries. Validate coordinate systems, bounding box sanity, video file size limits. Files: P11/Geospatial `.csproj`, new `Validators/`, P11/Geospatial `Program.cs`. [STIG V-222606] **HIGH**

### 22B. Global Exception Handling
- [x] 318. Create shared `WatchProblemDetailsMiddleware` — implement RFC 9457 Problem Details middleware in `TheWatch.Shared/Security/`. Map common exceptions: `ValidationException` → 400, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404, `InvalidOperationException` → 409, unhandled → 500. Never expose stack traces, type names, or connection strings. Include `traceId` from correlation ID. Files: new `WatchProblemDetailsMiddleware.cs` in Shared. [STIG V-222610, V-222656, OWASP A05] **HIGH**
- [x] 319. Wire `WatchProblemDetailsMiddleware` into all 12 services — add `app.UseMiddleware<WatchProblemDetailsMiddleware>()` to all `Program.cs` files (P1-P11 + Geospatial). Place after authentication but before endpoint routing. Admin.RestAPI already has `GlobalExceptionMiddleware` — verify it produces RFC 9457 format. Files: 12 `Program.cs` files. [STIG V-222610] **HIGH**
- [ ] 320. Suppress detailed error responses in production — configure `builder.Services.AddProblemDetails()` with custom `ProblemDetailsOptions.CustomizeProblemDetails` that strips exception details when `IHostEnvironment.IsProduction()`. Verify `ASPNETCORE_ENVIRONMENT` is set to `Production` in all Dockerfiles and deploy manifests. Files: shared configuration, Dockerfiles, deploy manifests. [STIG V-222656, OWASP A05] **MEDIUM**

### 22C. Request Size & SSRF Protection
- [x] 321. Apply consistent Kestrel request limits to all services — create shared `ConfigureWatchKestrel()` extension: `MaxRequestBodySize = 10_485_760` (10MB), `MaxRequestHeadersTotalSize = 32_768` (32KB), `MaxRequestLineSize = 8_192` (8KB), `RequestHeadersTimeout = TimeSpan.FromSeconds(30)`, suppress Server header. Apply to all 12 services (Admin.RestAPI already has this). Files: new shared extension, 12 `Program.cs`. [NIST SC-5, STIG V-222602] **MEDIUM**
- [x] 322. Implement SSRF protection — create `SafeHttpClientHandler` that blocks requests to: private IP ranges (10.x, 172.16-31.x, 192.168.x), link-local (169.254.x — cloud metadata endpoint), localhost. Apply to all `HttpClient` instances that fetch user-supplied URLs (evidence processing, webhook delivery). Files: new `SafeHttpClientHandler.cs` in Shared. [OWASP A10] **MEDIUM**
- [ ] 323. Validate all query string parameters against allowlists — audit all `MapGet`/`MapPost` endpoints accepting query parameters. Replace raw `string` parameters with typed enums or validated DTOs. Specifically fix P5 `/api/onboarding/complete-step` which accepts raw `string step` — change to request body with validated enum. Files: affected endpoints across all services. [STIG V-222606, OWASP A03] **MEDIUM**

### 22D. CORS & CSRF
- [ ] 324. Enforce production CORS origins — remove `SetIsOriginAllowed(_ => true)` from all code paths. Create `docker-compose.production.yml` override that sets `Cors:AllowedOrigins` to production domain only. Verify `WatchCorsExtensions.cs` is used consistently by all services (currently some use `SetIsOriginAllowed`). Add CI check that greps for `SetIsOriginAllowed` and fails if found. Files: all `Program.cs` with CORS config, `WatchCorsExtensions.cs`, new CI check. [NIST AC-4, STIG V-222602] **MEDIUM**
- [ ] 325. Add anti-forgery tokens to Dashboard and Admin — implement `AntiforgeryStateProvider` for Blazor Server forms. Add `[ValidateAntiForgeryToken]` to any MVC controller actions. Configure `SameSite=Strict` on antiforgery cookies. Files: Dashboard/Admin `Program.cs`, form components. [STIG V-222603, OWASP A01] **MEDIUM**

---

## Stage 23: SDLC, Supply Chain & CI/CD Hardening (Items 326–345)

> **Source:** NIST 800-218 SSDF (mandatory per EO 14028), NIST 800-53 SA-11/SR-4, OWASP A06/A08. Supply chain security is increasingly critical for DoD contracts.

### 23A. Software Bill of Materials (SBOM)
- [x] 326. Add CycloneDX SBOM generation to CI build — install `dotnet-CycloneDX` tool in `docker-publish.yml`. Generate SBOM in CycloneDX JSON format for each service. Upload SBOMs as build artifacts. Store alongside container images in registry. EO 14028 requires SBOM for all federal software. Files: `.github/workflows/docker-publish.yml`. [NIST SR-4, SSDF PS.2, OWASP A08] **HIGH**
- [ ] 327. Generate aggregate SBOM for full solution — create a merged SBOM covering all 14 web projects + Shared + Generators. Include transitive dependencies. Capture NuGet package versions, license identifiers, and package hashes. Output as both CycloneDX and SPDX formats. Files: `scripts/generate-sbom.sh` script, CI workflow update. [NIST SR-4, SSDF PS.2] **MEDIUM**
- [ ] 328. Automate SBOM vulnerability cross-reference — after SBOM generation, run `grype` or `trivy sbom` against the SBOM to identify known CVEs. Fail builds on CRITICAL/HIGH CVEs. Generate vulnerability report as CI artifact. Files: CI workflow update. [NIST SI-2, SSDF RV.1] **MEDIUM**

### 23B. Container Security
- [x] 329. Add non-root USER directive to all 12 Dockerfiles — add `RUN adduser --disabled-password --no-create-home --uid 1001 appuser` and `USER appuser` to the `final` stage of all Dockerfiles. Verify .NET 10 `aspnet` base image supports non-root. Test that file permissions allow the app to run. Files: 12 Dockerfiles (P1-P11, Geospatial, Dashboard). [NIST CM-7, STIG V-222425] **HIGH**
- [x] 330. Add Trivy container image scanning to CI — add `aquasecurity/trivy-action@master` step to `docker-publish.yml` after image build. Scan for OS and library vulnerabilities. Fail on CRITICAL severity. Generate SARIF report and upload to GitHub Security tab. Files: `.github/workflows/docker-publish.yml`. [NIST SI-2, SSDF RV.1] **HIGH**
- [x] 331. Implement container image signing with Cosign — install `sigstore/cosign` in CI. Sign each image after push to registry. Store signatures in the same OCI registry. In Kubernetes, add `kyverno` or OPA Gatekeeper admission policy to reject unsigned images. Files: `.github/workflows/docker-publish.yml`, Helm chart admission policy. [NIST SA-10, SSDF PS.2, OWASP A08] **HIGH**
- [ ] 332. Pin base images to digest — replace `FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview` with digest-pinned reference (e.g., `@sha256:abc123...`). Create Renovate/Dependabot config to auto-update digest pins. Use `-cbl-mariner` or `-alpine` distroless variants for smaller attack surface. Files: 12 Dockerfiles. [NIST CM-2, SI-2] **MEDIUM**
- [x] 333. Add `HEALTHCHECK` with proper non-root curl alternative — current Dockerfiles use `curl -f http://localhost:8080/health` but container runs as non-root (after item 329) and may not have `curl`. Switch to `wget --no-verbose --tries=1 --spider` or custom .NET health check binary. Files: 12 Dockerfiles. [NIST CM-7] **LOW**

### 23C. CI/CD Pipeline Security
- [x] 334. Add DAST (Dynamic Application Security Testing) to staging pipeline — add OWASP ZAP automated scan step to `deploy-staging.yml`. After deploying to staging, run ZAP baseline scan against all service `/swagger` endpoints. Parse ZAP report and fail deployment on HIGH/CRITICAL findings. Archive report as CI artifact. Files: `.github/workflows/deploy-staging.yml`. [NIST SA-11, SSDF PW.7] **HIGH**
- [x] 335. Replace regex secret scanner with Gitleaks — replace the grep-based secret scanning in `security.yml` with `gitleaks/gitleaks-action@v2`. Gitleaks uses entropy analysis + regex patterns to detect secrets that simple grep misses (API keys, tokens, private keys). Configure `.gitleaks.toml` with TheWatch-specific rules. Files: `.github/workflows/security.yml`, new `.gitleaks.toml`. [NIST IA-5, SSDF PS.1] **MEDIUM**
- [x] 336. Enable NuGet audit and signature verification — create `Directory.Build.props` at solution root with `<NuGetAudit>true</NuGetAudit>`, `<NuGetAuditLevel>low</NuGetAuditLevel>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` for audit warnings. Add `<trustedSigners>` section to `nuget.config` requiring Microsoft and nuget.org repository signatures. Files: new `Directory.Build.props`, `nuget.config`. [NIST SR-4, SSDF PS.2] **MEDIUM**
- [ ] 337. Enforce signed Git commits — enable `Require signed commits` branch protection rule on `master`/`main`. Document GPG/SSH key setup for all contributors. Add CI step that verifies commit signature on PR head. Files: GitHub repository settings, new `docs/developer-setup.md` section. [SSDF PS.1, NIST CM-3] **MEDIUM**
- [ ] 338. Implement CI pipeline integrity — add `slsa-verifier` or Sigstore provenance to GitHub Actions. Generate SLSA Level 2+ provenance attestation for each build. Verify provenance before deployment. Files: CI workflow updates. [SSDF PS.2, OWASP A08] **LOW**

### 23D. Penetration Testing & Vulnerability Management
- [ ] 339. Establish penetration testing program — document annual pentest schedule with independent assessor. Define scope (all 12 services, MAUI app, Dashboard, Admin portal, infrastructure). Create pentest rules of engagement. Maintain remediation tracker linked to POA&M. Files: new `docs/pentest-program.md`. [NIST CA-8, SSDF PW.7] **HIGH**
- [ ] 340. Define vulnerability remediation SLAs — CRITICAL: 7 days, HIGH: 30 days, MEDIUM: 90 days, LOW: 180 days (aligned with DISA STIG CAT I/II/III timelines). Track in POA&M. Create Hangfire recurring job that queries NuGet vulnerability database and alerts on new findings. Files: new `VulnerabilityMonitorService.cs`, `docs/vulnerability-management-policy.md`. [SSDF RV.2] **MEDIUM**

### 23E. SSDF Attestation
- [ ] 341. Complete CISA Secure Software Self-Attestation Common Form — document compliance with all 4 SSDF practice groups: PO (toolchains: CodeQL, Dependabot, Trivy, ZAP), PS (branch protection, signed commits, image signing, SBOM), PW (threat modeling via STRIDE, code review, SAST/DAST, secure defaults), RV (vulnerability scanning, remediation SLAs). Required before any federal procurement. Files: new `docs/ssdf-attestation.md`. [SSDF, OMB M-22-18] **MEDIUM**
- [ ] 342. Document secure development lifecycle (SDL) — write SDL document covering: security requirements gathering (per CMMC/STIG), threat modeling process, secure coding standards (Roslyn analyzers, banned APIs), code review checklist, security testing gates, release approval, vulnerability response. Files: new `docs/secure-development-lifecycle.md`. [SSDF PO.1, PO.4, PW.1] **MEDIUM**

### 23F. Network & Infrastructure Hardening
- [x] 343. Remove host port mappings for databases in production compose — create `docker-compose.production.yml` that does NOT expose `1433`, `5432`, `6379`, `9092` to the host. Only expose ports 5100-5112 (services) and 443 (gateway) via Docker internal networking. Files: new `docker-compose.production.yml`. [NIST SC-7, AC-4] **HIGH**
- [x] 344. Add Docker Compose resource limits — add `deploy.resources.limits` to all service definitions: `cpus: '0.5'`, `memory: 512M` for standard services; `cpus: '1.0'`, `memory: 1G` for P2/P5/P6 (high-traffic). Add `deploy.resources.reservations` for minimum guarantees. Prevents single service DoS. Files: `docker-compose.yml`. [NIST SC-5] **MEDIUM**
- [x] 345. Define Kubernetes NetworkPolicy segmentation — create NetworkPolicy manifests restricting inter-service communication to only required paths: P2↔P6 (dispatch), P7↔P9 (health), P1↔all (gateway), P5→all (auth validation). Block direct access between unrelated services (e.g., P10 Gamification cannot reach P11 Surveillance). Files: new `helm/templates/network-policies.yaml`. [NIST SC-7, AC-4] **MEDIUM**

---

## Stage 24: CMMC Assessment Documentation & Process (Items 346–365)

> **Source:** CMMC 2.0 Level 2 requires documented policies, plans, and evidence for all 14 NIST 800-171 control families. A C3PAO assessor will request these artifacts. Without them, technical controls cannot be scored.

### 24A. System Security Plan (SSP)
- [ ] 346. Write System Security Plan (SSP) — create comprehensive SSP documenting all 110 NIST 800-171 requirements mapped to TheWatch implementation. For each requirement: implementation status (Implemented/Partially Implemented/Planned/Not Applicable), responsible party, description of implementation, evidence location (file paths, screenshots, CI artifacts). Use NIST SP 800-171A assessment objectives as template. Files: new `docs/ssp/system-security-plan.md` (estimated 50-80 pages). [NIST CA-1, CA-5, CMMC L2] **HIGH**
- [ ] 347. Create system boundary diagram — visual diagram showing: all 12 microservices, API gateway, Dashboard, Admin portal, MAUI app, databases (SQL Server, PostgreSQL, Redis), message brokers (Kafka), external services (Firebase, Azure, GCP, Cloudflare), network zones (public, DMZ, private, data), authentication boundaries, encryption points, CUI data flows. Use draw.io, Visio, or Mermaid. Files: new `docs/ssp/system-boundary-diagram.png` + `.drawio` source. [NIST PL-2, SC-7] **HIGH**
- [ ] 348. Create data flow diagrams (DFDs) — Level 0 (context), Level 1 (service interactions), Level 2 (internal service flows) for the SOS lifecycle, evidence collection, and family health monitoring paths. Annotate CUI boundaries, encryption points, and authentication gates. Files: new `docs/ssp/data-flow-diagrams/` directory. [NIST PL-2, SC-7] **MEDIUM**

### 24B. Plan of Action & Milestones (POA&M)
- [ ] 349. Create POA&M from security analysis findings — create POA&M spreadsheet/document tracking all open findings from `DOD_SECURITY_ANALYSIS.md`. Columns: Finding ID, Title, NIST 800-171 Control, Severity, Status, Responsible Party, Planned Completion Date, Actual Completion Date, Evidence, Risk Rating. Update as items are remediated. Files: new `docs/POA&M.md`. [NIST CA-5, CMMC L2] **HIGH**
- [ ] 350. Define risk acceptance process — for findings that cannot be immediately remediated (e.g., Argon2id FIPS validation, physical protection for cloud), document formal risk acceptance with: risk description, likelihood, impact, compensating controls, approving authority, review date. Files: `docs/POA&M.md` risk acceptance section. [NIST CA-5, RA-3] **MEDIUM**

### 24C. Policy Documents (14 Families)
- [ ] 351. Write Access Control Policy — define: role definitions (Admin, Responder, Doctor, FamilyMember, Patient, ServiceAccount), access provisioning procedure, access deprovisioning on termination, principle of least privilege, quarterly access review process, remote access requirements (TLS + MFA), wireless access restrictions. Files: new `docs/policies/access-control-policy.md`. [NIST AC-1] **HIGH**
- [ ] 352. Write Identification & Authentication Policy — define: password requirements (15 chars, complexity, 60-day max age, 5-generation history), MFA requirements by role (admin: mandatory, user: encouraged), account lockout policy (3 attempts), CAC/PIV acceptance procedures, service account management, authenticator management lifecycle. Files: new `docs/policies/identification-authentication-policy.md`. [NIST IA-1] **HIGH**
- [ ] 353. Write Audit & Accountability Policy — define: auditable events list, audit record content requirements, log retention periods (1 year security, 90 days general), log review frequency (weekly automated, monthly manual), audit trail protection (HMAC integrity, immutable storage), audit failure response. Files: new `docs/policies/audit-accountability-policy.md`. [NIST AU-1] **HIGH**
- [ ] 354. Write Incident Response Plan (IRP) — define: incident categories (security breach, data exposure, system compromise, DoS, insider threat), severity levels (P1-P4), detection mechanisms (STRIDE, MITRE, rate limiting alerts), response team roles, containment procedures, eradication steps, recovery procedures, evidence preservation, post-incident review. Define 72-hour DoD reporting requirement. Files: new `docs/incident-response-plan.md`. [NIST IR-1, IR-8] **HIGH**
- [ ] 355. Write Configuration Management Policy — define: baseline configurations (IaC Terraform, Docker images), change control process (PR review, CI gates, approval), least functionality (disable unnecessary services, ports, protocols), authorized software list (NuGet packages, base images), configuration monitoring. Files: new `docs/policies/configuration-management-policy.md`. [NIST CM-1, CM-9] **MEDIUM**
- [ ] 356. Write System & Communications Protection Policy — define: CUI encryption requirements (TLS 1.2+ in transit, AES-256 at rest), boundary protection (network segmentation, API gateway), FIPS 140-2 cryptographic module requirements, session management rules, mobile code restrictions. Files: new `docs/policies/system-communications-protection-policy.md`. [NIST SC-1] **MEDIUM**
- [ ] 357. Write Media Protection Policy — define: CUI media handling (encrypted backups, secure transport), media sanitization (NIST 800-88), backup encryption, evidence storage lifecycle, disposal procedures for decommissioned hardware. Files: new `docs/policies/media-protection-policy.md`. [NIST MP-1] **MEDIUM**
- [ ] 358. Write Risk Assessment Policy — define: annual risk assessment schedule, vulnerability scanning frequency (weekly automated, quarterly manual), risk categorization (FIPS 199), risk treatment options (accept, mitigate, transfer, avoid), threat intelligence sources. Files: new `docs/policies/risk-assessment-policy.md`. [NIST RA-1] **MEDIUM**
- [ ] 359. Write Personnel Security Policy — define: personnel screening requirements (background checks), access agreements, CUI handling training, access termination procedures (disable account within 24 hours), transfer procedures. Files: new `docs/policies/personnel-security-policy.md`. [NIST PS-1] **MEDIUM**
- [ ] 360. Write remaining policy documents (AT, MA, PE, CA, SI) — create policy documents for: Awareness & Training (annual training, CUI handling, secure coding), Maintenance (system maintenance windows, patching cadence), Physical Protection (cloud provider responsibility documentation), Security Assessment (assessment schedule, POA&M process), System & Information Integrity (flaw remediation, malicious code protection, monitoring). Files: new `docs/policies/` — 5 additional policy documents. [NIST AT-1, MA-1, PE-1, CA-1, SI-1] **MEDIUM**

### 24D. Logging & SIEM
- [x] 361. Deploy centralized SIEM — add Seq (Datalust) or Elastic Security container to `docker-compose.siem.yml`. Configure Serilog sinks: `Serilog.Sinks.Seq` for dev, `Serilog.Sinks.Elasticsearch` for production. Forward all security audit events, auth events, and error events. Create correlation rules for: multi-service brute force, token theft patterns, lateral movement. Files: new `docker-compose.siem.yml`, all `appsettings.json` Serilog config. [NIST AU-6, STIG V-222481] **HIGH**
- [ ] 362. Implement audit log integrity — create `LogIntegrityService` that computes HMAC-SHA256 over each audit log entry using a signing key from Key Vault. Store HMAC alongside log entry. Periodically verify log chain integrity (detect any tampering). Alternative: use append-only storage (Azure Immutable Blob, AWS S3 Object Lock). Files: new `LogIntegrityService.cs` in Shared, `SecurityAuditMiddleware.cs` update. [NIST AU-9, STIG V-222507] **HIGH**
- [x] 363. Configure log retention policies — set Serilog `rollingInterval: Day`, `retainedFileCountLimit: 365` for security logs, `retainedFileCountLimit: 90` for general logs. Archive security logs older than 90 days to immutable storage (S3 Object Lock or Azure Immutable Blob). Configure SIEM retention to match. Files: all `appsettings.json` Serilog configuration. [NIST AU-11] **MEDIUM**
- [x] 364. Implement PII masking in logs — create `PiiMaskingEnricher` for Serilog that detects and masks: email addresses (`u***@domain.com`), phone numbers (`***-**-1234`), IP addresses (`192.168.xxx.xxx` — keep first two octets for forensics), SSN (full mask), GPS coordinates (round to city-level precision). Apply via `Enrich.With<PiiMaskingEnricher>()` in all services. Files: new `PiiMaskingEnricher.cs` in Shared, all Serilog configuration. [NIST SI-4, PT-2] **MEDIUM**
- [ ] 365. Restrict `/info` endpoints — add `RequireAuthorization(WatchPolicies.AdminOnly)` to all `/info` endpoints across 12 services. The `/info` endpoint exposes service name, version, and description — this constitutes system enumeration information. Keep `/health` unauthenticated but strip it to return only `{ "status": "Healthy" }` — remove service name, program ID, and timestamp from unauthenticated response. Files: all 12 `Program.cs` files. [NIST AC-3, STIG V-222522] **LOW**

---

## Quick Reference

| Range | Stage | Theme | Status |
|-------|-------|-------|--------|
| 1–25 | 5 | Database (EF Core + SQL Server) | **DONE** |
| 26–45 | 6 | Real-Time (SignalR + Kafka + Push) | **DONE** |
| 46–60 | 7 | Geospatial (PostGIS + Maps) | **DONE** |
| 61–80 | 8 | Auth & Security (P5 Full + RBAC) | **DONE** |
| 81–105 | 9 | MAUI Production (Speech, Offline, Evidence) | **DONE** |
| 106–120 | 10 | DevOps (Docker + K8s + CI/CD) | **DONE** |
| 121–140 | 11 | Cloud (Azure + GCP + Cloudflare) | **DONE** |
| 141–150 | 12 | Advanced (ML, Compliance, Graph, Observability) | **DONE** |
| 151–180 | 13 | AWS Infrastructure (Compute, Data, Security, DevOps) | **DONE** |
| 181–195 | 14 | P11 Surveillance Integration | Pending |
| 196–220 | 15–16 | Cloud Providers + Contract Wiring | Pending |
| 221–250 | 17 | Production Hardening | Pending |
| 251–265 | 18 | Integration & E2E Testing | Pending |
| 266–280 | 19 | FIPS Cryptographic Compliance | Pending |
| 281–295 | 20 | Data Protection — CUI at Rest & in Transit | Pending |
| 296–310 | 21 | Authentication & Password Hardening — STIG | Pending |
| 311–325 | 22 | Input Validation, Error Handling & API Hardening | Pending |
| 326–345 | 23 | SDLC, Supply Chain & CI/CD Hardening | Pending |
| 346–365 | 24 | CMMC Assessment Documentation & Process | Pending |

---

## DoD Compliance Assessment Summary

### What's DONE (180/365 items — 49%)
All foundational stages complete: 12 microservices with real endpoints, EF Core persistence, SignalR real-time, Kafka events, PostGIS geospatial, full auth/MFA, production MAUI app, Docker/K8s/CI-CD, multi-cloud Terraform (Azure/AWS/GCP), compliance frameworks, ML models.

### What BLOCKS Launch (85 items — Stages 14-18)

**CRITICAL (must fix before any deployment):**
- P11 Surveillance not in docker-compose, Terraform, or health checks (Stage 14)
- `SyncEngine.SendAsync()` throws NotImplementedException in MAUI (Item 232)
- P5 AuthSecurity has no EF migration (relies on EnsureCreated) (Item 221)
- No end-to-end tests proving the SOS lifecycle works across services (Stage 18)
- Cloud provider stubs throw NotImplementedException if toggled on (Stage 15)

**HIGH (required for production confidence):**
- No FluentValidation on request DTOs (Item 226 → superseded by Stage 22)
- No global exception handler/ProblemDetails (Item 227 → superseded by Stage 22)
- No request signing for evidence uploads (Item 239)
- No health check dependency verification (Item 246)
- Remaining ConcurrentDictionary stores not production-safe (Item 225)
- Sub-2-second SOS benchmark not measured (Item 260)

### What BLOCKS DoD Contract (100 items — Stages 19-24)

**CRITICAL (will fail CMMC Level 2 C3PAO assessment without these):**
- No FIPS 140-2 cryptographic mode (Items 266-268)
- JWT symmetric key shared across all services (Items 269-271)
- No mTLS between microservices (Item 272)
- No data-at-rest encryption (TDE, column-level) (Items 281-282)
- Default passwords in source code (Item 287)
- No System Security Plan (SSP) (Item 346)
- No POA&M (Item 349)

**HIGH (required for DISA STIG compliance):**
- Password minimum = 8 chars (needs 15) (Item 296)
- Max failed logins = 5 (needs 3) (Item 297)
- No password history enforcement (Item 298)
- No password max age (60 days) (Item 299)
- No FluentValidation on any service DTOs (Items 311-317)
- No global exception handler (Items 318-319)
- Containers run as root (Item 329)
- No container image scanning (Item 330)
- No SBOM (Item 326)
- No SIEM (Item 361)
- No audit log integrity (Item 362)
- Missing policy documents (Items 351-360)

**MEDIUM (should have before C3PAO assessment):**
- No DAST in CI pipeline (Item 334)
- No penetration testing program (Item 339)
- No SSDF self-attestation (Item 341)
- No CUI data classification matrix (Item 290)
- No data retention policies (Item 293)
- No network segmentation policies (Item 345)

---

*DoD compliance stages last updated: 2026-02-26. Source: `DOD_SECURITY_ANALYSIS.md`.*

---

## ═══════════════════════════════════════════════════════════════════════
## NEW API EXPANSION — Stages 25–32 (Items 366–685)
## ═══════════════════════════════════════════════════════════════════════
##
## Tier reference: See `E:\json_output\APIS\NEW-API-TIERS.md`
## Primary mission: Sub-2-second geospatial emergency response.
## Chain: Detect → Locate → Dispatch → Arrive → Act
##
## PREREQUISITE: Stages 14–24 (items 181–365) should be prioritized first.
## P2/P3/P4/P6 have ZERO microservice code behind their existing specs.
## New APIs layered on empty services are useless.
## ═══════════════════════════════════════════════════════════════════════

---

## Stage 25: Tier 1A — Geospatial Detection & Auto-Triggering (Items 366–405)

> **Mission impact:** These APIs trigger dispatch BEFORE a human dials 911.
> IoT sensors, cameras, and microphone arrays detect emergencies in milliseconds.

### 25A. IoT Sensor Mesh Emergency API — `iot-sensors` (new domain)

- [ ] 366. Create OpenAPI spec `iot-sensors/iot-sensor-mesh-api.yaml` (v1.0.0, stable) — endpoints: `POST /devices/register`, `POST /telemetry/ingest`, `POST /alerts/trigger`, `GET /devices/{deviceId}/status`, `GET /zones/{zoneId}/heatmap`, `PUT /devices/{deviceId}/thresholds`
- [ ] 367. Add `iot-sensors` domain to `index.yaml` registry with domain description and contact
- [ ] 368. Create `TheWatch.P12.IoTSensors` microservice project — .NET 10, Clean Architecture, registered in `TheWatch.sln`
- [ ] 369. Implement `SensorDeviceController` — device registration with type, location, capabilities; store in EF Core `IoTSensorsDbContext`
- [ ] 370. Implement `TelemetryController` — high-throughput ingest accepting MQTT-bridged HTTP payloads; batch insert to partitioned SQL table
- [ ] 371. Implement `AlertController` — threshold breach detection triggers Kafka `sensor.alert.triggered` event; auto-creates incident in P2 via typed client
- [ ] 372. Implement `HeatmapController` — aggregate sensor readings into geographic heatmaps using PostGIS `ST_SnapToGrid`
- [ ] 373. Set up MQTT broker (EMQX/Mosquitto) container in Aspire AppHost — bridge `thewatch/sensor/+/alarm` to HTTP ingest
- [ ] 374. Create `TheWatch.Contracts.IoTSensors` typed client library with `IIoTSensorsClient`
- [ ] 375. Add P12 to docker-compose, Helm, Terraform (Azure/AWS/GCP), CI/CD, health checks
- [ ] 376. Create `TheWatch.P12.IoTSensors.Tests` — device registration, ingest throughput (target: 10K msg/s), alert triggering, heatmap

### 25B. Visual Hazard Detection API — `ai-ml`

- [ ] 377. Create OpenAPI spec `ai-ml/visual-hazard-detection-api.yaml` (v1.0.0, stable) — endpoints: `POST /detect`, `POST /stream` (WebSocket), `GET /detections/{detectionId}`, `POST /flood-level`, `POST /structural-assessment`
- [ ] 378. Add spec to `index.yaml` under `ai-ml`
- [ ] 379. Create `TheWatch.P13.AiMl` microservice — .NET 10, ONNX Runtime, GPU-accelerated where available
- [ ] 380. Implement `VisualHazardController` — YOLOv8 ONNX model for fire/smoke/flood/structural-damage, returns bounding boxes + confidence + severity
- [ ] 381. Implement WebSocket streaming for continuous camera feed at configurable FPS (default 5), emits alerts on threshold breach
- [ ] 382. Implement `FloodLevelController` — ViT+CNN depth estimation from reference objects, returns water depth in meters
- [ ] 383. Implement `StructuralAssessmentController` — drone imagery damage classification per FEMA categories (none/minor/major/destroyed)
- [ ] 384. Bundle ONNX models: fire-smoke-v1.onnx, flood-depth-v1.onnx, structural-damage-v1.onnx
- [ ] 385. Wire detections to Kafka `ai.visual.hazard.detected` → P2 auto-creates incidents
- [ ] 386. Create integration tests: model loading, inference accuracy, WebSocket streaming, Kafka emission

### 25C. Environmental Audio Intelligence API — `ai-ml`

- [ ] 387. Create OpenAPI spec `ai-ml/environmental-audio-api.yaml` (v1.0.0, stable) — endpoints: `POST /classify`, `POST /localize`, `POST /noise-cancel`, `GET /soundscape/{regionId}`
- [ ] 388. Add spec to `index.yaml` under `ai-ml`
- [ ] 389. Implement `AudioClassificationController` in P13.AiMl — AudioSet-trained transformer ONNX, classifies: explosion, scream, crash, alarm, structural-collapse, glass-break
- [ ] 390. Implement `AudioLocalizationController` — SELD from multi-mic array, returns 3D coordinates + direction
- [ ] 391. Implement `NoiseCancellationController` — AI noise removal from emergency call audio, speech enhancement model
- [ ] 392. Implement `SoundscapeController` — real-time acoustic monitoring with anomaly detection + baseline comparison
- [ ] 393. Wire audio alerts to Kafka `ai.audio.event.detected` → P2 auto-creates incident if explosion/gunshot (confidence > 0.85)
- [ ] 394. Create integration tests: classification accuracy, localization precision, noise cancellation (SDR, PESQ)

### 25D. Weapon & Threat Object Detection API — `ai-ml`

- [ ] 395. Create OpenAPI spec `ai-ml/weapon-threat-detection-api.yaml` (v1.0.0, stable) — endpoints: `POST /weapon`, `POST /abandoned-object`, `POST /stream`, `GET /alerts/{alertId}`
- [ ] 396. Add spec to `index.yaml` under `ai-ml`
- [ ] 397. Implement `WeaponDetectionController` in P13 — FMR-CNN + YOLOv8 ensemble for firearm/knife/blunt-weapon detection
- [ ] 398. Implement `AbandonedObjectController` — temporal background subtraction, stationary > 120s triggers alert
- [ ] 399. Implement WebSocket streaming with immediate Kafka `ai.threat.weapon.detected` event
- [ ] 400. Bundle ONNX models: weapon-fmrcnn-v1.onnx, abandoned-object-v1.onnx
- [ ] 401. Wire weapon alerts to P2 (auto-create active-shooter incident) and P11 (correlate with camera source)
- [ ] 402. Create integration tests: precision/recall benchmarks, temporal logic, false positive rates

### 25E. P12/P13 Infrastructure

- [ ] 403. Add P13.AiMl to Aspire AppHost with GPU resource hints (CUDA if available, CPU fallback)
- [ ] 404. Create `TheWatch.Contracts.AiMl` typed client library — `IVisualHazardClient`, `IAudioIntelligenceClient`, `IWeaponDetectionClient`
- [ ] 405. Add P13 to docker-compose (NVIDIA runtime), Helm (GPU node selector), Terraform (GPU instances), CI/CD

---

## Stage 26: Tier 1B — Precise Location & Dispatch Acceleration (Items 406–455)

> **Mission impact:** Sub-meter location + traffic-aware routing + signal preemption.
> Every second saved in location/routing directly reduces response time.

### 26A. RapidSOS Device Telemetry Bridge — `external`

- [ ] 406. Create OpenAPI spec `external/rapidsos-bridge-api.yaml` (v1.0.0) — endpoints: `POST /emergency`, `GET /caller/{callId}/data`, `POST /devices/register`, `GET /devices/{deviceId}/telemetry`, `POST /monitoring/escalate`, `GET /incidents/{incidentId}/media`
- [ ] 407. Add spec to `index.yaml` under `external`
- [ ] 408. Implement `RapidSOSController` — outbound push of TheWatch telemetry to RapidSOS, inbound pull of enriched caller data
- [ ] 409. Implement device registration with RapidSOS Emergency API on first MAUI app launch
- [ ] 410. Implement webhook receiver for dispatch status updates from RapidSOS
- [ ] 411. Wire enriched caller data into P2 incident creation — auto-populate name, location, device info, health data
- [ ] 412. Create integration tests with RapidSOS sandbox or mock adapter

### 26B. what3words Location — `location`

- [ ] 413. Create OpenAPI spec `location/what3words-api.yaml` (v1.0.0) — endpoints: `GET /convert-to-coordinates`, `GET /convert-to-3wa`, `GET /autosuggest`, `GET /grid-section`
- [ ] 414. Implement `What3WordsService` in Geospatial — wraps w3w REST API, Redis caching, rate limit handling
- [ ] 415. Add w3w input to P2 dispatch UI and MAUI MapPage — dispatchers accept "///filled.count.soap" as location input
- [ ] 416. Create integration tests with w3w sandbox key

### 26C. Building Intelligence & Indoor Maps — `location`

- [ ] 417. Create OpenAPI spec `location/building-intelligence-api.yaml` (v1.0.0) — endpoints: `GET /buildings/{id}/floorplan`, `GET /buildings/{id}/bim`, `GET /buildings/{id}/systems/status`, `POST /buildings/{id}/navigate`, `GET /buildings/{id}/preplan`, `POST /buildings/{id}/occupants/locate`
- [ ] 418. Implement `BuildingIntelligenceController` in Geospatial — IFC/BIM ingestion, floor plan storage (SVG/GeoJSON), pre-incident plans
- [ ] 419. Implement indoor A* navigation — pathfinding on floor plan graph with stairwells, fire barriers, blocked paths
- [ ] 420. Implement building systems status — BACnet/Modbus bridge for fire alarm, sprinkler, elevator, HVAC real-time data
- [ ] 421. Implement occupant locating — BLE beacon + Wi-Fi RTT + UWB positioning, per-floor estimates
- [ ] 422. Store building data in PostGIS with 3D geometry (ST_3DDistance, floor-level indexing)
- [ ] 423. Create integration tests: IFC parsing, A* pathfinding, system status ingestion

### 26D. Mapbox Isochrone & Navigation — `dispatch`

- [ ] 424. Create OpenAPI spec `dispatch/isochrone-navigation-api.yaml` (v1.0.0) — endpoints: `GET /isochrone`, `GET /directions`, `GET /optimized-trips`, `GET /matrix`, `GET /geocode`
- [ ] 425. Implement `IsochroneService` — Mapbox Isochrone API wrapper, 4/8/12-min driving contour polygons
- [ ] 426. Implement `TravelTimeMatrixService` — given N responders + 1 incident, return N travel times sorted ascending
- [ ] 427. Integrate isochrone into Dashboard — "4-minute reach zone" overlay when selecting dispatch candidate
- [ ] 428. Replace P2 Haversine distance with actual drive-time nearest-responder from Mapbox Matrix
- [ ] 429. Create integration tests with Mapbox sandbox key

### 26E. Emergency Call Triage (LLM+RAG) — `dispatch`

- [ ] 430. Create OpenAPI spec `dispatch/call-triage-api.yaml` (v1.0.0) — endpoints: `POST /analyze-call`, `POST /recommend`, `POST /multi-language`, `GET /protocol/{incidentType}`, `POST /summarize`
- [ ] 431. Implement `CallTriageController` in P2 — severity classification (P1–P5) via Claude API
- [ ] 432. Implement RAG pipeline — vector DB indexing emergency protocols (MPDS, EMD, NFPA), grounded LLM responses
- [ ] 433. Implement dispatch recommendation — triage + available resources + travel times → ranked recommendations with cited rationale
- [ ] 434. Implement multi-language triage — 50+ languages, cultural context
- [ ] 435. Implement real-time SITREP summarization from ongoing call transcript
- [ ] 436. Create integration tests: classification accuracy, RAG relevance, recommendation quality

### 26F. Emergency Vehicle Preemption — `infrastructure`

- [ ] 437. Create OpenAPI spec `infrastructure/vehicle-preemption-api.yaml` (v1.0.0) — endpoints: `POST /preemption-request`, `GET /route/{routeId}/signals`, `PUT /signals/{signalId}/priority`, `POST /corridor`, `DELETE /corridor/{corridorId}`, `GET /vehicle/{vehicleId}/eta`
- [ ] 438. Implement `PreemptionController` — NTCIP 1202 protocol adapter for traffic signal controllers
- [ ] 439. Implement `GreenCorridorService` — sequential signal preemption along response route, auto-release after vehicle passes
- [ ] 440. Wire to P6 — on dispatch, auto-request preemption along calculated route
- [ ] 441. Create V2X adapter — SAE J2735 message translation
- [ ] 442. Create integration tests with simulated signal controller

### 26G. HERE Fleet Telematics — `dispatch`

- [ ] 443. Create OpenAPI spec `dispatch/fleet-routing-api.yaml` (v1.0.0) — endpoints: `GET /routes/truck`, `GET /traffic/flow`, `GET /traffic/incidents`, `GET /routes/avoid`, `GET /fleet/route`
- [ ] 444. Implement `FleetRoutingService` — HERE truck routing for fire engines/hazmat vehicles with dimension constraints
- [ ] 445. Wire to P2 dispatch — use HERE for heavy apparatus, Mapbox for standard vehicles, based on vehicle type

### 26H. Tier 1 Contract Libraries

- [ ] 446. Create `TheWatch.Contracts.Dispatch` extensions — `IIsochroneClient`, `ICallTriageClient`, `IFleetRoutingClient`, `IPreemptionClient`
- [ ] 447. Create `TheWatch.Contracts.LocationIntel` — `IWhat3WordsClient`, `IBuildingIntelligenceClient`
- [ ] 448. Create `TheWatch.Contracts.ExternalBridge` — `IRapidSOSClient`
- [ ] 449. Add all Tier 1B services to docker-compose, Helm, Terraform, CI/CD

### 26I. Tier 1 Benchmarks

- [ ] 450. Benchmark: IoT sensor alert → P2 incident creation (target: < 500ms)
- [ ] 451. Benchmark: Visual hazard detection → dispatch notification (target: < 2 seconds)
- [ ] 452. Benchmark: what3words resolution → dispatch routing → ETA (target: < 1 second)
- [ ] 453. Benchmark: Mapbox isochrone generation at 500 concurrent requests
- [ ] 454. Load test: P12 IoT ingest at 50K messages/second sustained
- [ ] 455. Load test: P13 AI inference at 100 concurrent image classification requests

---

## Stage 27: Tier 2A — Environmental & Medical Situational Awareness (Items 456–510)

> **Mission impact:** What responders know on arrival determines outcomes.

### 27A. NWS Weather Alerts — `disaster`

- [ ] 456. Create OpenAPI spec `disaster/nws-weather-api.yaml` (v1.0.0) — active alerts, area/zone filtering, forecasts, observations
- [ ] 457. Implement `NWSWeatherService` in P8 — poll api.weather.gov every 60s, parse CAP 1.2, store in SQL
- [ ] 458. Wire tornado/flood warnings to P2 — auto-create pre-staged incidents with evacuation zones
- [ ] 459. Wire alerts to FCM/SMS push for affected zones
- [ ] 460. Create integration tests against live api.weather.gov (free, no auth)

### 27B. USGS Earthquake — `disaster`

- [ ] 461. Create OpenAPI spec `disaster/usgs-earthquake-api.yaml` (v1.0.0) — query, count, feeds, ShakeMap
- [ ] 462. Implement `EarthquakeService` in P8 — poll USGS GeoJSON every 30s for M4.0+
- [ ] 463. Wire M5.0+ to P2 — auto-create disaster incident, dispatch USAR via P6, activate P8 shelters
- [ ] 464. Create integration tests against live earthquake.usgs.gov (free, no auth)

### 27C. Wildfire Intelligence — `disaster`

- [ ] 465. Upgrade stub `disaster/wildfire-spread-api.yaml` v0.1.0 → v1.0.0 — active fires, perimeter, predict, cameras, evacuations, weather
- [ ] 466. Implement `WildfireService` in P8 — aggregate NASA FIRMS, NOAA NGFS, ALERTWildfire cameras
- [ ] 467. Implement fire perimeter tracking — GeoJSON polygons from NIFC GeoMAC in PostGIS, 15-min updates
- [ ] 468. Implement fire spread prediction — simplified Farsite model (wind, humidity, terrain, fuel)
- [ ] 469. Wire to evacuation-api — auto-generate evacuation zones from spread prediction

### 27D. EPA AirNow — `disaster`

- [ ] 470. Create OpenAPI spec `disaster/airquality-api.yaml` (v1.0.0) — current AQI, forecasts, pollutants
- [ ] 471. Implement `AirQualityService` in P8 — wraps AirNow API, Redis cache (5-min TTL)
- [ ] 472. Wire AQI > 150 to P7 — flag respiratory patients, push health advisory
- [ ] 473. Add AQI heatmap overlay to Dashboard and MAUI MapPage

### 27E. Hazmat Chemical Intelligence — `disaster`

- [ ] 474. Create `disaster/hazmat-intelligence-api.yaml` (v1.0.0) — chemical lookup, search, dispersion model, ERG guide, incidents, reactivity
- [ ] 475. Implement `HazmatController` in P8 — embedded CAMEO Chemicals DB (offline capable), ERG guide lookup
- [ ] 476. Implement atmospheric dispersion model — simplified ALOHA (wind, temp, release rate → threat zone polygon in PostGIS)
- [ ] 477. Wire threat zone to maps — overlay dispersion plume, push evacuation alerts to users in zone

### 27F. FHIR R4 Healthcare Interop — `medical`

- [ ] 478. Create OpenAPI spec `medical/fhir-interop-api.yaml` (v1.0.0) — patient lookup, allergies, meds, conditions, vitals, bundles
- [ ] 479. Implement `FhirInteropService` in P9 — SMART on FHIR OAuth2, HL7.Fhir.R4 NuGet, Epic/Oracle Health
- [ ] 480. Wire FHIR data to MCI triage — auto-pull medical history for drug interaction checks
- [ ] 481. Implement `FhirBundleController` — submit field observations back to hospital EHR
- [ ] 482. Create integration tests against HAPI FHIR sandbox

### 27G. Hospital Availability Exchange (HAVE) — `medical`

- [ ] 483. Create OpenAPI spec `medical/hospital-availability-api.yaml` (v1.0.0) — hospitals list, capacity, self-report, search, divert, regional
- [ ] 484. Implement `HospitalAvailabilityController` in P9 — EDXL-HAVE v2.0 compliant, beds by type, services, diversion status
- [ ] 485. Implement capability search — "nearest Level 1 trauma with available OR and burn unit" + PostGIS distance
- [ ] 486. Wire to P2 dispatch — route MCI patients to hospitals with actual capacity

### 27H. Mass Casualty Triage — `medical`

- [ ] 487. Upgrade stub `medical/triage-api.yaml` v0.1.0 → v1.0.0 — patients, status, dashboard, transport, chain, handoff
- [ ] 488. Implement `MciTriageController` in P9 — EDXL-TEP v1.1 digital triage tags (START/JumpSTART/SALT)
- [ ] 489. Implement MCI dashboard — real-time counts by category, SignalR hub for live updates
- [ ] 490. Implement patient tracking chain — every status change logged with timestamp, handler, GPS
- [ ] 491. Implement transport assignment using hospital availability (27G)

### 27I. Biometric Stress & MCI Triage — `wearable`

- [ ] 492. Create OpenAPI spec `wearable/biometric-triage-api.yaml` (v1.0.0) — assess, batch-assess, trend, stress-detect, identity-verify
- [ ] 493. Implement `BiometricTriageController` in P4 — wearable vitals → automated START/SALT/MARCH classification
- [ ] 494. Implement batch assessment — multiple casualties with priority ranking; wire to MCI dashboard

### 27J. Tier 2A Contract Libraries

- [ ] 495. Create `TheWatch.Contracts.DisasterIntel` — `IWeatherAlertClient`, `IEarthquakeClient`, `IWildfireClient`, `IAirQualityClient`, `IHazmatClient`
- [ ] 496. Create `TheWatch.Contracts.MedicalIntel` — `IFhirInteropClient`, `IHospitalAvailabilityClient`, `IMciTriageClient`, `IBiometricTriageClient`
- [ ] 497. Add all Tier 2A services to docker-compose, Helm, Terraform, CI/CD
- [ ] 498. Load test: FHIR patient lookup at 200 concurrent requests
- [ ] 499. E2E test: NWS tornado warning → auto-incident → evacuation zones → mass SMS → shelter activation
- [ ] 500. E2E test: Wildfire (FIRMS) → perimeter → AQI monitoring → evacuation → animal welfare shelter

### 27K. Tier 2A Benchmarks

- [ ] 501. Benchmark: FHIR patient lookup → triage data available (target: < 3 seconds)
- [ ] 502. Benchmark: NWS alert received → P2 incident created (target: < 5 seconds)
- [ ] 503. Benchmark: Hospital availability query → dispatch recommendation (target: < 2 seconds)
- [ ] 504. Benchmark: Hazmat chemical lookup + dispersion model (target: < 3 seconds)
- [ ] 505. Benchmark: MCI triage tag creation → dashboard update (target: < 1 second)

---

## Stage 28: Tier 2B — Responder Force Enhancement (Items 506–545)

> **Mission impact:** Drones, AI assistants, edge inference, knowledge bases.
> Make each responder 10x more effective.

### 28A. Drone Swarm Coordination — `logistics`

- [ ] 506. Create OpenAPI spec `logistics/drone-swarm-api.yaml` (v1.0.0) — mission, status, confirm detection, replan, analytics
- [ ] 507. Implement `DroneSwarmController` — SAR mission with search area polygon, drone count, sensor types, priority zones
- [ ] 508. Implement swarm coordination — Reynolds boids + AI collision avoidance, coverage tracking, autonomous pattern adaptation
- [ ] 509. Implement thermal person detection — YOLO from thermal imagery with human-in-the-loop confirmation → P6 ground dispatch
- [ ] 510. Wire confirmed detections to P2 — create rescue sub-incidents with GPS

### 28B. Responder Cognitive Assistant — `responder`

- [ ] 511. Create OpenAPI spec `responder/cognitive-assistant-api.yaml` (v1.0.0) — query, checklist, handoff-brief, transcript
- [ ] 512. Implement `CognitiveAssistantController` in P6 — voice/text LLM with incident context, responder role, equipment awareness
- [ ] 513. Implement dynamic checklist generation — incident + protocol + conditions → adaptive checklist
- [ ] 514. Implement handoff briefing — structured brief on rotation/transfer (timeline, actions, outstanding tasks, resources)
- [ ] 515. Wire to MAUI — voice-activated via existing speech pipeline, AssistantPage.razor

### 28C. Edge AI Inference Management — `ai-ml`

- [ ] 516. Create OpenAPI spec `ai-ml/edge-inference-api.yaml` (v1.0.0) — deploy-model, devices, inference, config, telemetry
- [ ] 517. Implement `EdgeInferenceController` in P13 — model registry, version tracking, OTA deployment to edge devices
- [ ] 518. Implement hybrid edge-cloud inference — edge first, cloud fallback if unreachable or low confidence
- [ ] 519. Implement health monitoring — inference latency, accuracy drift, battery, connectivity per device

### 28D. Emergency Knowledge Base (RAG) — `ai-ml`

- [ ] 520. Create OpenAPI spec `ai-ml/emergency-knowledge-api.yaml` (v1.0.0) — query, protocol-check, compliance-audit, ingest, audit-trail
- [ ] 521. Implement `KnowledgeBaseController` in P13 — RAG with Azure AI Search, emergency protocols, regulations, AARs
- [ ] 522. Implement document ingestion — auto-chunk, embed (text-embedding-3-small), index with metadata (source, version, expiry, jurisdiction)
- [ ] 523. Implement protocol-check — retrieve applicable SOPs for incident scenario with jurisdiction filtering + citations
- [ ] 524. Implement compliance audit — compare incident response against protocols, score + gap analysis
- [ ] 525. Implement tamper-proof audit trail for legal proceedings
- [ ] 526. Wire to Cognitive Assistant (28B) — RAG grounding for protocol guidance

### 28E. Tier 2B Contract Libraries & Infrastructure

- [ ] 527. Create typed clients: `IDroneSwarmClient`, `ICognitiveAssistantClient`, `IEdgeInferenceClient`, `IKnowledgeBaseClient`
- [ ] 528. Add all Tier 2B services to docker-compose, Helm, Terraform, CI/CD
- [ ] 529. E2E test: Drone detects person → human confirms → P2 incident → P6 dispatch → responder arrives
- [ ] 530. Chaos test: P13 AI down → edge AI takes over → P13 recovers → hybrid resumes

---

## Stage 29: Tier 3 — National Interoperability & Standards (Items 531–590)

> **Mission impact:** TheWatch is locked out of the national emergency ecosystem without these.
> NENA, FEMA, NIMS, FirstNet, NIEM define how agencies communicate.

### 29A. EIDO Incident Data Exchange — `dispatch`

- [ ] 531. Create OpenAPI spec `dispatch/eido-exchange-api.yaml` (v1.0.0) — incidents CRUD, subscribe, convey, validate
- [ ] 532. Implement `EidoController` in P2 — NENA-STA-021.1a EIDO JSON, map TheWatch incident model to EIDO schema
- [ ] 533. Implement EIDO conveyance to external PSAPs/CAD per NENA-STA-024.1.1-2025
- [ ] 534. Implement EIDO subscription — IDX for real-time updates via WebSocket/SSE
- [ ] 535. Implement EIDO schema validation against NENA-STA-021 JSON Schema

### 29B. IPAWS Alert Origination — `alerting` (new domain)

- [ ] 536. Create OpenAPI spec `alerting/ipaws-alert-api.yaml` (v1.0.0) — originate, feed, validate, archived, cancel, status
- [ ] 537. Add `alerting` domain to `index.yaml`
- [ ] 538. Implement `IpawsController` — CAP 1.2 message composition + IPAWS-OPEN submission
- [ ] 539. Implement IPAWS HIF consumer — background service polling, parsing CAP alerts, Kafka `alerting.ipaws.alert.received`
- [ ] 540. Wire IPAWS alerts to P2 — incoming WEA/EAS create incident context; outgoing publishable to WEA/EAS/NWR

### 29C. NG911 Multimedia Session — `voice`

- [ ] 541. Create OpenAPI spec `voice/ng911-multimedia-api.yaml` (v1.0.0) — sessions, media, stream, text-to-911, DVC, transfer
- [ ] 542. Implement `NG911SessionController` in P2 — NENA i3 v3 compliant multimedia (voice, text, video, RTT)
- [ ] 543. Implement text-to-911 — inbound text with auto-location, threading, PSAP routing
- [ ] 544. Implement Direct Video Calling — WebRTC for ASL users with geo-routing to nearest capable PSAP
- [ ] 545. Implement session transfer — between PSAPs with full media context preserved

### 29D. NIEM Data Exchange — `platform`

- [ ] 546. Create OpenAPI spec `platform/niem-exchange-api.yaml` (v1.0.0) — exchange, transform, domains, validate, model types
- [ ] 547. Implement `NiemController` — NIEMOpen 6.0 JSON-LD/XML transformation, IEP construction
- [ ] 548. Implement NDR 6.0 validator for outbound message conformance

### 29E. FirstNet Priority & PTT — `responder`

- [ ] 549. Create OpenAPI spec `responder/firstnet-api.yaml` (v1.0.0) — priority request/status, PTT channels/transmit, SSO, coverage
- [ ] 550. Implement `FirstNetService` — App Priority API for QoS, PTT Web API for broadband push-to-talk
- [ ] 551. Implement PTT bridging — FirstNet broadband ↔ LMR/P25 via ISSI/CSSI gateway
- [ ] 552. Implement FirstNet SSO federation with TheWatch auth

### 29F. Mutual Aid Resource Exchange — `logistics`

- [ ] 553. Create OpenAPI spec `logistics/mutual-aid-api.yaml` (v1.0.0) — requests, offers, agreements, tracking, reimbursement
- [ ] 554. Implement `MutualAidController` — NIMS-typed resources, digital agreements, GPS tracking of deployed resources
- [ ] 555. Implement reimbursement workflow per EMAC guidelines

### 29G. OpenFEMA Integration — `disaster`

- [ ] 556. Create OpenAPI spec `disaster/openfema-api.yaml` (v1.0.0) — declarations, assistance, mitigation, datasets
- [ ] 557. Implement `OpenFemaService` in P8 — wraps OpenFEMA API v2, caches declarations
- [ ] 558. Wire to P8 — auto-determine eligibility zones, push notifications to affected users

### 29H. CDC Public Health Data — `medical`

- [ ] 559. Create OpenAPI spec `medical/cdc-health-data-api.yaml` (v1.0.0) — surveillance, outbreaks, guidance, eCR reports
- [ ] 560. Implement `CdcHealthDataService` in P9 — wraps CDC SODA API, content syndication
- [ ] 561. Wire outbreak data to P7 — auto-push health advisories to families in outbreak zones

### 29I. Twilio Mass Notification — `notifications`

- [ ] 562. Create OpenAPI spec `notifications/twilio-notification-api.yaml` (v1.0.0) — SMS send/broadcast, voice call/escalation, email, verify
- [ ] 563. Implement `TwilioNotificationService` in Shared — SMS, Voice, SendGrid with geographic targeting
- [ ] 564. Implement escalation loop — cycle roster until acknowledgment
- [ ] 565. Wire to P2 — mass SMS for evacuations, voice calls to vulnerable populations

### 29J. MQTT IoT Protocol Broker — `iot-sensors`

- [ ] 566. Add EMQX/Mosquitto broker to Aspire + docker-compose — TLS, auth, topic ACLs
- [ ] 567. Implement MQTT-to-Kafka bridge — `thewatch/sensor/#` → Kafka `iot.sensor.telemetry`
- [ ] 568. Implement MQTT-to-HTTP bridge — alarm topics → P12 alert endpoint
- [ ] 569. Configure QoS: 0 telemetry, 1 status, 2 alarms
- [ ] 570. Add broker to Terraform (Azure IoT Hub, AWS IoT Core, self-hosted) + Helm

### 29K. Social Media & OSINT — `ai-ml` / `security`

- [ ] 571. Create OpenAPI spec `ai-ml/social-media-monitor-api.yaml` (v1.0.0) — monitors, threats, analyze-post, trends
- [ ] 572. Implement `SocialMediaMonitorService` in P13 — X API v2 Filtered Stream, NLP threat classification
- [ ] 573. Create OpenAPI spec `security/osint-threat-api.yaml` (v1.0.0) — monitor, threats, analyze, trends
- [ ] 574. Implement `OsintController` — aggregated social/dark web/OSINT feeds, threat scoring, geographic correlation
- [ ] 575. Wire high-confidence signals to P2 — "unverified report" incidents for dispatcher review

### 29L. Tier 3 Contract Libraries & Infrastructure

- [ ] 576. Create typed clients: `IEidoClient`, `IIpawsClient`, `ING911Client`, `INiemClient`, `IFirstNetClient`, `IMutualAidClient`
- [ ] 577. Create typed clients: `IOpenFemaClient`, `ICdcClient`, `ITwilioClient`, `ISocialMediaClient`, `IOsintClient`
- [ ] 578. Add all Tier 3 services to docker-compose, Helm, Terraform, CI/CD
- [ ] 579. E2E test: EIDO incident creation → conveyance to external PSAP → subscription updates
- [ ] 580. E2E test: IPAWS alert received → P2 incident → mass notification → shelter activation
- [ ] 581. Chaos test: MQTT broker failure → sensors queue locally → recovery → no data loss
- [ ] 582. Benchmark: EIDO conveyance latency to external system (target: < 1 second)
- [ ] 583. Benchmark: IPAWS alert received → P2 incident (target: < 3 seconds)
- [ ] 584. Benchmark: Twilio mass SMS broadcast to 10K numbers (target: < 60 seconds)
- [ ] 585. Benchmark: NG911 text-to-911 → dispatcher notification (target: < 2 seconds)

---

## Stage 30: Tier 4A — Accessibility & Vulnerable Populations (Items 586–625)

> **Mission impact:** The 2-second response fails if 37M deaf Americans can't trigger it,
> 6.7M dementia patients can't be found, or 50M students can't be protected.

### 30A. Accessible Emergency Communication — `accessibility` (new domain)

- [ ] 586. Create OpenAPI spec `accessibility/accessible-emergency-api.yaml` (v1.0.0) — text-to-911 sessions, alert config, accessible formats, communication profiles, PSAP capabilities
- [ ] 587. Add `accessibility` domain to `index.yaml`
- [ ] 588. Implement `AccessibleEmergencyController` — text-based 911 with auto-location, disability-profile-aware routing
- [ ] 589. Implement multi-format alert delivery — plain language, AAC symbols, large-print, audio description, haptic patterns

### 30B. Sign Language Video Relay — `accessibility`

- [ ] 590. Create OpenAPI spec `accessibility/video-relay-api.yaml` (v1.0.0) — sessions, interpreters, transfer, transcript
- [ ] 591. Implement `VideoRelayController` — on-demand ASL with correct PSAP geo-routing (caller's location, not interpreter's)
- [ ] 592. Implement interpreter pool tracking — specialization, language, real-time availability

### 30C. Silver Alert & Wandering Prevention — `emergency`

- [ ] 593. Create OpenAPI spec `emergency/silver-alert-api.yaml` (v1.0.0) — alerts, geofence, active, profiles, triggers
- [ ] 594. Implement `SilverAlertController` in P2 — cognitive-impairment-aware alerts with wandering history, medication timelines
- [ ] 595. Implement dynamic geofencing — expanding radius based on mobility profile + elapsed time via PostGIS

### 30D. School Safety & Child Protection — `emergency`

- [ ] 596. Create OpenAPI spec `emergency/school-safety-api.yaml` (v1.0.0) — lockdowns, accountability, reunification queue, status, mandatory reports
- [ ] 597. Implement `SchoolSafetyController` — lockdown with protocol type, auto parent notification, custody-verified reunification

### 30E. Disaster Animal Welfare — `emergency`

- [ ] 598. Create OpenAPI spec `emergency/animal-welfare-api.yaml` (v1.0.0) — evacuations, shelters, vet triage, reunification, livestock transport
- [ ] 599. Implement `AnimalWelfareController` — co-located shelters per PETS Act, vet triage routing, post-disaster reunification

### 30F. 988 Behavioral Health Crisis — `community`

- [ ] 600. Create OpenAPI spec `community/crisis-988-api.yaml` (v1.0.0) — 988 transfer, teams, dispatch, facilities, followup, routing
- [ ] 601. Implement `BehavioralCrisisController` — warm 911→988 handoffs, mobile crisis team dispatch, facility availability

### 30G. Verified Helper & Trust Framework — `community`

- [ ] 602. Create OpenAPI spec `community/trust-framework-api.yaml` (v1.0.0) — verification, profiles, endorsements, dispatch-eligible, feedback
- [ ] 603. Implement `TrustFrameworkController` — verification tiers, peer endorsements, post-incident feedback; integrate with P10 Gamification

### 30H. Cultural Liaison & Refugee Support — `community`

- [ ] 604. Create OpenAPI spec `community/cultural-liaison-api.yaml` (v1.0.0) — register, dispatch, resources, safe-check-in, coverage
- [ ] 605. Implement `CulturalLiaisonController` — bilingual liaison dispatch, documentation-free wellness checks

---

## Stage 31: Tier 4B — Recovery, Resilience & Post-Incident (Items 626–665)

> **Mission impact:** TheWatch's mission doesn't end when sirens stop.

### 31A. Post-Incident Recovery Case Management — `disaster`

- [ ] 626. Create OpenAPI spec `disaster/recovery-case-mgmt-api.yaml` (v1.0.0) — cases, milestones, referrals, duplication-check, dashboard
- [ ] 627. Implement `RecoveryCaseController` in P8 — long-term case management linking incident, damage, insurance, FEMA, milestones

### 31B. Responder PTSD & Peer Support — `responder`

- [ ] 628. Create OpenAPI spec `responder/peer-support-api.yaml` (v1.0.0) — sessions, CISD, providers, screening, cumulative-exposure
- [ ] 629. Implement `PeerSupportController` in P6 — confidential peer support, CISD, anonymous PCL-5/PHQ-9, auto-resource routing
- [ ] 630. Implement cumulative exposure tracking — calculate traumatic exposure score from incident history, proactive flagging

### 31C. Community Trauma & Grief Support — `medical`

- [ ] 631. Create OpenAPI spec `medical/trauma-support-api.yaml` (v1.0.0) — groups, screenings, resources, check-ins, community-pulse
- [ ] 632. Implement `TraumaSupportController` in P9 — support groups, trauma screening, post-disaster mental health resource matching

### 31D. Elder Abuse Reporting — `legal`

- [ ] 633. Create OpenAPI spec `legal/elder-abuse-api.yaml` (v1.0.0) — reports, status, evidence, mandatory-reporter obligations, protective-orders
- [ ] 634. Implement `ElderAbuseController` — mandatory reporting with jurisdiction routing, chain-of-custody evidence, vulnerable-persons link

### 31E. Offline-First Emergency Gateway — `platform`

- [ ] 635. Create OpenAPI spec `platform/offline-gateway-api.yaml` (v1.0.0) — sync-package, mesh-relay, sms-gateway inbound/outbound, reconnect-sync
- [ ] 636. Implement `OfflineGatewayService` — extend P3 MeshNetwork with SMS parsing ("FIRE 123 OAK ST"), offline packages, progressive degradation
- [ ] 637. Implement reconnect sync — conflict resolution + deduplication on connectivity restoration

### 31F. Neighborhood Preparedness & Drills — `community`

- [ ] 638. Create OpenAPI spec `community/neighborhood-prep-api.yaml` (v1.0.0) — plans, drills, results, readiness-score, resource-map
- [ ] 639. Implement `NeighborhoodPrepController` — emergency plans, drill coordination, block-level resource maps, composite readiness scoring

### 31G. Tier 4 Contract Libraries & Infrastructure

- [ ] 640. Create `TheWatch.Contracts.Accessibility` — `IAccessibleEmergencyClient`, `IVideoRelayClient`
- [ ] 641. Create `TheWatch.Contracts.CommunityResilience` — `ITrustFrameworkClient`, `ICulturalLiaisonClient`, `INeighborhoodPrepClient`, `ICrisis988Client`
- [ ] 642. Extend `TheWatch.Contracts.DisasterRelief` — add `IRecoveryCaseClient`
- [ ] 643. Extend `TheWatch.Contracts.FirstResponder` — add `IPeerSupportClient`, `ICognitiveAssistantClient`
- [ ] 644. Add all Tier 4 services to docker-compose, Helm, Terraform, CI/CD
- [ ] 645. E2E test: Text-to-911 → accessible format alerts → disability-profile routing
- [ ] 646. E2E test: Silver alert → geofence expand → sighting reported → resolved
- [ ] 647. E2E test: School lockdown → accountability → parent notification → reunification
- [ ] 648. E2E test: Offline SOS → BLE mesh relay → reconnect → sync → incident created
- [ ] 649. E2E test: Disaster → damage assessment → recovery case opened → milestones tracked → duplication check
- [ ] 650. Benchmark: Text-to-911 → dispatcher notification (target: < 3 seconds)
- [ ] 651. Benchmark: Silver alert geofence expansion cycle (target: < 5 seconds per expansion)
- [ ] 652. Benchmark: SMS gateway inbound parsing → incident creation (target: < 5 seconds)

---

## Stage 32: Tier 5 — Predictive & Synthetic (Items 653–685)

> **Mission impact:** Shift from reactive to predictive.

### 32A. Predictive Risk Intelligence — `ai-ml`

- [ ] 653. Create OpenAPI spec `ai-ml/predictive-risk-api.yaml` (v1.0.0) — disaster-probability, resource-forecast, responder-fatigue, hotspot-analysis, explanations
- [ ] 654. Implement `PredictiveRiskController` in P13 — place-based temporal hotspots (never person-based), disaster probability, fatigue prediction
- [ ] 655. Implement XAI requirement — every prediction includes feature importance, confidence intervals, bias audit
- [ ] 656. Wire resource forecasts to P6 — proactive shift scheduling + pre-positioning

### 32B. City Digital Twin Simulation — `disaster`

- [ ] 657. Create OpenAPI spec `disaster/digital-twin-api.yaml` (v1.0.0) — simulate, results, evacuate building, infrastructure stress, optimize
- [ ] 658. Implement `DigitalTwinController` — agent-based simulation on BIM data, IoT integration, multi-agency modeling
- [ ] 659. Wire outputs to disaster planning — evacuation bottlenecks, staging optimization, resource gap analysis

### 32C. Synthetic Data & Adversarial Testing — `ai-ml`

- [ ] 660. Create OpenAPI spec `ai-ml/synthetic-data-api.yaml` (v1.0.0) — generate-scenario, generate-training-data, adversarial-test, test-results
- [ ] 661. Implement `SyntheticDataController` in P13 — GAN/diffusion scenario generation, labeled training data, adversarial testing
- [ ] 662. Wire adversarial testing to CI/CD — nightly runs, fail pipeline if robustness drops

### 32D. OSM Overpass Queries — `location`

- [ ] 663. Create OpenAPI spec `location/osm-overpass-api.yaml` (v1.0.0) — hospitals, shelters, infrastructure, custom queries
- [ ] 664. Implement `OverpassService` in Geospatial — pre-built Overpass QL templates for emergency POIs
- [ ] 665. Wire to P8 — post-disaster infrastructure queries when commercial maps outdated

### 32E. Plaid Identity Verification — `security`

- [ ] 666. Create OpenAPI spec `security/identity-verification-api.yaml` (v1.0.0) — verify create/get/list, watchlist-screen
- [ ] 667. Implement `IdentityVerificationService` — Plaid IDV for gov ID + selfie liveness + OFAC screening
- [ ] 668. Wire to P8 disaster aid disbursement + evidence chain-of-custody verification

### 32F. Tier 5 Infrastructure & Testing

- [ ] 669. Add all Tier 5 services to docker-compose, Helm, Terraform, CI/CD
- [ ] 670. E2E test: IoT alert → P12 → P2 incident → P6 dispatch → P13 imagery → P9 triage (full chain)
- [ ] 671. E2E test: Predictive risk forecast → proactive resource pre-positioning → incident validates forecast
- [ ] 672. E2E test: Digital twin simulation → identifies bottleneck → optimized plan generated
- [ ] 673. Chaos test: MQTT broker down → sensors queue → recovery → no data loss
- [ ] 674. Chaos test: P13 down → edge inference takes over → P13 recovers
- [ ] 675. Load test: IoT ingest at 50K msg/s sustained
- [ ] 676. Load test: P13 inference at 100 concurrent image requests
- [ ] 677. Load test: Mapbox isochrone at 500 concurrent requests
- [ ] 678. Load test: FHIR lookup at 200 concurrent requests
- [ ] 679. Benchmark: IoT sensor → P2 incident (target: < 500ms)
- [ ] 680. Benchmark: Camera detection → dispatch (target: < 2s)
- [ ] 681. Benchmark: w3w → route → ETA (target: < 1s)
- [ ] 682. Benchmark: FHIR lookup → triage (target: < 3s)
- [ ] 683. Benchmark: Full SOS lifecycle with all new APIs (target: < 2s)
- [ ] 684. Benchmark: EIDO conveyance (target: < 1s)
- [ ] 685. Benchmark: SMS gateway → incident (target: < 5s)

---

## Quick Reference

| Range | Stage | Theme | Status |
|-------|-------|-------|--------|
| 1–25 | 5 | Database (EF Core + SQL Server) | **DONE** |
| 26–45 | 6 | Real-Time (SignalR + Kafka + Push) | **DONE** |
| 46–60 | 7 | Geospatial (PostGIS + Maps) | **DONE** |
| 61–80 | 8 | Auth & Security (P5 Full + RBAC) | **DONE** |
| 81–105 | 9 | MAUI Production (Speech, Offline, Evidence) | **DONE** |
| 106–120 | 10 | DevOps (Docker + K8s + CI/CD) | **DONE** |
| 121–140 | 11 | Cloud (Azure + GCP + Cloudflare) | **DONE** |
| 141–150 | 12 | Advanced (ML, Compliance, Graph, Observability) | **DONE** |
| 151–180 | 13 | AWS Infrastructure (Compute, Data, Security, DevOps) | **DONE** |
| 181–195 | 14 | P11 Surveillance Integration | Pending |
| 196–220 | 15–16 | Cloud Providers + Contract Wiring | Pending |
| 221–250 | 17 | Production Hardening | Pending |
| 251–265 | 18 | Integration & E2E Testing | Pending |
| 266–280 | 19 | FIPS Cryptographic Compliance | Pending |
| 281–295 | 20 | Data Protection — CUI at Rest & in Transit | Pending |
| 296–310 | 21 | Authentication & Password Hardening — STIG | Pending |
| 311–325 | 22 | Input Validation, Error Handling & API Hardening | Pending |
| 326–345 | 23 | SDLC, Supply Chain & CI/CD Hardening | Pending |
| 346–365 | 24 | CMMC Assessment Documentation & Process | Pending |
| 366–405 | 25 | **Tier 1A: Geospatial Detection (IoT, Visual, Audio, Weapon)** | Pending |
| 406–455 | 26 | **Tier 1B: Location & Dispatch (RapidSOS, w3w, Indoor, Isochrone, Triage, Preemption, Fleet)** | Pending |
| 456–505 | 27 | **Tier 2A: Environmental & Medical Intel (NWS, USGS, Wildfire, AQI, Hazmat, FHIR, HAVE, MCI)** | Pending |
| 506–530 | 28 | **Tier 2B: Force Enhancement (Drones, Cognitive Assistant, Edge AI, Knowledge Base)** | Pending |
| 531–585 | 29 | **Tier 3: Interoperability (EIDO, IPAWS, NG911, NIEM, FirstNet, Mutual Aid, FEMA, CDC, Twilio, MQTT, OSINT)** | Pending |
| 586–625 | 30 | **Tier 4A: Accessibility & Vulnerable Populations (ADA, Silver Alert, School, Animal, 988, Trust, Cultural)** | Pending |
| 626–652 | 31 | **Tier 4B: Recovery & Resilience (Case Mgmt, PTSD, Trauma, Elder Abuse, Offline, Neighborhood Prep)** | Pending |
| 653–685 | 32 | **Tier 5: Predictive & Synthetic (Risk, Digital Twin, Synthetic Data, OSM, Identity, Benchmarks)** | Pending |

---

## Master Totals

| Category | Items | Status |
|----------|-------|--------|
| Stages 5–13 (Foundation) | 180 | **DONE** |
| Stages 14–18 (Launch Readiness) | 85 | Pending |
| Stages 19–24 (DoD Compliance) | 100 | Pending |
| Stages 25–32 (API Expansion) | 320 | Pending |
| **Grand Total** | **685** | **180 done / 505 pending** |

## New Projects Required (API Expansion)

| Project | Type | Stage |
|---------|------|-------|
| `TheWatch.P12.IoTSensors` | Microservice (.NET 10) | 25 |
| `TheWatch.P12.IoTSensors.Tests` | Test project | 25 |
| `TheWatch.P13.AiMl` | Microservice (.NET 10, GPU) | 25 |
| `TheWatch.P13.AiMl.Tests` | Test project | 25 |
| `TheWatch.Contracts.IoTSensors` | Client library | 25 |
| `TheWatch.Contracts.AiMl` (extended) | Client library | 25 |
| `TheWatch.Contracts.Dispatch` (extended) | Client library | 26 |
| `TheWatch.Contracts.LocationIntel` | Client library | 26 |
| `TheWatch.Contracts.ExternalBridge` | Client library | 26 |
| `TheWatch.Contracts.DisasterIntel` | Client library | 27 |
| `TheWatch.Contracts.MedicalIntel` | Client library | 27 |
| `TheWatch.Contracts.Accessibility` | Client library | 30 |
| `TheWatch.Contracts.CommunityResilience` | Client library | 30 |

## New OpenAPI Specs Required (55 total)

| Tier | Stage | New Specs | New Domains |
|------|-------|-----------|-------------|
| 1A | 25 | 4 (IoT sensor, visual hazard, environmental audio, weapon threat) | `iot-sensors` |
| 1B | 26 | 7 (RapidSOS, w3w, building intel, isochrone, call triage, preemption, fleet) | — |
| 2A | 27 | 9 (NWS, USGS, wildfire↑, AQI, hazmat↑, FHIR, HAVE, triage↑, biometric) | — |
| 2B | 28 | 4 (drone swarm, cognitive assistant, edge inference, knowledge base) | — |
| 3 | 29 | 11 (EIDO, IPAWS, NG911, NIEM, FirstNet, mutual aid, OpenFEMA, CDC, Twilio, social, OSINT) | `alerting` |
| 4A | 30 | 8 (accessible comms, VRS, silver alert, school safety, animal welfare, 988, trust, cultural) | `accessibility` |
| 4B | 31 | 6 (recovery, peer support, trauma, elder abuse, offline gateway, neighborhood prep) | — |
| 5 | 32 | 6 (predictive risk, digital twin, synthetic data, OSM, identity, OSINT) | — |

## Implementation Sequence

### Phase 1: Ship What Exists (Stages 14–18)
85 items. No new APIs — fix P11, cloud stubs, typed clients, hardening, E2E tests.

### Phase 2: Survive DoD Audit (Stages 19–24)
100 items. FIPS crypto, data protection, STIG auth, validation, SBOM, CMMC docs.

### Phase 3: Geospatial Fast Response (Stages 25–26)
90 items. The detection-to-dispatch critical path: IoT sensors, camera AI, audio AI, RapidSOS, indoor maps, isochrone, call triage, signal preemption. **This is where the 2-second promise lives.**

### Phase 4: Full Situational Awareness (Stages 27–28)
70 items. Weather, earthquake, wildfire, hazmat, FHIR, hospital capacity, MCI triage, drones, cognitive assistant, edge AI, knowledge base.

### Phase 5: National Ecosystem (Stage 29)
55 items. EIDO, IPAWS, NG911, NIEM, FirstNet, mutual aid, Twilio, MQTT, OSINT.

### Phase 6: No One Left Behind (Stages 30–31)
67 items. Accessibility, Silver Alert, school safety, 988, cultural liaisons, recovery, PTSD, offline.

### Phase 7: Predictive (Stage 32)
33 items. Risk prediction, digital twins, synthetic data, adversarial testing, identity verification.

---

*Last updated: 2026-02-26 — Full expansion plan. 685 total items across 28 stages. 180 done, 505 pending.*
*DoD compliance: Stages 19–24 (source: `DOD_SECURITY_ANALYSIS.md`)*
*API expansion: Stages 25–32 (source: `E:\json_output\APIS\NEW-API-TIERS.md`)*
