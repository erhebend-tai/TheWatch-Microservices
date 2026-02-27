# TheWatch — Government Data Sources: Crimes, Injuries, and Related Incidents

> **Classification:** UNCLASSIFIED  
> **Purpose:** Reference catalog of authoritative U.S. government open-data sources
> covering crimes, injuries, disasters, and related events that inform TheWatch's
> emergency-response, incident-management, and health-monitoring capabilities.  
> **Effective:** February 2026  
> **Maintained by:** TheWatch Program Office

---

## Overview

TheWatch integrates, cross-references, and responds to events across the crime,
injury, and public-safety spectrum. The datasets below are the primary authoritative
government sources for that spectrum. Each entry provides:

- **Agency / Program** — publishing authority  
- **Dataset / Publication** — official title  
- **URL** — canonical public access point  
- **Data Updated** — publication cadence  
- **Coverage** — geographic / temporal scope  
- **Relevant TheWatch Services** — which microservices consume or contextualise this data  
- **CUI Category** — data-handling classification when ingested (per `data-classification-matrix.md`)

---

## 1. Crime Statistics

### 1.1 FBI — Uniform Crime Reporting (UCR) Program

| Field | Detail |
|-------|--------|
| **Agency** | Federal Bureau of Investigation (FBI) |
| **Dataset** | Uniform Crime Reporting (UCR) Program |
| **URL** | <https://ucr.fbi.gov/> |
| **Data Updated** | Annual (calendar-year release, typically Q4 following year) |
| **Coverage** | National; all 50 states + D.C. + territories; 18,000+ law-enforcement agencies |
| **Relevant Services** | P2 VoiceEmergency, P6 FirstResponder, P11 Surveillance |
| **CUI Category** | CUI//SP-LEI |

**Description:** The UCR Program collects and publishes crime data voluntarily
submitted by law-enforcement agencies. Publications include *Crime in the United
States* (Part I Index crimes: violent & property), *Law Enforcement Officers Killed
and Assaulted (LEOKA)*, *Hate Crime Statistics*, and *Cargo Theft*.  
**Key tables used by TheWatch:** violent-crime rates by jurisdiction (incident
prioritisation), LEOKA data (responder-safety context), hate-crime category codes
(incident classification taxonomy).

**Citation:**
> Federal Bureau of Investigation. (Annual). *Uniform Crime Reporting Program.*
> U.S. Department of Justice. <https://ucr.fbi.gov/>

---

### 1.2 FBI — National Incident-Based Reporting System (NIBRS)

| Field | Detail |
|-------|--------|
| **Agency** | Federal Bureau of Investigation (FBI) |
| **Dataset** | National Incident-Based Reporting System (NIBRS) |
| **URL** | <https://www.fbi.gov/services/cjis/ucr/nibrs> |
| **Data Updated** | Annual; bulk downloads at <https://crime-data-explorer.app.cloud.gov/> |
| **Coverage** | National; 10,000+ NIBRS-certified agencies (growing annually toward full transition) |
| **Relevant Services** | P2 VoiceEmergency, P6 FirstResponder, P11 Surveillance |
| **CUI Category** | CUI//SP-LEI |

**Description:** NIBRS replaces legacy UCR Summary Reporting. It captures up to
57 offence types with full incident, victim, offender, and arrestee attributes.
TheWatch incident-type taxonomy (`IncidentType` enum in P2) mirrors NIBRS Group A / Group B
offence codes, enabling structured triage and category-aware dispatch.

**Citation:**
> Federal Bureau of Investigation. (Annual). *National Incident-Based Reporting System
> (NIBRS).* U.S. Department of Justice Crime Data Explorer.
> <https://crime-data-explorer.app.cloud.gov/>

---

### 1.3 Bureau of Justice Statistics (BJS) — National Crime Victimization Survey (NCVS)

| Field | Detail |
|-------|--------|
| **Agency** | Bureau of Justice Statistics (BJS), Office of Justice Programs, U.S. DOJ |
| **Dataset** | National Crime Victimization Survey (NCVS) |
| **URL** | <https://bjs.ojp.gov/data-collection/ncvs> |
| **Data Updated** | Annual bulletin; microdata via ICPSR |
| **Coverage** | National; household survey (~240,000 interviews/year) |
| **Relevant Services** | P2 VoiceEmergency, P7 FamilyHealth |
| **CUI Category** | CUI//SP-LEI (aggregated; individual records SP-PRIV) |

**Description:** The NCVS is the primary source for unreported crime estimates.
It measures personal and household victimisation (rape/sexual assault, robbery,
assault, burglary, motor-vehicle theft, and theft) regardless of police reporting.
TheWatch uses NCVS prevalence data to calibrate alert thresholds and inform risk
scoring in the family-health geofencing module.

