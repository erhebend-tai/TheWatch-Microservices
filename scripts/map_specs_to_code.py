#!/usr/bin/env python3
"""
map_specs_to_code.py — Cross-reference consolidated C# entities against OpenAPI specs.

Reads:
  - E:/json_output/Controllers.json (128 controllers)
  - E:/json_output/Services.json (1,244 services)
  - E:/json_output/Models.json (1,278 models)
  - E:/json_output/Interfaces.json (637 interfaces)
  - E:/json_output/APIS/_catalog.json (251 OpenAPI specs, 2,902 operations, 3,627 schemas)

Outputs:
  - E:/json_output/Microservices/_mapping.json
"""

import json
import re
from pathlib import Path
from collections import defaultdict
from difflib import SequenceMatcher

BASE = Path("E:/json_output")
API_CATALOG = BASE / "APIS" / "_catalog.json"
OUT = BASE / "Microservices" / "_mapping.json"

# Program-to-domain mapping (from plan)
PROGRAM_DOMAINS = {
    "P1": ["platform", "admin", "schemas", "infrastructure"],
    "P2": ["emergency", "dispatch", "voice", "evidence"],
    "P3": ["messaging", "notifications"],
    "P4": ["wearable", "caching"],
    "P5": ["auth", "security", "cryptography"],
    "P6": ["responder", "location"],
    "P7": ["medical", "wearable"],
    "P8": ["disaster", "logistics", "community"],
    "P9": ["medical"],
    "P10": ["community"],
}

# Domain-to-program reverse lookup (first match wins for primary assignment)
DOMAIN_TO_PROGRAM = {}
for prog, domains in PROGRAM_DOMAINS.items():
    for d in domains:
        if d not in DOMAIN_TO_PROGRAM:
            DOMAIN_TO_PROGRAM[d] = prog

# Namespace keywords to program mapping
NS_TO_PROGRAM = {
    "Authentication": "P5", "Auth": "P5", "Security": "P5", "Crypto": "P5",
    "Emergency": "P2", "Voice": "P2", "SOS": "P2", "Dispatch": "P2",
    "Evidence": "P2",
    "Mesh": "P3", "Messaging": "P3", "Notification": "P3", "SignalR": "P3",
    "Wearable": "P4", "Device": "P4", "Sensor": "P4", "Garmin": "P4",
    "Responder": "P6", "FirstResponder": "P6", "Location": "P6", "Geo": "P6",
    "Family": "P7", "Health": "P7", "Child": "P7", "Vital": "P7",
    "Disaster": "P8", "Relief": "P8", "Evacuation": "P8", "Shelter": "P8",
    "Doctor": "P9", "Telehealth": "P9", "Appointment": "P9", "Marketplace": "P9",
    "Gamification": "P10", "Reward": "P10", "Challenge": "P10", "Leaderboard": "P10",
    "Core": "P1", "Gateway": "P1", "Admin": "P1", "Platform": "P1", "Schema": "P1",
}


def load_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def normalize(name):
    """Lowercase, strip suffixes like Controller/Service/Model."""
    n = name.lower()
    for suffix in ["controller", "service", "services", "model", "dto",
                    "request", "response", "viewmodel", "interface", "handler"]:
        if n.endswith(suffix) and len(n) > len(suffix):
            n = n[:-len(suffix)]
    return n


def route_to_pattern(route_attr, action_route=""):
    """Convert ASP.NET route attributes to a normalized path pattern."""
    if not route_attr:
        return ""
    # Extract route string from attribute like Route("api/[controller]")
    parts = []
    for attr in (route_attr if isinstance(route_attr, list) else [route_attr]):
        m = re.search(r'Route\("([^"]+)"', str(attr))
        if m:
            parts.append(m.group(1))
    base = "/".join(parts) if parts else ""
    if action_route:
        base = f"{base}/{action_route}" if base else action_route
    return "/" + base.strip("/").lower().replace("[controller]", "{controller}")


def guess_program_from_namespace(namespace):
    """Guess program from namespace keywords."""
    if not namespace:
        return "P1"  # default to core
    for keyword, prog in NS_TO_PROGRAM.items():
        if keyword.lower() in namespace.lower():
            return prog
    return "P1"


