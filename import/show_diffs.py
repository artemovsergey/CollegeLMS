#!/usr/bin/env python3
"""Показать преподавателей с расхождениями."""
import sys
from pathlib import Path
from collections import defaultdict
from importlib.machinery import SourceFileLoader

mod = SourceFileLoader("cmp", str(Path(__file__).parent / "compare_schedules.py")).load_module()

db = mod.load_db(mod.DUMP_FILE)
docx = mod.load_docx(mod.DOCX_FILE)
diffs = mod.compare(db, docx)

by_teacher = defaultdict(list)
for t, ln, msg in diffs:
    by_teacher[ln].append((t, msg))

print(f"Преподаватели с расхождениями: {len(by_teacher)}\n")
for ln in sorted(by_teacher.keys()):
    items = by_teacher[ln]
    weeks = sum(1 for t, _ in items if t == "WEEKS_DIFF")
    only_db = sum(1 for t, _ in items if t == "ONLY_DB")
    only_docx = sum(1 for t, _ in items if t == "ONLY_DOCX")
    grp_db = sum(1 for t, _ in items if t == "GROUP_ONLY_DB")
    grp_docx = sum(1 for t, _ in items if t == "GROUP_ONLY_DOCX")
    parts = []
    if only_db:
        parts.append("только в БД")
    if only_docx:
        parts.append("только в docx")
    if grp_db:
        parts.append(f"группы ТОЛЬКО в БД: {grp_db}")
    if grp_docx:
        parts.append(f"группы ТОЛЬКО в docx: {grp_docx}")
    if weeks:
        parts.append(f"различия недель: {weeks}")
    print(f"  {ln}: {' | '.join(parts)}")
