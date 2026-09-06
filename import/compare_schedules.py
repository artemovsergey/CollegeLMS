#!/usr/bin/env python3
"""
Сравнение пофамильного расписания (docx) с данными в PostgreSQL (VPS dump).
"""
import re
import sys
from pathlib import Path
from collections import defaultdict
from dataclasses import dataclass

try:
    from docx import Document
except ImportError:
    print("Установка python-docx...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "python-docx", "-q"])
    from docx import Document


DUMP_FILE = Path(__file__).parent / "schedule" / "vps_dump.txt"
DOCX_FILE = Path(__file__).parent / "schedule" / "Пофамильное расписание 1 полугодие.docx"

DAY_MAP_DB = {1: "Пн", 2: "Вт", 3: "Ср", 4: "Чт", 5: "Пт", 6: "Сб", 0: "Вс"}


@dataclass
class DbEntry:
    group: str
    teacher: str
    subject: str
    room: str
    day_of_week: int
    number_pair: int
    weeks: list[int]
    lesson_type: str


def parse_pg_array(s: str) -> list[int]:
    """Парсит PostgreSQL-массив: '{1,4,8,10,12}' -> [1, 4, 8, 10, 12]"""
    s = s.strip().strip("{}")
    if not s:
        return []
    return [int(x.strip()) for x in s.split(",") if x.strip()]


def normalize_name(name: str) -> str:
    return re.sub(r"\s+", " ", name.upper().strip())


def get_last_name(full_name: str) -> str:
    """Извлекает фамилию (первое слово) из полного имени."""
    return normalize_name(full_name).split()[0] if full_name.strip() else ""


def load_dump(filepath: Path) -> list[DbEntry]:
    entries = []
    with open(filepath, "r", encoding="utf-8") as f:
        header = f.readline()  # пропускаем заголовок
        for line in f:
            line = line.strip()
            if not line:
                continue
            parts = line.split("|")
            if len(parts) < 8:
                continue
            group, teacher, subject, room, day_s, pair_s, weeks_s, ltype = parts[:8]
            try:
                day = int(day_s)
                pair = int(pair_s)
            except ValueError:
                continue
            weeks = parse_pg_array(weeks_s)
            entries.append(DbEntry(
                group=group, teacher=teacher, subject=subject, room=room,
                day_of_week=day, number_pair=pair, weeks=weeks,
                lesson_type=ltype,
            ))
    return entries


def parse_docx(filepath: Path) -> dict:
    """
    {teacher_upper: {pair: {day: [(group, weeks)]}}}
    """
    doc = Document(str(filepath))
    result: dict[str, dict[int, dict[int, list[tuple[str, list[int]]]]]] = {}
    current_teacher = None

    for table in doc.tables:
        for row in table.rows:
            cells = [cell.text.strip() for cell in row.cells]
            if all(not c for c in cells):
                continue

            first_cell = cells[0] if cells else ""

            if (first_cell
                and first_cell.isupper()
                and len(first_cell) > 5
                and not first_cell.isdigit()
                and any(c.isalpha() for c in first_cell)):
                current_teacher = normalize_name(first_cell)
                if current_teacher not in result:
                    result[current_teacher] = {}
                continue

            if not current_teacher:
                continue

            if not first_cell or not first_cell.isdigit():
                continue
            pair_num = int(first_cell)
            if pair_num < 1 or pair_num > 8:
                continue

            if pair_num not in result[current_teacher]:
                result[current_teacher][pair_num] = {}

            for day_idx in range(5):
                cell_idx = day_idx + 1
                if cell_idx >= len(cells):
                    break
                cell_text = cells[cell_idx]
                if not cell_text:
                    continue

                entries = []
                for m in re.finditer(
                    r"([А-Яа-яёЁ\-]+(?:\d+)(?:-[А-Яа-яёЁ\-]*\d+)?)\(([^)]+)\)",
                    cell_text,
                ):
                    group = m.group(1)
                    weeks = []
                    for part in m.group(2).split(","):
                        part = part.strip()
                        if "-" in part:
                            a, b = part.split("-", 1)
                            try:
                                weeks.extend(range(int(a), int(b) + 1))
                            except ValueError:
                                pass
                        else:
                            try:
                                weeks.append(int(part))
                            except ValueError:
                                pass
                    entries.append((group, sorted(weeks)))

                if entries:
                    result[current_teacher][pair_num][day_idx + 1] = entries

    return result


def compare(db_entries: list[DbEntry], docx_data: dict) -> list[dict]:
    diffs = []

    # Группируем БД по фамилии преподавателя
    db_by_teacher: dict[str, dict[str, dict[tuple[int, int], list[int]]]] = defaultdict(
        lambda: defaultdict(lambda: defaultdict(list))
    )
    # Маппинг фамилия -> полное имя из БД
    db_full_names: dict[str, str] = {}
    for e in db_entries:
        full = normalize_name(e.teacher)
        last = get_last_name(e.teacher)
        db_by_teacher[last][e.group][(e.day_of_week, e.number_pair)].extend(e.weeks)
        if last not in db_full_names:
            db_full_names[last] = full

    # Группируем docx по фамилии преподавателя
    docx_by_last: dict[str, dict[int, dict[int, list[tuple[str, list[int]]]]]] = defaultdict(dict)
    docx_full_names: dict[str, str] = {}
    for teacher_full, pairs in docx_data.items():
        last = teacher_full.split()[0] if teacher_full else ""
        if last:
            docx_by_last[last] = pairs
            docx_full_names[last] = teacher_full

    all_last_names = sorted(set(list(db_by_teacher.keys()) + list(docx_by_last.keys())))

    for last_name in all_last_names:
        db_groups = db_by_teacher.get(last_name, {})
        docx_teacher = docx_by_last.get(last_name, {})

        docx_groups_data: dict[str, dict[tuple[int, int], list[int]]] = defaultdict(
            lambda: defaultdict(list)
        )
        for pair_num, days in docx_teacher.items():
            for day_idx, entries in days.items():
                for group, weeks in entries:
                    for w in weeks:
                        docx_groups_data[group][(day_idx, pair_num)].append(w)

        db_name = db_full_names.get(last_name, last_name)
        docx_name = docx_full_names.get(last_name, last_name)

        if last_name not in db_by_teacher:
            groups_list = list(docx_groups_data.keys())[:5]
            diffs.append({
                "type": "ONLY_DOCX",
                "teacher": last_name,
                "message": f"  {docx_name}: ТОЛЬКО в docx (группы: {', '.join(groups_list)})",
            })
            continue
        if last_name not in docx_by_last:
            groups_list = list(db_groups.keys())[:5]
            diffs.append({
                "type": "ONLY_DB",
                "teacher": last_name,
                "message": f"  {db_name}: ТОЛЬКО в БД (группы: {', '.join(groups_list)})",
            })
            continue

        db_group_set = set(db_groups.keys())
        docx_group_set = set(docx_groups_data.keys())

        for g in sorted(db_group_set - docx_group_set):
            keys = sorted(db_groups[g].keys())[:3]
            details = [f"{DAY_MAP_DB.get(d, '?')} п.{p}" for d, p in keys]
            diffs.append({
                "type": "GROUP_ONLY_DB",
                "teacher": last_name,
                "group": g,
                "message": f"  {db_name}: группа {g} ТОЛЬКО в БД ({'; '.join(details)})",
            })

        for g in sorted(docx_group_set - db_group_set):
            keys = sorted(docx_groups_data[g].keys())[:3]
            details = [f"день {d} п.{p}" for d, p in keys]
            diffs.append({
                "type": "GROUP_ONLY_DOCX",
                "teacher": last_name,
                "group": g,
                "message": f"  {docx_name}: группа {g} ТОЛЬКО в docx ({'; '.join(details)})",
            })

        for g in sorted(db_group_set & docx_group_set):
            all_keys = set(db_groups[g].keys()) | set(docx_groups_data[g].keys())
            for key in sorted(all_keys):
                db_weeks = set(db_groups[g].get(key, []))
                docx_weeks = set(docx_groups_data[g].get(key, []))

                only_db = sorted(db_weeks - docx_weeks)
                only_docx = sorted(docx_weeks - db_weeks)

                if only_db or only_docx:
                    d, p = key
                    day_name = DAY_MAP_DB.get(d, f"д{d}")
                    parts = []
                    if only_docx:
                        parts.append(f"docx: {only_docx}")
                    if only_db:
                        parts.append(f"БД: {only_db}")
                    diffs.append({
                        "type": "WEEKS_DIFF",
                        "teacher": last_name,
                        "group": g,
                        "message": f"  {db_name}, {g}, {day_name} п.{p}: {'; '.join(parts)}",
                    })

    return diffs


def main():
    if not DUMP_FILE.exists():
        print(f"ОШИБКА: {DUMP_FILE}")
        print("Сначала сделай дамп: scp user1@VPS:/tmp/schedule_dump.txt import/schedule/vps_dump.txt")
        sys.exit(1)
    if not DOCX_FILE.exists():
        print(f"ОШИБКА: {DOCX_FILE}")
        sys.exit(1)

    print("=== Сравнение docx vs БД (VPS) ===\n")

    print(f"Загрузка дампа: {DUMP_FILE.name}")
    db_entries = load_dump(DUMP_FILE)
    db_teachers = set(get_last_name(e.teacher) for e in db_entries if e.teacher)
    print(f"  Записей: {len(db_entries)}")
    print(f"  Преподавателей: {len(db_teachers)}")

    print(f"\nПарсинг docx: {DOCX_FILE.name}")
    docx_data = parse_docx(DOCX_FILE)
    docx_teachers = set(k.split()[0] for k in docx_data.keys() if k)
    print(f"  Преподавателей: {len(docx_teachers)}")

    # Общие преподаватели
    common = db_teachers & docx_teachers
    only_db = db_teachers - docx_teachers
    only_docx = docx_teachers - db_teachers
    print(f"\nПересечение: {len(common)}")
    print(f"Только в БД: {len(only_db)}")
    print(f"Только в docx: {len(only_docx)}")

    print(f"\nСравнение...")
    diffs = compare(db_entries, docx_data)

    if not diffs:
        print("\nРасписания совпадают!")
        return

    print(f"\nРасхождений: {len(diffs)}\n")

    by_type: dict[str, list[dict]] = defaultdict(list)
    for d in diffs:
        by_type[d["type"]].append(d)

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
            for d in items:
                print(d["message"])
            print()


if __name__ == "__main__":
    main()