def similarity(a, b):
    return SequenceMatcher(None, a.lower(), b.lower()).ratio()


def match_controllers_to_operations(controllers, specs):
    """Match controller methods to OpenAPI operations by route, HTTP method, name."""
    matches = []
    unmatched_controllers = []
    matched_op_ids = set()

    # Build operation index by normalized path + method
    op_index = {}  # (method, path_normalized) -> (spec, operation)
    op_by_id = {}  # operationId -> (spec, operation)
    for spec in specs:
        for op in spec.get("operations", []):
            path_norm = op["path"].lower().rstrip("/")
            key = (op["method"].upper(), path_norm)
            op_index[key] = (spec, op)
            if op.get("operationId"):
                op_by_id[op["operationId"].lower()] = (spec, op)

    for ctrl in controllers:
        ctrl_name = ctrl["name"]
        ctrl_norm = normalize(ctrl_name)
        ctrl_ns = ctrl.get("namespace", "")
        program = guess_program_from_namespace(ctrl_ns)

        ctrl_matched = False
        for method in ctrl.get("methods", []):
            http_method = (method.get("httpMethod") or "GET").upper()
            action_route = method.get("actionRoute", "")
            method_name = method["name"]

            # Try route-based match
            route_pattern = route_to_pattern(ctrl.get("attributes", []), action_route)
            route_key = (http_method, route_pattern.rstrip("/"))

            matched_spec = None
            matched_op = None
            match_type = None

            # 1. Exact route match
            if route_key in op_index:
                matched_spec, matched_op = op_index[route_key]
                match_type = "route_exact"

            # 2. operationId match (try controller+method combo)
            if not matched_op:
                candidates = [
                    f"{ctrl_norm}{method_name}".lower(),
                    f"{method_name}{ctrl_norm}".lower(),
                    method_name.lower(),
                ]
                for cand in candidates:
                    if cand in op_by_id:
                        matched_spec, matched_op = op_by_id[cand]
                        match_type = "operationId"
                        break

            # 3. Fuzzy path match
            if not matched_op and route_pattern:
                best_score = 0
                for (om, opath), (sp, o) in op_index.items():
                    if om == http_method:
                        score = similarity(route_pattern, opath)
                        if score > 0.7 and score > best_score:
                            best_score = score
                            matched_spec = sp
                            matched_op = o
                            match_type = f"fuzzy_route({score:.2f})"

            if matched_op:
                op_id = matched_op.get("operationId", f"{matched_op['method']}_{matched_op['path']}")
                matched_op_ids.add(op_id)
                ctrl_matched = True
                matches.append({
                    "controller": ctrl_name,
                    "controllerNamespace": ctrl_ns,
                    "method": method_name,
                    "httpMethod": http_method,
                    "actionRoute": action_route,
                    "program": program,
                    "matchType": match_type,
                    "specName": matched_spec["name"],
                    "specDomain": matched_spec["domain"],
                    "operationId": matched_op.get("operationId", ""),
                    "operationPath": matched_op["path"],
                    "operationMethod": matched_op["method"],
                    "operationSummary": matched_op.get("summary", ""),
                })
            else:
                matches.append({
                    "controller": ctrl_name,
                    "controllerNamespace": ctrl_ns,
                    "method": method_name,
                    "httpMethod": http_method,
                    "actionRoute": action_route,
                    "program": program,
                    "matchType": "unmatched",
                    "specName": None,
                    "specDomain": None,
                    "operationId": None,
                    "operationPath": None,
                    "operationMethod": None,
                    "operationSummary": None,
                })

        if not ctrl_matched and ctrl.get("methods"):
            unmatched_controllers.append(ctrl_name)

    return matches, matched_op_ids, unmatched_controllers


