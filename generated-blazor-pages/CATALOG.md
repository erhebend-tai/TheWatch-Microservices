# Generated Blazor Pages Catalog

Generated: 500 pages across 19 domains

## Summary by Domain


### ADMIN (51 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| AdminCreatePage | Create | /admin/cost-recovery/invoices | POST | generateRecoveryInvoice |
| AdminCreatePage | Create | /admin/protocols/{protocolid}/versions | POST | createProtocolVersion |
| AdminDetailPage | Detail | /admin/cost-recovery/incidents/{incidentid}/summary | GET | getIncidentCostSummary |
| AdminListPage | List | /admin/protocols | GET | listActiveProtocols |
| AiCreatePage | Create | /ai/places/classify | POST | classifyPlace |
| AiCreatePage | Create | /ai/places/classify-image | POST | classifyPlaceFromImage |
| AiCreatePage | Create | /ai/analyze/incident | POST | analyzeIncident |
| AiCreatePage | Create | /ai/analyze/text | POST | analyzeText |
| AiCreatePage | Create | /ai/analyze/image | POST | analyzeImage |
| AiCreatePage | Create | /ai/chat | POST | sendChatMessage |
| AiCreatePage | Create | /ai/supervisor/create | POST | createSupervisor |
| AiCreatePage | Create | /ai/supervisor/assign | POST | assignAgents |
| AiDetailPage | Detail | /ai/insights/{incidentid} | GET | getAIInsights |
| AiListPage | List | /ai/chat/history | GET | getChatHistory |
| AiMlCreatePage | Create | /ai-ml/lpr/process | POST | extractLicensePlates |
| AiMlCreatePage | Create | /ai-ml/biometric/identify | POST | identifyBiometric |
| AiMlCreatePage | Create | /ai-ml/auto-aar/generate | POST | generateAAR |
| AiMlCreatePage | Create | /ai-ml/facial/identify | POST | identifyPersonFacial |
| AiMlCreatePage | Create | /ai-ml/facial/enroll | POST | enrollMissingPersonFace |
| AiMlCreatePage | Create | /ai-ml/satellite/task | POST | taskSatellite |
| AiMlCreatePage | Create | /ai-ml/acoustic/detect | POST | detectAcousticEvents |
| AiMlDetailPage | Detail | /ai-ml/auto-aar/reports/{reportid} | GET | getAARReport |
| AiMlListPage | List | /ai-ml/crowd/alerts | GET | listCrowdAlerts |
| AiMlListPage | List | /ai-ml/satellite/archive | GET | searchSatelliteArchive |
| AiMlListPage | List | /ai-ml/forecasting/volume | GET | getPredictedVolume |
| AiMlListPage | List | /ai-ml/forecasting/recommendations | GET | getStaffingRecommendations |
| AudioCreatePage | Create | /audio/upload | POST | uploadAudio |
| AudioCreatePage | Create | /audio/transcribe | POST | transcribeAudio |
| AudioDetailPage | Detail | /audio/transcriptions/{transcriptionid} | GET | getTranscription |
| AudioListPage | List | /audio/transcriptions | GET | listTranscriptions |
| MlCreatePage | Create | /ml/predict | POST | predict |
| MlListPage | List | /ml/models | GET | listModels |
| ProcessingDetailPage | Detail | /processing/jobs/{jobid} | GET | getJob |
| ProcessingListPage | List | /processing/jobs | GET | listJobs |
| RecognitionCreatePage | Create | /recognition/sessions | POST | startRecognitionSession |
| RecognitionCreatePage | Create | /recognition/alerts/{alertid}/acknowledge | POST | acknowledgeAlert |
| RecognitionCreatePage | Create | /recognition/evidence | POST | createEvidenceFromDetection |
| RecognitionDeletePage | Delete | /recognition/sessions/{sessionid} | DELETE | stopRecognitionSession |
| RecognitionDetailPage | Detail | /recognition/sessions/{sessionid}/objects | GET | getDetectedObjects |
| RecognitionDetailPage | Detail | /recognition/sessions/{sessionid}/ws | GET | realtimeObjectDetection |
| RecognitionDetailPage | Detail | /recognition/alerts/{alertid} | GET | getRecognitionAlert |
| RecognitionEditPage | Edit | /recognition/sessions/{sessionid} | PATCH | updateRecognitionSession |
| RecognitionListPage | List | /recognition/alerts | GET | listRecognitionAlerts |
| TicketsDetailPage | Detail | /tickets/{ticketid} | GET | getTicket |
| TicketsEditPage | Edit | /tickets/{ticketid} | PATCH | updateTicket |
| TicketsListPage | List | /tickets | GET | listTickets |
| VideoCreatePage | Create | /video/upload | POST | uploadVideo |
| VideoCreatePage | Create | /video/extract-gps | POST | extractGPS |
| VideoCreatePage | Create | /video/analyze | POST | analyzeVideo |
| VideoDetailPage | Detail | /video/analyses/{analysisid} | GET | getVideoAnalysis |
| VideoListPage | List | /video/analyses | GET | listVideoAnalyses |

