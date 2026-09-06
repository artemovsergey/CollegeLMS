#!/usr/bin/env python3
"""
Сравнение расписания преподавателей: docx vs PostgreSQL (VPS dump).

Логика:
1. Из БД: для каждого преподавателя собираем {группа: {недели}} (с учётом предметов)
2. Из docx: для каждого преподавателя собираем {группа: {недели}}
3. Сравниваем по фамилии → группе → неделям
"""
import re
import sys
from pathlib import Path
from collections import defaultdict

try:
    from docx import Document
except ImportError:
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "python-docx", "-q"])
    from docx import Document


DUMP_FILE = Path(__file__).parent / "schedule" / "vps_dump.txt"
DOCX_FILE = Path(__file__).parent / "schedule" / "Пофамильное расписание 1 полугодие.docx"


def parse_weeks(s: str) -> list[int]:
    s = s.strip().strip("{}")
    if not s:
        return []
    return [int(x.strip()) for x in s.split(",") if x.strip()]


def parse_range(s: str) -> list[int]:
    """'1-7,12-15,17' -> [1,2,...,7,12,...,15,17]"""
    weeks = set()
    for part in s.split(","):
        part = part.strip()
        if not part:
            continue
        if "-" in part:
            a, b = part.split("-", 1)
            try:
                weeks.update(range(int(a), int(b) + 1))
            except ValueError:
                pass
        else:
            try:
                weeks.add(int(part))
            except ValueError:
                pass
    return sorted(weeks)


def norm(name: str) -> str:
    return re.sub(r"\s+", " ", name.upper().strip())


def last_name(full: str) -> str:
    return norm(full).split()[0] if full.strip() else ""


# ── Загрузка БД ──────────────────────────────────────────────

def load_db(filepath: Path) -> dict[str, dict[str, dict[str, set[int]]]]:
    """
    Возвращает: {фамилия: {группа: {предмет: {недели}}}}
    """
    result: dict[str, dict[str, dict[str, set[int]]]] = defaultdict(
        lambda: defaultdict(lambda: defaultdict(set))
    )
    with open(filepath, "r", encoding="utf-8") as f:
        f.readline()  # заголовок
        for line in f:
            parts = line.strip().split("|")
            if len(parts) < 8:
                continue
            group, teacher, subject, *_ = parts
            weeks_s = parts[6]
            if not teacher:
                continue
            ln = last_name(teacher)
            for w in parse_weeks(weeks_s):
                result[ln][group][subject].add(w)
    return result


# ── Парсинг docx ─────────────────────────────────────────────

def load_docx(filepath: Path) -> dict[str, dict[str, set[int]]]:
    """
    Возвращает: {фамилия: {группа: {недели}}}
    (docx не содержит предметов — только группы и недели)
    """
    doc = Document(str(filepath))
    result: dict[str, dict[str, set[int]]] = defaultdict(lambda: defaultdict(set))
    current_teacher = None

    for table in doc.tables:
        for row in table.rows:
            cells = [cell.text.strip() for cell in row.cells]
            if all(not c for c in cells):
                continue

            first = cells[0] if cells else ""

            # Имя преподавателя — заглавные, длинные, не цифра
            if (first and first.isupper() and len(first) > 5
                    and not first.isdigit() and any(c.isalpha() for c in first)):
                current_teacher = last_name(first)
                continue

            if not current_teacher:
                continue

            if not first or not first.isdigit():
                continue
            pair = int(first)
            if pair < 1 or pair > 8:
                continue

            for day_idx in range(1, 6):  # 1=Пн..5=Пт
                if day_idx >= len(cells):
                    break
                cell = cells[day_idx]
                if not cell:
                    continue

                for m in re.finditer(
                    r"([А-Яа-яёЁ\-]+(?:\d+)(?:-[А-Яа-яёЁ\-]*\d+)?)\(([^)]+)\)",
                    cell,
                ):
                    group = m.group(1)
                    weeks = parse_range(m.group(2))
                    for w in weeks:
                        result[current_teacher][group].add(w)

    return result


# ── Сравнение ────────────────────────────────────────────────

