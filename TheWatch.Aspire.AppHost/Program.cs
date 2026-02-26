var builder = DistributedApplication.CreateBuilder(args);

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
    .WithReference(dbP1).WaitFor(dbP1);

var p2_voiceemergency = builder.AddProject<Projects.TheWatch_P2_VoiceEmergency>("p2-voiceemergency")
    .WithExternalHttpEndpoints()
    .WithReference(dbP2).WaitFor(dbP2)
    .WithReference(kafka).WaitFor(kafka);

var p3_meshnetwork = builder.AddProject<Projects.TheWatch_P3_MeshNetwork>("p3-meshnetwork")
    .WithExternalHttpEndpoints()
    .WithReference(dbP3).WaitFor(dbP3)
    .WithReference(kafka).WaitFor(kafka);

var p4_wearable = builder.AddProject<Projects.TheWatch_P4_Wearable>("p4-wearable")
    .WithExternalHttpEndpoints()
    .WithReference(dbP4).WaitFor(dbP4);

var p5_authsecurity = builder.AddProject<Projects.TheWatch_P5_AuthSecurity>("p5-authsecurity")
    .WithExternalHttpEndpoints()
    .WithReference(dbP5).WaitFor(dbP5);

var p6_firstresponder = builder.AddProject<Projects.TheWatch_P6_FirstResponder>("p6-firstresponder")
    .WithExternalHttpEndpoints()
    .WithReference(dbP6).WaitFor(dbP6)
    .WithReference(kafka).WaitFor(kafka);

var p7_familyhealth = builder.AddProject<Projects.TheWatch_P7_FamilyHealth>("p7-familyhealth")
    .WithExternalHttpEndpoints()
    .WithReference(dbP7).WaitFor(dbP7);

var p8_disasterrelief = builder.AddProject<Projects.TheWatch_P8_DisasterRelief>("p8-disasterrelief")
    .WithExternalHttpEndpoints()
    .WithReference(dbP8).WaitFor(dbP8);

var p9_doctorservices = builder.AddProject<Projects.TheWatch_P9_DoctorServices>("p9-doctorservices")
    .WithExternalHttpEndpoints()
    .WithReference(dbP9).WaitFor(dbP9);

var p10_gamification = builder.AddProject<Projects.TheWatch_P10_Gamification>("p10-gamification")
    .WithExternalHttpEndpoints()
    .WithReference(dbP10).WaitFor(dbP10);

// Geospatial — PostGIS spatial engine
var geospatial = builder.AddProject<Projects.TheWatch_Geospatial>("geospatial")
    .WithExternalHttpEndpoints()
    .WithReference(dbGeo).WaitFor(dbGeo);

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
    .WithReference(geospatial);

// Admin REST API Gateway — Zero Trust API gateway for all services
builder.AddProject<Projects.TheWatch_Admin_RestAPI>("admin-restapi")
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
    .WithReference(geospatial);

builder.Build().Run();