### AUTH (51 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| .WellKnownListPage | List | /.well-known/jwks.json | GET | getJwks |
| AgreementsDetailPage | Detail | /agreements/{agreementtype} | GET | getAgreement |
| AgreementsDetailPage | Detail | /agreements/{agreementtype}/versions | GET | getAgreementVersions |
| AgreementsListPage | List | /agreements | GET | getAllAgreements |
| AuthCreatePage | Create | /auth/login | POST | login |
| AuthCreatePage | Create | /auth/refresh | POST | refreshToken |
| AuthCreatePage | Create | /auth/logout | POST | logout |
| AuthCreatePage | Create | /auth/introspect | POST | introspectToken |
| AuthCreatePage | Create | /auth/step-up/challenge | POST | createStepUpChallenge |
| AuthCreatePage | Create | /auth/step-up/verify | POST | verifyStepUp |
| AuthCreatePage | Create | /auth/password-reset/request | POST | requestPasswordReset |
| AuthCreatePage | Create | /auth/password-reset/verify | POST | verifyPasswordReset |
| AuthCreatePage | Create | /auth/mfa/enroll | POST | enrollMfa |
| AuthCreatePage | Create | /auth/mfa/verify | POST | verifyMfa |
| AuthCreatePage | Create | /auth/mfa/disable | POST | disableMfa |
| AuthDeletePage | Delete | /auth/sessions/{sessionid} | DELETE | revokeSession |
| AuthListPage | List | /auth/me | GET | whoAmI |
| AuthListPage | List | /auth/sessions | GET | listSessions |
| DisclosuresListPage | List | /disclosures/incident-recording | GET | getRecordingDisclosure |
| DisclosuresListPage | List | /disclosures/responder-liability | GET | getResponderLiabilityDisclosure |
| OauthCreatePage | Create | /oauth/token | POST | oauthToken |
| OauthCreatePage | Create | /oauth/revoke | POST | oauthRevoke |
| OauthListPage | List | /oauth/authorize | GET | oauthAuthorize |
| OauthListPage | List | /oauth/userinfo | GET | oidcUserInfo |
| SignupCreatePage | Create | /signup/start | POST | startRegistration |
| SignupCreatePage | Create | /signup/{registrationid}/parental-consent | POST | submitParentalConsent |
| SignupCreatePage | Create | /signup/{registrationid}/walkthrough/start | POST | startWalkthrough |
| SignupCreatePage | Create | /signup/{registrationid}/walkthrough/step/{stepid}/complete | POST | completeWalkthroughStep |
| SignupCreatePage | Create | /signup/{registrationid}/walkthrough/simulate-incident | POST | simulateIncident |
| SignupCreatePage | Create | /signup/{registrationid}/walkthrough/simulate-response | POST | simulateResponse |
| SignupCreatePage | Create | /signup/{registrationid}/complete | POST | completeRegistration |
| SignupCreatePage | Create | /signup/{registrationid}/cancel | POST | cancelRegistration |
| SignupDetailPage | Detail | /signup/{registrationid}/age-verification | GET | getAgeVerification |
| SignupDetailPage | Detail | /signup/{registrationid}/parental-consent | GET | getParentalConsentStatus |
| SignupDetailPage | Detail | /signup/{registrationid}/walkthrough/status | GET | getWalkthroughStatus |
| SignupDetailPage | Detail | /signup/{registrationid}/walkthrough/step/{stepid} | GET | getWalkthroughStep |
| SignupDetailPage | Detail | /signup/{registrationid}/status | GET | getRegistrationStatus |
| SignupListPage | List | /signup/walkthrough-scenarios | GET | getWalkthroughScenarios |
| UsersCreatePage | Create | /users/{userid}/consents | POST | recordUserConsent |
| UsersCreatePage | Create | /users/{userid}/consents/withdraw | POST | withdrawConsent |
| UsersCreatePage | Create | /users/{userid}/parental-consent | POST | submitParentalConsent |
| UsersCreatePage | Create | /users/{userid}/parental-consent/verify | POST | verifyParentalConsent |
| UsersCreatePage | Create | /users/me/data-export | POST | requestDataExport |
| UsersDeletePage | Delete | /users/me | DELETE | deleteAccount |
| UsersDetailPage | Detail | /users/{userid}/consents | GET | getUserConsents |
| UsersDetailPage | Detail | /users/{userid}/consents/{agreementtype} | GET | getUserConsentForAgreement |
| UsersDetailPage | Detail | /users/{userid}/consents/pending | GET | getPendingConsents |
| UsersDetailPage | Detail | /users/{userid}/parental-consent | GET | getParentalConsentStatus |
| UsersDetailPage | Detail | /users/me/data-export/{jobid} | GET | getDataExportStatus |
| UsersListPage | List | /users/me | GET | getMyProfile |
| UsersListPage | List | /users/me/deletion-status | GET | getDeletionStatus |

### CACHING (6 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| CacheCreatePage | Create | /cache/inference | POST | storeInferenceResult |
| CacheDetailPage | Detail | /cache/inference/{cachekey} | GET | getCachedInferenceResult |
| CacheDetailPage | Detail | /cache/responders/{responderid} | GET | getCachedResponder |
| CacheDetailPage | Detail | /cache/incidents/{incidentid} | GET | getCachedIncident |
| CacheDetailPage | Detail | /cache/dispatch/{incidentid} | GET | getCachedDispatchCandidates |
| CacheListPage | List | /cache/responders/nearby | GET | getCachedNearbyResponders |

### COMMUNITY (9 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| CommunityCreatePage | Create | /community/volunteers/activate | POST | activateVolunteers |
| CommunityCreatePage | Create | /community/mutual-aid/requests | POST | requestMutualAid |
| CommunityCreatePage | Create | /community/vulnerable-persons | POST | registerVulnerablePerson |
| CommunityCreatePage | Create | /community/translate/speech | POST | translateSpeech |
| CommunityEditPage | Edit | /community/food/sites/{siteid}/status | PATCH | updateSiteStatus |
| CommunityListPage | List | /community/food/distribution-points | GET | searchDistributionPoints |
| CommunityListPage | List | /community/volunteers/search | GET | searchSkilledVolunteers |
| CommunityListPage | List | /community/mutual-aid/resources | GET | listAssistingResources |
| CommunityListPage | List | /community/translate/languages | GET | getSupportedLanguages |

