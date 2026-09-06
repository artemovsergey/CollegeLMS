#!/usr/bin/env python3
"""
Сравнение пофамильного расписания (docx) с данными в PostgreSQL.
Читает расписание преподавателей из docx, загружает из БД, сравнивает.
"""
import re
import sys
from pathlib import Path
from collections import defaultdict
from dataclasses import dataclass, field

import psycopg2

try:
    from docx import Document
except ImportError:
    print("Установка python-docx...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "python-docx", "-q"])
    from docx import Document


DB_CONFIG = {
    "host": "127.0.0.1",
    "port": 5432,
    "dbname": "collegelms",
    "user": "postgres",
    "password": "root",
}

DAY_MAP_DB = {
    1: "Понедельник", 2: "Вторник", 3: "Среда",
    4: "Четверг", 5: "Пятница", 6: "Суббота", 0: "Воскресенье",
}

DAY_NAME_TO_IDX = {
    "ПОНЕДЕЛЬНИК": 1, "ВТОРНИК": 2, "СРЕДА": 3,
    "ЧЕТВЕРГ": 4, "ПЯТНИЦА": 5, "СУББОТА": 6,
}


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


@dataclass
class DocxEntry:
    group: str
    day_of_week: int  # 1=Mon..5=Fri
    number_pair: int
    weeks: list[int]


def parse_weeks(s: str) -> list[int]:
    """Парсит строки недель: '1-7,12-17' -> [1,2,...,7,12,...,17]"""
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


def normalize_name(name: str) -> str:
    """Нормализует имя: upper + убирает лишние пробелы."""
    return re.sub(r"\s+", " ", name.upper().strip())


def load_db_entries() -> list[DbEntry]:
    """Загружает все записи расписания из PostgreSQL."""
    conn = psycopg2.connect(**DB_CONFIG)
    cur = conn.cursor()
    cur.execute("""
        SELECT
            g.name,
            COALESCE(u.full_name, ''),
            se.subject,
            se.room,
            se.day_of_week,
            se.number_pair,
            se.weeks,
            se.lesson_type
        FROM schedule_entries se
        JOIN groups g ON g.id = se.group_id
        LEFT JOIN teachers t ON t.id = se.teacher_id
        LEFT JOIN users u ON u.id = t.user_id
        ORDER BY u.full_name, g.name, se.day_of_week, se.number_pair
    """)
    rows = cur.fetchall()
    cur.close()
    conn.close()

    entries = []
    for row in rows:
        group, teacher, subject, room, day, pair, weeks_json, lesson_type = row
        # weeks_json приходит как JSON-массив из PostgreSQL
        if isinstance(weeks_json, str):
            import json
            weeks = json.loads(weeks_json)
        else:
            weeks = weeks_json or []
        entries.append(DbEntry(
            group=group, teacher=teacher, subject=subject, room=room,
            day_of_week=day, number_pair=pair, weeks=weeks,
            lesson_type=lesson_type,
        ))
    return entries


def parse_docx(filepath: str) -> dict[str, dict[int, dict[int, list[tuple[str, list[int]]]]]]:
    """
    Парсит docx файл пофамильного расписания.
    Возвращает: {teacher_name_upper: {pair_num: {day_idx: [(group, weeks), ...]}}}
    """
    doc = Document(filepath)
    result: dict[str, dict[int, dict[int, list[tuple[str, list[int]]]]]] = {}
    current_teacher = None

    for table in doc.tables:
        for row in table.rows:
            cells = [cell.text.strip() for cell in row.cells]
            if all(not c for c in cells):
                continue

            first_cell = cells[0] if cells else ""

            # Имя преподавателя — заглавные буквы, длинное
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

            pair_text = first_cell
            if not pair_text or not pair_text.isdigit():
                continue
            pair_num = int(pair_text)
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
                    weeks = parse_weeks(m.group(2))
                    entries.append((group, weeks))

                if entries:
                    result[current_teacher][pair_num][day_idx + 1] = entries

    return result


def compare(db_entries: list[DbEntry], docx_data: dict) -> list[dict]:
    """Сравнивает записи БД с docx. Возвращает список расхождений."""
    diffs = []

    # Группируем БД по преподавателю -> group -> {(day, pair): weeks}
    db_by_teacher: dict[str, dict[str, dict[tuple[int, int], list[int]]]] = defaultdict(
        lambda: defaultdict(lambda: defaultdict(list))
    )
    for e in db_entries:
        t = normalize_name(e.teacher)
        db_by_teacher[t][e.group][(e.day_of_week, e.number_pair)].extend(e.weeks)

    # Все уникальные имена преподавателей
    all_teachers = sorted(set(list(db_by_teacher.keys()) + list(docx_data.keys())))

    for teacher in all_teachers:
        db_groups = db_by_teacher.get(teacher, {})
        docx_teacher = docx_data.get(teacher, {})

        # Собираем группы из docx
        docx_groups_data: dict[str, dict[tuple[int, int], list[int]]] = defaultdict(
            lambda: defaultdict(list)
        )
        for pair_num, days in docx_teacher.items():
            for day_idx, entries in days.items():
                for group, weeks in entries:
                    for w in weeks:
                        docx_groups_data[group][(day_idx, pair_num)].append(w)

        # Преподаватель есть только в одном источнике
        if teacher not in db_by_teacher:
            groups_list = list(docx_groups_data.keys())[:3]
            diffs.append({
                "type": "ONLY_DOCX",
                "teacher": teacher,
                "message": f"  {teacher}: ТОЛЬКО в docx (группы: {', '.join(groups_list)})",
            })
            continue
        if teacher not in docx_data:
            groups_list = list(db_groups.keys())[:3]
            diffs.append({
                "type": "ONLY_DB",
                "teacher": teacher,
                "message": f"  {teacher}: ТОЛЬКО в БД (группы: {', '.join(groups_list)})",
            })
            continue

        # Сравниваем группы
        db_group_set = set(db_groups.keys())
        docx_group_set = set(docx_groups_data.keys())

        for g in sorted(db_group_set - docx_group_set):
            keys = list(db_groups[g].keys())[:3]
            details = [f"{DAY_MAP_DB.get(d, '?')} п.{p}" for d, p in keys]
            diffs.append({
                "type": "GROUP_ONLY_DB",
                "teacher": teacher,
                "group": g,
                "message": f"  {teacher}: группа {g} ТОЛЬКО в БД ({'; '.join(details)})",
            })

        for g in sorted(docx_group_set - db_group_set):
            keys = list(docx_groups_data[g].keys())[:3]
            details = [f"день {d} п.{p}" for d, p in keys]
            diffs.append({
                "type": "GROUP_ONLY_DOCX",
                "teacher": teacher,
                "group": g,
                "message": f"  {teacher}: группа {g} ТОЛЬКО в docx ({'; '.join(details)})",
            })

        # Сравниваем недели для общих групп
        for g in sorted(db_group_set & docx_group_set):
            all_keys = set(db_groups[g].keys()) | set(docx_groups_data[g].keys())
            for key in sorted(all_keys):
                db_weeks = set(db_groups[g].get(key, []))
                docx_weeks = set(docx_groups_data[g].get(key, []))

                only_db = sorted(db_weeks - docx_weeks)
                only_docx = sorted(docx_weeks - db_weeks)

                if only_db or only_docx:
                    d, p = key
                    day_name = DAY_MAP_DB.get(d, f"день{d}")
                    parts = []
                    if only_docx:
                        parts.append(f"docx: {only_docx}")
                    if only_db:
                        parts.append(f"БД: {only_db}")
                    diffs.append({
                        "type": "WEEKS_DIFF",
                        "teacher": teacher,
                        "group": g,
                        "message": f"  {teacher}, {g}, {day_name} п.{p}: {'; '.join(parts)}",
                    })

    return diffs


def main():
    base_dir = Path(__file__).parent
    docx_file = base_dir / "schedule" / "Пофамильное расписание 1 полугодие.docx"

    if not docx_file.exists():
        print(f"ОШИБКА: Файл не найден: {docx_file}")
        sys.exit(1)

    print(f"=== Сравнение docx vs БД ===\n")

    # 1. Загрузка из БД
    print("Загрузка из PostgreSQL...")
    try:
        db_entries = load_db_entries()
    except Exception as e:
        print(f"ОШИБКА подключения к БД: {e}")
        sys.exit(1)

    db_teachers = set(normalize_name(e.teacher) for e in db_entries if e.teacher)
    print(f"  Записей: {len(db_entries)}")
    print(f"  Преподавателей: {len(db_teachers)}")

    # 2. Парсинг docx
    print(f"\nПарсинг docx: {docx_file.name}...")
    docx_data = parse_docx(str(docx_file))
    docx_teachers = set(docx_data.keys())
    print(f"  Преподавателей: {len(docx_teachers)}")

    # 3. Сравнение
    print(f"\nСравнение...")
    diffs = compare(db_entries, docx_data)

    if not diffs:
        print("\nРасписания совпадают!")
        return

    print(f"\nРасхождений: {len(diffs)}\n")

    by_type: dict[str, list[dict]] = defaultdict(list)
    for d in diffs:
        by_type[d["type"]].append(d)

    if "ONLY_DOCX" in by_type:
        print(f"Только в docx ({len(by_type['ONLY_DOCX'])}):")
        for d in by_type["ONLY_DOCX"]:
            print(d["message"])
        print()

    if "ONLY_DB" in by_type:
        print(f"Только в БД ({len(by_type['ONLY_DB'])}):")
        for d in by_type["ONLY_DB"]:
            print(d["message"])
        print()

    if "GROUP_ONLY_DOCX" in by_type:
        print(f"Группы только в docx ({len(by_type['GROUP_ONLY_DOCX'])}):")
        for d in by_type["GROUP_ONLY_DOCX"]:
            print(d["message"])
        print()

    if "GROUP_ONLY_DB" in by_type:
        print(f"Группы только в БД ({len(by_type['GROUP_ONLY_DB'])}):")
        for d in by_type["GROUP_ONLY_DB"]:
            print(d["message"])
        print()

    if "WEEKS_DIFF" in by_type:
        print(f"Различия недель ({len(by_type['WEEKS_DIFF'])}):")
        for d in by_type["WEEKS_DIFF"]:
            print(d["message"])


if __name__ == "__main__":
    main()
