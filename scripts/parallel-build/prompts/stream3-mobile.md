# Stream 3: MOBILE — Stage 9 + Stage 7C (TODO Items 57-59, 81-105)

You are working in a git worktree of TheWatch microservices solution. Your task is to implement all MAUI mobile production features: speech recognition, offline resilience, native features, evidence collection, and maps.

## YOUR ASSIGNED TODO ITEMS

### 7C. Client Maps (57-59)
57. Add Microsoft.Maui.Controls.Maps to MAUI project
58. Create MapPage.razor with responder placement pins and incident zones
59. Implement real-time responder movement on map (SignalR + map updates)

### 9A. Platform Speech Recognition (81-86)
81. Implement Android SpeechRecognizer with continuous recognition
82. Implement iOS SFSpeechRecognizer with on-device model
83. Implement Windows SpeechRecognizer for desktop testing
84. Add microphone + speech recognition runtime permission requests
85. Implement battery-aware throttling (reduce polling when battery < 20%)
86. Implement background service for perpetual listening (Android foreground service, iOS background task)

### 9B. Offline Resilience (87-92)
87. Add SQLite local database to MAUI project for offline storage
88. Implement offline queue: buffer API requests when no connectivity
89. Implement sync engine: reconcile local SQLite with server on reconnect
90. Implement conflict resolution: server-wins with local notification
91. Cache user profile, family data, and recent incidents locally
92. Offline emergency mode: activate P3 mesh network fallback when no internet

### 9C. Native Features (93-98)
93. Implement camera integration for evidence photo/video capture
94. Implement high-accuracy GPS mode during active emergencies
95. Implement haptic feedback patterns (SOS confirm, alert received, check-in reminder)
96. Implement biometric authentication gate (fingerprint/face before app access)
97. Implement background location tracking with user consent flow
98. Add deep link handling for push notification tap → specific page

### 9D. Evidence Collection (99-105)
99. Create EvidencePage.razor for video recording with GPS/timestamp overlay
100. Implement photo capture with automatic metadata (GPS, time, device ID)
101. Implement speech-to-text incident reporting in SITREP format
102. Implement chain-of-custody: SHA-256 hash + timestamp + device signature per artifact
103. Implement client-side content moderation (nudity detection before upload)
104. Create evidence upload queue with retry and progress tracking
105. Create IncidentReportPage.razor — post-incident questionnaire (SITREP framework)

## FILES YOU MAY MODIFY (your exclusive scope)

- `TheWatch.Mobile/` — ENTIRE directory is yours exclusively
  - `Components/Pages/` — new pages (MapPage, EvidencePage, IncidentReportPage)
  - `Services/` — new services (SpeechService, OfflineStorageService, SyncEngine, CameraService, etc.)
  - `Platforms/Android/` — SpeechRecognizer, foreground service, permissions
  - `Platforms/iOS/` — SFSpeechRecognizer, background tasks, permissions
  - `Platforms/Windows/` — Windows SpeechRecognizer
  - `MauiProgram.cs` — register all new services
  - `TheWatch.Mobile.csproj` — add NuGet packages
- `TheWatch.Mobile.Tests/` — add tests

## FILES YOU MUST NOT TOUCH

- Any `TheWatch.P*/` directory
- `TheWatch.Shared/` (any file)
- `TheWatch.Dashboard/`
- `TheWatch.Aspire.AppHost/`
- `TheWatch.Generators/`
- `docker/`, `helm/`, `.github/`, `infra/`

## CURRENT STATE OF MOBILE

Read `TheWatch.Mobile/MauiProgram.cs` first. Currently has:
- Login, Home, SOS, PhraseSetup, Health, Profile pages
- WatchAuthService (JWT auth with SecureStorage)
- PhraseService (Levenshtein fuzzy matching)
- SpeechListenerService (framework stub — no-op)
- WatchApiClient (typed HTTP client)
- WatchSignalRService (6 hub connections)
- WatchPushNotificationService (FCM handlers)

## PACKAGES TO ADD TO TheWatch.Mobile.csproj

```xml
<PackageReference Include="Microsoft.Maui.Controls.Maps" Version="10.*" />
<PackageReference Include="sqlite-net-pcl" Version="1.9.*" />
<PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.1.*" />
<PackageReference Include="Microsoft.Maui.Essentials" Version="10.*" />
```

## IMPLEMENTATION PATTERNS

### Platform Speech Recognition
```csharp
// Services/ISpeechRecognitionService.cs
public interface ISpeechRecognitionService
{
    event EventHandler<string> OnSpeechRecognized;
    Task StartListeningAsync();
    Task StopListeningAsync();
    bool IsListening { get; }
}

// Platforms/Android/AndroidSpeechService.cs — use android.speech.SpeechRecognizer
// Platforms/iOS/IosSpeechService.cs — use Speech.SFSpeechRecognizer
// Platforms/Windows/WindowsSpeechService.cs — use Windows.Media.SpeechRecognition
```

Register platform-specific implementations in MauiProgram.cs using conditional compilation:
```csharp
#if ANDROID
builder.Services.AddSingleton<ISpeechRecognitionService, AndroidSpeechService>();
#elif IOS
builder.Services.AddSingleton<ISpeechRecognitionService, IosSpeechService>();
#elif WINDOWS
builder.Services.AddSingleton<ISpeechRecognitionService, WindowsSpeechService>();
#endif
```

### SQLite Offline Storage
```csharp
public class OfflineDatabase
{
    SQLiteAsyncConnection _db;
    public async Task InitAsync()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "watch_offline.db3");
        _db = new SQLiteAsyncConnection(path);
        await _db.CreateTableAsync<CachedUserProfile>();
        await _db.CreateTableAsync<OfflineRequest>();
        // ...
    }
}
```

### Evidence Chain of Custody
```csharp
public class EvidenceChainOfCustody
{
    public string ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(data));
    }

    public EvidenceMetadata CreateMetadata(byte[] data, Location gps, string deviceId)
    {
        return new EvidenceMetadata
        {
            Sha256Hash = ComputeHash(data),
            CapturedAt = DateTime.UtcNow,
            GpsLatitude = gps.Latitude,
            GpsLongitude = gps.Longitude,
            DeviceId = deviceId,
            DeviceSignature = SignWithDeviceKey(data)
        };
    }
}
```

### SITREP Format
The memorandum specifies SITREP format for incident reports:
- **S**ituation: What happened
- **I**njuries: Who is hurt
- **T**hreat: Is threat ongoing
- **R**esources: What's needed
- **E**xpanded: Additional details
- **P**lan: Next steps / ETA

## WHEN DONE

Commit all changes with message:
```
feat(mobile): implement speech recognition, offline storage, evidence collection, maps

Items 57-59, 81-105: Platform speech (Android/iOS/Windows), SQLite offline DB,
sync engine, camera/GPS/haptics/biometric, evidence chain-of-custody,
SITREP reporting, MAUI maps with responder pins
```