def compare(
    db: dict[str, dict[str, dict[str, set[int]]]],
    docx: dict[str, dict[str, set[int]]],
) -> list[dict]:
    diffs = []
    all_teachers = sorted(set(list(db.keys()) + list(docx.keys())))

    for ln in all_teachers:
        db_groups = db.get(ln, {})
        docx_groups = docx.get(ln, {})

        if not db_groups:
            groups = list(docx_groups.keys())[:5]
            diffs.append(("ONLY_DOCX", ln, f"  {ln}: ТОЛЬКО в docx (группы: {', '.join(groups)})"))
            continue
        if not docx_groups:
            groups = list(db_groups.keys())[:5]
            diffs.append(("ONLY_DB", ln, f"  {ln}: ТОЛЬКО в БД (группы: {', '.join(groups)})"))
            continue

        all_groups = sorted(set(list(db_groups.keys()) + list(docx_groups.keys())))

        for g in all_groups:
            db_subjects = db_groups.get(g, {})
            docx_weeks = docx_groups.get(g, set())

            if not db_subjects:
                diffs.append(("GROUP_ONLY_DOCX", ln,
                              f"  {ln}, {g}: ТОЛЬКО в docx (нед. {sorted(docx_weeks)[:6]}...)"))
                continue

            # Собираем все недели из БД по всем предметам для этой группы
            db_all_weeks: set[int] = set()
            db_subject_list: list[str] = []
            for subj, weeks in db_subjects.items():
                db_all_weeks |= weeks
                db_subject_list.append(subj)

            if not docx_weeks:
                diffs.append(("GROUP_ONLY_DB", ln,
                              f"  {ln}, {g}: ТОЛЬКО в БД (предметы: {', '.join(db_subject_list[:3])})"))
                continue

            # Сравниваем недели
            only_db = sorted(db_all_weeks - docx_weeks)
            only_docx = sorted(docx_weeks - db_all_weeks)

            if only_db or only_docx:
                parts = []
                if only_docx:
                    parts.append(f"docx: {only_docx}")
                if only_db:
                    parts.append(f"БД: {only_db}")
                subj_str = ", ".join(db_subject_list[:2])
                diffs.append(("WEEKS_DIFF", ln,
                              f"  {ln}, {g} ({subj_str}): {'; '.join(parts)}"))

    return diffs


# ── Main ─────────────────────────────────────────────────────

def main():
    if not DUMP_FILE.exists():
        print(f"ОШИБКА: {DUMP_FILE}")
        print("Сначала сделай дамп: scp user1@VPS:/tmp/schedule_dump.txt import/schedule/vps_dump.txt")
        sys.exit(1)
    if not DOCX_FILE.exists():
        print(f"ОШИБКА: {DOCX_FILE}")
        sys.exit(1)

    print("=== Сравнение: docx vs БД (VPS) ===\n")

    db = load_db(DUMP_FILE)
    db_teachers = set(db.keys())
    print(f"БД: {len(db_teachers)} преподавателей")

    docx = load_docx(DOCX_FILE)
    docx_teachers = set(docx.keys())
    print(f"Docx: {len(docx_teachers)} преподавателей")

    common = db_teachers & docx_teachers
    print(f"Совпадают: {len(common)}")
    print(f"Только в БД: {len(db_teachers - docx_teachers)}")
    print(f"Только в docx: {len(docx_teachers - db_teachers)}")

    diffs = compare(db, docx)

    if not diffs:
        print("\n✅ Расписания совпадают!")
        return

    print(f"\nРасхождений: {len(diffs)}\n")

    by_type = defaultdict(list)
    for t, ln, msg in diffs:
        by_type[t].append(msg)

    sections = [
        ("ONLY_DOCX", "Только в docx"),
        ("ONLY_DB", "Только в БД"),
        ("GROUP_ONLY_DOCX", "Группы только в docx"),
        ("GROUP_ONLY_DB", "Группы только в БД"),
        ("WEEKS_DIFF", "Различия недель"),
    ]

    for key, title in sections:
        items = by_type.get(key, [])
        if items:
            print(f"{title} ({len(items)}):")
            for m in items:
                print(m)
            print()


if __name__ == "__main__":
    main()