### DATABASE (35 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| ProjectsCreatePage | Create | /projects/{projectid}/datasets | POST | createDataset |
| ProjectsCreatePage | Create | /projects/{projectid}/datasets/{datasetid}/tables | POST | createTable |
| ProjectsCreatePage | Create | /projects/{projectid}/jobs | POST | insertJob |
| ProjectsCreatePage | Create | /projects/{projectid}/queries | POST | query |
| ProjectsCreatePage | Create | /projects/{projectid}/datasets/{datasetid}/routines | POST | createRoutine |
| ProjectsCreatePage | Create | /projects/{projectid}/datasets/{datasetid}/models | POST | createModel |
| ProjectsCreatePage | Create | /projects/{projectid}/databases/{databaseid}/documents | POST | createDocument |
| ProjectsCreatePage | Create | /projects/{projectid}/databases/{databaseid}/documents:runquery | POST | runQuery |
| ProjectsCreatePage | Create | /projects/{projectid}/databases/{databaseid}/documents:begintransaction | POST | beginTransaction |
| ProjectsCreatePage | Create | /projects/{projectid}/databases/{databaseid}/documents:commit | POST | commitTransaction |
| ProjectsCreatePage | Create | /projects/{projectid}/databases/{databaseid}/documents:rollback | POST | rollbackTransaction |
| ProjectsDeletePage | Delete | /projects/{projectid}/datasets/{datasetid} | DELETE | deleteDataset |
| ProjectsDeletePage | Delete | /projects/{projectid}/datasets/{datasetid}/tables/{tableid} | DELETE | deleteTable |
| ProjectsDeletePage | Delete | /projects/{projectid}/datasets/{datasetid}/routines/{routineid} | DELETE | deleteRoutine |
| ProjectsDeletePage | Delete | /projects/{projectid}/datasets/{datasetid}/models/{modelid} | DELETE | deleteModel |
| ProjectsDeletePage | Delete | /projects/{projectid}/databases/{databaseid}/documents/{documentpath} | DELETE | deleteDocument |
| ProjectsDetailPage | Detail | /projects/{projectid} | GET | getProject |
| ProjectsDetailPage | Detail | /projects/{projectid}/datasets | GET | listDatasets |
| ProjectsDetailPage | Detail | /projects/{projectid}/datasets/{datasetid} | GET | getDataset |
| ProjectsDetailPage | Detail | /projects/{projectid}/datasets/{datasetid}/tables | GET | listTables |
| ProjectsDetailPage | Detail | /projects/{projectid}/datasets/{datasetid}/tables/{tableid} | GET | getTable |
| ProjectsDetailPage | Detail | /projects/{projectid}/jobs | GET | listJobs |
| ProjectsDetailPage | Detail | /projects/{projectid}/jobs/{jobid} | GET | getJob |
| ProjectsDetailPage | Detail | /projects/{projectid}/datasets/{datasetid}/routines | GET | listRoutines |
| ProjectsDetailPage | Detail | /projects/{projectid}/datasets/{datasetid}/routines/{routineid} | GET | getRoutine |
| ProjectsDetailPage | Detail | /projects/{projectid}/datasets/{datasetid}/models | GET | listModels |
| ProjectsDetailPage | Detail | /projects/{projectid}/datasets/{datasetid}/models/{modelid} | GET | getModel |
| ProjectsDetailPage | Detail | /projects/{projectid}/databases/{databaseid}/documents/{documentpath} | GET | getDocument |
| ProjectsDetailPage | Detail | /projects/{projectid}/databases/{databaseid}/collectiongroups/{collectionid}/indexes | GET | listIndexes |
| ProjectsEditPage | Edit | /projects/{projectid}/datasets/{datasetid} | PATCH | updateDataset |
| ProjectsEditPage | Edit | /projects/{projectid}/datasets/{datasetid}/tables/{tableid} | PATCH | updateTable |
| ProjectsEditPage | Edit | /projects/{projectid}/datasets/{datasetid}/routines/{routineid} | PATCH | updateRoutine |
| ProjectsEditPage | Edit | /projects/{projectid}/datasets/{datasetid}/models/{modelid} | PATCH | updateModel |
| ProjectsEditPage | Edit | /projects/{projectid}/databases/{databaseid}/documents/{documentpath} | PATCH | updateDocument |
| ProjectsListPage | List | /projects | GET | listProjects |

### DISASTER (48 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| DisasterCreatePage | Create | /disaster/damage-assessment/analyze | POST | analyzeDamageImagery |
| DisasterCreatePage | Create | /disaster/grants/apply | POST | submitGrantApplication |
| DisasterCreatePage | Create | /disaster/incidents | POST | createDisasterIncident |
| DisasterCreatePage | Create | /disaster/damage-reports | POST | submitDamageReport |
| DisasterCreatePage | Create | /disaster/evacuations | POST | createEvacuationRequest |
| DisasterCreatePage | Create | /disaster/resources | POST | allocateResource |
| DisasterCreatePage | Create | /disaster/shelters | POST | registerShelter |
| DisasterCreatePage | Create | /disaster/wildfire/projections | POST | getWildfireProjection |
| DisasterCreatePage | Create | /disaster/housing/bookings | POST | bookHousing |
| DisasterDetailPage | Detail | /disaster/damage-assessment/results/{taskid} | GET | getDamageAnalysisResults |
| DisasterDetailPage | Detail | /disaster/grants/applications/{applicationid} | GET | getGrantApplicationStatus |
| DisasterDetailPage | Detail | /disaster/incidents/{incidentid} | GET | getDisasterIncident |
| DisasterListPage | List | /disaster/utilities/priorities | GET | listRestorationPriorities |
| DisasterListPage | List | /disaster/utilities/crews | GET | getUtilityCrewStatus |
| DisasterListPage | List | /disaster/infrastructure/status | GET | getInfrastructureStatus |
| DisasterListPage | List | /disaster/infrastructure/outages | GET | listActiveOutages |
| DisasterListPage | List | /disaster/incidents | GET | listDisasterIncidents |
| DisasterListPage | List | /disaster/evacuations | GET | listEvacuationRequests |
| DisasterListPage | List | /disaster/resources | GET | listResourceAllocations |
| DisasterListPage | List | /disaster/shelters | GET | listShelters |
| DisasterListPage | List | /disaster/flood/gauges | GET | searchFloodGauges |
| DisasterListPage | List | /disaster/flood/risk-zones | GET | getFloodRiskZones |
| DisasterListPage | List | /disaster/wildfire/perimeters | GET | listWildfirePerimeters |
| DisasterListPage | List | /disaster/housing/search | GET | searchAvailableHousing |
| DisasterZonesListPage | List | /disaster-zones | GET | listDisasterZones |
| EvacuationsCreatePage | Create | /evacuations/{evacuationid}/location | POST | updateEvacuationLocation |
| EvacuationsDetailPage | Detail | /evacuations/{evacuationid} | GET | getEvacuation |
| EvacuationsDetailPage | Detail | /evacuations/{evacuationid}/messages | GET | listEvacuationMessages |
| MatchesCreatePage | Create | /matches/{matchid}/respond | POST | respondToMatch |
| MatchesListPage | List | /matches | GET | listMatches |
| OffersCreatePage | Create | /offers/resources | POST | createResourceOffer |
| OffersCreatePage | Create | /offers/resources/{offerid}/withdraw | POST | withdrawEvacuationOffer |
| OffersDetailPage | Detail | /offers/resources/{offerid} | GET | getEvacuationOffer |
| OffersEditPage | Edit | /offers/resources/{offerid} | PATCH | updateEvacuationOffer |
| RequestsCreatePage | Create | /requests/evacuate | POST | createEvacuationRequest |
| RequestsCreatePage | Create | /requests/{requestid}/cancel | POST | cancelEvacuationRequest |
| RequestsDetailPage | Detail | /requests/{requestid} | GET | getEvacuationRequest |
| RequestsEditPage | Edit | /requests/{requestid} | PATCH | updateEvacuationRequest |
| RoutesCreatePage | Create | /routes/evacuation-route | POST | calculateEvacuationRoute |
| SarDetailPage | Detail | /sar/operations/{operationid} | GET | getSAROperation |
| SarDetailPage | Detail | /sar/operations/{operationid}/coverage | GET | getSearchCoverage |
| SheltersListPage | List | /shelters | GET | listShelters |
| SyncCreatePage | Create | /sync/evacuation-actions | POST | syncEvacuationActions |
| WeatherCreatePage | Create | /weather/alerts/subscribe | POST | subscribeWeatherAlerts |
| WeatherDetailPage | Detail | /weather/alerts/{alertid} | GET | getWeatherAlert |
| WeatherListPage | List | /weather/current | GET | getCurrentWeather |
| WeatherListPage | List | /weather/forecast | GET | getWeatherForecast |
| WeatherListPage | List | /weather/alerts | GET | getWeatherAlerts |

