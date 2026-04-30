import sys
import json

obj = json.loads(sys.stdin.read())
report = {}

if obj['ExitCode'] == 0:
    test_line = next(
        (
            line.strip()
            for line in obj['StdOut'].splitlines()
            if (line.startswith("ok") or line.startswith("not ok"))
            and obj['TestName'] in line
        ),
        None
    )
    report["Outcome"] = "Passed"
    if test_line is not None:
        if '# SKIP' in test_line:
            report['Outcome'] = 'Skipped'
        elif '# TODO' in test_line:
            report['Outcome'] = 'TODO'
else:
    report["Outcome"] = "Failed"
    report['ErrorMessage'] = obj['StdErr']
    report['ErrorStackTrace'] = '# void foo(void);\n# void bar(void);'

print(json.dumps(report))