**Citation:**
> Bureau of Justice Statistics. (Annual). *National Crime Victimization Survey (NCVS).*
> Office of Justice Programs, U.S. Department of Justice.
> <https://bjs.ojp.gov/data-collection/ncvs>

---

### 1.4 Bureau of Justice Statistics (BJS) — Criminal Justice Data Collections

| Field | Detail |
|-------|--------|
| **Agency** | Bureau of Justice Statistics (BJS), U.S. DOJ |
| **Dataset** | Multiple: Law Enforcement Management and Administrative Statistics (LEMAS), Census of State and Local Law Enforcement Agencies (CSLLEA), National Prisoner Statistics (NPS), Survey of Prison Inmates (SPI) |
| **URL** | <https://bjs.ojp.gov/> |
| **Data Updated** | Varies by collection (annual to every 5 years) |
| **Coverage** | National |
| **Relevant Services** | P6 FirstResponder, P1 CoreGateway |
| **CUI Category** | CUI//SP-LEI |

**Citation:**
> Bureau of Justice Statistics. (Multiple years). *Statistical collections.*
> Office of Justice Programs, U.S. Department of Justice.
> <https://bjs.ojp.gov/>

---

### 1.5 ATF — National Firearms Commerce and Trafficking Assessment (NFCTA)

| Field | Detail |
|-------|--------|
| **Agency** | Bureau of Alcohol, Tobacco, Firearms and Explosives (ATF) |
| **Dataset** | National Firearms Commerce and Trafficking Assessment; Crime Gun Intelligence |
| **URL** | <https://www.atf.gov/resource-center/data-statistics> |
| **Data Updated** | Annual / periodic |
| **Coverage** | National; firearms traces, trafficking patterns, crime-gun recovery data |
| **Relevant Services** | P2 VoiceEmergency, P11 Surveillance |
| **CUI Category** | CUI//SP-LEI |

**Citation:**
> Bureau of Alcohol, Tobacco, Firearms and Explosives. (Annual). *ATF Data & Statistics.*
> U.S. Department of Justice.
> <https://www.atf.gov/resource-center/data-statistics>

---

### 1.6 DHS / CISA — Homicide / Active Shooter Threat Assessments

| Field | Detail |
|-------|--------|
| **Agency** | Department of Homeland Security (DHS); Cybersecurity and Infrastructure Security Agency (CISA) |
| **Dataset** | K–12 School Security Guide; Active Shooter Incidents Annual Report; Targeted Violence Prevention Plan |
| **URL** | <https://www.cisa.gov/topics/physical-security/active-shooter-preparedness> |
| **Data Updated** | Annual |
| **Coverage** | National; location, victim counts, incident duration |
| **Relevant Services** | P2 VoiceEmergency, P11 Surveillance, P6 FirstResponder |
| **CUI Category** | CUI//SP-LEI |

**Description:** CISA maintains annual active-shooter incident statistics with venue
type, casualty counts, and response-time data. P2 VoiceEmergency active-shooter
detection thresholds are calibrated against CISA's published incident duration and
casualty-rate benchmarks.

**Citation:**
> Cybersecurity and Infrastructure Security Agency. (Annual). *Active Shooter Incidents
> in the United States* [Annual Report]. U.S. Department of Homeland Security.
> <https://www.cisa.gov/topics/physical-security/active-shooter-preparedness>

---

### 1.7 DEA — National Drug Threat Assessment (NDTA)

| Field | Detail |
|-------|--------|
| **Agency** | Drug Enforcement Administration (DEA) |
| **Dataset** | National Drug Threat Assessment (NDTA) |
| **URL** | <https://www.dea.gov/drug-information/drug-policy-and-prevention> |
| **Data Updated** | Annual |
| **Coverage** | National; drug trafficking, overdose nexus to violent crime |
| **Relevant Services** | P2 VoiceEmergency, P9 DoctorServices |
| **CUI Category** | CUI//SP-LEI |

**Citation:**
> Drug Enforcement Administration. (Annual). *National Drug Threat Assessment.*
> U.S. Department of Justice.
> <https://www.dea.gov/drug-information/drug-policy-and-prevention>

---

## 2. Injury and Mortality Data

### 2.1 CDC — WISQARS (Web-based Injury Statistics Query and Reporting System)

| Field | Detail |
|-------|--------|
| **Agency** | Centers for Disease Control and Prevention (CDC), National Center for Injury Prevention and Control (NCIPC) |
| **Dataset** | WISQARS — Fatal and Nonfatal Injury Query Tool |
| **URL** | <https://www.cdc.gov/injury/wisqars/> |
| **Data Updated** | Annual (fatal data ~2 years lag; nonfatal ED data ~1 year lag) |
| **Coverage** | National and state-level; all injury mechanisms and intents |
| **Relevant Services** | P2 VoiceEmergency, P4 Wearable, P7 FamilyHealth, P9 DoctorServices |
| **CUI Category** | CUI//SP-HLTH (individual case data); public aggregates are UNCLASSIFIED |

