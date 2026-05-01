#!/usr/bin/env bash
# Re-download FAA-H-8083-31B Aviation Maintenance Technician Handbook - Airframe
# Source: https://www.faa.gov/regulations_policies/handbooks_manuals/aviation/FAA-H-8083-31B_Aviation_Maintenance_Technician_Handbook.pdf
# Public domain US government document
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
curl -L -o "$SCRIPT_DIR/amt-handbook-airframe-31b.pdf" \
  "https://www.faa.gov/regulations_policies/handbooks_manuals/aviation/FAA-H-8083-31B_Aviation_Maintenance_Technician_Handbook.pdf"
