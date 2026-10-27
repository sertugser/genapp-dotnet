#!/usr/bin/env python3
"""LLM deneyi: LLM çıktısını aynı eşdeğerlik testleriyle çalıştırır (experiments/llm/README.md, bölüm 6).

Ana koda (src/, tests/) dokunmaz: git commit'inden bir çalışma kopyası çıkarır ve her çalıştırmayı orada yapar.

  baz   Elle yazılmış kod, hiçbir şey değiştirilmeden (karşılaştırma tabanı)
  A     İzole (ana ölçü): incelenen operasyonun elle yazılmış action'ı devre dışı, yerine LLM dosyaları;
        testlerin kurulum adımlarındaki diğer operasyonlar elle yazılmış koddur
  kontrol  Negatif kontrol: elle yazılmış action'lar devre dışı, LLM dosyası yok (testler kırılmalı)
  B     Bütünleşik (yardımcı): kapsamdaki bütün operasyonlar aynı tekrardan (T1, T2 ya da T3) LLM'den gelir

Kullanım (repo kökünden):
  python experiments/llm/run_tests.py --dotnet C:/d10/dotnet.exe --baglanti "Host=...;..." \
      --is-dizini C:/w --commit HEAD --calistirma 2026-10-27

Sonuçlar experiments/llm/sonuclar/<ad>.json ve .trx olarak yazılır.
"""
import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import io
import xml.etree.ElementTree as ET

KOK = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

# operasyon -> elle yazılmış, devre dışı bırakılacak action
KAPSAM = {
    "musteri-ekle": "CustomersController.Add",
    "musteri-sorgula": "CustomersController.Inquire",
    "musteri-guncelle": "CustomersController.Update",
    "motor-ekle": "MotorPoliciesController.Add",
    "motor-sorgula": "MotorPoliciesController.Inquire",
}
TEKRARLAR = ["T1", "T2", "T3"]


def calistir(komut, cwd, env, zaman_asimi=1500):
    p = subprocess.run(komut, cwd=cwd, env=env, capture_output=True, text=True, encoding="utf-8",
                       errors="replace", timeout=zaman_asimi)
    return p.returncode, (p.stdout or "") + (p.stderr or "")


def kopya_olustur(is_dizini, commit):
    """Commit'teki src/, tests/ ve global.json'u is_dizini/agac altına çıkarır."""
    hedef = os.path.join(is_dizini, "agac")
    if os.path.exists(hedef):
        shutil.rmtree(hedef)
    os.makedirs(hedef)
    veri = subprocess.run(["git", "archive", commit, "src", "tests", "global.json", "dotnet-tools.json"], cwd=KOK, capture_output=True, check=True).stdout
    with tarfile.open(fileobj=io.BytesIO(veri)) as t:
        t.extractall(hedef)
    api = os.path.join(hedef, "src", "GenApp.Api")
    shutil.copy(os.path.join(KOK, "experiments", "llm", "harness", "LlmIzoleKonvansiyonu.cs"), api)
    yoklama = os.path.join(hedef, "tests", "GenApp.EquivalenceTests", "Yoklama")
    os.makedirs(yoklama)
    shutil.copy(os.path.join(KOK, "experiments", "llm", "yoklama", "YoklamaTests.cs"), yoklama)
    prog = os.path.join(api, "Program.cs")
    metin = open(prog, encoding="utf-8").read()
    assert metin.count("builder.Services.AddControllers()") == 1, "Program.cs beklenen biçimde değil"
    metin = metin.replace("builder.Services.AddControllers()",
                          "builder.Services.AddControllers(o => o.Conventions.Add(new LlmIzoleKonvansiyonu()))")
    open(prog, "w", encoding="utf-8").write(metin)
    return hedef


def llm_dosyalarini_koy(agac, calistirma, operasyonlar, tekrar):
    """LLM çıktısını (cikti/src/... altındakini) GenApp.Api/Llm/<op>/ altına, bayt bayt kopyalar."""
    llm = os.path.join(agac, "src", "GenApp.Api", "Llm")
    if os.path.exists(llm):
        shutil.rmtree(llm)
    for op in operasyonlar:
        kaynak = os.path.join(KOK, "experiments", "llm", "runs", calistirma, op, tekrar, "cikti")
        for kok, _, dosyalar in os.walk(kaynak):
            for d in dosyalar:
                hedef = os.path.join(llm, op, d)
                os.makedirs(os.path.dirname(hedef), exist_ok=True)
                shutil.copy(os.path.join(kok, d), hedef)


def trx_oku(yol):
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    kok = ET.parse(yol).getroot()
    testler = {}
    for r in kok.iterfind(".//t:UnitTestResult", ns):
        ad = r.get("testName")
        sonuc = r.get("outcome")
        mesaj = r.findtext(".//t:ErrorInfo/t:Message", default="", namespaces=ns).strip()
        testler[ad] = {"sonuc": sonuc, "mesaj": mesaj[:600]}
    return testler