**Description:** WISQARS is the definitive query tool for U.S. injury morbidity and
mortality. It covers fatal injuries (all causes and mechanisms), nonfatal
emergency-department–treated injuries, and violent death (NVDRS). TheWatch uses WISQARS
mechanism-of-injury breakdowns to populate the `InjuryType` classification hierarchy
and to set vital-alert severity thresholds in P4 Wearable.

**Citation:**
> Centers for Disease Control and Prevention, National Center for Injury Prevention
> and Control. (Annual). *Web-based Injury Statistics Query and Reporting System
> (WISQARS).* U.S. Department of Health and Human Services.
> <https://www.cdc.gov/injury/wisqars/>

---

### 2.2 CDC — National Violent Death Reporting System (NVDRS)

| Field | Detail |
|-------|--------|
| **Agency** | CDC / NCIPC |
| **Dataset** | National Violent Death Reporting System (NVDRS) |
| **URL** | <https://www.cdc.gov/violenceprevention/datasources/nvdrs/index.html> |
| **Data Updated** | Annual |
| **Coverage** | All 50 states + D.C.; homicide, suicide, legal intervention, unintentional firearm |
| **Relevant Services** | P2 VoiceEmergency, P7 FamilyHealth |
| **CUI Category** | CUI//SP-HLTH + CUI//SP-LEI |

**Citation:**
> Centers for Disease Control and Prevention. (Annual). *National Violent Death
> Reporting System (NVDRS).* National Center for Injury Prevention and Control.
> <https://www.cdc.gov/violenceprevention/datasources/nvdrs/index.html>

---

### 2.3 CDC — National Vital Statistics System (NVSS)

| Field | Detail |
|-------|--------|
| **Agency** | CDC / National Center for Health Statistics (NCHS) |
| **Dataset** | National Vital Statistics System — Mortality and Morbidity |
| **URL** | <https://www.cdc.gov/nchs/nvss/index.htm> |
| **Data Updated** | Annual mortality data; provisional data monthly |
| **Coverage** | National; all cause-of-death ICD-10 coded death certificates |
| **Relevant Services** | P7 FamilyHealth, P9 DoctorServices |
| **CUI Category** | CUI//SP-HLTH |

**Citation:**
> Centers for Disease Control and Prevention, National Center for Health Statistics.
> (Annual). *National Vital Statistics System (NVSS).*
> U.S. Department of Health and Human Services.
> <https://www.cdc.gov/nchs/nvss/index.htm>

---

### 2.4 NHTSA — Fatality Analysis Reporting System (FARS)

| Field | Detail |
|-------|--------|
| **Agency** | National Highway Traffic Safety Administration (NHTSA), U.S. DOT |
| **Dataset** | Fatality Analysis Reporting System (FARS) |
| **URL** | <https://www.nhtsa.gov/research-data/fatality-analysis-reporting-system-fars> |
| **Data Updated** | Annual (final); provisional within ~6 months |
| **Coverage** | National; all fatal motor-vehicle crashes on public roads |
| **Relevant Services** | P2 VoiceEmergency, P8 DisasterRelief, Geospatial |
| **CUI Category** | UNCLASSIFIED (aggregated); individual crash records CUI//SP-PRIV |

**Description:** FARS contains data on every fatal crash: location (lat/lon),
crash type, roadway conditions, vehicle and occupant attributes, and EMS response
times. TheWatch Geospatial service uses FARS crash-location data to seed high-risk
road segments for proximity-alert and evacuation-routing logic.

**Citation:**
> National Highway Traffic Safety Administration. (Annual). *Fatality Analysis
> Reporting System (FARS).* U.S. Department of Transportation.
> <https://www.nhtsa.gov/research-data/fatality-analysis-reporting-system-fars>

---

### 2.5 BLS — Census of Fatal Occupational Injuries (CFOI)

| Field | Detail |
|-------|--------|
| **Agency** | Bureau of Labor Statistics (BLS), U.S. DOL |
| **Dataset** | Census of Fatal Occupational Injuries (CFOI); Survey of Occupational Injuries and Illnesses (SOII) |
| **URL** | <https://www.bls.gov/iif/> |
| **Data Updated** | Annual (preliminary April; final December) |
| **Coverage** | National; all fatal work injuries; nonfatal injury/illness rates by industry |
| **Relevant Services** | P6 FirstResponder, P4 Wearable |
| **CUI Category** | UNCLASSIFIED |

**Citation:**
> Bureau of Labor Statistics. (Annual). *Injuries, Illnesses, and Fatalities (IIF)
> Program.* U.S. Department of Labor.
> <https://www.bls.gov/iif/>

---

### 2.6 CPSC — National Electronic Injury Surveillance System (NEISS)