def match_models_to_schemas(models, specs):
    """Match consolidated models to OpenAPI schemas by name."""
    matches = []
    matched_schema_names = set()

    # Build schema index
    schema_index = {}  # normalized name -> [(spec, schema)]
    for spec in specs:
        for schema in spec.get("schemas", []):
            sname = schema["name"].lower()
            if sname not in schema_index:
                schema_index[sname] = []
            schema_index[sname].append((spec, schema))

    for model in models:
        model_name = model["name"]
        model_norm = normalize(model_name).lower()
        model_ns = model.get("namespace", "")
        program = guess_program_from_namespace(model_ns)

        matched = False
        # Try exact name match
        for try_name in [model_name.lower(), model_norm]:
            if try_name in schema_index:
                for spec, schema in schema_index[try_name]:
                    matched_schema_names.add(schema["name"])
                    matches.append({
                        "model": model_name,
                        "modelNamespace": model_ns,
                        "modelKind": model.get("kind", "class"),
                        "propertyCount": len(model.get("properties", [])),
                        "program": program,
                        "matchType": "name_exact" if try_name == model_name.lower() else "name_normalized",
                        "schemaName": schema["name"],
                        "specName": spec["name"],
                        "specDomain": spec["domain"],
                        "schemaProperties": schema.get("properties", []),
                    })
                    matched = True
                break

        if not matched:
            # Try fuzzy match
            best_score = 0
            best_match = None
            for sname, entries in schema_index.items():
                score = similarity(model_norm, sname)
                if score > 0.8 and score > best_score:
                    best_score = score
                    best_match = entries[0]

            if best_match:
                spec, schema = best_match
                matched_schema_names.add(schema["name"])
                matches.append({
                    "model": model_name,
                    "modelNamespace": model_ns,
                    "modelKind": model.get("kind", "class"),
                    "propertyCount": len(model.get("properties", [])),
                    "program": program,
                    "matchType": f"fuzzy({best_score:.2f})",
                    "schemaName": schema["name"],
                    "specName": spec["name"],
                    "specDomain": spec["domain"],
                    "schemaProperties": schema.get("properties", []),
                })
            else:
                matches.append({
                    "model": model_name,
                    "modelNamespace": model_ns,
                    "modelKind": model.get("kind", "class"),
                    "propertyCount": len(model.get("properties", [])),
                    "program": program,
                    "matchType": "unmatched",
                    "schemaName": None,
                    "specName": None,
                    "specDomain": None,
                    "schemaProperties": None,
                })

    return matches, matched_schema_names


def assign_services_to_programs(services):
    """Assign services to programs based on namespace/name heuristics."""
    assignments = []
    for svc in services:
        name = svc["name"]
        ns = svc.get("namespace", "")
        program = guess_program_from_namespace(ns)
        if program == "P1":
            # Try name-based as fallback
            for keyword, prog in NS_TO_PROGRAM.items():
                if keyword.lower() in name.lower():
                    program = prog
                    break
        assignments.append({
            "service": name,
            "namespace": ns,
            "kind": svc.get("kind", "file"),
            "program": program,
        })
    return assignments


def assign_interfaces_to_programs(interfaces):
    """Assign interfaces to programs."""
    assignments = []
    for iface in interfaces:
        name = iface["name"]
        ns = iface.get("namespace", "")
        program = guess_program_from_namespace(ns)
        if program == "P1":
            for keyword, prog in NS_TO_PROGRAM.items():
                if keyword.lower() in name.lower():
                    program = prog
                    break
        assignments.append({
            "interface": name,
            "namespace": ns,
            "kind": iface.get("kind", "interface"),
            "program": program,
        })
    return assignments


def collect_unmatched_operations(specs, matched_op_ids):
    """Find API operations with no matching controller method."""
    unmatched = []
    for spec in specs:
        for op in spec.get("operations", []):
            op_id = op.get("operationId", f"{op['method']}_{op['path']}")
            if op_id not in matched_op_ids:
                domain = spec["domain"]
                program = DOMAIN_TO_PROGRAM.get(domain, "P1")
                unmatched.append({
                    "specName": spec["name"],
                    "specDomain": domain,
                    "operationId": op.get("operationId", ""),
                    "method": op["method"],
                    "path": op["path"],
                    "summary": op.get("summary", ""),
                    "tags": op.get("tags", []),
                    "program": program,
                })
    return unmatched


