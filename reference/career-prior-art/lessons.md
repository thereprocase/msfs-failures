# Key Takeaways for v2 Design

## 1. Dual-Entry Model Reduces Friction

**OnAir Pattern**: Freelance-first, optional airline progression.

The clearest successful pattern is separating initial engagement (mission-based, low commitment) from deep engagement (full business ownership). Players can "try before buying" without commitment to fleet management. This reduces barrier-to-entry while preserving upside for engaged players.

**Implication for v2**: Consider progressive unlock of career features rather than requiring full business sim at Day 1. Start with simple flight contracts, unlock fleet ownership later.

---

## 2. Offline Progression Drives Retention & Monetization

**OnAir Pattern**: AI crews execute flights and trades while player is offline.

The ability to accumulate wealth, progress, and see world change without active play is a powerful retention mechanic. It also creates natural subscription monetization (pay for faster offline accumulation, crew quality, etc.).

**Implication for v2**: Design for asynchronous state changes. A player's fleet should generate revenue and wear between sessions. This supports both casual (login monthly) and hardcore (daily) playstyles.

---

## 3. Real-world Costs Create Meaningful Decisions

**Air Hauler 2 Pattern**: Aircraft purchase, fuel, landing fees, crew salaries, maintenance all subtract from profit.

All three systems converge on realism: multiple cost centers force tradeoffs. A cheap aircraft doesn't need expensive fuel but has low payload. High-value cargo requires capital for specialized aircraft. These constraints create emergent strategy—players optimize, not just accumulate.

**Implication for v2**: Avoid single-cost-center (fuel only). Layer costs: aircraft depreciation, crew wages, maintenance by flight hours, landing fees by airport tier, specialized payload requirements. Make player choices matter.

---

## 4. Specialization Gating via Qualifications Prevents Overpower

**Air Hauler 2 Pattern**: Type rating system. Pilots need certificates before flying larger/specialized aircraft.

This prevents new players from immediately piloting jets or specialized cargo aircraft. It creates natural progression—must fly smaller aircraft first, earn experience, then unlock larger types. FSEconomy and OnAir use implicit gating (aircraft capabilities, mission eligibility); Air Hauler makes it explicit via crew qualifications.

**Implication for v2**: Implement explicit progression gates. Don't let players buy a 777F and haul cargo immediately. Require flight-hour minimums, type certification, or company experience unlocks. This creates sense of advancement beyond wealth accumulation.

---

## 5. Persistent Shared World Economics > Single-Player Spreadsheet

**OnAir/FSEconomy Pattern**: Shared persistent multiplayer world with player-driven supply and demand.

Both multiplayer systems emphasize emergent economics—aircraft locations, fuel levels, cargo availability all shift based on player actions. This creates dynamic difficulty and social pressure to perform. Single-player (Air Hauler 2) is more content-driven but less dynamic.

**Implication for v2**: If targeting community, design for multiplayer persistence from the start. Aircraft available in a region depends on what other players flew there. Fuel prices vary by local supply. This requires client-server architecture and demand careful design, but payoff is significantly higher engagement.

---

## 6. Factorization of Roles: Pilot vs. Owner vs. Operator

**Emerging Pattern**: OnAir and Air Hauler allow players to hire AI or real pilots. This separates the flying role from the business role.

A player can own an airline but not fly every mission. This scales engagement—casual players can hire others, experienced players can operate multiple aircraft, and multiplayer players can be recruited to fly for others' airlines.

**Implication for v2**: Consider multi-role design. A player character might be a pilot, but the company is a separate entity. Allows for guild/alliance structures where skilled pilots hire on to established companies. This is a long-term design but powerful for community play.

---

## References

- **OnAir Company**: Dual-path progression, offline automation, multiplayer persistence
- **FSEconomy**: Decentralized economy, emergent pricing, player-driven state
- **Air Hauler 2**: Realistic cost modeling, specialization gating, crew development
- **Neofly**: Data unavailable (website unreachable at time of collection)