| Field | Detail |
|-------|--------|
| **Agency** | Consumer Product Safety Commission (CPSC) |
| **Dataset** | National Electronic Injury Surveillance System (NEISS) |
| **URL** | <https://www.cpsc.gov/Research--Statistics/NEISS-Injury-Data> |
| **Data Updated** | Annual; query tool updated continuously |
| **Coverage** | National probability sample of ~100 hospital EDs; product-related injuries |
| **Relevant Services** | P7 FamilyHealth, P9 DoctorServices |
| **CUI Category** | UNCLASSIFIED (aggregated) |

**Citation:**
> Consumer Product Safety Commission. (Annual). *National Electronic Injury
> Surveillance System (NEISS).* U.S. Consumer Product Safety Commission.
> <https://www.cpsc.gov/Research--Statistics/NEISS-Injury-Data>

---

### 2.7 VA / DoD — Military Casualty & Veteran Injury Data

| Field | Detail |
|-------|--------|
| **Agency** | Department of Veterans Affairs (VA); Defense Casualty Analysis System (DCAS), Defense Manpower Data Center (DMDC) |
| **Dataset** | VA National Veteran Suicide Prevention Annual Report; DoD Personnel & Casualty Reports |
| **URL** (VA) | <https://www.mentalhealth.va.gov/docs/data-sheets/2023/2023-National-Veteran-Suicide-Prevention-Annual-Report-FINAL-508.pdf> |
| **URL** (DMDC) | <https://www.dmdc.osd.mil/appj/dwp/dwp_reports.jsp> |
| **Data Updated** | Annual |
| **Coverage** | Veterans and active-duty service members |
| **Relevant Services** | P7 FamilyHealth, P9 DoctorServices, P6 FirstResponder |
| **CUI Category** | CUI//SP-HLTH + CUI//SP-PRIV |

**Citation:**
> Department of Veterans Affairs. (Annual). *National Veteran Suicide Prevention Annual
> Report.* Office of Mental Health and Suicide Prevention.
> <https://www.mentalhealth.va.gov/suicide_prevention/data.asp>
>
> Defense Manpower Data Center. (Annual). *Personnel & Casualty Reports.*
> U.S. Department of Defense.
> <https://www.dmdc.osd.mil/appj/dwp/dwp_reports.jsp>

---

## 3. Disaster and Emergency Data

### 3.1 FEMA — Disaster Declarations Summary

| Field | Detail |
|-------|--------|
| **Agency** | Federal Emergency Management Agency (FEMA) |
| **Dataset** | OpenFEMA Disaster Declarations Summary (API + CSV) |
| **URL** | <https://www.fema.gov/openfema-data-page/disaster-declarations-summaries-v2> |
| **Data Updated** | Real-time (OpenFEMA API) |
| **Coverage** | All federally declared disasters since 1953; county/state level |
| **Relevant Services** | P8 DisasterRelief, Geospatial, P1 CoreGateway |
| **CUI Category** | UNCLASSIFIED |

**Description:** OpenFEMA provides machine-readable disaster declaration data via a
REST API. P8 DisasterRelief polls this endpoint to auto-populate `DisasterEvent`
records and trigger resource-matching and evacuation-routing workflows.

**Citation:**
> Federal Emergency Management Agency. (Real-time). *OpenFEMA Disaster Declarations
> Summary (v2).* U.S. Department of Homeland Security.
> <https://www.fema.gov/openfema-data-page/disaster-declarations-summaries-v2>

---

### 3.2 FEMA — National Fire Incident Reporting System (NFIRS) / U.S. Fire Administration

| Field | Detail |
|-------|--------|
| **Agency** | U.S. Fire Administration (USFA), FEMA |
| **Dataset** | National Fire Incident Reporting System (NFIRS); Fire Statistics |
| **URL** | <https://www.usfa.fema.gov/data/statistics/> |
| **Data Updated** | Annual |
| **Coverage** | National; structure fires, civilian casualties, fire-fighter fatalities |
| **Relevant Services** | P2 VoiceEmergency, P8 DisasterRelief, P11 Surveillance |
| **CUI Category** | CUI//SP-LEI |

**Citation:**
> U.S. Fire Administration, Federal Emergency Management Agency. (Annual).
> *National Fire Incident Reporting System (NFIRS) Data & Statistics.*
> U.S. Department of Homeland Security.
> <https://www.usfa.fema.gov/data/statistics/>

---

### 3.3 NOAA — Storm Events Database

| Field | Detail |
|-------|--------|
| **Agency** | National Oceanic and Atmospheric Administration (NOAA), National Weather Service (NWS) |
| **Dataset** | Storm Events Database |
| **URL** | <https://www.ncdc.noaa.gov/stormevents/> |
| **Data Updated** | Monthly updates; bulk CSV download available |
| **Coverage** | National; 48 event types (tornadoes, floods, hurricanes, winter storms, etc.) with deaths, injuries, and property damage |
| **Relevant Services** | P8 DisasterRelief, Geospatial |
| **CUI Category** | UNCLASSIFIED |

