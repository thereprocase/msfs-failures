# Reference / Reliability — MANIFEST

Collected 2026-05-01. All documents are public domain US government publications or
open-access academic works unless otherwise noted.

---

## 1. FAA Advisory Circular 43.13-1B — Aircraft Inspection and Repair

| Field | Value |
|-------|-------|
| File | `ac43-13-1b.pdf` + `ac43-13-1b.txt` |
| Size | 21 MB PDF / 2.0 MB text |
| Source | https://www.faa.gov/documentlibrary/media/advisory_circular/ac_43.13-1b_w-chg1.pdf |
| License | US Government public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** Chapter headings in correct order (Chapter 7: Hardware, Bolts, etc.).
Page numbers monotonic through the document. Two-column interleave artifacts present in ~5%
of lines (identifiable: lines like `"0.032 inch thickness. Width of sheet (i.e.,        may impair..."`
where two paragraph columns merge). The interleave does not affect chapter-level or section-level
content. Trustworthy for inspection intervals, material specifications, repair procedures.
Flagged sections: none at chapter level, minor artifacts in multi-column figure-adjacent paragraphs.

**Content:** ~1300 pages. Covers fabric covering, corrosion, metal structural repair, welding,
riveting, hydraulic systems, electrical systems, landing gear, propellers. The primary source
for what is "acceptable" maintenance practice under 14 CFR 43.13(a).

---

## 2. FAA Advisory Circular 43.13-2B — Acceptable Alterations

| Field | Value |
|-------|-------|
| File | `ac43-13-2b.pdf` + `ac43-13-2b.txt` |
| Size | 6.1 MB PDF / 370 KB text |
| Source | https://www.faa.gov/documentlibrary/media/advisory_circular/ac%2043.13-2b.pdf |
| License | US Government public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** 12 chapters in correct order (Structural Data, Comm/Nav, Antenna,
Lighting, Ski, Oxygen, Rotorcraft, Glider Tow, Shoulder Harness, Battery, Instruments, Cargo
Tiedown). No column interleave detected. Trustworthy throughout.

**Content:** ~140 pages. Structural data for alterations, battery installation requirements (Ch 10),
instrument installations (Ch 11). Battery chapter gives charging/capacity specs for lead-acid
and NiCad batteries.

---

## 3. FAA-H-8083-31B — Aviation Maintenance Technician Handbook: Airframe

| Field | Value |
|-------|-------|
| File | `amt-handbook-airframe-31b.txt` + `fetch-amt-airframe-31b.sh` |
| PDF | Stripped (107 MB, >50 MB threshold). Re-download via fetch script. |
| Size | 1.1 MB text |
| Source | https://www.faa.gov/regulations_policies/handbooks_manuals/aviation/FAA-H-8083-31B_Aviation_Maintenance_Technician_Handbook.pdf |
| License | US Government public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** 15 chapters confirmed in order: rotor systems (1), hydraulics (2–3), metal
repair (3), instruments (7, 10), navigation (11–12), brakes (13), fuel (14–15). No significant column
interleave. Tables extracted as whitespace-separated columns — adequate for reading but not
machine-parseable without cleanup.

**Content:** Covers airframe systems, landing gear, brakes (Chapter 13 — includes lining
thickness minimums, wear limits), fuel systems (Chapter 14), hydraulics, avionics.

---

## 4. MMEL — Single Engine Airplanes, Revision 2 (2023)

| Field | Value |
|-------|-------|
| File | `mmel-single-engine-rev2.pdf` + `mmel-single-engine-rev2.txt` |
| Size | 154 KB PDF / 17 KB text |
| Source | https://www.faa.gov/sites/faa.gov/files/FCL_MMEL_SE_Rev-2.pdf |
| License | US Government public domain |
| Extractor | `pdftotext -layout` |

**Note:** This file is the Final Comment Log for SE Rev 2 (the public draft review record),
not the operational MMEL table itself. The comment log contains discussions of specific
Category A/B/C/D entries and their rationale. The operational MMEL SE Rev 2 HTML is at
https://fsims.faa.gov/wdocs/mmel/se%20rev%201.htm (host unreachable at collection time).
For operational category tables, see the BE-200 MMEL below.

**Content:** Covers C172 / single-engine piston class. Discusses AED, passenger address, exterior
lighting (Cat A–D), autopilot systems, navigation instruments. Repair interval rationale visible
in commenter exchanges.

---

## 5. MMEL — King Air BE-200, Revision 15 (Textron Aviation)

| Field | Value |
|-------|-------|
| File | `mmel-king-air-be200-rev15.pdf` + `mmel-king-air-be200-rev15.txt` |
| Size | 1.4 MB PDF / 280 KB text |
| Source | https://www.faa.gov/sites/faa.gov/files/MMEL_BE-200_Rev_15_Draft.pdf |
| License | US Government public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** Log of revisions (Rev 1–15, 1989–present) followed by Definitions,
Preamble, Guidelines, then the TABLE KEY with columns: ATA number / system / repair category
(A/B/C/D) / no. installed / no. required for dispatch / remarks & exceptions. Table structure
verified: entries visible starting ATA 21 (Air Conditioning) through ATA 79 (Oil). Trustworthy.

