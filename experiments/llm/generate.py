#!/usr/bin/env python3
"""LLM deneyi: 18 operasyon için talimat üretir ve modele gönderir (experiments/llm/README.md).

Kullanım (repo kökünden):
  python experiments/llm/generate.py --dry-run                  # yalnızca talimatları yazar, API çağırmaz
  python experiments/llm/generate.py --tekrar 1 --tekrar 2 --tekrar 3
  python experiments/llm/generate.py --operasyon musteri-ekle --tekrar 1

Kurallar README bölüm 5'tedir: her operasyon temiz bir oturum, tek atış, araç yok, geri bildirim yok.
API hatasında çağrı yeniden denenmez ve yeniden örneklenmez; başarısız kayıt "hata.txt"ye yazılır,
aynı komut yalnızca eksik (cevap.md'si olmayan) operasyon/tekrarları çalıştırır.
Yalnızca standart kütüphane kullanılır. Anahtar: ANTHROPIC_API_KEY ortam değişkeni.
"""
import argparse
import datetime
import hashlib
import json
import os
import re
import sys
import urllib.error
import urllib.request

MODEL = "claude-opus-5-5"   # README bölüm 2; takma ad kullanılmaz
MAX_TOKENS = 16000
API_URL = "https://api.anthropic.com/v1/messages"
API_VERSION = "2023-06-01"

KOK = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
README = os.path.join(KOK, "experiments", "llm", "README.md")

ORTAK = ["lgcmarea.cpy", "lgpolicy.cpy"]
EKLE = ["lgapol01", "lgapdb01", "lgapvs01"]
SORGULA = ["lgipol01", "lgipdb01"]
GUNCELLE = ["lgupol01", "lgupdb01", "lgupvs01"]
SIL = ["lgdpol01", "lgdpdb01", "lgdpvs01"]
TURLER = {  # ad, ad alanı öneki, adres parçası, istek harfleri
    "motor": ("Motor", "motor-policies", "MOT"),
    "konut": ("Konut", "house-policies", "HOU"),
    "hayat": ("Hayat", "endowment-policies", "END"),
    "ticari": ("Ticari", "commercial-policies", "COM"),
}
POLICE = "/api/customers/{customerNumber}/%s"
POLICE_NO = POLICE + "/{policyNumber}"


def operasyonlar():
    """(ad, istek kodu, yöntem ve adres, COBOL dosyaları) — README bölüm 4."""
    o = [
        ("musteri-ekle", "01ACUS", "POST /api/customers",
         ["lgacus01", "lgacdb01", "lgacdb02", "lgacvs01"]),
        ("musteri-sorgula", "01ICUS", "GET /api/customers/{customerNumber}", ["lgicus01", "lgicdb01"]),
        ("musteri-guncelle", "01UCUS", "PUT /api/customers/{customerNumber}",
         ["lgucus01", "lgucdb01", "lgucvs01"]),
    ]
    for tur, (_, yol, kod) in TURLER.items():
        o.append((f"{tur}-ekle", f"01A{kod}", "POST " + POLICE % yol, EKLE))
        if tur == "ticari":
            o.append(("ticari-sorgula", "01ICOM, 02ICOM, 03ICOM, 05ICOM",
                      "GET " + POLICE_NO % yol + "  (01ICOM)\n"
                      "GET /api/commercial-policies/{policyNumber}  (02ICOM)\n"
                      "GET " + POLICE % yol + "  (03ICOM)\n"
                      "GET /api/commercial-policies?postcode={postcode}  (05ICOM)", SORGULA))
        else:
            o.append((f"{tur}-sorgula", f"01I{kod}", "GET " + POLICE_NO % yol, SORGULA))
            o.append((f"{tur}-guncelle", f"01U{kod}", "PUT " + POLICE_NO % yol, GUNCELLE))
        o.append((f"{tur}-sil", f"01D{kod}", "DELETE " + POLICE_NO % yol, SIL))
    return o


def ad_alani(ad):
    return "GenApp.Llm." + "".join(p.capitalize() for p in ad.split("-"))


def oku(yol):
    with open(yol, encoding="utf-8", newline="") as f:
        return f.read().replace("\r\n", "\n")


def bolum(metin, baslangic, bitis):
    m = re.search(r"^## %s .*$" % re.escape(baslangic), metin, re.M)
    b = re.search(r"^## %s .*$" % re.escape(bitis), metin, re.M)
    if not m or not b:
        sys.exit(f"architecture.md'de bölüm {baslangic} / {bitis} bulunamadı")
    return metin[m.start():b.start()].rstrip() + "\n"


def sablon():
    metin = oku(README)
    m = re.search(r"````text\n(.*?)\n````", metin, re.S)
    if not m:
        sys.exit("README'de talimat şablonu (````text) bulunamadı")
    return m.group(1)


def belgeler():
    mimari = oku(os.path.join(KOK, "docs", "architecture.md"))
    return {
        "api": oku(os.path.join(KOK, "docs", "api-design.md")),
        "sema": oku(os.path.join(KOK, "docs", "database-schema.md")),
        "mimari": bolum(mimari, "4.", "5.") + "\n" + bolum(mimari, "8.", "9."),
    }


def cobol(dosyalar):
    parcalar = []
    for d in dosyalar + ORTAK:
        ad = d if "." in d else d + ".cbl"
        parcalar.append(f"### base/src/{ad}\n```cobol\n{oku(os.path.join(KOK, 'base', 'src', ad)).rstrip()}\n```")
    return "\n\n".join(parcalar)