### DISPATCH (7 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| DispatchCreatePage | Create | /dispatch | POST | createDispatch |
| DispatchCreatePage | Create | /dispatch/{dispatchid}/units | POST | addDispatchUnits |
| DispatchCreatePage | Create | /dispatch/cad-systems/{systemid}/sync | POST | syncCADSystem |
| DispatchDetailPage | Detail | /dispatch/{dispatchid} | GET | getDispatch |
| DispatchDetailPage | Detail | /dispatch/emergency/{emergencyid} | GET | getDispatchesByEmergency |
| DispatchEditPage | Edit | /dispatch/{dispatchid}/status | PATCH | updateDispatchStatus |
| DispatchListPage | List | /dispatch/cad-systems | GET | listCADSystems |

### EMERGENCY (58 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| DetectionCreatePage | Create | /detection/start | POST | startDetection |
| DetectionCreatePage | Create | /detection/{sessionid}/stop | POST | stopDetection |
| DetectionCreatePage | Create | /detection/trigger | POST | processTrigger |
| DetectionDetailPage | Detail | /detection/{sessionid}/status | GET | getDetectionStatus |
| DispatchCreatePage | Create | /dispatch/alert | POST | createEmergencyDispatch |
| DispatchCreatePage | Create | /dispatch/{dispatchid}/expand | POST | expandDispatchRadius |
| DispatchDetailPage | Detail | /dispatch/{dispatchid}/status | GET | getDispatchStatus |
| GuidanceCreatePage | Create | /guidance/select | POST | selectGuidance |
| GuidanceCreatePage | Create | /guidance/incidents/{incidentid}/reselect | POST | reselectGuidance |
| GuidanceDetailPage | Detail | /guidance/playbooks/{playbookid} | GET | getPlaybook |
| GuidanceDetailPage | Detail | /guidance/incidents/{incidentid} | GET | getIncidentGuidance |
| IncidentsCreatePage | Create | /incidents/{incidentid}/respond | POST | respondToIncident |
| IncidentsCreatePage | Create | /incidents/{incidentid}/checkin | POST | responderCheckin |
| IncidentsCreatePage | Create | /incidents/{incidentid}/user-confirm | POST | userConfirmContact |
| IncidentsCreatePage | Create | /incidents/{incidentid}/safe | POST | markIncidentSafe |
| IncidentsCreatePage | Create | /incidents | POST | reportIncident |
| IncidentsCreatePage | Create | /incidents/{incidentid}/accept | POST | acceptDispatch |
| IncidentsCreatePage | Create | /incidents/{incidentid}/decline | POST | declineDispatch |
| IncidentsCreatePage | Create | /incidents/{incidentid}/on-scene | POST | markOnScene |
| IncidentsCreatePage | Create | /incidents/{incidentid}/resolved | POST | markResolved |
| IncidentsCreatePage | Create | /incidents/{incidentid}/role-transition | POST | transitionRole |
| IncidentsCreatePage | Create | /incidents/{incidentid}/disagreement | POST | flagDisagreement |
| IncidentsCreatePage | Create | /incidents/{incidentid}/responder-distress | POST | triggerResponderDistress |
| IncidentsDeletePage | Delete | /incidents/{incidentid}/summoner-photo | DELETE | deleteSummonerPhoto |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/timing | GET | getIncidentTiming |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/summoner-photo | GET | getSummonerPhoto |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/responder-roles | GET | getResponderRoles |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/disagreement | GET | getDisagreements |
| IncidentsEditPage | Edit | /incidents/{incidentid} | PATCH | updateIncidentStatus |
| IncidentsListPage | List | /incidents | GET | getIncidents |
| LostFoundDetailPage | Detail | /lost-found/reports/{reportid}/matches | GET | getReportMatches |
| LostPetsCreatePage | Create | /lost-pets/reports/{reportid}/sightings | POST | reportPetSighting |
| PoisoningCreatePage | Create | /poisoning/claims | POST | createPoisoningClaim |
| PoisoningCreatePage | Create | /poisoning/claims/{claimid}/evidence | POST | addClaimEvidence |
| PoisoningDetailPage | Detail | /poisoning/claims/{claimid} | GET | getPoisoningClaim |
| PoisoningDetailPage | Detail | /poisoning/claims/{claimid}/guidance | GET | getClaimGuidance |
| PoisoningEditPage | Edit | /poisoning/claims/{claimid} | PATCH | updatePoisoningClaim |
| PoisoningListPage | List | /poisoning/claims | GET | listPoisoningClaims |
| RescueLaunchersDetailPage | Detail | /rescue-launchers/{launcherid}/events | GET | listLauncherEvents |
| RescueLaunchersDetailPage | Detail | /rescue-launchers/{launcherid}/inventory | GET | getLauncherInventory |
| RescueLaunchersListPage | List | /rescue-launchers | GET | listLaunchers |
| RespondersCreatePage | Create | /responders/register | POST | registerResponder |
| RespondersEditPage | Edit | /responders/availability | PUT | updateResponderAvailability |
| ResponseTypesListPage | List | /response-types | GET | getResponseTypes |
| SuggestedPhrasesListPage | List | /suggested-phrases | GET | getSuggestedPhrases |
| SyncCreatePage | Create | /sync/incident-actions | POST | syncOfflineActions |
| UsersCreatePage | Create | /users/{userid}/trigger-phrases | POST | createTriggerPhrase |
| UsersCreatePage | Create | /users/{userid}/trigger-phrases/{phraseid}/activate | POST | activateTriggerPhrase |
| UsersCreatePage | Create | /users/{userid}/trigger-phrases/{phraseid}/deactivate | POST | deactivateTriggerPhrase |
| UsersCreatePage | Create | /users/me/triggers | POST | createTrigger |
| UsersDeletePage | Delete | /users/{userid}/trigger-phrases/{phraseid} | DELETE | deleteTriggerPhrase |
| UsersDeletePage | Delete | /users/me/triggers/{triggerid} | DELETE | deleteTrigger |
| UsersDeletePage | Delete | /users/me/duress-pin | DELETE | removeDuressPin |
| UsersDetailPage | Detail | /users/{userid}/trigger-phrases | GET | getUserTriggerPhrases |
| UsersDetailPage | Detail | /users/{userid}/trigger-phrases/{phraseid} | GET | getTriggerPhrase |
| UsersEditPage | Edit | /users/{userid}/trigger-phrases/{phraseid} | PUT | updateTriggerPhrase |
| UsersEditPage | Edit | /users/me/safe-pin | PUT | setSafePin |
| UsersListPage | List | /users/me/triggers | GET | listTriggers |

