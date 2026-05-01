# PT6A Engine References

## Files

### pt6a-140a-factsheet.pdf + .txt
- Source: https://prd-sc102-cdn.rtx.com/-/media/pw/newsroom/collateral/documents/general-aviation-and-services/22-0311-pt6a-140a-fact-sheet_web.pdf
- Downloaded: 2026-05-01
- License: Pratt & Whitney Canada / RTX — **proprietary marketing document**. Do not redistribute. Kept for reference only. Link to source instead.
- Size: 1.58 MB PDF, 8.2 KB text
- OCR method: `pdftotext -layout` — text-based PDF, no OCR needed
- Verification: Single-column layout, text extracts cleanly. Page numbers and headings present.

**Key data points:**
- PT6A-140A: 900 SHP mechanical / 1,161 ESHP thermodynamic
- Max propeller speed: 1,900 RPM, diameter 19 inches, length 64 inches
- Basic TBO: 4,000 hours (extendable to 6,000 with FAST solution, 8,000 for fleet operators)
- No cycle limitations on TBO
- Warranty: 1,000 hours (extendable to 2,500 hours / 5 years)
- Single Crystal CT blades for enhanced durability
- All-aluminum gearbox housings for corrosion resistance
- FAST telematics: captures full-flight data, transmitted wirelessly post-shutdown
- Component life limits >50% higher than competing engines in class

### pt6a-new-owner-guide.pdf + pt6a-new-owner-guide-raw.txt
- Source: https://www.caravannation.com/pt6a.pdf (originally from P&WC)
- Downloaded: 2026-05-01
- License: Pratt & Whitney Canada — **proprietary training/marketing document**. Do not redistribute. Link to source.
- Size: 3.6 MB PDF, 116 KB raw text
- OCR method: `pdftotext -raw` — text-based PDF. Note: document is a two-column layout; both -layout and -raw produce some line duplication. Content is readable and greppable. Headings and page numbers run in correct order.
- Verification: Two-column duplication artifact present (each paragraph appears twice). Content is still fully usable for keyword search and reference reading.

**Key content (engine systems relevant to WearEngine/FailureEngine):**

#### Engine instrumentation (pages ~12-15)
- **Torque**: Sensed via torque pressure transducer on reduction gearbox; displayed as %torque in cockpit
- **ITT (Inter-Turbine Temperature)**: Primary thermal limit monitor; protects hot section from overtemperature damage
- **N1 (Gas Generator Speed)**: Controlled by PLA (Power Lever Angle) via FCU
- **Oil Pressure + Oil Temperature**: Self-contained oil system; checked after shutdown while still hot

#### Overtorque limiter
- IELU (Integrated Electronic Limiter Unit) present on some variants; backs off throttle if torque exceeds preset limit
- Some military variants add both torque AND ITT limiting via DEEC

#### Hot section life management
- Hot starts (ITT exceedance during start) require mandatory inspection or component replacement
- Borescope inspection of hot section required at defined intervals
- Fuel nozzle exchange is a routine maintenance item (field-replaceable)

#### Modular design
- Engine is built in replaceable modules: compressor, hot section, power turbine, reduction gearbox
- Each module has separate logbook and overhaul interval
- Time Between Overhaul (TBO) is calendar-based or hours-based depending on variant

#### Power management for longevity
- Operating above torque limits accelerates hot section wear
- ITT exceedances are logged and trigger mandatory inspection
- Using minimum power required extends hot section life

#### Failure modes mentioned
- Uncommanded power rollback (PLA stuck, FCU fault)
- No response to PLA movement from idle (FCU failure)
- PLA stuck at idle

## PDFs Not Retrieved
- **FAA/CT-92-29 "Aircraft Turbine Engine Reliability and Investigations"**: URL https://www.tc.faa.gov/its/worldpac/techrpt/ct92-29.pdf timed out after 90s. Retry manually. This report contains aggregate IFSD (In-Flight Shutdown) rates by engine type and failure cause breakdown.
- **PT6A Troubleshooting guide from Scribd**: Behind paywall/login, not downloaded.