**Description:** NOAA Storm Events is the definitive record of severe weather
casualties and property damage. P8 DisasterRelief references this dataset for
historical storm-track data to pre-position shelters and model evacuation corridors.

**Citation:**
> National Oceanic and Atmospheric Administration, National Weather Service. (Monthly).
> *Storm Events Database.* U.S. Department of Commerce.
> <https://www.ncdc.noaa.gov/stormevents/>

---

### 3.4 FEMA — National Flood Insurance Program (NFIP) Claims / Flood Map Service

| Field | Detail |
|-------|--------|
| **Agency** | FEMA — National Flood Insurance Program (NFIP) |
| **Dataset** | OpenFEMA NFIP Redacted Claims; FEMA Flood Map Service Center |
| **URL** | <https://www.fema.gov/openfema-data-page/fima-nfip-redacted-claims-v2> |
| **Flood Maps** | <https://msc.fema.gov/portal/home> |
| **Data Updated** | Periodic (claims data); Flood maps updated continuously |
| **Coverage** | National; flood zones, historical claims, damage extent |
| **Relevant Services** | P8 DisasterRelief, Geospatial |
| **CUI Category** | UNCLASSIFIED (redacted aggregates) |

**Citation:**
> Federal Emergency Management Agency. (Periodic). *OpenFEMA FIMA NFIP Redacted
> Claims (v2).* U.S. Department of Homeland Security.
> <https://www.fema.gov/openfema-data-page/fima-nfip-redacted-claims-v2>

---

### 3.5 USGS — Earthquake Hazards Program

| Field | Detail |
|-------|--------|
| **Agency** | U.S. Geological Survey (USGS), Department of the Interior |
| **Dataset** | Earthquake Catalog API (real-time); ShakeMaps |
| **URL** | <https://earthquake.usgs.gov/earthquakes/feed/> |
| **Data Updated** | Real-time feed (GeoJSON, Atom) |
| **Coverage** | Global; magnitude, depth, casualties for significant events |
| **Relevant Services** | P8 DisasterRelief, Geospatial |
| **CUI Category** | UNCLASSIFIED |

**Citation:**
> U.S. Geological Survey. (Real-time). *Earthquake Hazards Program — Feeds &
> Notifications.* U.S. Department of the Interior.
> <https://earthquake.usgs.gov/earthquakes/feed/>

---

## 4. Public Health and EMS Data

### 4.1 HHS AHRQ — Healthcare Cost and Utilization Project (HCUP)

| Field | Detail |
|-------|--------|
| **Agency** | Agency for Healthcare Research and Quality (AHRQ), HHS |
| **Dataset** | Healthcare Cost and Utilization Project (HCUP) — NIS, NEDS, KID |
| **URL** | <https://www.ahrq.gov/data/hcup/index.html> |
| **Data Updated** | Annual |
| **Coverage** | National; inpatient admissions, ED visits, pediatric encounters |
| **Relevant Services** | P7 FamilyHealth, P9 DoctorServices |
| **CUI Category** | CUI//SP-HLTH |

**Description:** HCUP is the largest U.S. all-payer inpatient database. The Nationwide
Emergency Department Sample (NEDS) provides ED-visit volume and diagnosis codes
(ICD-10-CM) for trauma, poisoning, and assault. TheWatch P9 DoctorServices references
HCUP ICD coding for appointment-type classification.

**Citation:**
> Agency for Healthcare Research and Quality. (Annual). *Healthcare Cost and
> Utilization Project (HCUP).* U.S. Department of Health and Human Services.
> <https://www.ahrq.gov/data/hcup/index.html>

---

### 4.2 SAMHSA — Drug Abuse Warning Network (DAWN) / NSDUH

| Field | Detail |
|-------|--------|
| **Agency** | Substance Abuse and Mental Health Services Administration (SAMHSA), HHS |
| **Dataset** | National Survey on Drug Use and Health (NSDUH); Drug Abuse Warning Network (DAWN) |
| **URL** | <https://www.samhsa.gov/data/> |
| **Data Updated** | Annual |
| **Coverage** | National; substance use disorders, overdose ED visits, treatment demand |
| **Relevant Services** | P7 FamilyHealth, P9 DoctorServices, P2 VoiceEmergency |
| **CUI Category** | CUI//SP-HLTH |

**Citation:**
> Substance Abuse and Mental Health Services Administration. (Annual).
> *National Survey on Drug Use and Health (NSDUH).*
> U.S. Department of Health and Human Services.
> <https://www.samhsa.gov/data/>

---

### 4.3 NHTSA — EMS and Crash Injury Research

