import json, glob, os

target_tests = [
    "test_cli_creates_a_drawing_with_named_pages",
    "test_cli_closes_its_session",
    "test_cli_reads_back_what_it_created",
    "test_cli_builds_a_connected_flowchart",
    "test_cli_consults_the_design_catalog_first",
    "test_cli_discovers_commands_before_using_them",
]

# Find the most recent file with CLI tests
files = sorted(glob.glob("aitest-reports/results_pytest-aitest_*.json"), reverse=True)

for f in files:
    with open(f) as fh:
        data = json.load(fh)
    tests = data.get("tests", data.get("results", []))
    cli_tests = [t for t in tests if "cli" in t.get("name", "").lower()]
    if len(cli_tests) >= len(target_tests):
        print(f"=== REPORT FILE: {os.path.basename(f)} ===")
        print(f"Total tests: {len(tests)}, CLI tests: {len(cli_tests)}\n")
        
        for target in target_tests:
            matches = [t for t in tests if target in t.get("name", "")]
            if matches:
                t = matches[0]
                print(f"{'='*80}")
                print(f"TEST: {t['name']}")
                print(f"STATUS: {t['status']}")
                print(f"FAILURE REASON: {t.get('failure_reason', 'N/A')}")
                
                # Print tool calls if available
                tool_calls = t.get("tool_calls", [])
                if tool_calls:
                    print(f"\nTOOL CALLS ({len(tool_calls)}):")
                    for i, tc in enumerate(tool_calls):
                        if isinstance(tc, dict):
                            # Show exit codes and errors
                            output = tc.get("output", tc.get("result", ""))
                            if isinstance(output, str) and ("exit_code" in output or "error" in output or "Error" in output):
                                print(f"  Call {i}: {json.dumps(tc, indent=2)[:500]}")
                
                # Print conversation excerpts if available
                conversation = t.get("conversation", t.get("messages", []))
                if conversation:
                    print(f"\nCONVERSATION EXCERPTS (last 3):")
                    for msg in conversation[-3:]:
                        role = msg.get("role", "?")
                        content = str(msg.get("content", ""))[:300]
                        print(f"  [{role}]: {content}")
                print()
            else:
                print(f"TEST: {target} - NOT FOUND in report")
        break
else:
    print(f"No report with {len(target_tests)}+ CLI tests found")
