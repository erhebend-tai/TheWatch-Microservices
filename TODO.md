# TheWatch — Master TODO List

> 300 items organized by stage. Status: `[ ]` pending, `[x]` done, `[~]` in progress.
> See `ROADMAP.md` for full stage descriptions.

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

- [ ] 15. Map P0 `UniversalMeasurementsDB` tables from `00_Schema.sql`–`10_Devices.sql` to EF seed data
- [ ] 16. Adapt P2 DDL from `Watch_Program_02_VoiceEmergency.md` to EF entity configs
- [ ] 17. Adapt P3 DDL from `Watch-MeshNetwork.md` (19 tables) to EF entity configs
- [ ] 18. Adapt P4 DDL from `Program4_Watch_Wearable.sql` to EF entity configs
- [ ] 19. Adapt P5 DDL from `Watch-AuthSecurity/` (4 parts) to EF entity configs
- [ ] 20. Adapt P8 DDL from `Watch-DisasterRelief/` to EF entity configs (18 tables)
- [ ] 21. Create seed data scripts for development (test users, sample incidents, demo families)

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
- [ ] 161. Create Terraform module for Amazon ElastiCache Redis cluster (cluster mode enabled, 3 shards x 2 replicas) for session store, rate limiting counters, and SignalR backplane — configure encryption at rest (KMS) and in transit (TLS)
- [ ] 162. Create Terraform module for Amazon MSK (Managed Streaming for Kafka) cluster with 3 brokers across 3 AZs, topic auto-creation disabled, and IAM authentication — create topics matching existing Kafka event bus configuration
- [ ] 163. Create Terraform module for Amazon S3 buckets: `thewatch-evidence-{env}` (Intelligent-Tiering, versioning, object lock for legal hold), `thewatch-backups-{env}` (Glacier Deep Archive lifecycle at 90 days), `thewatch-static-{env}` (CloudFront origin)
- [ ] 164. Configure S3 event notifications to SQS to Lambda pipeline for evidence processing: on `PutObject` to evidence bucket, trigger Lambda that validates SHA-256 chain-of-custody hash, runs content moderation via Rekognition, and writes metadata to DynamoDB
- [ ] 165. Create Terraform module for Amazon DynamoDB table `thewatch-audit-log` with on-demand capacity, partition key `ServiceName#Date`, sort key `Timestamp#EventId`, TTL on `ExpiresAt` (365 days), and DynamoDB Streams enabled for real-time audit forwarding

### 13C. AWS Security & Identity
- [ ] 166. Create Terraform module for Amazon Cognito User Pool with custom attributes mapping to TheWatch roles (Admin, Responder, FamilyMember, Doctor, Patient), MFA enforcement (TOTP + SMS), and advanced security features (compromised credentials detection, adaptive authentication)
- [ ] 167. Create IAM roles with least-privilege policies for each ECS task: P2 gets `rekognition:DetectModerationLabels` + `transcribe:StartStreamTranscription`, P7/P9 get `healthlake:*`, P5 gets `cognito-idp:Admin*`, all get `s3:PutObject` scoped to their service prefix
- [ ] 168. Create Terraform module for AWS KMS customer-managed keys: `thewatch-data-key` (RDS, ElastiCache, S3 encryption), `thewatch-jwt-key` (JWT signing), `thewatch-evidence-key` (evidence bucket SSE with key rotation every 365 days)
- [ ] 169. Configure AWS Secrets Manager for all service secrets: database connection strings, Kafka SASL credentials, Redis AUTH tokens, Firebase server key, third-party API keys — create rotation Lambda for database credentials (30-day rotation)
- [ ] 170. Create Terraform module for AWS WAF v2 WebACL attached to ALB/API Gateway: rate-based rule (2,000 requests/5min per IP), SQL injection rule set, XSS rule set, known bad inputs rule set, geo-restriction (block OFAC-sanctioned countries), and IP reputation list
- [ ] 171. Configure AWS GuardDuty with S3 protection, ECS runtime monitoring, and Malware Protection — create EventBridge rule to forward HIGH and CRITICAL findings to SNS topic linked to PagerDuty integration
- [ ] 172. Configure AWS Security Hub with CIS AWS Foundations Benchmark v1.4, AWS Foundational Security Best Practices, and NIST 800-53 standards — create custom actions for automated remediation of non-compliant resources via Lambda

### 13D. AWS Observability & DevOps
- [ ] 173. Create Terraform module for Amazon CloudWatch: log groups per service (`/ecs/thewatch/{service}`, 90-day retention), metric filters for error rate and latency percentiles, composite alarms for service health, and Synthetics canaries for endpoint monitoring
- [ ] 174. Configure AWS X-Ray tracing integrated with OpenTelemetry SDK in each service — create X-Ray groups per service, sampling rules (1% for health checks, 100% for errors, 5% for normal traffic), and service map dashboards
- [ ] 175. Create CodePipeline with CodeBuild stages: Source (GitHub webhook) to Build (dotnet publish + Docker build) to Test (dotnet test + Trivy container scan) to Deploy-Staging (ECS rolling update) to Manual-Approval to Deploy-Prod (ECS blue/green via CodeDeploy)
- [ ] 176. Create Terraform module for Amazon ECR repositories (one per service + dashboard) with lifecycle policies (keep last 20 tagged images, expire untagged after 7 days), image scanning on push, and cross-region replication to DR region
- [ ] 177. Configure AWS Backup plan: daily RDS snapshots (35-day retention), weekly DynamoDB backups (90-day retention), monthly S3 cross-region copy to DR region — create backup vault with vault lock (72-hour cool-off, 365-day min retention)
- [ ] 178. Create CloudFormation StackSets for multi-account deployment: `thewatch-dev`, `thewatch-staging`, `thewatch-prod` accounts with Service Control Policies preventing region usage outside us-east-1 and us-west-2
- [ ] 179. Configure Amazon EventBridge event bus `thewatch-events` with rules routing domain events (IncidentCreated, DispatchRequested, SOSActivated) to targets: CloudWatch Logs (audit), Lambda (processing), SNS (notifications), Step Functions (orchestration)
- [ ] 180. Create AWS Step Functions state machine for incident lifecycle orchestration: SOS received to validate caller to dispatch nearest responder to start evidence collection to monitor resolution to generate post-incident report — with error handling, retries, and timeouts at each step

---

## Quick Reference

| Range | Stage | Theme |
|-------|-------|-------|
| 1–25 | 5 | Database (EF Core + SQL Server) |
| 26–45 | 6 | Real-Time (SignalR + Kafka + Push) |
| 46–60 | 7 | Geospatial (PostGIS + Maps) |
| 61–80 | 8 | Auth & Security (P5 Full + RBAC) |
| 81–105 | 9 | MAUI Production (Speech, Offline, Evidence) |
| 106–120 | 10 | DevOps (Docker + K8s + CI/CD) |
| 121–140 | 11 | Cloud (Azure + GCP + Cloudflare) |
| 141–150 | 12 | Advanced (ML, Compliance, Graph, Observability) |

---

*Last updated: 2026-02-26 (Session 21 — Stage 12 complete: ML/AI, Compliance, Graph DB, Observability)*