| Field | Detail |
|-------|--------|
| **Agency** | NHTSA, U.S. DOT |
| **Dataset** | National EMS Information System (NEMSIS); Crashworthiness Data System (CDS) |
| **URL** (NEMSIS) | <https://nemsis.org/> |
| **URL** (CDS) | <https://www.nhtsa.gov/crash-data-systems/crashworthiness-data-system> |
| **Data Updated** | Annual |
| **Coverage** | National EMS activations, crash biomechanics, injury severity scores |
| **Relevant Services** | P2 VoiceEmergency, P4 Wearable, P6 FirstResponder |
| **CUI Category** | CUI//SP-HLTH + CUI//SP-LEI |

**Citation:**
> National Highway Traffic Safety Administration. (Annual). *National EMS Information
> System (NEMSIS).* U.S. Department of Transportation.
> <https://nemsis.org/>

---

### 4.4 CDC — Behavioral Risk Factor Surveillance System (BRFSS)

| Field | Detail |
|-------|--------|
| **Agency** | CDC / NCCDPHP |
| **Dataset** | Behavioral Risk Factor Surveillance System (BRFSS) |
| **URL** | <https://www.cdc.gov/brfss/index.html> |
| **Data Updated** | Annual |
| **Coverage** | National + state; 400,000+ adult respondents on health behaviours |
| **Relevant Services** | P7 FamilyHealth |
| **CUI Category** | UNCLASSIFIED (aggregated) |

**Citation:**
> Centers for Disease Control and Prevention. (Annual). *Behavioral Risk Factor
> Surveillance System (BRFSS).* U.S. Department of Health and Human Services.
> <https://www.cdc.gov/brfss/index.html>

---

## 5. Law Enforcement and Responder Safety

### 5.1 FBI — Law Enforcement Officers Killed and Assaulted (LEOKA)

| Field | Detail |
|-------|--------|
| **Agency** | Federal Bureau of Investigation (FBI) |
| **Dataset** | Law Enforcement Officers Killed and Assaulted (LEOKA) |
| **URL** | <https://le.fbi.gov/additional-ucr-publications/leoka> |
| **Data Updated** | Annual |
| **Coverage** | National; all line-of-duty deaths and assaults on officers |
| **Relevant Services** | P6 FirstResponder, P2 VoiceEmergency |
| **CUI Category** | CUI//SP-LEI |

**Description:** LEOKA provides incident circumstances (weapon, activity, assignment,
time of day) for officer fatalities and assaults. TheWatch FirstResponder responder-safety
risk score references LEOKA weapon-type distributions and assault circumstances to
generate contextual safety advisories during dispatch.

**Citation:**
> Federal Bureau of Investigation. (Annual). *Law Enforcement Officers Killed and
> Assaulted (LEOKA).* U.S. Department of Justice.
> <https://le.fbi.gov/additional-ucr-publications/leoka>

---

### 5.2 NIOSH — First Responder Fatality and Injury Surveillance

| Field | Detail |
|-------|--------|
| **Agency** | National Institute for Occupational Safety and Health (NIOSH), CDC |
| **Dataset** | Fire Fighter Fatality Investigation and Prevention Program (FFFIPP); Emergency Medical Services Worker Injury Reports |
| **URL** | <https://www.cdc.gov/niosh/fire/> |
| **Data Updated** | Continuous (case reports); annual summaries |
| **Coverage** | National; fire-fighter and EMS worker line-of-duty deaths |
| **Relevant Services** | P6 FirstResponder, P4 Wearable |
| **CUI Category** | CUI//SP-LEI |

**Citation:**
> National Institute for Occupational Safety and Health. (Continuous).
> *Fire Fighter Fatality Investigation and Prevention Program.*
> Centers for Disease Control and Prevention.
> <https://www.cdc.gov/niosh/fire/>

---

## 6. Summary Table