def collect_unmatched_schemas(specs, matched_schema_names):
    """Find API schemas with no matching model."""
    unmatched = []
    for spec in specs:
        for schema in spec.get("schemas", []):
            if schema["name"] not in matched_schema_names:
                domain = spec["domain"]
                program = DOMAIN_TO_PROGRAM.get(domain, "P1")
                unmatched.append({
                    "specName": spec["name"],
                    "specDomain": domain,
                    "schemaName": schema["name"],
                    "schemaType": schema.get("type", "object"),
                    "properties": schema.get("properties", []),
                    "program": program,
                })
    return unmatched


def build_program_summary(controller_matches, model_matches, service_assignments,
                          interface_assignments, unmatched_ops, unmatched_schemas):
    """Build per-program summary of what's mapped."""
    programs = {}
    for prog in ["P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10"]:
        programs[prog] = {
            "controllers": [],
            "controllerMethods": {"matched": 0, "unmatched": 0},
            "models": {"matched": 0, "unmatched": 0},
            "services": 0,
            "interfaces": 0,
            "unmatchedOperations": 0,
            "unmatchedSchemas": 0,
            "domains": set(),
        }

    for m in controller_matches:
        p = m["program"]
        if p not in programs:
            continue
        ctrl = m["controller"]
        if ctrl not in programs[p]["controllers"]:
            programs[p]["controllers"].append(ctrl)
        if m["matchType"] != "unmatched":
            programs[p]["controllerMethods"]["matched"] += 1
            if m.get("specDomain"):
                programs[p]["domains"].add(m["specDomain"])
        else:
            programs[p]["controllerMethods"]["unmatched"] += 1

    for m in model_matches:
        p = m["program"]
        if p not in programs:
            continue
        if m["matchType"] != "unmatched":
            programs[p]["models"]["matched"] += 1
            if m.get("specDomain"):
                programs[p]["domains"].add(m["specDomain"])
        else:
            programs[p]["models"]["unmatched"] += 1

    for s in service_assignments:
        p = s["program"]
        if p in programs:
            programs[p]["services"] += 1

    for i in interface_assignments:
        p = i["program"]
        if p in programs:
            programs[p]["interfaces"] += 1

    for op in unmatched_ops:
        p = op["program"]
        if p in programs:
            programs[p]["unmatchedOperations"] += 1
            programs[p]["domains"].add(op["specDomain"])

    for s in unmatched_schemas:
        p = s["program"]
        if p in programs:
            programs[p]["unmatchedSchemas"] += 1

    # Convert sets to lists for JSON serialization
    for p in programs:
        programs[p]["controllers"] = sorted(programs[p]["controllers"])
        programs[p]["domains"] = sorted(programs[p]["domains"])

    return programs