### EVIDENCE (27 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| AdminListPage | List | /admin/analytics/responder-patterns | GET | getResponderPatterns |
| AdminListPage | List | /admin/analytics/summoner-patterns | GET | getSummonerPatterns |
| HistoryListPage | List | /history/search | GET | searchHistory |
| HistoryListPage | List | /history/export | GET | exportHistory |
| IncidentsCreatePage | Create | /incidents/{incidentid}/evidence | POST | uploadEvidence |
| IncidentsCreatePage | Create | /incidents/{incidentid}/evidence/{evidenceid}/tags | POST | addEvidenceTags |
| IncidentsCreatePage | Create | /incidents/{incidentid}/evidence/{evidenceid}/transfer | POST | transferEvidence |
| IncidentsCreatePage | Create | /incidents/{incidentid}/evidence/export | POST | exportEvidence |
| IncidentsCreatePage | Create | /incidents/{incidentid}/evidence/{evidenceid}/hold | POST | placeOnHold |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/evidence/{evidenceid} | GET | getEvidenceDetails |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/evidence/{evidenceid}/integrity | GET | verifyEvidenceIntegrity |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/evidence/{evidenceid}/chain-of-custody | GET | getChainOfCustody |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/evidence/{evidenceid}/access-log | GET | getAccessLog |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/evidence-sets | GET | getEvidenceSets |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/evidence/{evidenceid}/retention | GET | getRetention |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/evidence-summary | GET | getEvidenceSummary |
| IncidentsDetailPage | Detail | /incidents/{incidentid}/timeline | GET | getIncidentTimeline |
| IncidentsEditPage | Edit | /incidents/{incidentid}/evidence/{evidenceid}/metadata | PATCH | updateEvidenceMetadata |
| ResponderDetailPage | Detail | /responder/incidents/{incidentid} | GET | getResponderIncidentView |
| ResponderListPage | List | /responder/incidents | GET | getResponderIncidents |
| ResponderListPage | List | /responder/designated | GET | getDesignatedHistory |
| StreamsCreatePage | Create | /streams/{streamid}/pause | POST | pauseStream |
| StreamsCreatePage | Create | /streams/{streamid}/resume | POST | resumeStream |
| StreamsDetailPage | Detail | /streams/{streamid}/recording | GET | getRecordingStatus |
| SummonerDetailPage | Detail | /summoner/incidents/{incidentid} | GET | getSummonerIncidentView |
| SummonerListPage | List | /summoner/incidents | GET | getSummonerIncidents |
| SummonerListPage | List | /summoner/feedback-history | GET | getSummonerFeedback |