def calistir_bir(ad, agac, dotnet, env, operasyonlar, dislanan, hedef_klasor, iz="Operasyon"):
    """Derle, filtreli testleri çalıştır, sonucu {derleme, testler} olarak döndür."""
    env = dict(env)
    env["LLM_DISLANAN_ACTIONLAR"] = ";".join(dislanan)
    proje = os.path.join(agac, "tests", "GenApp.EquivalenceTests", "GenApp.EquivalenceTests.csproj")
    kod, cikti = calistir([dotnet, "build", proje, "-c", "Debug", "-nologo", "-v", "q", "-clp:ErrorsOnly"], agac, env)
    if kod != 0:
        hatalar = sorted({l.strip() for l in cikti.splitlines() if "error" in l})[:15]
        return {"derleme": False, "derleme_hatalari": hatalar, "testler": {}}
    filtre = "|".join(f"{iz}={o}" for o in operasyonlar)
    trx = f"{ad}.trx"
    kod, cikti = calistir([dotnet, "test", proje, "--no-build", "--filter", filtre, "--logger", f"trx;LogFileName={trx}",
                           "--results-directory", os.path.join(agac, "TestResults")], agac, env)
    yol = os.path.join(agac, "TestResults", trx)
    if not os.path.exists(yol):
        return {"derleme": True, "testler": {}, "calistirma_cikti": cikti[-1500:]}
    shutil.copy(yol, os.path.join(hedef_klasor, trx))
    return {"derleme": True, "testler": trx_oku(yol)}


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--dotnet", required=True)
    p.add_argument("--baglanti", required=True, help="ConnectionStrings__GenApp değeri")
    p.add_argument("--is-dizini", default="C:/w")
    p.add_argument("--commit", default="HEAD")
    p.add_argument("--calistirma", default="2026-10-27")
    p.add_argument("--yalniz", action="append", help="yalnızca bu çalıştırma(lar): baz, A-<op>-<T>, B-<T>")
    a = p.parse_args()

    env = dict(os.environ)
    env["ConnectionStrings__GenApp"] = a.baglanti
    env["DOTNET_NOLOGO"] = "1"
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    env["DOTNET_ROOT"] = os.path.dirname(a.dotnet)
    env["PATH"] = os.path.dirname(a.dotnet) + os.pathsep + env["PATH"]
    commit = subprocess.run(["git", "rev-parse", a.commit], cwd=KOK, capture_output=True, text=True).stdout.strip()
    agac = kopya_olustur(a.is_dizini, commit)
    cikti = os.path.join(KOK, "experiments", "llm", "sonuclar")
    os.makedirs(cikti, exist_ok=True)

    # kontrol: elle yazılmış action'lar devre dışı, LLM dosyası yok. Testler kırılmalı; kırılmıyorsa düzenek geçersizdir.
    plan = [("baz", list(KAPSAM), [], None), ("kontrol", list(KAPSAM), list(KAPSAM.values()), None)]
    for op in KAPSAM:
        for t in TEKRARLAR:
            plan.append((f"A-{op}-{t}", [op], [KAPSAM[op]], t))
    # ek yoklama (ölçüm dışı): aynı A düzeni, "Yoklama" etiketli testler; "Ybaz" elle yazılmış kodu sınar
    plan.append(("Ybaz", list(KAPSAM), [], None))
    for op in KAPSAM:
        for t in TEKRARLAR:
            plan.append((f"Y-{op}-{t}", [op], [KAPSAM[op]], t))
    for t in TEKRARLAR:
        plan.append((f"B-{t}", list(KAPSAM), list(KAPSAM.values()), t))

    for ad, ops, dislanan, tekrar in plan:
        if a.yalniz and ad not in a.yalniz:
            continue
        if tekrar:
            llm_dosyalarini_koy(agac, a.calistirma, ops, tekrar)
        elif os.path.exists(os.path.join(agac, "src", "GenApp.Api", "Llm")):
            shutil.rmtree(os.path.join(agac, "src", "GenApp.Api", "Llm"))
        print(f"== {ad}", flush=True)
        sonuc = calistir_bir(ad, agac, a.dotnet, env, ops, dislanan, cikti,
                            "Yoklama" if ad.startswith("Y") else "Operasyon")
        sonuc.update({"ad": ad, "operasyonlar": ops, "tekrar": tekrar, "commit": commit, "dislanan_action": dislanan})
        with open(os.path.join(cikti, f"{ad}.json"), "w", encoding="utf-8") as f:
            json.dump(sonuc, f, ensure_ascii=False, indent=2)
        gecen = sum(1 for v in sonuc["testler"].values() if v["sonuc"] == "Passed")
        print(f"   derleme={sonuc['derleme']} geçen={gecen}/{len(sonuc['testler'])}", flush=True)


if __name__ == "__main__":
    main()