def talimat(sab, doc, op):
    ad, istek, adres, dosyalar = op
    satir = [s for s in sab.split("\n") if s.startswith("{COBOL_DOSYALARI}")]
    if len(satir) != 1:
        sys.exit("şablonda {COBOL_DOSYALARI} satırı bulunamadı")
    metin = sab.replace(satir[0], cobol(dosyalar))
    yerine = {
        "{OPERASYON_ADI}": ad, "{ISTEK_KODU}": istek, "{YONTEM_VE_ADRES}": adres,
        "{OPERASYON_AD_ALANI}": ad_alani(ad).removeprefix("GenApp.Llm.") if hasattr(str, "removeprefix")
        else ad_alani(ad)[len("GenApp.Llm."):],
        "{API_TASARIMI_METNI}": doc["api"], "{VERITABANI_SEMASI_METNI}": doc["sema"],
        "{MIMARI_BELGE_BOLUMLERI}": doc["mimari"],
    }
    for k, v in yerine.items():
        metin = metin.replace(k, v)
    kalan = re.findall(r"\{[A-Z_]+\}", metin)
    if kalan:
        sys.exit(f"{ad}: doldurulmamış yer tutucu: {sorted(set(kalan))}")
    return metin


def cagir(anahtar, istem):
    govde = json.dumps({"model": MODEL, "max_tokens": MAX_TOKENS,
                        "messages": [{"role": "user", "content": istem}]}).encode("utf-8")
    istek = urllib.request.Request(API_URL, govde, {
        "x-api-key": anahtar, "anthropic-version": API_VERSION, "content-type": "application/json"})
    with urllib.request.urlopen(istek, timeout=600) as r:
        return json.loads(r.read().decode("utf-8"))


def dosyalari_ayikla(cevap, hedef):
    """### dosya: <yol> başlıklı ```csharp bloklarını olduğu gibi yazar (içerik değiştirilmez)."""
    sayi = 0
    for yol, icerik in re.findall(r"### dosya:\s*(\S+)\s*\n```csharp\n(.*?)\n```", cevap, re.S):
        yol = os.path.normpath(yol.strip("`"))
        if os.path.isabs(yol) or yol.startswith(".."):
            continue
        tam = os.path.join(hedef, yol)
        os.makedirs(os.path.dirname(tam), exist_ok=True)
        with open(tam, "w", encoding="utf-8", newline="\n") as f:
            f.write(icerik + "\n")
        sayi += 1
    return sayi


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--dry-run", action="store_true", help="API çağırma; yalnızca talimatları yaz")
    p.add_argument("--operasyon", action="append", help="yalnızca bu operasyon(lar)")
    p.add_argument("--tekrar", action="append", type=int, choices=[1, 2, 3], help="T1, T2, T3 (varsayılan: 1)")
    p.add_argument("--tarih", default=datetime.date.today().isoformat(), help="runs/<tarih> klasörü")
    a = p.parse_args()

    ops = operasyonlar()
    assert len(ops) == 18, len(ops)
    if a.operasyon:
        bilinmeyen = set(a.operasyon) - {o[0] for o in ops}
        if bilinmeyen:
            sys.exit(f"bilinmeyen operasyon: {sorted(bilinmeyen)}")
        ops = [o for o in ops if o[0] in a.operasyon]
    tekrarlar = a.tekrar or [1]

    anahtar = os.environ.get("ANTHROPIC_API_KEY")
    if not a.dry_run and not anahtar:
        sys.exit("ANTHROPIC_API_KEY tanımlı değil (denemek için --dry-run)")

    sab, doc = sablon(), belgeler()
    kayit = os.path.join(KOK, "experiments", "llm", "runs", a.tarih)
    for op in ops:
        istem = talimat(sab, doc, op)
        ozet = hashlib.sha256(istem.encode("utf-8")).hexdigest()
        for t in tekrarlar:
            klasor = os.path.join(kayit, op[0], f"T{t}")
            os.makedirs(klasor, exist_ok=True)
            with open(os.path.join(klasor, "talimat.md"), "w", encoding="utf-8", newline="\n") as f:
                f.write(istem)
            if a.dry_run:
                print(f"{op[0]} T{t}: talimat {len(istem)} karakter, sha256 {ozet[:12]}")
                continue
            if os.path.exists(os.path.join(klasor, "cevap.md")):
                print(f"{op[0]} T{t}: zaten var, atlandı")
                continue
            try:
                yanit = cagir(anahtar, istem)
            except (urllib.error.URLError, TimeoutError) as e:
                detay = e.read().decode("utf-8", "replace") if isinstance(e, urllib.error.HTTPError) else str(e)
                with open(os.path.join(klasor, "hata.txt"), "w", encoding="utf-8") as f:
                    f.write(detay)
                print(f"{op[0]} T{t}: HATA (hata.txt)")
                continue
            cevap = "".join(b.get("text", "") for b in yanit.get("content", []))
            with open(os.path.join(klasor, "cevap.md"), "w", encoding="utf-8", newline="\n") as f:
                f.write(cevap)
            with open(os.path.join(klasor, "meta.json"), "w", encoding="utf-8") as f:
                json.dump({
                    "operasyon": op[0], "tekrar": t, "model_istenen": MODEL, "model_donen": yanit.get("model"),
                    "zaman_utc": datetime.datetime.now(datetime.timezone.utc).isoformat(timespec="seconds"),
                    "talimat_sha256": ozet, "kullanim": yanit.get("usage"),
                    "durdurma_nedeni": yanit.get("stop_reason"),
                }, f, ensure_ascii=False, indent=2)
            n = dosyalari_ayikla(cevap, os.path.join(klasor, "cikti"))
            print(f"{op[0]} T{t}: {n} dosya ({yanit.get('stop_reason')})")


if __name__ == "__main__":
    main()