### INFRASTRUCTURE (53 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| CloudflareCreatePage | Create | /cloudflare/zones | POST | createZone |
| CloudflareCreatePage | Create | /cloudflare/dns | POST | createDNSRecord |
| CloudflareCreatePage | Create | /cloudflare/firewall | POST | createFirewallRule |
| CloudflareCreatePage | Create | /cloudflare/workers | POST | deployWorker |
| CloudflareCreatePage | Create | /cloudflare/ssl | POST | createSSLCertificate |
| CloudflareDeletePage | Delete | /cloudflare/zones/{zoneid} | DELETE | deleteZone |
| CloudflareDeletePage | Delete | /cloudflare/dns/{recordid} | DELETE | deleteDNSRecord |
| CloudflareDetailPage | Detail | /cloudflare/zones/{zoneid} | GET | getZone |
| CloudflareEditPage | Edit | /cloudflare/zones/{zoneid} | PATCH | updateZone |
| CloudflareEditPage | Edit | /cloudflare/dns/{recordid} | PATCH | updateDNSRecord |
| CloudflareListPage | List | /cloudflare/zones | GET | listZones |
| CloudflareListPage | List | /cloudflare/dns | GET | listDNSRecords |
| CloudflareListPage | List | /cloudflare/firewall | GET | listFirewallRules |
| CloudflareListPage | List | /cloudflare/workers | GET | listWorkers |
| CloudflareListPage | List | /cloudflare/ssl | GET | listSSLCertificates |
| DeploymentsCreatePage | Create | /deployments | POST | createDeployment |
| DeploymentsCreatePage | Create | /deployments/{deploymentid}/rollback | POST | rollbackDeployment |
| DeploymentsListPage | List | /deployments | GET | listDeployments |
| EnvironmentsDetailPage | Detail | /environments/{env}/config | GET | getEnvironmentConfig |
| EnvironmentsEditPage | Edit | /environments/{env}/config | PATCH | updateEnvironmentConfig |
| EnvironmentsListPage | List | /environments | GET | listEnvironments |
| FeatureFlagsCreatePage | Create | /feature-flags | POST | upsertFeatureFlag |
| FeatureFlagsCreatePage | Create | /feature-flags/{flagkey}/toggle | POST | toggleFeatureFlag |
| FeatureFlagsListPage | List | /feature-flags | GET | listFeatureFlags |
| GatewayCreatePage | Create | /gateway/token/introspect | POST | introspectToken |
| GatewayListPage | List | /gateway/health | GET | getGatewayHealth |
| GatewayListPage | List | /gateway/ready | GET | getGatewayReadiness |
| GatewayListPage | List | /gateway/rate-limits/me | GET | getMyRateLimits |
| HealthListPage | List | /health/services | GET | getServiceHealth |
| PipelinesCreatePage | Create | /pipelines/{pipelineid}/runs | POST | triggerPipelineRun |
| PipelinesDetailPage | Detail | /pipelines/{pipelineid}/runs/{runid} | GET | getPipelineRun |
| PipelinesListPage | List | /pipelines | GET | listPipelines |
| ReposCreatePage | Create | /repos/{owner}/{repo}/pulls | POST | createPullRequest |
| ReposCreatePage | Create | /repos/{owner}/{repo}/pulls/{pull_number}/merge | POST | mergePullRequest |
| ReposCreatePage | Create | /repos/{owner}/{repo}/pulls/{pull_number}/reviews | POST | createPullRequestReview |
| ReposCreatePage | Create | /repos/{owner}/{repo}/tags | POST | createTag |
| ReposCreatePage | Create | /repos/{owner}/{repo}/webhooks | POST | createRepoWebhook |
| ReposDeletePage | Delete | /repos/{owner}/{repo}/webhooks/{webhook_id} | DELETE | deleteRepoWebhook |
| ReposDetailPage | Detail | /repos/{owner}/{repo} | GET | getRepository |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/branches | GET | listBranches |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/branches/{branch} | GET | getBranch |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/branches/{branch}/protection | GET | getBranchProtection |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/commits | GET | listCommits |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/commits/{sha} | GET | getCommit |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/compare/{base}...{head} | GET | compareCommits |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/pulls | GET | listPullRequests |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/pulls/{pull_number} | GET | getPullRequest |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/pulls/{pull_number}/reviews | GET | listPullRequestReviews |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/tags | GET | listTags |
| ReposDetailPage | Detail | /repos/{owner}/{repo}/webhooks | GET | listRepoWebhooks |
| ReposEditPage | Edit | /repos/{owner}/{repo}/pulls/{pull_number} | PATCH | updatePullRequest |
| ReposListPage | List | /repos | GET | listRepositories |
| SecretsDetailPage | Detail | /secrets/{scope} | GET | listSecrets |

### LEGAL (6 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| LegalCreatePage | Create | /legal/insurance/authorize | POST | authorizeInsuranceRelease |
| LegalCreatePage | Create | /legal/discovery/requests | POST | submitDiscoveryRequest |
| LegalCreatePage | Create | /legal/subpoenas | POST | registerSubpoena |
| LegalDetailPage | Detail | /legal/insurance/claims/{claimid}/data | GET | getClaimData |
| LegalDetailPage | Detail | /legal/discovery/packages/{requestid}/download | GET | downloadDiscoveryPackage |
| LegalDetailPage | Detail | /legal/subpoenas/{subpoenaid}/compliance | GET | getSubpoenaCompliance |

### LINT (4 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| HealthListPage | List | /health | GET | getHealth |
| ValidateCreatePage | Create | /validate | POST | validateSpec |
| ValidateCreatePage | Create | /validate/batch | POST | validateBatch |
| ValidateCreatePage | Create | /validate/domain/{domain} | POST | validateDomain |