def main():
    print("Loading consolidated entities...")
    controllers = load_json(BASE / "Controllers.json")["entries"]
    services = load_json(BASE / "Services.json")["entries"]
    models = load_json(BASE / "Models.json")["entries"]
    interfaces = load_json(BASE / "Interfaces.json")["entries"]

    print(f"  Controllers: {len(controllers)}")
    print(f"  Services:    {len(services)}")
    print(f"  Models:      {len(models)}")
    print(f"  Interfaces:  {len(interfaces)}")

    print("\nLoading API catalog...")
    catalog = load_json(API_CATALOG)
    specs = catalog["specs"]
    total_ops = sum(len(s["operations"]) for s in specs)
    total_schemas = sum(len(s["schemas"]) for s in specs)
    print(f"  Specs:      {len(specs)}")
    print(f"  Operations: {total_ops}")
    print(f"  Schemas:    {total_schemas}")

    print("\nMatching controllers to operations...")
    ctrl_matches, matched_op_ids, unmatched_ctrls = match_controllers_to_operations(controllers, specs)
    matched_ctrl_methods = sum(1 for m in ctrl_matches if m["matchType"] != "unmatched")
    print(f"  Matched methods:   {matched_ctrl_methods}")
    print(f"  Unmatched methods: {len(ctrl_matches) - matched_ctrl_methods}")

    print("\nMatching models to schemas...")
    model_matches, matched_schema_names = match_models_to_schemas(models, specs)
    matched_models = sum(1 for m in model_matches if m["matchType"] != "unmatched")
    print(f"  Matched models:   {matched_models}")
    print(f"  Unmatched models: {len(model_matches) - matched_models}")

    print("\nAssigning services to programs...")
    svc_assignments = assign_services_to_programs(services)

    print("Assigning interfaces to programs...")
    iface_assignments = assign_interfaces_to_programs(interfaces)

    print("\nFinding unmatched operations (specs with no code)...")
    unmatched_ops = collect_unmatched_operations(specs, matched_op_ids)
    print(f"  Operations with no code: {len(unmatched_ops)}")

    print("Finding unmatched schemas (specs with no model)...")
    unmatched_schemas = collect_unmatched_schemas(specs, matched_schema_names)
    print(f"  Schemas with no model:   {len(unmatched_schemas)}")

    print("\nBuilding program summary...")
    program_summary = build_program_summary(
        ctrl_matches, model_matches, svc_assignments,
        iface_assignments, unmatched_ops, unmatched_schemas
    )

    # Build final mapping
    mapping = {
        "generatedAt": "2026-02-25T00:00:00Z",
        "sources": {
            "controllers": len(controllers),
            "services": len(services),
            "models": len(models),
            "interfaces": len(interfaces),
            "specs": len(specs),
            "totalOperations": total_ops,
            "totalSchemas": total_schemas,
        },
        "coverage": {
            "controllerMethodsMatched": matched_ctrl_methods,
            "controllerMethodsTotal": len(ctrl_matches),
            "modelsMatched": matched_models,
            "modelsTotal": len(model_matches),
            "operationsWithCode": len(matched_op_ids),
            "operationsTotal": total_ops,
            "schemasWithModel": len(matched_schema_names),
            "schemasTotal": total_schemas,
        },
        "programSummary": program_summary,
        "controllerMatches": ctrl_matches,
        "modelMatches": model_matches,
        "serviceAssignments": svc_assignments,
        "interfaceAssignments": iface_assignments,
        "unmatchedOperations": unmatched_ops,
        "unmatchedSchemas": unmatched_schemas,
    }

    print(f"\nWriting mapping to {OUT}...")
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(mapping, f, indent=2)

    # Print coverage report
    print("\n" + "=" * 60)
    print("COVERAGE REPORT")
    print("=" * 60)
    cov = mapping["coverage"]
    print(f"Controller methods matched to operations: {cov['controllerMethodsMatched']}/{cov['controllerMethodsTotal']}")
    print(f"Models matched to schemas:                {cov['modelsMatched']}/{cov['modelsTotal']}")
    print(f"API operations with code:                 {cov['operationsWithCode']}/{cov['operationsTotal']}")
    print(f"API schemas with model:                   {cov['schemasWithModel']}/{cov['schemasTotal']}")

    print("\nPER-PROGRAM BREAKDOWN:")
    print(f"{'Program':<8} {'Controllers':<14} {'Methods(M/U)':<15} {'Models(M/U)':<14} {'Services':<10} {'Ifaces':<8} {'GapOps':<8} {'Domains'}")
    print("-" * 100)
    for prog in ["P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10"]:
        ps = program_summary[prog]
        cm = ps["controllerMethods"]
        mm = ps["models"]
        domains_str = ", ".join(ps["domains"][:4])
        if len(ps["domains"]) > 4:
            domains_str += f" +{len(ps['domains'])-4}"
        print(f"{prog:<8} {len(ps['controllers']):<14} {cm['matched']}/{cm['unmatched']:<12} {mm['matched']}/{mm['unmatched']:<11} {ps['services']:<10} {ps['interfaces']:<8} {ps['unmatchedOperations']:<8} {domains_str}")

    print(f"\nMapping written to: {OUT}")
    print(f"File size: {OUT.stat().st_size / 1024:.0f} KB")


if __name__ == "__main__":
    main()
