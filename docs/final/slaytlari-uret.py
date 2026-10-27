#!/usr/bin/env python3
"""docs/final/oguz-slaytlar.pptx dosyasını üretir (3 slayt, konuşmacı notlarıyla).

Sayılar docs/report/gnucobol-ve-llm.md ve onun dayandığı dosyalardan alınmıştır; yeni sonuç içermez.
Kullanım: python docs/final/slaytlari-uret.py
"""
import os

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN
from pptx.util import Emu, Inches, Pt

KOYU = RGBColor(0x1F, 0x2A, 0x37)
VURGU = RGBColor(0x1D, 0x6F, 0xB8)
GRI = RGBColor(0x5B, 0x65, 0x70)
ACIK = RGBColor(0xEE, 0xF3, 0xF8)
UYARI = RGBColor(0xA6, 0x4B, 0x00)

prs = Presentation()
prs.slide_width = Inches(13.333)
prs.slide_height = Inches(7.5)
bos = prs.slide_layouts[6]


def metin(slide, x, y, w, h, satirlar, boyut=18, renk=KOYU, kalin=False):
    kutu = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    tf = kutu.text_frame
    tf.word_wrap = True
    for i, s in enumerate(satirlar):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.text = s
        p.font.size = Pt(boyut)
        p.font.color.rgb = renk
        p.font.bold = kalin
        p.space_after = Pt(6)
    return kutu


def baslik(slide, ust, ana):
    metin(slide, 0.6, 0.35, 12.1, 0.4, [ust], 14, VURGU, True)
    metin(slide, 0.6, 0.7, 12.1, 1.0, [ana], 30, KOYU, True)


def kutu(slide, x, y, w, h, baslik_, satirlar, boyut=16):
    sekil = slide.shapes.add_shape(1, Inches(x), Inches(y), Inches(w), Inches(h))
    sekil.fill.solid()
    sekil.fill.fore_color.rgb = ACIK
    sekil.line.fill.background()
    metin(slide, x + 0.2, y + 0.12, w - 0.4, 0.5, [baslik_], 18, VURGU, True)
    metin(slide, x + 0.2, y + 0.65, w - 0.4, h - 0.75, satirlar, boyut)


def notlar(slide, metni):
    slide.notes_slide.notes_text_frame.text = metni


def tablo(slide, x, y, w, satirlar, sutun_genislikleri, boyut=16):
    t = slide.shapes.add_table(len(satirlar), len(satirlar[0]), Inches(x), Inches(y), Inches(w),
                               Inches(0.5 * len(satirlar))).table
    for j, gen in enumerate(sutun_genislikleri):
        t.columns[j].width = Inches(gen)
    for i, satir in enumerate(satirlar):
        for j, h in enumerate(satir):
            c = t.cell(i, j)
            c.text = h
            for p in c.text_frame.paragraphs:
                p.font.size = Pt(boyut)
                p.font.bold = i == 0 or i == len(satirlar) - 1
                p.alignment = PP_ALIGN.LEFT if j == 0 else PP_ALIGN.CENTER
    return t


# ---- Slayt 1: GnuCOBOL düzeneği -------------------------------------------------------------
s = prs.slides.add_slide(bos)
baslik(s, "Oğuz · GnuCOBOL ve LLM deneyi · 1/3", "Eski kodu CICS ve Db2 olmadan çalıştırdık")
kutu(s, 0.6, 1.9, 5.9, 4.7, "Sorun", [
    "Elimizde z/OS, CICS ve Db2 yok.",
    "COBOL kodu GnuCOBOL'da derlenmiyor: ilk denemede 12 hata, hepsi CICS bağımlılığı (EIB alanları, EXEC CICS, EXEC SQL).",
    "Kodu sadece okumak, tahminin doğruluğunu göstermez.",
])
kutu(s, 6.8, 1.9, 5.9, 4.7, "Karar (ADR 0004): sahte parçalar", [
    "EIB alanları programın kendi değişkenleri olur.",
    "EXEC CICS → GnuCOBOL karşılığı (RETURN → GOBACK, LINK → CALL).",
    "Bağlı programlar stub: sonuç uydurmaz, sadece ne aldığını yazar.",
    "Bir sürücü program CICS'in rolünü oynar; her durum ayrı süreçte.",
    "EXEC SQL: sahte SQLCODE; sonuç \"Db2 bu kodu verirse\" koşulludur.",
], 15)
notlar(s, "Sorun: eski sistem CICS ve Db2 ister, bizde yok. Karar: denediğimiz programı kendi başına çalıştırıp "
          "dışındaki her şeyi sahte parçalarla değiştirdik (bağımlılık izolasyonu). Önemli kural: stub sonuç "
          "uydurmaz; ekranda görünen şey programın kendi kodundan gelir. Bedel: gerçek Db2, VSAM ve CICS "
          "davranışını ölçemeyiz. Kaynak: docs/adr/0004-legacy-test-harness.md.")

