# Career Management Prior Art - Collection Manifest

**Collection Date:** 2026-05-01  
**Collector:** Claude Code Research  
**Total Size:** ~45 KB (under 30 MB limit)

## Files

### onair.md
- **Source:** https://onair.company/ (primary), https://onair.company/pricing (supplementary)
- **Content Type:** Public website documentation extract
- **License:** Fair Use (public marketing content)
- **Retrieval Method:** WebFetch HTML→Markdown conversion
- **Sections:** Career system, economy mechanics, fleet management, FBO/airport ownership, pricing, platform compatibility
- **Completeness:** ~85% (pricing tiers not fully exposed on marketing site)
- **Status:** ✓ Complete

### fseconomy.md
- **Source:** https://www.fseconomy.net/ (primary)
- **Content Type:** Public website documentation extract
- **License:** Fair Use (public marketing content)
- **Retrieval Method:** WebFetch HTML→Markdown conversion
- **Sections:** Economy mechanics, fleet models, assignment system, persistent world, design principles
- **Completeness:** ~75% (wiki and manual pages returned 404; relied on homepage documentation)
- **Status:** ⚠ Partial (wiki unavailable; only homepage documented)

### airhauler2.md
- **Source:** https://www.justflight.com/product/air-hauler-2
- **Content Type:** Public marketing page extract
- **License:** Fair Use (public product documentation)
- **Retrieval Method:** WebFetch HTML→Markdown conversion
- **Sections:** Business management, flight operations, fleet management, crew development system, platform support, system requirements
- **Completeness:** ~95%
- **Status:** ✓ Complete

### neofly.md
- **Source:** https://www.neoflysim.com/ (attempted)
- **Content Type:** Documentation status report
- **License:** N/A
- **Retrieval Method:** WebFetch attempted; connection refused
- **Sections:** Placeholder for career system, missions, fleet, VR
- **Completeness:** 0% (unreachable)
- **Status:** ✗ Failed (website unreachable; included placeholder for future work)

### comparison.md
- **Content Type:** Synthesis/analysis (original work)
- **Source Data:** OnAir, FSEconomy, Air Hauler 2 documentation above
- **License:** Original (proprietary to msfs-failures project)
- **Matrix Dimensions:** 
  - 14 feature rows
  - 4 system columns (OnAir, FSEconomy, Air Hauler 2, Neofly)
  - Economic primitives deep-dive (3 systems)
  - Mission/assignment comparison table
  - Fleet state management comparison table
- **Purpose:** Enable side-by-side feature analysis for v2 design decisions

### lessons.md
- **Content Type:** Synthesis/analysis (original work)
- **Source Data:** OnAir, FSEconomy, Air Hauler 2 documentation above
- **License:** Original (proprietary to msfs-failures project)
- **Sections:** 6 key takeaways for career v2 design
  1. Dual-entry model reduces friction
  2. Offline progression drives retention
  3. Real-world costs create decisions
  4. Specialization gating prevents overpower
  5. Persistent shared world > single-player
  6. Multi-role factorization (pilot vs. owner vs. operator)
- **Purpose:** Design patterns and principles extracted for implementation guidance

## Collection Methodology

### Tools Used
- **WebFetch**: Fetched live public documentation from vendor sites, converted HTML to Markdown
- **Manual Synthesis**: Comparison matrix and lessons derived from fetched documentation

### Retrieval Attempts

| Source | URL | Attempt 1 | Attempt 2 | Status |
|--------|-----|-----------|-----------|--------|
| OnAir | https://onair.company/ | Success | N/A | ✓ |
| OnAir | https://onair.company/how-it-works | 404 | N/A | ✗ |
| OnAir | https://onair.company/pricing | Success | N/A | ✓ |
| Neofly | https://www.neoflysim.com/ | ECONNREFUSED | https://neoflysim.com/features | ✗✗ |
| FSEconomy | https://www.fseconomy.net/ | Success | N/A | ✓ |
| FSEconomy | https://www.fseconomy.net/wiki | 404 | N/A | ✗ |
| FSEconomy | https://www.fseconomy.net/pages/about | 404 | N/A | ✗ |
| FSEconomy | https://www.fseconomy.net/pages/manual | 404 | N/A | ✗ |
| Air Hauler 2 | https://www.justflight.com/product/air-hauler-2 | Success | N/A | ✓ |
| Air Hauler 2 | https://www.justflight.com/knowledge-base/air-hauler-2-manual | 404 | N/A | ✗ |

### Data Gaps

1. **Neofly**: Website unreachable (connection refused). Known from external sources to support MSFS/XP with VR, but no primary documentation collected. Recommend retrying or requesting team access to knowledge base.

2. **FSEconomy Wiki & Manual**: Wiki (404) and manual pages (404) unavailable. Homepage documentation captured but lacks depth on economy primitives and assignments mechanics. Wiki is GFDL-licensed and would be valuable primary source.

3. **Air Hauler 2 Manual**: Knowledge base manual unavailable (404). Marketing page comprehensive but lacks technical economy details (e.g., exact cost models, depreciation curves).

4. **OnAir Detailed Tiers**: Pricing page mentions tier system but specific subscription costs not visible in marketing content. May require account creation to view.

## License Summary

| Source | License | Notes |
|--------|---------|-------|
| OnAir | Fair Use | Public marketing content |
| FSEconomy | Fair Use | Public marketing content (wiki is GFDL, not collected) |
| Air Hauler 2 | Fair Use | Public marketing content |
| Neofly | N/A | Unreachable |
| Comparison Matrix | Original | Proprietary synthesis |
| Lessons Document | Original | Proprietary synthesis |

## Recommendations for Future Collection

1. **Neofly**: Contact vendor directly or try again at different time (ISP block?). Request documentation package if available.

2. **FSEconomy Wiki**: Wiki appears to exist (GFDL-licensed) but 404'd during collection. Try accessing via Wayback Machine (archive.org) or direct URL search.

3. **Air Hauler 2 Manual PDF**: Request demo or manual from Just Flight marketing. Likely available via support channel.

4. **OnAir Detailed Pricing**: Create free trial account to screenshot all pricing tiers for future reference.

## Verification Checklist

- [x] All collected content is publicly available (no leaked/private documentation)
- [x] Collection is under 30 MB (actual: ~45 KB)
- [x] Original synthesis documents included (comparison.md, lessons.md)
- [x] License information documented per source
- [x] Retrieval methods documented
- [x] Data gaps identified with recommendations
- [ ] Neofly documentation complete (blocked by connectivity)

## Next Steps

1. Retry Neofly collection in 24 hours or request via support
2. Archive FSEconomy and Air Hauler 2 pages via Wayback Machine for long-term reference
3. Use comparison matrix and lessons as input to career v2 design phase
4. Validate design assumptions against competitor economies before implementation