### LOCATION (45 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| DirectionsCreatePage | Create | /directions/routes | POST | calculateRoute |
| DirectionsCreatePage | Create | /directions/routes/matrix | POST | calculateRouteMatrix |
| DirectionsCreatePage | Create | /directions/routes/optimize | POST | optimizeRoute |
| DirectionsCreatePage | Create | /directions/navigation | POST | startNavigation |
| DirectionsCreatePage | Create | /directions/isochrones | POST | calculateIsochrone |
| DirectionsDeletePage | Delete | /directions/navigation/{sessionid} | DELETE | endNavigation |
| DirectionsDetailPage | Detail | /directions/navigation/{sessionid} | GET | getNavigationSession |
| DirectionsListPage | List | /directions/traffic/road-closures | GET | listRoadClosures |
| GeofencesCreatePage | Create | /geofences | POST | createGeofence |
| GeofencesCreatePage | Create | /geofences/{geofenceid}/check | POST | checkPointInGeofence |
| GeofencesCreatePage | Create | /geofences | POST | createGeofence |
| GeofencesDeletePage | Delete | /geofences/{geofenceid} | DELETE | deleteGeofence |
| GeofencesDeletePage | Delete | /geofences/{geofenceid} | DELETE | deleteGeofence |
| GeofencesDetailPage | Detail | /geofences/{geofenceid} | GET | getGeofence |
| GeofencesEditPage | Edit | /geofences/{geofenceid} | PATCH | updateGeofence |
| GeofencesEditPage | Edit | /geofences/{geofenceid} | PUT | updateGeofence |
| GeofencesListPage | List | /geofences | GET | listGeofences |
| GeofencesListPage | List | /geofences/active | GET | getActiveGeofencesForLocation |
| GeofencesListPage | List | /geofences | GET | listGeofences |
| MapsCreatePage | Create | /maps/annotations | POST | createAnnotation |
| MapsDeletePage | Delete | /maps/annotations/{annotationid} | DELETE | deleteAnnotation |
| MapsDetailPage | Detail | /maps/tiles/{style}/{z}/{x}/{y} | GET | getMapTile |
| MapsDetailPage | Detail | /maps/annotations/{annotationid} | GET | getAnnotation |
| MapsDetailPage | Detail | /maps/styles/{styleid} | GET | getMapStyle |
| MapsDetailPage | Detail | /maps/layers/{layerid}/data | GET | getLayerData |
| MapsEditPage | Edit | /maps/annotations/{annotationid} | PUT | updateAnnotation |
| MapsListPage | List | /maps/annotations | GET | listAnnotations |
| MapsListPage | List | /maps/styles | GET | listMapStyles |
| MapsListPage | List | /maps/layers | GET | listMapLayers |
| MapsListPage | List | /maps/snapshots | GET | listMapSnapshots |
| PlacesCreatePage | Create | /places/resolve | POST | resolvePlace |
| PlacesDetailPage | Detail | /places/{placeid} | GET | getPlace |
| PlacesDetailPage | Detail | /places/geohash/{geohashprefix} | GET | getPlacesByGeohash |
| PlacesListPage | List | /places | GET | searchPlaces |
| PlacesListPage | List | /places | GET | listPlaces |
| PlacesListPage | List | /places/nearby | GET | findNearbyPlaces |
| PlacesListPage | List | /places/types | GET | getPlaceTypes |
| QueriesCreatePage | Create | /queries/distance | POST | calculateDistance |
| QueriesCreatePage | Create | /queries/point-in-geofence | POST | checkPointInGeofence |
| QueriesCreatePage | Create | /queries/bounding-box | POST | queryBoundingBox |
| SpatialListPage | List | /spatial/nearby-users | GET | findNearbyUsers |
| SpatialListPage | List | /spatial/geohash | GET | getGeohash |
| UsersDetailPage | Detail | /users/{userid}/location | GET | getUserLocation |
| UsersDetailPage | Detail | /users/{userid}/location/history | GET | getLocationHistory |
| UsersEditPage | Edit | /users/{userid}/location | PUT | updateUserLocation |

### LOGISTICS (15 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| LogisticsCreatePage | Create | /logistics/telematics/vehicles/{vehicleid}/stream | POST | streamVehicleTelemetry |
| LogisticsCreatePage | Create | /logistics/av/override/command | POST | sendAVEmergencyCommand |
| LogisticsCreatePage | Create | /logistics/relief/shipments | POST | createReliefShipment |
| LogisticsCreatePage | Create | /logistics/fleet/vehicles/{vehicleid}/service | POST | scheduleMaintenance |
| LogisticsCreatePage | Create | /logistics/traffic/preemption/request | POST | requestSignalPriority |
| LogisticsCreatePage | Create | /logistics/airspace/reservations | POST | reserveAirspace |
| LogisticsCreatePage | Create | /logistics/airspace/tracking/stream | POST | streamAirspaceTracking |
| LogisticsCreatePage | Create | /logistics/transit/reroute | POST | requestTransitReroute |
| LogisticsDetailPage | Detail | /logistics/traffic/intersections/{intersectionid}/status | GET | getIntersectionStatus |
| LogisticsListPage | List | /logistics/roads/obstructions | GET | listRoadObstructions |
| LogisticsListPage | List | /logistics/telematics/crashes | GET | getCrashEventData |
| LogisticsListPage | List | /logistics/av/fleet/status | GET | getNearbyAVStatus |
| LogisticsListPage | List | /logistics/relief/inventory | GET | listReliefInventory |
| LogisticsListPage | List | /logistics/fleet/readiness | GET | getFleetReadiness |
| LogisticsListPage | List | /logistics/transit/status | GET | getTransitStatus |

### MEDICAL (13 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| MedicalCreatePage | Create | /medical/pharmacy/prescriptions | POST | issueEmergencyPrescription |
| MedicalCreatePage | Create | /medical/telemedicine/sessions | POST | createTelemedicineSession |
| MedicalCreatePage | Create | /medical/telemedicine/sessions/{sessionid}/diagnostics | POST | uploadDiagnosticData |
| MedicalCreatePage | Create | /medical/triage/tags | POST | createTriageTag |
| MedicalCreatePage | Create | /medical/mental-health/consultations | POST | requestMentalHealthConsultation |
| MedicalCreatePage | Create | /medical/exposure/reports | POST | reportPathogenExposure |
| MedicalCreatePage | Create | /medical/blood/alerts | POST | triggerBloodAlert |
| MedicalDetailPage | Detail | /medical/triage/incidents/{incidentid}/summary | GET | getIncidentTriageSummary |
| MedicalDetailPage | Detail | /medical/records/{userid} | GET | getEmergencyMedicalRecord |
| MedicalListPage | List | /medical/pharmacy/inventory | GET | searchMedicationInventory |
| MedicalListPage | List | /medical/vitals/alerts/config | GET | getVitalAlertConfig |
| MedicalListPage | List | /medical/exposure/outbreaks | GET | listActiveOutbreaks |
| MedicalListPage | List | /medical/blood/inventory | GET | getBloodInventory |

### MESSAGING (15 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| CatalogDetailPage | Detail | /catalog/event-types/{eventtype} | GET | getEventType |
| CatalogDetailPage | Detail | /catalog/event-types/{eventtype}/versions | GET | listEventTypeVersions |
| CatalogDetailPage | Detail | /catalog/domains/{domain}/integration-map | GET | getDomainIntegrationMap |
| CatalogListPage | List | /catalog/event-types | GET | listEventTypes |
| CatalogListPage | List | /catalog/domains | GET | listDomainEventSummaries |
| EventsCreatePage | Create | /events | POST | publishEvents |
| EventsDetailPage | Detail | /events/{eventid} | GET | getEvent |
| EventsDetailPage | Detail | /events/{eventid}/deliveries | GET | getEventDeliveries |
| HealthListPage | List | /health | GET | getEventBusHealth |
| SubscriptionsCreatePage | Create | /subscriptions | POST | createSubscription |
| TopicsListPage | List | /topics | GET | listTopics |
| WebhooksCreatePage | Create | /webhooks/{webhookid}/verify | POST | verifyWebhook |
| WebhooksCreatePage | Create | /webhooks/{webhookid}/rotate-secret | POST | rotateWebhookSecret |
| WebhooksCreatePage | Create | /webhooks/{webhookid}/deliveries/{deliveryid}/retry | POST | retryWebhookDelivery |
| WebhooksDetailPage | Detail | /webhooks/{webhookid}/deliveries/{deliveryid} | GET | getWebhookDelivery |

