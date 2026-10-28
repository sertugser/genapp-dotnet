"""Summarizes one Stryker.NET mutation report (mutation-testing-report-schema JSON).

python3 summarize.py <mutation-report.json> <suite> <project> <out-dir>
Writes <out-dir>/<suite>-<project>.json (counts and surviving mutants) and a short markdown table for the job summary.
Score = (killed + timeout) / (killed + timeout + survived + no coverage), Stryker's own formula; compile errors and
ignored mutants are not counted.
"""
import json
import os
import sys

report_path, suite, project, out = sys.argv[1:5]
os.makedirs(out, exist_ok=True)

counts = {}
survivors = []
if os.path.exists(report_path):
    with open(report_path, encoding="utf-8") as f:
        report = json.load(f)
    for path, data in sorted(report.get("files", {}).items()):
        lines = data.get("source", "").splitlines()
        rel = path.replace("\\", "/")
        rel = rel[rel.find("src/"):] if "src/" in rel else rel
        for m in data.get("mutants", []):
            status = m.get("status", "?")
            counts[status] = counts.get(status, 0) + 1
            if status in ("Survived", "NoCoverage"):
                line = m["location"]["start"]["line"]
                original = lines[line - 1].strip() if 0 < line <= len(lines) else ""
                survivors.append({
                    "file": rel, "line": line, "status": status, "mutator": m.get("mutatorName", ""),
                    "replacement": (m.get("replacement") or "").strip(), "original": original,
                })
    found = True
else:
    found = False

k, t = counts.get("Killed", 0), counts.get("Timeout", 0)
s, n = counts.get("Survived", 0), counts.get("NoCoverage", 0)
total = k + t + s + n
score = round(100 * (k + t) / total, 1) if total else None
summary = {"suite": suite, "project": project, "found": found, "counts": counts, "score": score,
           "detected": k + t, "valid": total, "survivors": survivors}
with open(os.path.join(out, f"{suite}-{project}.json"), "w", encoding="utf-8") as f:
    json.dump(summary, f, ensure_ascii=False, indent=1)
with open(os.path.join(out, f"{suite}-{project}.md"), "w", encoding="utf-8") as f:
    if not found:
        f.write(f"**{suite} / {project}:** rapor yok (Stryker çalışmadı ya da başarısız oldu).\n")
    else:
        f.write(f"**{suite} / {project}:** skor {score}% ({k + t}/{total}); durumlar: {counts}\n")
print(json.dumps({key: summary[key] for key in ("suite", "project", "found", "counts", "score")}, ensure_ascii=False))