| # | Dataset | Agency | URL | TheWatch Service(s) | Update Cadence |
|---|---------|--------|-----|---------------------|----------------|
| 1 | Uniform Crime Reporting (UCR) | FBI / DOJ | <https://ucr.fbi.gov/> | P2, P6, P11 | Annual |
| 2 | NIBRS | FBI / DOJ | <https://crime-data-explorer.app.cloud.gov/> | P2, P6, P11 | Annual |
| 3 | NCVS | BJS / DOJ | <https://bjs.ojp.gov/data-collection/ncvs> | P2, P7 | Annual |
| 4 | BJS Statistical Collections | BJS / DOJ | <https://bjs.ojp.gov/> | P6, P1 | Annual / periodic |
| 5 | ATF Crime Gun / NFCTA | ATF / DOJ | <https://www.atf.gov/resource-center/data-statistics> | P2, P11 | Annual |
| 6 | CISA Active Shooter Report | CISA / DHS | <https://www.cisa.gov/topics/physical-security/active-shooter-preparedness> | P2, P11, P6 | Annual |
| 7 | DEA NDTA | DEA / DOJ | <https://www.dea.gov/drug-information/drug-policy-and-prevention> | P2, P9 | Annual |
| 8 | CDC WISQARS | CDC / HHS | <https://www.cdc.gov/injury/wisqars/> | P2, P4, P7, P9 | Annual |
| 9 | CDC NVDRS | CDC / HHS | <https://www.cdc.gov/violenceprevention/datasources/nvdrs/index.html> | P2, P7 | Annual |
| 10 | CDC NVSS | CDC / NCHS / HHS | <https://www.cdc.gov/nchs/nvss/index.htm> | P7, P9 | Annual |
| 11 | NHTSA FARS | NHTSA / DOT | <https://www.nhtsa.gov/research-data/fatality-analysis-reporting-system-fars> | P2, P8, Geo | Annual |
| 12 | BLS CFOI / SOII | BLS / DOL | <https://www.bls.gov/iif/> | P6, P4 | Annual |
| 13 | CPSC NEISS | CPSC | <https://www.cpsc.gov/Research--Statistics/NEISS-Injury-Data> | P7, P9 | Annual |
| 14 | VA Veteran Suicide Report | VA / DoD | <https://www.mentalhealth.va.gov/suicide_prevention/data.asp> | P7, P9, P6 | Annual |
| 15 | FEMA Disaster Declarations | FEMA / DHS | <https://www.fema.gov/openfema-data-page/disaster-declarations-summaries-v2> | P8, Geo, P1 | Real-time |
| 16 | USFA / NFIRS Fire Statistics | USFA / FEMA / DHS | <https://www.usfa.fema.gov/data/statistics/> | P2, P8, P11 | Annual |
| 17 | NOAA Storm Events | NWS / NOAA / DOC | <https://www.ncdc.noaa.gov/stormevents/> | P8, Geo | Monthly |
| 18 | FEMA NFIP Claims | FEMA / DHS | <https://www.fema.gov/openfema-data-page/fima-nfip-redacted-claims-v2> | P8, Geo | Periodic |
| 19 | USGS Earthquake Feed | USGS / DOI | <https://earthquake.usgs.gov/earthquakes/feed/> | P8, Geo | Real-time |
| 20 | AHRQ HCUP | AHRQ / HHS | <https://www.ahrq.gov/data/hcup/index.html> | P7, P9 | Annual |
| 21 | SAMHSA NSDUH / DAWN | SAMHSA / HHS | <https://www.samhsa.gov/data/> | P7, P9, P2 | Annual |
| 22 | NHTSA NEMSIS / CDS | NHTSA / DOT | <https://nemsis.org/> | P2, P4, P6 | Annual |
| 23 | CDC BRFSS | CDC / HHS | <https://www.cdc.gov/brfss/index.html> | P7 | Annual |
| 24 | FBI LEOKA | FBI / DOJ | <https://le.fbi.gov/additional-ucr-publications/leoka> | P6, P2 | Annual |
| 25 | NIOSH FFFIPP | NIOSH / CDC / HHS | <https://www.cdc.gov/niosh/fire/> | P6, P4 | Continuous |

---

## 7. Data Access and Integration Notes

### Licensing

All datasets listed above are published by U.S. federal government agencies and are
released as **public-domain works** under 17 U.S.C. § 105 (works of the U.S.
Government are not eligible for copyright protection). They are freely downloadable,
redistributable, and usable without licence fee.

### Data Formats

| Format | Datasets |
|--------|----------|
| CSV / TSV | NIBRS bulk downloads, FARS, CFOI, NCVS, Storm Events, NFIP Claims |
| JSON / REST API | FEMA OpenFEMA API, USGS Earthquake Feed, NEMSIS API |
| Excel / SAS | BJS collections, HCUP, BRFSS, NVDRS microdata |
| PDF (reports) | LEOKA, CISA Active Shooter, DEA NDTA, VA Suicide Report |

### Integration Guidelines for TheWatch

1. **CUI Handling** — When individual-level records are ingested (e.g., NCVS
   microdata, HCUP patient records), they must be classified CUI//SP-HLTH or
   CUI//SP-LEI and handled per `docs/data-classification-matrix.md`.  
2. **PII Stripping** — Strip or pseudonymise all personal identifiers before
   persisting to any TheWatch database per NIST SP 800-171 § 3.13.  
3. **Aggregate-Only in Production** — Only aggregated, non-identifiable statistics
   may be stored in production databases. Raw microdata must remain in an
   isolated analysis environment with access controls per SP-HLTH / SP-LEI policy.  
4. **Citation in API Responses** — When TheWatch APIs surface statistics derived
   from these datasets, the response body **must** include a `"dataSource"` field
   citing the originating agency and URL per this document.  
