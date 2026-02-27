var builder = DistributedApplication.CreateBuilder(args);

// Security+ 1.1: JWT signing key delivered as a secret parameter (no hardcoded fallbacks)
var jwtKey = builder.AddParameter("jwt-key", secret: true);
var serviceApiKey = builder.AddParameter("service-api-key", secret: true);

// SQL Server with per-program databases
var sqlServer = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var dbP1 = sqlServer.AddDatabase("WatchCoreGatewayDB");
var dbP2 = sqlServer.AddDatabase("WatchVoiceEmergencyDB");
var dbP3 = sqlServer.AddDatabase("WatchMeshNetworkDB");
var dbP4 = sqlServer.AddDatabase("WatchWearableDB");
var dbP5 = sqlServer.AddDatabase("WatchAuthSecurityDB");
var dbP6 = sqlServer.AddDatabase("WatchFirstResponderDB");
var dbP7 = sqlServer.AddDatabase("WatchFamilyHealthDB");
var dbP8 = sqlServer.AddDatabase("WatchDisasterReliefDB");
var dbP9 = sqlServer.AddDatabase("WatchDoctorServicesDB");
var dbP10 = sqlServer.AddDatabase("WatchGamificationDB");
var dbP11 = sqlServer.AddDatabase("WatchSurveillanceDB");

// PostgreSQL + PostGIS for geospatial engine
var postgres = builder.AddPostgres("postgis")
    .WithLifetime(ContainerLifetime.Persistent);
var dbGeo = postgres.AddDatabase("WatchGeospatialDB");

// Kafka message broker for inter-service event bus
var kafka = builder.AddKafka("kafka")
    .WithLifetime(ContainerLifetime.Persistent);

// Microservices — each wired to its database
var p1_coregateway = builder.AddProject<Projects.TheWatch_P1_CoreGateway>("p1-coregateway")
    .WithExternalHttpEndpoints()
    .WithReference(dbP1).WaitFor(dbP1)
    .WithEnvironment("Jwt__Key", jwtKey);

var p2_voiceemergency = builder.AddProject<Projects.TheWatch_P2_VoiceEmergency>("p2-voiceemergency")
    .WithExternalHttpEndpoints()
    .WithReference(dbP2).WaitFor(dbP2)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Jwt__Key", jwtKey);

var p3_meshnetwork = builder.AddProject<Projects.TheWatch_P3_MeshNetwork>("p3-meshnetwork")
    .WithExternalHttpEndpoints()
    .WithReference(dbP3).WaitFor(dbP3)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Jwt__Key", jwtKey);

var p4_wearable = builder.AddProject<Projects.TheWatch_P4_Wearable>("p4-wearable")
    .WithExternalHttpEndpoints()
    .WithReference(dbP4).WaitFor(dbP4)
    .WithEnvironment("Jwt__Key", jwtKey);

var p5_authsecurity = builder.AddProject<Projects.TheWatch_P5_AuthSecurity>("p5-authsecurity")
    .WithExternalHttpEndpoints()
    .WithReference(dbP5).WaitFor(dbP5)
    .WithEnvironment("Jwt__Key", jwtKey);

var p6_firstresponder = builder.AddProject<Projects.TheWatch_P6_FirstResponder>("p6-firstresponder")
    .WithExternalHttpEndpoints()
    .WithReference(dbP6).WaitFor(dbP6)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Jwt__Key", jwtKey);

var p7_familyhealth = builder.AddProject<Projects.TheWatch_P7_FamilyHealth>("p7-familyhealth")
    .WithExternalHttpEndpoints()
    .WithReference(dbP7).WaitFor(dbP7)
    .WithEnvironment("Jwt__Key", jwtKey);

var p8_disasterrelief = builder.AddProject<Projects.TheWatch_P8_DisasterRelief>("p8-disasterrelief")
    .WithExternalHttpEndpoints()
    .WithReference(dbP8).WaitFor(dbP8)
    .WithEnvironment("Jwt__Key", jwtKey);

var p9_doctorservices = builder.AddProject<Projects.TheWatch_P9_DoctorServices>("p9-doctorservices")
    .WithExternalHttpEndpoints()
    .WithReference(dbP9).WaitFor(dbP9)
    .WithEnvironment("Jwt__Key", jwtKey);

var p10_gamification = builder.AddProject<Projects.TheWatch_P10_Gamification>("p10-gamification")
    .WithExternalHttpEndpoints()
    .WithReference(dbP10).WaitFor(dbP10)
    .WithEnvironment("Jwt__Key", jwtKey);

var p11_surveillance = builder.AddProject<Projects.TheWatch_P11_Surveillance>("p11-surveillance")
    .WithExternalHttpEndpoints()
    .WithReference(dbP11).WaitFor(dbP11)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Jwt__Key", jwtKey);

// Geospatial — PostGIS spatial engine
var geospatial = builder.AddProject<Projects.TheWatch_Geospatial>("geospatial")
    .WithExternalHttpEndpoints()
    .WithReference(dbGeo).WaitFor(dbGeo)
    .WithEnvironment("Jwt__Key", jwtKey);

// Dashboard — frontend to all microservices
builder.AddProject<Projects.TheWatch_Dashboard>("dashboard")
    .WithExternalHttpEndpoints()
    .WithReference(p1_coregateway)
    .WithReference(p2_voiceemergency)
    .WithReference(p3_meshnetwork)
    .WithReference(p4_wearable)
    .WithReference(p5_authsecurity)
    .WithReference(p6_firstresponder)
    .WithReference(p7_familyhealth)
    .WithReference(p8_disasterrelief)
    .WithReference(p9_doctorservices)
    .WithReference(p10_gamification)
    .WithReference(p11_surveillance)
    .WithReference(geospatial);

// Admin Portal — admin-only management interface
builder.AddProject<Projects.TheWatch_Admin>("admin")
    .WithExternalHttpEndpoints()
    .WithReference(p1_coregateway)
    .WithReference(p2_voiceemergency)
    .WithReference(p3_meshnetwork)
    .WithReference(p4_wearable)
    .WithReference(p5_authsecurity)
    .WithReference(p6_firstresponder)
    .WithReference(p7_familyhealth)
    .WithReference(p8_disasterrelief)
    .WithReference(p9_doctorservices)
    .WithReference(p10_gamification)
    .WithReference(p11_surveillance)
    .WithReference(geospatial);

// Admin REST API Gateway — Zero Trust API gateway for all services
builder.AddProject<Projects.TheWatch_Admin_RestAPI>("admin-restapi")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("ServiceAuth__ApiKey", serviceApiKey)
    .WithReference(p1_coregateway)
    .WithReference(p2_voiceemergency)
    .WithReference(p3_meshnetwork)
    .WithReference(p4_wearable)
    .WithReference(p5_authsecurity)
    .WithReference(p6_firstresponder)
    .WithReference(p7_familyhealth)
    .WithReference(p8_disasterrelief)
    .WithReference(p9_doctorservices)
    .WithReference(p10_gamification)
    .WithReference(p11_surveillance)
    .WithReference(geospatial);

builder.Build().Run();