### NOTIFICATIONS (15 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| CommunityCreatePage | Create | /community/alerts | POST | createCommunityAlert |
| CommunityCreatePage | Create | /community/alerts/{alertid}/confirm | POST | confirmCommunityAlert |
| CommunityDetailPage | Detail | /community/alerts/{alertid} | GET | getCommunityAlert |
| CommunityListPage | List | /community/alerts | GET | listCommunityAlerts |
| CommunityListPage | List | /community/alerts/nearby | GET | getNearbyCommunityAlerts |
| DevicesCreatePage | Create | /devices/register | POST | registerDevice |
| DevicesDeletePage | Delete | /devices/{deviceid} | DELETE | unregisterDevice |
| NegotiateCreatePage | Create | /negotiate | POST | negotiateConnection |
| NotificationsCreatePage | Create | /notifications/send | POST | sendNotification |
| SmsCreatePage | Create | /sms/send | POST | sendSms |
| StreamingCreatePage | Create | /streaming/location/{sessionid} | POST | startLocationStreaming |
| StreamingCreatePage | Create | /streaming/location/{sessionid}/update | POST | pushLocationUpdate |
| StreamingDeletePage | Delete | /streaming/location/{sessionid} | DELETE | stopLocationStreaming |
| SubscriptionsCreatePage | Create | /subscriptions/evacuations/{evacuationid} | POST | subscribeToEvacuation |
| SubscriptionsDeletePage | Delete | /subscriptions/incidents/{incidentid} | DELETE | unsubscribeFromIncident |

### PLATFORM (34 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| ApiCreatePage | Create | /agentchat/supervisors | POST | createSupervisor |
| ApiCreatePage | Create | /agentchat/supervisors/{supervisorid}/agents | POST | assignAgentToFile |
| ApiCreatePage | Create | /agentchat/supervisors/{supervisorid}/query | POST | querySupervisor |
| ApiCreatePage | Create | /agentchat/query | POST | globalQuery |
| ApiCreatePage | Create | /googlesearch/search | POST | searchPost |
| ApiListPage | List | /agentchat/supervisors | GET | getSupervisors |
| ApiListPage | List | /googlesearch/search | GET | searchGet |
| ApiListPage | List | /googlesearch/quick | GET | quickSearch |
| ApiListPage | List | /googlesearch/suggestions | GET | getSuggestions |
| CartCreatePage | Create | /cart | POST | createCart |
| CartCreatePage | Create | /cart/items | POST | addCartItem |
| CartCreatePage | Create | /cart/discounts | POST | applyDiscountCode |
| CartCreatePage | Create | /cart/calculate | POST | calculateCartTotals |
| CartDeletePage | Delete | /cart | DELETE | clearCart |
| CartDeletePage | Delete | /cart/items/{itemid} | DELETE | removeCartItem |
| CartDeletePage | Delete | /cart/discounts/{discountid} | DELETE | removeDiscountCode |
| CartEditPage | Edit | /cart/items/{itemid} | PUT | updateCartItem |
| CartListPage | List | /cart | GET | getCart |
| CheckoutCreatePage | Create | /checkout | POST | proceedToCheckout |
| DeviceStorageCreatePage | Create | /device-storage/vaults | POST | createVault |
| DeviceStorageCreatePage | Create | /device-storage/vaults/{vaultid}/items | POST | createVaultItem |
| DeviceStorageCreatePage | Create | /device-storage/keychain | POST | createKeychainEntry |
| DeviceStorageDeletePage | Delete | /device-storage/vaults/{vaultid} | DELETE | deleteVault |
| DeviceStorageDeletePage | Delete | /device-storage/vaults/{vaultid}/items/{itemid} | DELETE | deleteVaultItem |
| DeviceStorageDeletePage | Delete | /device-storage/keychain/{entryid} | DELETE | deleteKeychainEntry |
| DeviceStorageDetailPage | Detail | /device-storage/vaults/{vaultid} | GET | getVault |
| DeviceStorageDetailPage | Detail | /device-storage/vaults/{vaultid}/items | GET | listVaultItems |
| DeviceStorageDetailPage | Detail | /device-storage/vaults/{vaultid}/items/{itemid} | GET | getVaultItem |
| DeviceStorageDetailPage | Detail | /device-storage/devices/{deviceid}/status | GET | getDeviceStorageStatus |
| DeviceStorageListPage | List | /device-storage/vaults | GET | listVaults |
| DeviceStorageListPage | List | /device-storage/keychain | GET | listKeychainEntries |
| DeviceStorageListPage | List | /device-storage/policies | GET | getStoragePolicies |
| PreferencesCreatePage | Create | /preferences | POST | setCookiePreferences |
| PreferencesListPage | List | /preferences | GET | getCookiePreferences |

### _TESTING (8 pages)

| Page | Type | Route | Method | Operation |
|------|------|-------|--------|------------|
| ApiCreatePage | Create | /supervisors | POST | createSupervisor |
| ApiCreatePage | Create | /supervisors/{supervisorid}/agents | POST | assignAgentToFile |
| ApiCreatePage | Create | /supervisors/{supervisorid}/query | POST | querySupervisor |
| ApiCreatePage | Create | /agentchat/global-query | POST | globalQuery |
| ApiCreatePage | Create | /agentchat/analyze-solution | POST | analyzeSolution |
| ApiCreatePage | Create | /agentchat/query | POST | individualQuery |
| ApiListPage | List | /health | GET | getHealth |
| TestListPage | List | /test | GET | test |

## Summary by Page Type

- **Create**: 208 pages
- **Delete**: 28 pages
- **Detail**: 120 pages
- **Edit**: 26 pages
- **List**: 118 pages
