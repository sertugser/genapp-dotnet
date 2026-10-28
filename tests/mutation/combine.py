"""Combines the per-job summaries of summarize.py into markdown comments.

python3 combine.py <artifacts-dir> <out-dir>
Writes 1-score.md (score table) and 2-*.md (surviving mutants of each suite, split to stay under the comment size limit).
"""
import glob
import json
import os
import sys

src, out = sys.argv[1:3]
os.makedirs(out, exist_ok=True)
PROJECTS = ["GenApp.Domain", "GenApp.Application", "GenApp.Infrastructure", "GenApp.Api"]
SUITES = {"esdegerlik": "Yalnız eşdeğerlik testleri", "tum": "Bütün testler"}

data = {}
for path in glob.glob(os.path.join(src, "**", "*.json"), recursive=True):
    if os.path.basename(path) == "mutation-report.json":
        continue
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    if "suite" in d:
        data[(d["suite"], d["project"])] = d


def cell(d):
    if not d or not d["found"]:
        return "rapor yok"
    if not d["valid"]:
        return "mutant yok"
    return f"%{d['score']} ({d['detected']}/{d['valid']})"


lines = ["## Mutasyon testi sonucu", "",
         "Skor = yakalanan mutant / geçerli mutant (yakalanan: test kırıldı ya da zaman aşımı; geçerli: derlenen ve "
         "sayılan mutantlar).", "",
         "| Proje | " + " | ".join(SUITES.values()) + " |", "|---|" + "---|" * len(SUITES)]
totals = {}
for p in PROJECTS:
    row = [cell(data.get((s, p))) for s in SUITES]
    lines.append(f"| {p} | " + " | ".join(row) + " |")
    for s in SUITES:
        d = data.get((s, p))
        if d and d["found"]:
            t = totals.setdefault(s, [0, 0])
            t[0] += d["detected"]
            t[1] += d["valid"]
tot = [f"**%{round(100 * totals[s][0] / totals[s][1], 1)} ({totals[s][0]}/{totals[s][1]})**" if s in totals and totals[s][1]
       else "-" for s in SUITES]
lines.append("| **Toplam** | " + " | ".join(tot) + " |")
lines += ["", "| Proje | Takım | Durumlar |", "|---|---|---|"]
for p in PROJECTS:
    for s in SUITES:
        d = data.get((s, p))
        if d and d["found"]:
            lines.append(f"| {p} | {s} | {', '.join(f'{k}: {v}' for k, v in sorted(d['counts'].items()))} |")
with open(os.path.join(out, "1-score.md"), "w", encoding="utf-8") as f:
    f.write("\n".join(lines) + "\n")

for s, title in SUITES.items():
    rows = []
    for p in PROJECTS:
        d = data.get((s, p))
        for m in (d or {}).get("survivors", []):
            orig = m["original"].replace("|", "\\|").replace("`", "'")[:90]
            rep = m["replacement"].replace("|", "\\|").replace("`", "'").replace("\n", " ")[:60]
            rows.append(f"| {m['file']}:{m['line']} | {m['status']} | {m['mutator']} | `{orig}` | `{rep}` |")
    header = [f"## Yakalanmayan mutantlar: {title}", "", "| Yer | Durum | Mutasyon | Satır | Yerine |", "|---|---|---|---|---|"]
    part, size, n = [], 0, 1
    for r in rows:
        if size + len(r) > 55000:
            with open(os.path.join(out, f"2-{s}-{n}.md"), "w", encoding="utf-8") as f:
                f.write("\n".join(header + part) + "\n")
            part, size, n = [], 0, n + 1
        part.append(r)
        size += len(r) + 1
    if part or n == 1:
        with open(os.path.join(out, f"2-{s}-{n}.md"), "w", encoding="utf-8") as f:
            f.write("\n".join(header + (part or ["| (yok) | | | | |"])) + "\n")
print(open(os.path.join(out, "1-score.md"), encoding="utf-8").read())
