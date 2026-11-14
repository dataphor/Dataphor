"""Summarize a dotnet build log: distinct errors, by code, with samples."""
import collections
import os
import re
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")) + os.sep

log = sys.argv[1]
limit = int(sys.argv[2]) if len(sys.argv) > 2 else 15
seen = set()
bycode = collections.Counter()
samples = collections.defaultdict(list)
missing = collections.Counter()
for line in open(log, encoding="utf-8", errors="replace"):
    m = re.match(r"^(.*?)\((\d+),(\d+)\): error (\w+): (.*?) \[", line)
    if not m:
        m2 = re.search(r"error (\w+): (.*?)( \[|$)", line)
        if m2 and " error " in line:
            key = (m2.group(1), m2.group(2))
            if key not in seen:
                seen.add(key)
                bycode[m2.group(1)] += 1
                samples[m2.group(1)].append(m2.group(2)[:220])
        continue
    path, ln, col, code, msg = m.groups()
    key = (path, ln, col, code)
    if key in seen:
        continue
    seen.add(key)
    bycode[code] += 1
    short = path.replace("C:\\projects\\Dataphor\\", "")
    samples[code].append(f"{short}({ln}): {msg[:200]}")
    mm = re.search(r"name '([^']+)'", msg)
    if mm:
        missing[mm.group(1)] += 1
print(f"{log}: {len(seen)} distinct errors")
for code, n in bycode.most_common():
    print(f"  {code} x{n}")
    for s in samples[code][:limit]:
        print(f"      {s}")
if missing:
    print("  missing names:", ", ".join(f"{k}x{v}" for k, v in missing.most_common(40)))
