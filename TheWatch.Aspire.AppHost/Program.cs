var builder = DistributedApplication.CreateBuilder(args);

var p1_coregateway = builder.AddProject<Projects.TheWatch_P1_CoreGateway>("p1-coregateway")
    .WithExternalHttpEndpoints();

var p2_voiceemergency = builder.AddProject<Projects.TheWatch_P2_VoiceEmergency>("p2-voiceemergency")
    .WithExternalHttpEndpoints();

var p3_meshnetwork = builder.AddProject<Projects.TheWatch_P3_MeshNetwork>("p3-meshnetwork")
    .WithExternalHttpEndpoints();

var p4_wearable = builder.AddProject<Projects.TheWatch_P4_Wearable>("p4-wearable")
    .WithExternalHttpEndpoints();

var p5_authsecurity = builder.AddProject<Projects.TheWatch_P5_AuthSecurity>("p5-authsecurity")
    .WithExternalHttpEndpoints();

var p6_firstresponder = builder.AddProject<Projects.TheWatch_P6_FirstResponder>("p6-firstresponder")
    .WithExternalHttpEndpoints();

var p7_familyhealth = builder.AddProject<Projects.TheWatch_P7_FamilyHealth>("p7-familyhealth")
    .WithExternalHttpEndpoints();

var p8_disasterrelief = builder.AddProject<Projects.TheWatch_P8_DisasterRelief>("p8-disasterrelief")
    .WithExternalHttpEndpoints();

var p9_doctorservices = builder.AddProject<Projects.TheWatch_P9_DoctorServices>("p9-doctorservices")
    .WithExternalHttpEndpoints();

var p10_gamification = builder.AddProject<Projects.TheWatch_P10_Gamification>("p10-gamification")
    .WithExternalHttpEndpoints();

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
    .WithReference(p10_gamification);

builder.Build().Run();