**Content:** ~100 pages. Full MMEL table for King Air 200/250/260 (turboprop). Categories C
and D dominate. ATA 21: air cond Cat C. ATA 22: autopilot Cat C. ATA 27: flight controls.
ATA 34: navigation. ATA 61: propeller. ATA 73: engine fuel. ATA 79: engine oil.

---

## 6. MMEL — King Air BE-300, Revision 11

| Field | Value |
|-------|-------|
| File | `mmel-king-air-be300-rev11.pdf` + `mmel-king-air-be300-rev11.txt` |
| Size | 1.1 MB PDF / 295 KB text |
| Source | https://www.faa.gov/sites/faa.gov/files/MMEL_BE-300_Rev_11_Draft.pdf |
| License | US Government public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** Same structure as BE-200 MMEL. Covers Textron King Air 350/350ER
(BE300 series). Trustworthy.

**Content:** King Air 350 (turboprop) — closer to the target BE350 than BE-200. ATA chapters
21–97.

---

## 7. NASA/CR-2001-210647 — General Aviation Aircraft Reliability Study

| Field | Value |
|-------|-------|
| File | `nasa-ga-reliability-2001.pdf` + `nasa-ga-reliability-2001.txt` |
| Size | 3.8 MB PDF / 327 KB text |
| Source | https://ntrs.nasa.gov/api/citations/20010027423/downloads/20010027423.pdf |
| License | NASA open access / public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** TOC confirmed: Background (II), Statement of Problem (III), Appendices
A–H including Weibull parameter bounds (H). Figures 1–3 visible. Two-column interleave
artifacts in table cells (OCR merged left/right columns in some tables). Weibull parameter
tables (Tables 6–9) successfully extracted with beta and alpha values legible. Section headings
monotonic. Trustworthy for numerical parameters; spot-flag table rows with unusual spacing.

**Content:** Weibull beta and alpha (characteristic life in hours) for 21 GA aircraft subsystems
derived from 6-year field data. Key tables below (see summary.md for full extraction).

---

## 8. NASA/TM-2002-211348 — Weibull-Based Design for Rotating Aircraft Engine Structures

| Field | Value |
|-------|-------|
| File | `nasa-weibull-rotating-components-2002.pdf` + `nasa-weibull-rotating-components-2002.txt` |
| Size | 1.3 MB PDF / 110 KB text |
| Source | https://ntrs.nasa.gov/api/citations/20020062747/downloads/20020062747.pdf |
| License | NASA open access / public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** Introduction, Weibull analysis method, NASA E3-Engine case study,
references. Figures referenced but not reproduced in text. Weibull slope values extracted.
Some OCR noise in equations (subscripts merged). Trustworthy for methodology and numeric
slopes. Flag equations for manual verification if implementing directly.

**Content:** Covers Weibull-based reliability for turbine engine rotating structures (blades, disks,
bearings). Weibull slopes (beta) 3, 6, 9 used for engine life prediction. Rolling-element bearing
Lundberg-Palmgren theory extended to Weibull framework.

---

## 9. University of Washington — Weibull Reliability Analysis (Scholz)

| Field | Value |
|-------|-------|
| File | `weibull-analysis-scholz.pdf` + `weibull-analysis-scholz.txt` |
| Size | 921 KB PDF / 115 KB text |
| Source | https://faculty.washington.edu/fscholz/Reports/weibullanalysis.pdf |
| License | Open academic report (not copyrighted commercial publication) |
| Extractor | `pdftotext -layout` |

**Spot-check results:** Introductory sections, MLE estimation, confidence intervals, goodness-of-fit
tests. Mathematics mostly intact; some LaTeX-derived formulas lost in OCR. Conceptual
exposition is clean. Trustworthy for methodology; use source PDF for equations.

**Content:** Complete statistical treatment: 2-parameter Weibull, MLE estimation, Kaplan-Meier,
Anderson-Darling, accelerated life testing, confidence bounds. Covers relationship between
Weibull beta and wear patterns (beta<1: infant mortality, beta=1: random, beta>1: wear-out).

---

## 10. NASA NEPP — Reliability Analysis of Avionics in Commercial Aircraft (Weibull)

| Field | Value |
|-------|-------|
| File | `nasa-avionics-reliability-weibull.pdf` + `nasa-avionics-reliability-weibull.txt` |
| Size | 2.6 MB PDF / 143 KB text |
| Source | https://nepp.nasa.gov/docuploads/1b2665d3-01d3-4d2f-b7065eb2dd51ca20/rac.pdf |
| License | NASA open access / public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** Title page visible. Contains avionics MTBF data from commercial fleet
records. Tables present. Some two-column interleave in dense table sections. Trustworthy for
component MTBF ranges.

**Content:** Avionics component MTBF data; notes that Weibull beta > 2 indicates wear-out mode
for avionics. Includes FMD (Failure Mode Distribution) data for line-replaceable units (LRUs).

---

## 11. FAA Advisory Circular 20-97B — Aircraft Tire Maintenance and Operational Practices