# ---- Slayt 2: Tahmin doğruluğu --------------------------------------------------------------
s = prs.slides.add_slide(bos)
baslik(s, "Oğuz · GnuCOBOL ve LLM deneyi · 2/3", "Tahminler çalıştırmayla çelişmedi: 53 / 53")
tablo(s, 0.6, 1.9, 12.1, [
    ["Çalıştırılan zincir", "Satır", "tuttu", "tutmadı", "belirsiz", "Ana ölçü"],
    ["LGICUS01 → LGICDB01 (müşteri sorgula)", "17", "14", "0", "3", "14/14"],
    ["LGACUS01 → LGACDB01 → LGACVS01, LGACDB02 (müşteri ekle)", "25", "21", "0", "4", "21/21"],
    ["LGIPOL01 → LGIPDB01 (motor sorgula)", "21", "18", "0", "3", "18/18"],
    ["Toplam", "63", "53", "0", "10", "53/53 = %100"],
], [6.0, 1.0, 1.0, 1.1, 1.1, 1.9], 14)
metin(s, 0.6, 4.55, 12.1, 0.5, ["Yardımcı (kötümser) ölçü: 53/63 = %84,1. \"Belirsiz\" satırları tahminin kendisi işaretledi; puanlanmadı."],
      15, GRI)
kutu(s, 0.6, 5.2, 12.1, 1.9, "Bu yüzde ne demek değil", [
    "Gerçek Db2/CICS ölçülmedi: düzenek ve tahmin aynı varsayımı paylaşıyorsa fark görünmez. Güncelle, sil, konut, hayat, ticari ölçülmedi.",
    "Tek kişi ölçtü; kurallar çalıştırmadan önce commit'lendi, betik bilerek bozulmuş çıktılarla sınandı.",
], 14)
notlar(s, "Her tahmin satırını iddialara ayırıp çıktıyla bayt bayt karşılaştırdık. Tutmayan satır çıkmadı. "
          "Toplamlar üç ayrı çalıştırma kaydının toplamı: 14+21+18 = 53 tuttu, 3+4+3 = 10 belirsiz, 17+25+21 = 63 satır. "
          "İlk iki satır tests/results/prediction-accuracy.md'de, motor satırı runs/lgipol01.md'de. "
          "Yüzde yüz, tahminler kusursuz demek değil: ölçtüğümüz COBOL'un Db2'ye ne verdiği, Db2'nin ne yaptığı değil.")

# ---- Slayt 3: LLM deneyi --------------------------------------------------------------------
s = prs.slides.add_slide(bos)
baslik(s, "Oğuz · GnuCOBOL ve LLM deneyi · 3/3", "LLM'in tek seferde yazdığı kod: aynı 10 testten 10'u")
tablo(s, 0.6, 1.9, 12.1, [
    ["Beş operasyon (10 test)", "Bizim kod", "LLM T1", "LLM T2", "LLM T3"],
    ["musteri-ekle · musteri-sorgula · musteri-guncelle", "4/4", "4/4", "4/4", "4/4"],
    ["motor-ekle · motor-sorgula", "6/6", "6/6", "6/6", "6/6"],
    ["Toplam", "10/10", "10/10", "10/10", "10/10"],
], [6.0, 1.5, 1.5, 1.5, 1.6])
kutu(s, 0.6, 4.0, 5.9, 3.2, "Nasıl ölçtük", [
    "Model: claude-opus-5-5; COBOL + API/şema belgeleri verildi, bizim C# kodumuz ve testler verilmedi.",
    "Tek atış, geri bildirim yok, 3 bağımsız tekrar.",
    "Negatif kontrol: LLM dosyası olmadan testler 0/10.",
], 14)
kutu(s, 6.8, 4.0, 5.9, 3.2, "Sınır", [
    "Sadece bu 5 operasyon; 18'e genellenmez. Güncellemede kodun ezilmesi, silmede tür kontrolü, ticari liste sorguları kapsam dışı.",
    "Test seti küçük: ikisi de %100, testler ayıramadı.",
    "Seçim rastgele değil: çalıştırma sırasında ilk gelen 5 operasyon.",
], 14)
notlar(s, "Aynı operasyonları bir LLM'e çevirttik, kodunu bizim eşdeğerlik testlerimizle çalıştırdık. 15 çıktının hepsi derlendi ve 10 "
          "testin 10'unu geçti; bizim kodumuz da 10/10. Testlerin kendisi 10 tane olduğu için ikisini ayıramadı; ek olarak belgedeki 16 "
          "davranışı ölçüm dışı yokladık, onda da hata çıkmadı. Plan 18 operasyondu; çalıştırma ekip kararıyla 16 çağrıda durduruldu ve "
          "beş operasyon (15 çağrı) ölçüme kaldı, bu yüzden sonuç genellenemez. Bizim kodumuz da Claude Code yardımıyla "
          "yazıldı; karşılaştırma insan ile yapay zekâ arasında değil, adım adım yazılan kod ile tek seferde çevrilen kod arasındadır. Kaynak: tests/results/llm-comparison.md.")

hedef = os.path.join(os.path.dirname(os.path.abspath(__file__)), "oguz-slaytlar.pptx")
prs.save(hedef)
print("yazıldı:", hedef)