5. **Refresh Schedule** — Annual datasets should be refreshed within 30 days of
   public release. Real-time feeds (FEMA OpenFEMA, USGS Earthquake) should be
   polled at intervals specified in each service's Hangfire job configuration.

---

## References

1. Federal Bureau of Investigation. (Annual). *Crime in the United States.* U.S. Department of Justice. <https://ucr.fbi.gov/>
2. Federal Bureau of Investigation. (Annual). *National Incident-Based Reporting System.* U.S. DOJ Crime Data Explorer. <https://crime-data-explorer.app.cloud.gov/>
3. Bureau of Justice Statistics. (Annual). *National Crime Victimization Survey.* Office of Justice Programs, U.S. DOJ. <https://bjs.ojp.gov/data-collection/ncvs>
4. Bureau of Justice Statistics. (Multiple years). *Statistical collections.* Office of Justice Programs, U.S. DOJ. <https://bjs.ojp.gov/>
5. Bureau of Alcohol, Tobacco, Firearms and Explosives. (Annual). *ATF Data & Statistics.* U.S. DOJ. <https://www.atf.gov/resource-center/data-statistics>
6. Cybersecurity and Infrastructure Security Agency. (Annual). *Active Shooter Incidents in the United States.* U.S. DHS. <https://www.cisa.gov/topics/physical-security/active-shooter-preparedness>
7. Drug Enforcement Administration. (Annual). *National Drug Threat Assessment.* U.S. DOJ. <https://www.dea.gov/drug-information/drug-policy-and-prevention>
8. Centers for Disease Control and Prevention, NCIPC. (Annual). *WISQARS.* U.S. HHS. <https://www.cdc.gov/injury/wisqars/>
9. Centers for Disease Control and Prevention. (Annual). *National Violent Death Reporting System.* NCIPC. <https://www.cdc.gov/violenceprevention/datasources/nvdrs/index.html>
10. Centers for Disease Control and Prevention, NCHS. (Annual). *National Vital Statistics System.* U.S. HHS. <https://www.cdc.gov/nchs/nvss/index.htm>
11. National Highway Traffic Safety Administration. (Annual). *Fatality Analysis Reporting System.* U.S. DOT. <https://www.nhtsa.gov/research-data/fatality-analysis-reporting-system-fars>
12. Bureau of Labor Statistics. (Annual). *Injuries, Illnesses, and Fatalities Program.* U.S. DOL. <https://www.bls.gov/iif/>
13. Consumer Product Safety Commission. (Annual). *National Electronic Injury Surveillance System.* U.S. CPSC. <https://www.cpsc.gov/Research--Statistics/NEISS-Injury-Data>
14. Department of Veterans Affairs. (Annual). *National Veteran Suicide Prevention Annual Report.* VA OMHSP. <https://www.mentalhealth.va.gov/suicide_prevention/data.asp>
15. Federal Emergency Management Agency. (Real-time). *OpenFEMA Disaster Declarations Summary (v2).* U.S. DHS. <https://www.fema.gov/openfema-data-page/disaster-declarations-summaries-v2>
16. U.S. Fire Administration, FEMA. (Annual). *National Fire Incident Reporting System.* U.S. DHS. <https://www.usfa.fema.gov/data/statistics/>
17. National Oceanic and Atmospheric Administration, NWS. (Monthly). *Storm Events Database.* U.S. DOC. <https://www.ncdc.noaa.gov/stormevents/>
18. Federal Emergency Management Agency. (Periodic). *OpenFEMA FIMA NFIP Redacted Claims (v2).* U.S. DHS. <https://www.fema.gov/openfema-data-page/fima-nfip-redacted-claims-v2>
19. U.S. Geological Survey. (Real-time). *Earthquake Hazards Program — Feeds & Notifications.* U.S. DOI. <https://earthquake.usgs.gov/earthquakes/feed/>
20. Agency for Healthcare Research and Quality. (Annual). *Healthcare Cost and Utilization Project.* U.S. HHS. <https://www.ahrq.gov/data/hcup/index.html>
21. Substance Abuse and Mental Health Services Administration. (Annual). *National Survey on Drug Use and Health.* U.S. HHS. <https://www.samhsa.gov/data/>
22. National Highway Traffic Safety Administration. (Annual). *National EMS Information System.* U.S. DOT. <https://nemsis.org/>
23. Centers for Disease Control and Prevention. (Annual). *Behavioral Risk Factor Surveillance System.* U.S. HHS. <https://www.cdc.gov/brfss/index.html>
24. Federal Bureau of Investigation. (Annual). *Law Enforcement Officers Killed and Assaulted.* U.S. DOJ. <https://le.fbi.gov/additional-ucr-publications/leoka>
25. National Institute for Occupational Safety and Health. (Continuous). *Fire Fighter Fatality Investigation and Prevention Program.* CDC. <https://www.cdc.gov/niosh/fire/>