| Field | Value |
|-------|-------|
| File | `ac20-97b-tire-maintenance.pdf` + `ac20-97b-tire-maintenance.txt` |
| Size | 177 KB PDF / 26 KB text |
| Source | https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_20-97B.pdf |
| License | US Government public domain |
| Extractor | `pdftotext -layout` |

**Spot-check results:** Clear single-column document, no interleave. Sections on inspection,
removal criteria, inflation, storage all present. Trustworthy throughout.

**Content:** Tire removal criteria (tread to base of any groove = replace), inspection intervals,
inflation pressure maintenance, storage limits (6-year calendar life from cure date, typically
marked on tire sidewall). References TSO-C62d, SAE-ARP 4834.

---

## 12. Michelin Aircraft Tire Care and Service Manual (2021)

| Field | Value |
|-------|-------|
| File | `michelin-tire-care-service-2021.pdf` + `michelin-tire-care-service-2021.txt` |
| Size | 13 MB PDF / 256 KB text |
| Source | https://www.faasafety.gov/files/gslac/courses/content/269/1100/Michelin%20ALC-269%20CSM%202021.pdf |
| License | Hosted by FAASafety.gov for pilot education (manufacturer-provided). Copyright Michelin. Do NOT redistribute standalone; link only. |
| Extractor | `pdftotext -layout` |

**Spot-check results:** 9 major sections confirmed (Introduction, Glossary, Markings, Retread,
Inflation/Storage, Mounting, Operational, Service). Section numbering monotonic. No interleave.
Trustworthy.

**Note:** Copyright Michelin. Vendored here because it is hosted by FAA Safety for pilot
training purposes and is publicly accessible. If in doubt, remove PDF and keep text only.

**Content:** Tire stretch procedures (12-hour initial inflation hold), pressure retention limits
(≤5% loss/24 hours acceptable), TSO-C62 50-cycle acceptance test, retread criteria, removal
criteria. Includes quantitative limits for all operational parameters.

---

## 13. T-Craft / General Aviation — Aircraft Oil Usage Reference

| Field | Value |
|-------|-------|
| File | `aircraft-oil-usage-reference.pdf` + `aircraft-oil-usage-reference.txt` |
| Size | 140 KB PDF / 15 KB text |
| Source | https://www.t-craft.org/documents/reference/Aircraft.Oil.Usage.pdf |
| License | Open club reference document |
| Extractor | `pdftotext -layout` |

**Spot-check results:** 3-page document, clean single-column. Trustworthy.

**Content:** Oil capacity rules (FAR 33.39: half sump minimum), CAA 1949 ratio (1 gal oil per
25 gal fuel), Lycoming formula: (0.006 × BHP × 4) / 7.4 = max qt/hr consumption. Cessna 182
(230 HP O-470) real-world: 9 qt sweet spot in 12-qt sump.

---

## 14. EAA Sport Aviation — High Oil Consumption (Busch, Feb 2014)

| Field | Value |
|-------|-------|
| File | `savvy-oil-consumption-eaa2014.pdf` + `savvy-oil-consumption-eaa2014.txt` |
| Size | 569 KB PDF / 17 KB text |
| Source | https://www.savvyaviation.com/wp-content/uploads/articles_eaa/EAA_2014-02_high-oil-consumption.pdf |
| License | EAA Sport Aviation article, publicly accessible. Copyright EAA/Savvy Aviation. Link only for redistribution. |
| Extractor | `pdftotext -layout` |

**Spot-check results:** Two-column magazine format — interleave in body text but content
is readable. Trustworthy for figures and thresholds cited.

**Content:** Continental maximum permissible consumption: 1 qt/hr for 310-HP engine.
Lycoming O-320 (160 HP): ~0.5 qt/hr max. Troubleshooting tree: breather discharge, leaks,
combustion consumption. Continental Service Bulletin M89-9 crankcase pressure limits:
≤44 mph (small-bore A/C/O-200/O-300), ≤90 mph (big-bore 360/470/520/550-series).

---

## Not collected (with rationale)

| Item | Reason |
|------|--------|
| FAA-H-8083-30B General AMT Handbook | 93 MB PDF; text-only version not extractable at source; content mostly general tools/math/electricity — lower priority than airframe/powerplant specifics. Fetch URL: https://www.faa.gov/regulations_policies/handbooks_manuals/aviation/amtg_handbook.pdf |
| FAA-H-8083-32B Powerplant AMT Handbook | 215 MB PDF; individual chapters accessible by chapter at faa.gov/sites/faa.gov/files/ (see 01_amtp_front.pdf etc). Too large; would exceed budget. |
| C172 MMEL (aircraft-specific) | C172 does not have a dedicated MMEL; it is covered by the generic SE airplane MMEL. The SE Rev 2 comment log is collected. |
| Battery SOH curves | No public-domain quantitative SOH curve found for GA lead-acid batteries. AC 43.13-2B Ch 10 covers installation and capacity testing. |

---

## Disk usage summary

```
Total: ~56 MB
  PDFs:  ~46 MB
  Text:  ~5.2 MB
```
